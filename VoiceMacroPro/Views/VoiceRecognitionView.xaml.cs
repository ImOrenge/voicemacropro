using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VoiceMacroPro.Models;
using VoiceMacroPro.Services;
using VoiceMacroPro.Utils;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// GPT-4o 실시간 음성인식을 위한 View 클래스
    /// WebSocket 기반 실시간 트랜스크립션, 매크로 매칭, 결과 표시 기능을 포함합니다.
    /// </summary>
    public partial class VoiceRecognitionView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region 서비스 의존성
        private readonly ApiService _apiService;
        private readonly LoggingService _loggingService;
        private readonly VoiceRecognitionWrapperService _voiceService;
        #endregion

        #region 실시간 트랜스크립션 관련 프로퍼티
        
        /// <summary>
        /// 실시간 트랜스크립션 결과를 저장하는 컬렉션
        /// </summary>
        private ObservableCollection<TranscriptionResult> _transcriptionResults;
        public ObservableCollection<TranscriptionResult> TranscriptionResults
        {
            get => _transcriptionResults;
            set
            {
                _transcriptionResults = value;
                OnPropertyChanged(nameof(TranscriptionResults));
            }
        }

        /// <summary>
        /// 매크로 실행 결과를 저장하는 컬렉션
        /// </summary>
        private ObservableCollection<VoiceMatchResultModel> _macroResults;
        public ObservableCollection<VoiceMatchResultModel> MacroResults
        {
            get => _macroResults;
            set
            {
                _macroResults = value;
                OnPropertyChanged(nameof(MacroResults));
            }
        }

        /// <summary>
        /// 현재 트랜스크립션 텍스트 (실시간 업데이트)
        /// </summary>
        private string _currentTranscription = string.Empty;
        public string CurrentTranscription
        {
            get => _currentTranscription;
            set
            {
                _currentTranscription = value;
                OnPropertyChanged(nameof(CurrentTranscription));
            }
        }

        /// <summary>
        /// 현재 신뢰도 점수 (0.0 ~ 1.0)
        /// </summary>
        private double _currentConfidence = 0.0;
        public double CurrentConfidence
        {
            get => _currentConfidence;
            set
            {
                _currentConfidence = value;
                OnPropertyChanged(nameof(CurrentConfidence));
                OnPropertyChanged(nameof(ConfidencePercentage));
                OnPropertyChanged(nameof(ConfidenceColor));
            }
        }

        /// <summary>
        /// 신뢰도를 백분율로 표시
        /// </summary>
        public string ConfidencePercentage => $"{CurrentConfidence * 100:F1}%";

        /// <summary>
        /// 신뢰도에 따른 색상 (높음: 녹색, 중간: 주황색, 낮음: 빨간색)
        /// </summary>
        public System.Windows.Media.Brush ConfidenceColor
        {
            get
            {
                if (CurrentConfidence >= 0.8) return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                if (CurrentConfidence >= 0.6) return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }
        #endregion

        #region 연결 및 녹음 상태 관리
        
        /// <summary>
        /// WebSocket 연결 상태
        /// </summary>
        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        public ConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        /// <summary>
        /// 녹음 상태
        /// </summary>
        private bool _isRecording = false;
        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                _isRecording = value;
                OnPropertyChanged(nameof(IsRecording));
                OnPropertyChanged(nameof(RecordingStatusText));
                OnPropertyChanged(nameof(RecordingButtonText));
            }
        }

        /// <summary>
        /// 연결 상태 텍스트
        /// </summary>
        public string ConnectionStatusText => ConnectionStatus == ConnectionStatus.Connected ? "연결됨" : "연결 해제됨";

        /// <summary>
        /// 연결 상태 색상
        /// </summary>
        public System.Windows.Media.Brush ConnectionStatusColor => ConnectionStatus == ConnectionStatus.Connected ? 
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) : 
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);

        /// <summary>
        /// 녹음 상태 텍스트
        /// </summary>
        public string RecordingStatusText => IsRecording ? "녹음 중..." : "대기 중";

        /// <summary>
        /// 녹음 버튼 텍스트
        /// </summary>
        public string RecordingButtonText => IsRecording ? "중지" : "시작";

        /// <summary>
        /// 오디오 입력 레벨 (0.0 ~ 1.0)
        /// </summary>
        private double _audioLevel = 0.0;
        public double AudioLevel
        {
            get => _audioLevel;
            set
            {
                _audioLevel = value;
                OnPropertyChanged(nameof(AudioLevel));
                OnPropertyChanged(nameof(AudioLevelPercentage));
            }
        }

        /// <summary>
        /// 오디오 레벨을 백분율로 표시
        /// </summary>
        public string AudioLevelPercentage => $"{AudioLevel * 100:F1}%";

        /// <summary>
        /// 현재 사용 중인 마이크 장치 정보
        /// </summary>
        private string _currentMicrophoneDevice = "윈도우 기본 마이크";
        public string CurrentMicrophoneDevice
        {
            get => _currentMicrophoneDevice;
            set
            {
                _currentMicrophoneDevice = value;
                OnPropertyChanged(nameof(CurrentMicrophoneDevice));
            }
        }

        /// <summary>
        /// 마이크 장치 새로고침 중 여부
        /// </summary>
        private bool _isRefreshingAudioDevice = false;
        public bool IsRefreshingAudioDevice
        {
            get => _isRefreshingAudioDevice;
            set
            {
                _isRefreshingAudioDevice = value;
                OnPropertyChanged(nameof(IsRefreshingAudioDevice));
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }

        /// <summary>
        /// 새로고침 버튼 텍스트
        /// </summary>
        public string RefreshButtonText => IsRefreshingAudioDevice ? "새로고침 중..." : "마이크 새로고침";
        #endregion

        #region 세션 통계
        
        /// <summary>
        /// 현재 세션 정보
        /// </summary>
        private VoiceSession _currentSession;
        public VoiceSession CurrentSession
        {
            get => _currentSession;
            set
            {
                _currentSession = value;
                OnPropertyChanged(nameof(CurrentSession));
                OnPropertyChanged(nameof(SessionDuration));
                OnPropertyChanged(nameof(SessionStats));
            }
        }

        /// <summary>
        /// 세션 지속 시간
        /// </summary>
        public string SessionDuration
        {
            get
            {
                if (CurrentSession == null) return "00:00:00";
                var duration = DateTime.Now - CurrentSession.StartTime;
                return duration.ToString(@"hh\:mm\:ss");
            }
        }

        /// <summary>
        /// 세션 통계 문자열
        /// </summary>
        public string SessionStats
        {
            get
            {
                if (CurrentSession == null) return "통계 없음";
                return $"트랜스크립션: {CurrentSession.TranscriptionCount}개 | 매크로 실행: {CurrentSession.SuccessfulMacroExecutions}개 | 평균 신뢰도: {CurrentSession.AverageConfidence:F2}";
            }
        }
        #endregion

        /// <summary>
        /// VoiceRecognitionView 생성자
        /// GPT-4o 실시간 음성인식 서비스를 초기화합니다.
        /// </summary>
        public VoiceRecognitionView()
        {
            InitializeComponent();
            
            try
            {
                // 서비스 초기화
                _apiService = new ApiService();
                _loggingService = LoggingService.Instance;
                _voiceService = new VoiceRecognitionWrapperService();
                
                // 컬렉션 초기화
                TranscriptionResults = new ObservableCollection<TranscriptionResult>();
                MacroResults = new ObservableCollection<VoiceMatchResultModel>();
                CurrentSession = new VoiceSession();
                
                // 데이터 컨텍스트 설정
                DataContext = this;
                
                // 이벤트 핸들러 등록
                SetupEventHandlers();
                Loaded += VoiceRecognitionView_Loaded;
                
                _loggingService.LogInfo("GPT-4o 음성 인식 View 초기화 완료");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"GPT-4o 음성 인식 시스템 초기화에 실패했습니다.\n{ex.Message}");
                _loggingService.LogError($"VoiceRecognitionView 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 이벤트 핸들러들을 설정하는 함수
        /// </summary>
        private void SetupEventHandlers()
        {
            // 트랜스크립션 결과 수신 이벤트
            _voiceService.TranscriptionReceived += OnTranscriptionReceived;
            
            // 매크로 실행 결과 수신 이벤트
            _voiceService.MacroExecuted += OnMacroExecuted;
            
            // 에러 발생 이벤트
            _voiceService.ErrorOccurred += OnErrorOccurred;
            
            // 연결 상태 변경 이벤트
            _voiceService.ConnectionChanged += OnConnectionChanged;
            
            // 오디오 레벨 변경 이벤트
            _voiceService.AudioLevelChanged += OnAudioLevelChanged;
        }

        /// <summary>
        /// View가 로드될 때 실행되는 이벤트 핸들러
        /// </summary>
        private async void VoiceRecognitionView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeGpt4oVoiceSystemAsync();
        }

        /// <summary>
        /// GPT-4o 음성 인식 시스템을 초기화하는 비동기 함수
        /// </summary>
        private async Task InitializeGpt4oVoiceSystemAsync()
        {
            try
            {
                _loggingService.LogInfo("GPT-4o 음성 인식 시스템 초기화 시작");
                
                // 버튼 비활성화
                SetUIButtonsEnabled(false);
                
                // WebSocket 서비스 초기화
                bool initialized = await _voiceService.InitializeAsync();
                
                if (initialized)
                {
                    _loggingService.LogInfo("GPT-4o 음성 인식 시스템 초기화 성공");
                    
                    // 현재 세션 정보 업데이트
                    CurrentSession = _voiceService.GetCurrentSession();
                    
                    // 버튼 활성화
                    SetUIButtonsEnabled(true);
                    
                    UIHelper.ShowInfo("GPT-4o 실시간 음성인식 시스템이 준비되었습니다.");
                }
                else
                {
                    _loggingService.LogError("GPT-4o 음성 인식 시스템 초기화 실패");
                    UIHelper.ShowError("GPT-4o 서비스 연결에 실패했습니다.\n네트워크 연결과 API 키를 확인해주세요.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"GPT-4o 음성 인식 시스템 초기화 실패: {ex.Message}");
                UIHelper.ShowError($"GPT-4o 음성 인식 시스템 초기화에 실패했습니다: {ex.Message}");
            }
        }

        #region 이벤트 핸들러들

        /// <summary>
        /// 트랜스크립션 결과를 처리하는 이벤트 핸들러
        /// </summary>
        private void OnTranscriptionReceived(object sender, TranscriptionResult result)
        {
            // UI 스레드에서 실행
            Dispatcher.Invoke(() =>
            {
                if (result.Type == "partial")
                {
                    // 부분 결과는 현재 트랜스크립션 텍스트를 업데이트
                    CurrentTranscription = result.Text;
                }
                else if (result.Type == "final")
                {
                    // 최종 결과는 컬렉션에 추가하고 현재 텍스트 초기화
                    TranscriptionResults.Insert(0, result); // 최신 결과를 맨 위에 표시
                    CurrentTranscription = string.Empty;
                    CurrentConfidence = result.Confidence;
                    
                    // 컬렉션 크기 제한 (성능 최적화)
                    while (TranscriptionResults.Count > 100)
                    {
                        TranscriptionResults.RemoveAt(TranscriptionResults.Count - 1);
                    }
                    
                    // 세션 통계 업데이트
                    CurrentSession = _voiceService.GetCurrentSession();
                }
            });
        }

        /// <summary>
        /// 매크로 실행 결과를 처리하는 이벤트 핸들러
        /// </summary>
        private void OnMacroExecuted(object sender, VoiceMatchResultModel result)
        {
            Dispatcher.Invoke(() =>
            {
                MacroResults.Insert(0, result); // 최신 결과를 맨 위에 표시
                
                // 컬렉션 크기 제한
                while (MacroResults.Count > 50)
                {
                    MacroResults.RemoveAt(MacroResults.Count - 1);
                }
                
                // 성공적인 매크로 실행 시 사용자 알림
                if (result.IsExecuted)
                {
                    _loggingService.LogInfo($"매크로 실행 성공: {result.MacroName}");
                }
            });
        }

        /// <summary>
        /// 에러를 처리하는 이벤트 핸들러
        /// </summary>
        private void OnErrorOccurred(object sender, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                _loggingService.LogError($"음성인식 에러: {errorMessage}");
                
                // 중요한 에러만 사용자에게 표시
                if (errorMessage.Contains("연결") || errorMessage.Contains("초기화"))
                {
                    UIHelper.ShowError($"음성인식 오류: {errorMessage}");
                }
            });
        }

        /// <summary>
        /// 연결 상태 변경을 처리하는 이벤트 핸들러
        /// </summary>
        private void OnConnectionChanged(object sender, ConnectionStatus status)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ConnectionStatus = status;
                SetUIButtonsEnabled(status == ConnectionStatus.Connected);
                
                if (status == ConnectionStatus.Connected)
                {
                    _loggingService.LogInfo("✅ GPT-4o 음성인식 서비스에 연결됨");
                }
                else if (status == ConnectionStatus.Error)
                {
                    _loggingService.LogError("❌ GPT-4o 음성인식 서비스 연결 오류");
                }
            });
        }

        /// <summary>
        /// 오디오 레벨 변경을 처리하는 이벤트 핸들러
        /// </summary>
        private void OnAudioLevelChanged(object sender, double level)
        {
            Dispatcher.Invoke(() =>
            {
                AudioLevel = level;
            });
        }

        #endregion

        #region 버튼 이벤트 핸들러들

        /// <summary>
        /// 녹음 시작/중지 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void RecordingToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsRecording)
                {
                    // 녹음 시작
                    bool started = await _voiceService.StartRecordingAsync();
                    if (started)
                    {
                        IsRecording = true;
                        _loggingService.LogInfo("실시간 음성 녹음 시작");
                    }
                    else
                    {
                        UIHelper.ShowError("녹음을 시작할 수 없습니다.");
                    }
                }
                else
                {
                    // 녹음 중지
                    bool stopped = await _voiceService.StopRecordingAsync();
                    if (stopped)
                    {
                        IsRecording = false;
                        _loggingService.LogInfo("실시간 음성 녹음 중지");
                    }
                    else
                    {
                        UIHelper.ShowError("녹음을 중지할 수 없습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"녹음 토글 오류: {ex.Message}");
                UIHelper.ShowError($"녹음 제어 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 연결 재시도 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("🔄 GPT-4o 서비스 재연결 시도");
                
                // 1단계: 백엔드 WebSocket 서버 연결 확인
                UIHelper.ShowInfo("WebSocket 서버에 연결하는 중...");
                
                // 기존 연결 정리
                _voiceService.Dispose();
                
                // 2단계: 새 서비스 인스턴스로 재초기화
                await InitializeGpt4oVoiceSystemAsync();
                
                if (!_voiceService.IsConnected)
                {
                    UIHelper.ShowError("WebSocket 서버 연결에 실패했습니다.\n백엔드 서버가 실행 중인지 확인하세요.");
                    return;
                }
                
                // 3단계: GPT-4o 트랜스크립션 서비스 상태 확인
                UIHelper.ShowInfo("GPT-4o 서비스 연결 상태를 확인하는 중...");
                
                bool gpt4oReady = await CheckGpt4oServiceStatus();
                
                if (gpt4oReady)
                {
                    UIHelper.ShowInfo("✅ GPT-4o 실시간 음성인식 서비스가 성공적으로 연결되었습니다!\n이제 마이크 테스트를 진행할 수 있습니다.");
                    _loggingService.LogInfo("✅ GPT-4o 서비스 재연결 성공");
                }
                else
                {
                    // 4단계: GPT-4o 연결 시도
                    UIHelper.ShowInfo("GPT-4o 서비스에 연결하는 중...");
                    bool gpt4oConnected = await ConnectToGpt4oService();
                    
                    if (gpt4oConnected)
                    {
                        UIHelper.ShowInfo("✅ GPT-4o 서비스 연결 완료!\n실시간 음성인식이 준비되었습니다.");
                        _loggingService.LogInfo("✅ GPT-4o 서비스 연결 성공");
                    }
                    else
                    {
                        UIHelper.ShowWarning("⚠️ GPT-4o 서비스 연결에 실패했습니다.\nWhisper 기반 음성인식을 사용합니다.\n\n가능한 원인:\n- OpenAI API 키 미설정\n- 네트워크 연결 문제\n- GPT-4o 베타 권한 부족");
                        _loggingService.LogWarning("⚠️ GPT-4o 서비스 연결 실패, Whisper 폴백 사용");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 재연결 오류: {ex.Message}");
                UIHelper.ShowError($"재연결 중 오류가 발생했습니다:\n{ex.Message}\n\n네트워크 연결과 백엔드 서버 상태를 확인하세요.");
            }
        }

        /// <summary>
        /// GPT-4o 트랜스크립션 서비스 상태를 확인하는 함수
        /// </summary>
        /// <returns>GPT-4o 서비스가 준비되었는지 여부</returns>
        private async Task<bool> CheckGpt4oServiceStatus()
        {
            try
            {
                // HttpClient를 통해 직접 GPT-4o 서비스 상태 확인
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await httpClient.GetAsync("http://localhost:5000/api/gpt4o/status");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDocument = System.Text.Json.JsonDocument.Parse(content);
                    var root = jsonDocument.RootElement;
                    
                    if (root.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                    {
                        var data = root.GetProperty("data");
                        bool serviceAvailable = data.GetProperty("service_available").GetBoolean();
                        bool enabled = data.GetProperty("enabled").GetBoolean();
                        bool apiKeyConfigured = data.GetProperty("api_key_configured").GetBoolean();
                        bool realTimeConnected = data.GetProperty("real_time_connected").GetBoolean();
                        
                        _loggingService.LogInfo($"📊 GPT-4o 상태 - 서비스: {serviceAvailable}, 활성화: {enabled}, API키: {apiKeyConfigured}, 연결: {realTimeConnected}");
                        
                        return serviceAvailable && enabled && apiKeyConfigured && realTimeConnected;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ GPT-4o 상태 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// GPT-4o 트랜스크립션 서비스에 연결을 시도하는 함수
        /// </summary>
        /// <returns>연결 성공 여부</returns>
        private async Task<bool> ConnectToGpt4oService()
        {
            try
            {
                // HttpClient를 통해 직접 GPT-4o 서비스 연결 시도
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("http://localhost:5000/api/gpt4o/connect", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _loggingService.LogInfo("🤖 GPT-4o 서비스 연결 요청 전송됨");
                    
                    // 연결 완료까지 대기 (최대 10초)
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(1000); // 1초 대기
                        
                        bool isConnected = await CheckGpt4oServiceStatus();
                        if (isConnected)
                        {
                            return true;
                        }
                    }
                    
                    _loggingService.LogWarning("⏱️ GPT-4o 연결 타임아웃");
                    return false;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _loggingService.LogError($"❌ GPT-4o 연결 요청 실패: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ GPT-4o 연결 시도 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 결과 지우기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TranscriptionResults.Clear();
                MacroResults.Clear();
                CurrentTranscription = string.Empty;
                CurrentConfidence = 0.0;
                AudioLevel = 0.0;
                
                _loggingService.LogInfo("음성인식 결과 초기화 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"결과 초기화 오류: {ex.Message}");
                UIHelper.ShowError($"결과 초기화 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 세션 통계 새로고침 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void RefreshStatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CurrentSession = _voiceService.GetCurrentSession();
                _loggingService.LogInfo("세션 통계 새로고침 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"통계 새로고침 오류: {ex.Message}");
                UIHelper.ShowError($"통계 새로고침 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 마이크 장치 새로고침 버튼 클릭 이벤트 핸들러
        /// 윈도우 기본 마이크 설정을 다시 감지하고 적용합니다.
        /// </summary>
        private async void RefreshAudioDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsRefreshingAudioDevice = true;
                _loggingService.LogInfo("🔄 마이크 장치 새로고침 시작");

                // 마이크 장치 새로고침 실행
                bool success = await _voiceService.RefreshAudioDeviceAsync();
                
                if (success)
                {
                    CurrentMicrophoneDevice = "윈도우 기본 마이크 (새로고침됨)";
                    _loggingService.LogInfo("✅ 마이크 장치 새로고침 완료");
                    
                    // 사용자에게 성공 메시지 표시
                    UIHelper.ShowInfo("마이크 장치가 성공적으로 새로고침되었습니다.\n윈도우 기본 마이크를 사용합니다.");
                }
                else
                {
                    _loggingService.LogWarning("⚠️ 마이크 장치 새로고침 실패");
                    UIHelper.ShowWarning("마이크 장치 새로고침에 실패했습니다.\n마이크가 올바르게 연결되어 있는지 확인하세요.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 마이크 장치 새로고침 오류: {ex.Message}");
                UIHelper.ShowError($"마이크 장치 새로고침 중 오류가 발생했습니다:\n{ex.Message}");
            }
            finally
            {
                IsRefreshingAudioDevice = false;
            }
        }

        /// <summary>
        /// 마이크 테스트 버튼 클릭 이벤트 핸들러
        /// 현재 마이크 장치의 동작 상태를 테스트합니다.
        /// </summary>
        private async void TestMicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("🎤 마이크 테스트 시작");

                // 연결 상태 확인 (잘못된 IsRecording 대신 ConnectionStatus 사용)
                if (ConnectionStatus != ConnectionStatus.Connected)
                {
                    UIHelper.ShowWarning("GPT-4o 서비스에 연결되어 있지 않습니다.\n먼저 '연결 재시도' 버튼을 클릭하여 서비스에 연결해주세요.");
                    _loggingService.LogWarning("⚠️ GPT-4o 서비스 미연결 상태에서 마이크 테스트 시도");
                    return;
                }

                // WebSocket 연결 확인
                if (!_voiceService.IsConnected)
                {
                    UIHelper.ShowWarning("WebSocket 서버에 연결되어 있지 않습니다.\n서비스를 다시 초기화하겠습니다.");
                    await InitializeGpt4oVoiceSystemAsync();
                    return;
                }

                // 짧은 녹음 테스트 (3초)
                UIHelper.ShowInfo("마이크 테스트를 시작합니다.\n3초간 말씀해 주세요.");
                
                bool testStarted = await _voiceService.StartRecordingAsync();
                if (testStarted)
                {
                    IsRecording = true;
                    _loggingService.LogInfo("✅ 마이크 테스트 녹음 시작");
                    
                    // 3초 후 자동 중지
                    await Task.Delay(3000);
                    
                    bool testStopped = await _voiceService.StopRecordingAsync();
                    if (testStopped)
                    {
                        IsRecording = false;
                        _loggingService.LogInfo("✅ 마이크 테스트 완료");
                        UIHelper.ShowInfo("마이크 테스트가 완료되었습니다.\n트랜스크립션 결과를 확인하세요.");
                    }
                }
                else
                {
                    UIHelper.ShowError("마이크 테스트를 시작할 수 없습니다.\n마이크가 올바르게 연결되어 있는지 확인하세요.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 마이크 테스트 오류: {ex.Message}");
                UIHelper.ShowError($"마이크 테스트 중 오류가 발생했습니다:\n{ex.Message}");
                
                // 오류 발생 시 녹음 상태 정리
                if (IsRecording)
                {
                    await _voiceService.StopRecordingAsync();
                    IsRecording = false;
                }
            }
        }

        #endregion

        #region UI 헬퍼 함수들

        /// <summary>
        /// UI 버튼들의 활성화 상태를 설정하는 함수
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        private void SetUIButtonsEnabled(bool enabled)
        {
            // 버튼들이 XAML에 정의되어 있다고 가정
            // 실제 구현 시 XAML의 버튼 이름에 맞춰 수정 필요
            Dispatcher.Invoke(() =>
            {
                // RecordingToggleButton.IsEnabled = enabled;
                // ReconnectButton.IsEnabled = enabled;
                // ClearResultsButton.IsEnabled = enabled;
                // RefreshStatsButton.IsEnabled = enabled;
            });
        }

        #endregion

        #region INotifyPropertyChanged 구현

        /// <summary>
        /// 프로퍼티 변경 이벤트
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 프로퍼티 변경 알림을 발생시키는 함수
        /// </summary>
        /// <param name="propertyName">변경된 프로퍼티 이름</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region 리소스 정리

        /// <summary>
        /// View가 언로드될 때 리소스를 정리하는 함수
        /// </summary>
        public void Cleanup()
        {
            try
            {
                // 이벤트 핸들러 해제
                if (_voiceService != null)
                {
                    _voiceService.TranscriptionReceived -= OnTranscriptionReceived;
                    _voiceService.MacroExecuted -= OnMacroExecuted;
                    _voiceService.ErrorOccurred -= OnErrorOccurred;
                    _voiceService.ConnectionChanged -= OnConnectionChanged;
                    _voiceService.AudioLevelChanged -= OnAudioLevelChanged;
                    
                    _voiceService.Dispose();
                }
                
                _loggingService.LogInfo("VoiceRecognitionView 리소스 정리 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"VoiceRecognitionView 리소스 정리 오류: {ex.Message}");
            }
        }

        #endregion
    }
} 