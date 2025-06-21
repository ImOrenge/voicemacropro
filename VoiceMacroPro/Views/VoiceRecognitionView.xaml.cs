using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    public partial class VoiceRecognitionView : UserControl, INotifyPropertyChanged
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
        private ObservableCollection<VoiceMatchResult> _macroResults;
        public ObservableCollection<VoiceMatchResult> MacroResults
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
        public Brush ConfidenceColor
        {
            get
            {
                if (CurrentConfidence >= 0.8) return Brushes.Green;
                if (CurrentConfidence >= 0.6) return Brushes.Orange;
                return Brushes.Red;
            }
        }
        #endregion

        #region 연결 및 녹음 상태 관리
        
        /// <summary>
        /// WebSocket 연결 상태
        /// </summary>
        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(ConnectionStatusText));
                OnPropertyChanged(nameof(ConnectionStatusColor));
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
        public string ConnectionStatusText => IsConnected ? "연결됨" : "연결 해제됨";

        /// <summary>
        /// 연결 상태 색상
        /// </summary>
        public Brush ConnectionStatusColor => IsConnected ? Brushes.Green : Brushes.Red;

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
                MacroResults = new ObservableCollection<VoiceMatchResult>();
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
        private void OnMacroExecuted(object sender, VoiceMatchResult result)
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
                if (result.IsSuccessful)
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
            Dispatcher.Invoke(() =>
            {
                IsConnected = status.IsConnected;
                _loggingService.LogInfo($"연결 상태 변경: {status.Status}");
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
                _loggingService.LogInfo("GPT-4o 서비스 재연결 시도");
                
                // 기존 연결 정리
                _voiceService.Dispose();
                
                // 새 서비스 인스턴스로 재초기화
                await InitializeGpt4oVoiceSystemAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"재연결 오류: {ex.Message}");
                UIHelper.ShowError($"재연결 중 오류가 발생했습니다: {ex.Message}");
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