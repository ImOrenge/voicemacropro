using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketIOClient;
using NAudio.Wave;
using VoiceMacroPro.Models;
using System.Text.Json;
using System.IO;

namespace VoiceMacroPro.Services
{
    /// <summary>
    /// GPT-4o 실시간 음성인식을 위한 WebSocket 기반 래퍼 서비스
    /// NAudio를 사용한 실시간 오디오 캡처와 SocketIO를 통한 실시간 통신을 제공합니다.
    /// </summary>
    public class VoiceRecognitionWrapperService : IDisposable
    {
        private SocketIOClient.SocketIO _socket;
        private WaveInEvent _waveIn;
        private bool _isRecording = false;
        private bool _isInitialized = false;
        private readonly string _serverUrl;
        private readonly LoggingService _loggingService;
        private readonly AudioCaptureSettings _audioSettings;
        private VoiceSession _currentSession;
        private readonly List<TranscriptionResult> _sessionHistory;

        #region 이벤트 정의
        /// <summary>
        /// 트랜스크립션 결과를 받을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<TranscriptionResult> TranscriptionReceived;

        /// <summary>
        /// 에러 발생 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// 연결 상태 변경 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<ConnectionStatus> ConnectionChanged;

        /// <summary>
        /// 매크로 실행 결과를 받을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<VoiceMatchResult> MacroExecuted;

        /// <summary>
        /// 오디오 레벨 변경 시 발생하는 이벤트 (음성 입력 시각화용)
        /// </summary>
        public event EventHandler<double> AudioLevelChanged;
        #endregion

        /// <summary>
        /// VoiceRecognitionWrapperService 생성자
        /// </summary>
        /// <param name="serverUrl">WebSocket 서버 URL (기본값: http://localhost:5000)</param>
        public VoiceRecognitionWrapperService(string serverUrl = "http://localhost:5000")
        {
            _serverUrl = serverUrl;
            _loggingService = LoggingService.Instance;
            _audioSettings = new AudioCaptureSettings(); // GPT-4o 최적화 설정
            _sessionHistory = new List<TranscriptionResult>();
            _currentSession = new VoiceSession();
        }

        /// <summary>
        /// 음성 인식 서비스를 초기화하는 함수
        /// WebSocket 연결, 오디오 캡처 설정, 이벤트 핸들러 등록을 수행합니다.
        /// </summary>
        /// <returns>초기화 성공 여부</returns>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                _loggingService.LogInfo("GPT-4o 음성인식 서비스 초기화 시작");

                // WebSocket 클라이언트 초기화
                _socket = new SocketIOClient.SocketIO(_serverUrl);

                // WebSocket 이벤트 핸들러 등록
                SetupSocketEventHandlers();

                // WebSocket 서버에 연결
                await _socket.ConnectAsync();
                _loggingService.LogInfo("WebSocket 서버 연결 성공");

                // 실시간 오디오 캡처 초기화
                InitializeAudioCapture();
                _loggingService.LogInfo("오디오 캡처 시스템 초기화 완료");

                // 새 세션 생성
                _currentSession = new VoiceSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.Now
                };

                _isInitialized = true;
                
                // 연결 상태 이벤트 발생
                ConnectionChanged?.Invoke(this, new ConnectionStatus 
                { 
                    IsConnected = true, 
                    Status = "연결됨",
                    LastConnectionAttempt = DateTime.Now
                });

                _loggingService.LogInfo("GPT-4o 음성인식 서비스 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성인식 서비스 초기화 실패: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"초기화 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// WebSocket 이벤트 핸들러들을 설정하는 함수
        /// 각종 서버 응답 이벤트에 대한 처리를 정의합니다.
        /// </summary>
        private void SetupSocketEventHandlers()
        {
            // 트랜스크립션 결과 수신
            _socket.On("transcription_result", HandleTranscriptionResult);

            // 매크로 실행 결과 수신
            _socket.On("macro_executed", HandleMacroExecuted);

            // 음성인식 에러 수신
            _socket.On("voice_recognition_error", HandleError);

            // 연결 상태 변경 수신
            _socket.On("connection_status", HandleConnectionStatus);

            // 서버 연결 이벤트
            _socket.OnConnected += (sender, e) =>
            {
                _loggingService.LogInfo("WebSocket 서버 연결됨");
                ConnectionChanged?.Invoke(this, new ConnectionStatus 
                { 
                    IsConnected = true, 
                    Status = "연결됨" 
                });
            };

            // 서버 연결 해제 이벤트
            _socket.OnDisconnected += (sender, e) =>
            {
                _loggingService.LogWarning("WebSocket 서버 연결 해제됨");
                ConnectionChanged?.Invoke(this, new ConnectionStatus 
                { 
                    IsConnected = false, 
                    Status = "연결 해제됨" 
                });
            };
        }

        /// <summary>
        /// 실시간 오디오 캡처를 초기화하는 함수
        /// GPT-4o에 최적화된 오디오 설정 (24kHz, 16-bit, 모노)을 적용합니다.
        /// </summary>
        private void InitializeAudioCapture()
        {
            _waveIn = new WaveInEvent();
            
            // GPT-4o 최적화 오디오 설정: 24000Hz (24kHz) 샘플링 레이트
            _waveIn.WaveFormat = new WaveFormat(
                24000,     // 24kHz 샘플링 레이트 (GPT-4o 최적화)
                _audioSettings.BitsPerSample,  // 16비트 깊이
                _audioSettings.Channels        // 모노 (1채널)
            );
            
            // 실시간 처리를 위한 100ms 버퍼
            _waveIn.BufferMilliseconds = _audioSettings.BufferMilliseconds;

            // 오디오 데이터 수신 이벤트 등록
            _waveIn.DataAvailable += OnAudioDataAvailable;
            
            // 녹음 중지 이벤트 등록
            _waveIn.RecordingStopped += OnRecordingStopped;

            _loggingService.LogInfo($"오디오 캡처 설정: 24000Hz, {_audioSettings.BitsPerSample}bit, {_audioSettings.Channels}ch");
        }

        /// <summary>
        /// 실시간 오디오 데이터 처리 및 서버 전송 함수
        /// NAudio에서 캡처된 오디오를 Base64로 인코딩하여 WebSocket으로 전송합니다.
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">오디오 데이터 이벤트 인자</param>
        private async void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_isRecording && _socket.Connected)
            {
                try
                {
                    // 오디오 레벨 계산 (음성 입력 시각화용)
                    double audioLevel = CalculateAudioLevel(e.Buffer, e.BytesRecorded);
                    AudioLevelChanged?.Invoke(this, audioLevel);

                    // 오디오 데이터를 Base64로 인코딩
                    string audioBase64 = Convert.ToBase64String(e.Buffer, 0, e.BytesRecorded);

                    // WebSocket을 통해 실시간 오디오 스트리밍
                    await _socket.EmitAsync("audio_chunk", new { audio = audioBase64 });
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"오디오 전송 오류: {ex.Message}");
                    ErrorOccurred?.Invoke(this, $"오디오 전송 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 오디오 레벨을 계산하는 함수 (음성 입력 시각화용)
        /// </summary>
        /// <param name="buffer">오디오 버퍼</param>
        /// <param name="bytesRecorded">녹음된 바이트 수</param>
        /// <returns>정규화된 오디오 레벨 (0.0 ~ 1.0)</returns>
        private double CalculateAudioLevel(byte[] buffer, int bytesRecorded)
        {
            double sum = 0;
            int samples = bytesRecorded / 2; // 16비트 샘플

            for (int i = 0; i < bytesRecorded; i += 2)
            {
                if (i + 1 < bytesRecorded)
                {
                    short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                    sum += Math.Abs(sample);
                }
            }

            double average = sum / samples;
            return Math.Min(1.0, average / 32768.0); // 0.0 ~ 1.0으로 정규화
        }

        /// <summary>
        /// 음성 녹음을 시작하는 함수
        /// </summary>
        /// <returns>녹음 시작 성공 여부</returns>
        public async Task<bool> StartRecordingAsync()
        {
            if (!_isInitialized)
            {
                ErrorOccurred?.Invoke(this, "서비스가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                if (!_isRecording)
                {
                    _isRecording = true;
                    _waveIn.StartRecording();
                    
                    // 서버에 녹음 시작 알림
                    await _socket.EmitAsync("start_recording");
                    
                    _loggingService.LogInfo("음성 녹음 시작");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"녹음 시작 오류: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"녹음 시작 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 음성 녹음을 중지하는 함수
        /// </summary>
        /// <returns>녹음 중지 성공 여부</returns>
        public async Task<bool> StopRecordingAsync()
        {
            try
            {
                if (_isRecording)
                {
                    _isRecording = false;
                    _waveIn.StopRecording();
                    
                    // 서버에 녹음 중지 알림
                    await _socket.EmitAsync("stop_recording");
                    
                    _loggingService.LogInfo("음성 녹음 중지");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"녹음 중지 오류: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"녹음 중지 실패: {ex.Message}");
                return false;
            }
        }

        #region WebSocket 이벤트 핸들러들

        /// <summary>
        /// 트랜스크립션 결과를 처리하는 함수
        /// GPT-4o에서 받은 음성 인식 결과를 UI에 전달합니다.
        /// </summary>
        /// <param name="response">WebSocket 응답</param>
        private void HandleTranscriptionResult(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<TranscriptionData>();
                
                var result = new TranscriptionResult
                {
                    Type = data.type,
                    Text = data.text,
                    Confidence = data.confidence,
                    Timestamp = DateTime.Parse(data.timestamp)
                };

                // 세션 통계 업데이트
                if (result.Type == "final")
                {
                    _currentSession.TranscriptionCount++;
                    _sessionHistory.Add(result);
                }

                // UI에 트랜스크립션 결과 전달
                TranscriptionReceived?.Invoke(this, result);
                
                _loggingService.LogInfo($"트랜스크립션 결과: '{result.Text}' (신뢰도: {result.Confidence:F2})");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"트랜스크립션 결과 처리 오류: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"트랜스크립션 결과 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 실행 결과를 처리하는 함수
        /// </summary>
        /// <param name="response">WebSocket 응답</param>
        private void HandleMacroExecuted(SocketIOResponse response)
        {
            try
            {
                var macroResult = response.GetValue<VoiceMatchResult>();
                
                // 세션 통계 업데이트
                if (macroResult.IsSuccessful)
                {
                    _currentSession.SuccessfulMacroExecutions++;
                }

                // UI에 매크로 실행 결과 전달
                MacroExecuted?.Invoke(this, macroResult);
                
                _loggingService.LogInfo($"매크로 실행: {macroResult.MacroName} (성공: {macroResult.IsSuccessful})");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 실행 결과 처리 오류: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"매크로 실행 결과 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 에러를 처리하는 함수
        /// </summary>
        /// <param name="response">WebSocket 응답</param>
        private void HandleError(SocketIOResponse response)
        {
            try
            {
                var error = response.GetValue<ErrorData>();
                _loggingService.LogError($"서버 에러: {error.error}");
                ErrorOccurred?.Invoke(this, error.error);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"에러 처리 중 오류: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"에러 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 연결 상태 변경을 처리하는 함수
        /// </summary>
        /// <param name="response">WebSocket 응답</param>
        private void HandleConnectionStatus(SocketIOResponse response)
        {
            try
            {
                var status = response.GetValue<ConnectionStatus>();
                ConnectionChanged?.Invoke(this, status);
                _loggingService.LogInfo($"연결 상태 변경: {status.Status}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"연결 상태 처리 오류: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 녹음 중지 이벤트를 처리하는 함수
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">중지 이벤트 인자</param>
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                _loggingService.LogError($"녹음 중지 오류: {e.Exception.Message}");
                ErrorOccurred?.Invoke(this, $"녹음 중지 오류: {e.Exception.Message}");
            }
        }

        /// <summary>
        /// 현재 세션의 통계 정보를 가져오는 함수
        /// </summary>
        /// <returns>음성 인식 세션 정보</returns>
        public VoiceSession GetCurrentSession()
        {
            if (_sessionHistory.Count > 0)
            {
                double totalConfidence = 0;
                foreach (var result in _sessionHistory)
                {
                    totalConfidence += result.Confidence;
                }
                _currentSession.AverageConfidence = totalConfidence / _sessionHistory.Count;
            }

            return _currentSession;
        }

        /// <summary>
        /// 연결 상태를 확인하는 함수
        /// </summary>
        /// <returns>연결 상태</returns>
        public bool IsConnected => _socket?.Connected ?? false;

        /// <summary>
        /// 녹음 상태를 확인하는 함수
        /// </summary>
        /// <returns>녹음 상태</returns>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// 리소스를 해제하는 함수
        /// WebSocket 연결 해제, 오디오 캡처 중지, 이벤트 핸들러 정리를 수행합니다.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_isRecording)
                {
                    StopRecordingAsync().Wait(1000); // 1초 대기
                }

                _waveIn?.Dispose();
                _socket?.DisconnectAsync().Wait(1000); // 1초 대기
                _socket?.Dispose();

                _loggingService.LogInfo("VoiceRecognitionWrapperService 리소스 해제 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"리소스 해제 중 오류: {ex.Message}");
            }
        }
    }
} 