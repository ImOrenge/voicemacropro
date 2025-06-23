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
        private SocketIO? _socket;
        private WaveIn? _waveIn;
        private bool _isRecording = false;
        private bool _isInitialized = false;
        private readonly string _serverUrl;
        private readonly LoggingService _loggingService;
        private readonly AudioCaptureSettings _audioSettings;
        private VoiceSession _currentSession;
        private readonly List<TranscriptionResult> _sessionHistory;
        private readonly object _lock = new();
        private bool _isDisposed;

        #region 이벤트 정의
        /// <summary>
        /// 트랜스크립션 결과를 받을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<TranscriptionResult>? TranscriptionReceived;

        /// <summary>
        /// 에러 발생 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<string>? ErrorOccurred;

        /// <summary>
        /// 연결 상태 변경 시 발생하는 이벤트
        /// </summary>
        public event EventHandler<ConnectionStatus>? ConnectionChanged;

        /// <summary>
        /// 매크로 실행 결과를 받을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<VoiceMatchResultModel>? MacroExecuted;

        /// <summary>
        /// 오디오 레벨 변경 시 발생하는 이벤트 (음성 입력 시각화용)
        /// </summary>
        public event EventHandler<double>? AudioLevelChanged;
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
            InitializeAudioCapture();
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
                _socket = new SocketIO(_serverUrl);

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
                ConnectionChanged?.Invoke(this, ConnectionStatus.Connected);

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
            // 연결 확인 이벤트 수신
            _socket.OnConnected += (sender, e) =>
            {
                _loggingService.LogInfo("✅ Socket.IO 서버에 연결됨");
                ConnectionChanged?.Invoke(this, ConnectionStatus.Connected);
            };

            // 서버 연결 해제 이벤트
            _socket.OnDisconnected += (sender, e) =>
            {
                _loggingService.LogWarning("❌ Socket.IO 서버 연결 해제됨");
                ConnectionChanged?.Invoke(this, ConnectionStatus.Disconnected);
            };

            // 연결 오류 이벤트
            _socket.OnError += (sender, e) =>
            {
                _loggingService.LogError($"❌ Socket.IO 연결 오류: {e}");
                ErrorOccurred?.Invoke(this, $"연결 오류: {e}");
            };

            // 트랜스크립션 결과 수신
            _socket.On("transcription_result", response =>
            {
                try
                {
                    var result = response.GetValue<TranscriptionResult>();
                    TranscriptionReceived?.Invoke(this, result);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"트랜스크립션 결과 처리 오류: {ex.Message}");
                }
            });

            // 매크로 실행 결과 수신
            _socket.On("macro_executed", response =>
            {
                try
                {
                    var result = response.GetValue<VoiceMatchResultModel>();
                    MacroExecuted?.Invoke(this, result);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"매크로 실행 결과 처리 오류: {ex.Message}");
                }
            });

            // 음성인식 상태 이벤트들
            _socket.On("voice_recognition_started", HandleVoiceRecognitionStarted);
            _socket.On("voice_recognition_stopped", HandleVoiceRecognitionStopped);
            _socket.On("voice_recognition_error", HandleVoiceRecognitionError);

            // 오디오 처리 이벤트들
            _socket.On("audio_chunk_received", HandleAudioChunkReceived);
            _socket.On("audio_processing_error", HandleAudioProcessingError);
            _socket.On("transcription_error", HandleTranscriptionError);
        }

        /// <summary>
        /// 실시간 오디오 캡처를 초기화하는 함수
        /// 윈도우 기본 마이크를 자동 감지하여 GPT-4o에 최적화된 오디오 설정을 적용합니다.
        /// </summary>
        private void InitializeAudioCapture()
        {
            try
            {
                // 윈도우 기본 마이크 장치 정보 로깅
                LogAvailableAudioDevices();

                _waveIn = new WaveIn
                {
                    WaveFormat = new WaveFormat(16000, 1)
                };
                
                // 윈도우 기본 마이크 장치 설정 (DeviceNumber -1은 시스템 기본 장치)
                _waveIn.DeviceNumber = -1;  // 윈도우 기본 마이크 사용
                
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

                // 현재 사용 중인 마이크 장치 정보 로깅
                string deviceInfo = GetCurrentAudioDeviceInfo();
                _loggingService.LogInfo($"✅ 오디오 캡처 설정 완료: {deviceInfo}");
                _loggingService.LogInfo($"🎵 오디오 형식: 24000Hz, {_audioSettings.BitsPerSample}bit, {_audioSettings.Channels}ch, {_audioSettings.BufferMilliseconds}ms 버퍼");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 오디오 캡처 초기화 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 사용 가능한 오디오 입력 장치 목록을 로깅하는 함수
        /// 윈도우에서 인식되는 모든 마이크 장치를 확인하여 디버깅에 도움을 줍니다.
        /// </summary>
        private void LogAvailableAudioDevices()
        {
            try
            {
                int deviceCount = WaveInEvent.DeviceCount;
                _loggingService.LogInfo($"🎤 감지된 오디오 입력 장치 수: {deviceCount}개");

                if (deviceCount == 0)
                {
                    _loggingService.LogWarning("⚠️ 오디오 입력 장치를 찾을 수 없습니다. 마이크가 연결되어 있는지 확인하세요.");
                    return;
                }

                // 각 장치 정보 출력
                for (int i = 0; i < deviceCount; i++)
                {
                    var capabilities = WaveInEvent.GetCapabilities(i);
                    _loggingService.LogInfo($"📱 장치 {i}: {capabilities.ProductName} (채널: {capabilities.Channels})");
                }

                // 시스템 기본 장치 정보
                _loggingService.LogInfo("🔧 윈도우 기본 마이크 장치를 사용합니다. (DeviceNumber: -1)");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 오디오 장치 정보 조회 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 사용 중인 오디오 장치 정보를 반환하는 함수
        /// </summary>
        /// <returns>오디오 장치 정보 문자열</returns>
        private string GetCurrentAudioDeviceInfo()
        {
            try
            {
                if (_waveIn.DeviceNumber == -1)
                {
                    return "윈도우 시스템 기본 마이크 (자동 선택)";
                }
                else if (_waveIn.DeviceNumber >= 0 && _waveIn.DeviceNumber < WaveInEvent.DeviceCount)
                {
                    var capabilities = WaveInEvent.GetCapabilities(_waveIn.DeviceNumber);
                    return $"{capabilities.ProductName} (장치 번호: {_waveIn.DeviceNumber})";
                }
                else
                {
                    return $"알 수 없는 장치 (장치 번호: {_waveIn.DeviceNumber})";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 장치 정보 조회 실패: {ex.Message}");
                return "장치 정보 조회 실패";
            }
        }

        /// <summary>
        /// 실시간 오디오 데이터 처리 및 서버 전송 함수
        /// NAudio에서 캡처된 오디오를 Base64로 인코딩하여 WebSocket으로 전송합니다.
        /// Voice Activity Detection (VAD) 로직을 포함하여 실제 음성이 있을 때만 전송합니다.
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">오디오 데이터 이벤트 인자</param>
        private async void OnAudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_isRecording && _socket != null && _socket.Connected)
            {
                try
                {
                    // 오디오 레벨 계산 (음성 입력 시각화용)
                    double audioLevel = CalculateAudioLevel(e.Buffer, e.BytesRecorded);
                    AudioLevelChanged?.Invoke(this, audioLevel);

                    // Voice Activity Detection (VAD) - 실제 음성이 있는지 확인
                    bool hasVoiceActivity = IsVoiceActivityDetected(e.Buffer, e.BytesRecorded, audioLevel);
                    
                    if (hasVoiceActivity)
                    {
                        // 오디오 데이터를 Base64로 인코딩
                        string audioBase64 = Convert.ToBase64String(e.Buffer, 0, e.BytesRecorded);

                        // WebSocket을 통해 실시간 오디오 스트리밍 (음성 감지시만)
                        await _socket.EmitAsync("audio_chunk", new { 
                            audio = audioBase64,
                            audio_level = audioLevel,
                            has_voice = true 
                        });
                        
                        // 음성 감지 로그 (디버그용 - 너무 많은 로그 방지)
                        // _loggingService.LogDebug($"🎤 음성 감지됨 (레벨: {audioLevel:F3})");
                    }
                    else
                    {
                        // 음성이 감지되지 않으면 침묵 데이터 전송하지 않음
                        // _loggingService.LogDebug($"🔇 침묵 감지됨 (레벨: {audioLevel:F3}) - 전송 건너뜀");
                    }
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
        /// Voice Activity Detection (VAD) - 실제 음성이 감지되었는지 확인하는 함수
        /// 마이크 잡음이나 침묵 상태에서는 오디오 데이터를 전송하지 않기 위해 사용됩니다.
        /// </summary>
        /// <param name="buffer">오디오 버퍼</param>
        /// <param name="bytesRecorded">녹음된 바이트 수</param>
        /// <param name="audioLevel">이미 계산된 오디오 레벨</param>
        /// <returns>음성 활동이 감지되면 true, 침묵이나 잡음이면 false</returns>
        private bool IsVoiceActivityDetected(byte[] buffer, int bytesRecorded, double audioLevel)
        {
            // 1. 기본 볼륨 임계값 체크 (침묵 필터링)
            const double MIN_VOLUME_THRESHOLD = 0.02; // 2% 이상의 볼륨 필요
            if (audioLevel < MIN_VOLUME_THRESHOLD)
            {
                return false; // 너무 조용하면 침묵으로 판단
            }

            // 2. 최대 볼륨 임계값 체크 (과도한 잡음 필터링)
            const double MAX_VOLUME_THRESHOLD = 0.95; // 95% 이상이면 클리핑으로 판단
            if (audioLevel > MAX_VOLUME_THRESHOLD)
            {
                return false; // 너무 크면 클리핑/잡음으로 판단
            }

            // 3. 음성 신호의 동적 범위 확인 (음성은 변화가 있어야 함)
            bool hasVariation = CheckSignalVariation(buffer, bytesRecorded);
            if (!hasVariation)
            {
                return false; // 일정한 신호는 전자 잡음으로 판단
            }

            // 4. 제로 크로싱 비율 확인 (음성은 적절한 주파수 변화를 가짐)
            double zeroCrossingRate = CalculateZeroCrossingRate(buffer, bytesRecorded);
            const double MIN_ZCR = 0.01; // 최소 제로 크로싱 비율
            const double MAX_ZCR = 0.30; // 최대 제로 크로싱 비율
            if (zeroCrossingRate < MIN_ZCR || zeroCrossingRate > MAX_ZCR)
            {
                return false; // 제로 크로싱이 너무 적거나 많으면 잡음
            }

            // 모든 조건을 만족하면 음성으로 판단
            return true;
        }

        /// <summary>
        /// 신호의 변화량을 확인하여 일정한 잡음을 필터링하는 함수
        /// </summary>
        /// <param name="buffer">오디오 버퍼</param>
        /// <param name="bytesRecorded">녹음된 바이트 수</param>
        /// <returns>신호에 충분한 변화가 있으면 true</returns>
        private bool CheckSignalVariation(byte[] buffer, int bytesRecorded)
        {
            if (bytesRecorded < 4) return false;

            double variance = 0;
            double mean = 0;
            int samples = bytesRecorded / 2;

            // 평균 계산
            for (int i = 0; i < bytesRecorded; i += 2)
            {
                if (i + 1 < bytesRecorded)
                {
                    short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                    mean += sample;
                }
            }
            mean /= samples;

            // 분산 계산
            for (int i = 0; i < bytesRecorded; i += 2)
            {
                if (i + 1 < bytesRecorded)
                {
                    short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                    variance += Math.Pow(sample - mean, 2);
                }
            }
            variance /= samples;

            // 표준편차가 일정 값 이상이어야 음성으로 판단
            double standardDeviation = Math.Sqrt(variance);
            const double MIN_VARIATION = 100.0; // 최소 변화량 임계값
            
            return standardDeviation > MIN_VARIATION;
        }

        /// <summary>
        /// 제로 크로싱 비율을 계산하는 함수 (음성/잡음 구분에 도움)
        /// </summary>
        /// <param name="buffer">오디오 버퍼</param>
        /// <param name="bytesRecorded">녹음된 바이트 수</param>
        /// <returns>제로 크로싱 비율 (0.0 ~ 1.0)</returns>
        private double CalculateZeroCrossingRate(byte[] buffer, int bytesRecorded)
        {
            if (bytesRecorded < 4) return 0.0;

            int zeroCrossings = 0;
            short previousSample = 0;
            int samples = bytesRecorded / 2;

            for (int i = 0; i < bytesRecorded; i += 2)
            {
                if (i + 1 < bytesRecorded)
                {
                    short currentSample = (short)((buffer[i + 1] << 8) | buffer[i]);
                    
                    // 부호가 바뀌었는지 확인 (제로 크로싱)
                    if (i > 0 && ((previousSample >= 0 && currentSample < 0) || 
                                  (previousSample < 0 && currentSample >= 0)))
                    {
                        zeroCrossings++;
                    }
                    
                    previousSample = currentSample;
                }
            }

            // 전체 샘플 수에 대한 제로 크로싱 비율
            return samples > 1 ? (double)zeroCrossings / (samples - 1) : 0.0;
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
                    
                    // 서버에 음성인식 시작 알림
                    await _socket.EmitAsync("start_voice_recognition");
                    
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
                    
                    // 서버에 음성인식 중지 알림
                    await _socket.EmitAsync("stop_voice_recognition");
                    
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
        /// 음성인식 시작 확인 이벤트 핸들러
        /// </summary>
        private void HandleVoiceRecognitionStarted(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<VoiceRecognitionStatusData>();
                _loggingService.LogInfo($"🎤 음성인식 시작 확인: {data.message}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성인식 시작 확인 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 음성인식 중지 확인 이벤트 핸들러
        /// </summary>
        private void HandleVoiceRecognitionStopped(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<VoiceRecognitionStatusData>();
                _loggingService.LogInfo($"🛑 음성인식 중지 확인: {data.message}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성인식 중지 확인 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 음성인식 오류 이벤트 핸들러
        /// </summary>
        private void HandleVoiceRecognitionError(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<VoiceRecognitionErrorData>();
                _loggingService.LogError($"🚨 음성인식 오류: {data.error}");
                ErrorOccurred?.Invoke(this, $"음성인식 오류: {data.error}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성인식 오류 처리 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 오디오 청크 수신 확인 이벤트 핸들러
        /// </summary>
        private void HandleAudioChunkReceived(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<AudioChunkReceivedData>();
                // 로그 스팸 방지를 위해 Debug 레벨로 처리
                // _loggingService.LogDebug($"🎵 오디오 청크 수신 확인: {data.audio_length} bytes");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"오디오 청크 수신 확인 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 오디오 처리 오류 이벤트 핸들러
        /// </summary>
        private void HandleAudioProcessingError(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<AudioProcessingErrorData>();
                _loggingService.LogError($"🚨 오디오 처리 오류: {data.error}");
                ErrorOccurred?.Invoke(this, $"오디오 처리 오류: {data.error}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"오디오 처리 오류 핸들러 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 트랜스크립션 오류 이벤트 핸들러
        /// </summary>
        private void HandleTranscriptionError(SocketIOResponse response)
        {
            try
            {
                var data = response.GetValue<TranscriptionErrorData>();
                _loggingService.LogError($"🚨 트랜스크립션 오류: {data.error}");
                ErrorOccurred?.Invoke(this, $"트랜스크립션 오류: {data.error}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"트랜스크립션 오류 핸들러 오류: {ex.Message}");
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
        /// 오디오 장치를 다시 감지하고 설정을 새로고침하는 함수
        /// 마이크가 변경되거나 새로 연결되었을 때 사용합니다.
        /// </summary>
        public async Task<bool> RefreshAudioDeviceAsync()
        {
            try
            {
                _loggingService.LogInfo("🔄 오디오 장치 새로고침 시작...");

                // 현재 녹음 중이면 중지
                bool wasRecording = _isRecording;
                if (wasRecording)
                {
                    await StopRecordingAsync();
                }

                // 기존 WaveIn 해제
                _waveIn?.Dispose();

                // 오디오 캡처 재초기화
                InitializeAudioCapture();

                // 녹음이 진행 중이었다면 다시 시작
                if (wasRecording)
                {
                    await StartRecordingAsync();
                }

                _loggingService.LogInfo("✅ 오디오 장치 새로고침 완료");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 오디오 장치 새로고침 실패: {ex.Message}");
                ErrorOccurred?.Invoke(this, $"오디오 장치 새로고침 실패: {ex.Message}");
                return false;
            }
        }

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