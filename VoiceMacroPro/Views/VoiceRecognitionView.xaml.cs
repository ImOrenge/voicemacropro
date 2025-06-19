using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VoiceMacroPro.Models;
using VoiceMacroPro.Services;
using VoiceMacroPro.Utils;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 음성 인식 기능을 제공하는 View 클래스
    /// 실시간 음성 인식, 매크로 매칭, 결과 표시 기능을 포함합니다.
    /// </summary>
    public partial class VoiceRecognitionView : UserControl, INotifyPropertyChanged
    {
        // 서비스 의존성
        private readonly ApiService _apiService;
        private readonly LoggingService _loggingService;
        private readonly VoiceRecognitionWrapperService _voiceService;
        
        // 음성 인식 결과를 위한 컬렉션
        private ObservableCollection<VoiceAnalysisResult> _voiceResults;
        public ObservableCollection<VoiceAnalysisResult> VoiceResults
        {
            get => _voiceResults;
            set
            {
                _voiceResults = value;
                OnPropertyChanged(nameof(VoiceResults));
            }
        }
        
        // 녹음 상태 관리
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
        
        // UI 상태 프로퍼티들
        public string RecordingStatusText => IsRecording ? "녹음 중..." : "대기 중";
        public string RecordingButtonText => IsRecording ? "중지" : "시작";
        
        // 취소 토큰 (비동기 작업 관리용)
        private CancellationTokenSource? _cancellationTokenSource;
        
        // 마이크 입력 레벨
        private double _inputLevel = 0.0;
        public double InputLevel
        {
            get => _inputLevel;
            set
            {
                _inputLevel = value;
                OnPropertyChanged(nameof(InputLevel));
            }
        }
        
        /// <summary>
        /// VoiceRecognitionView 생성자
        /// 서비스들을 초기화하고 초기 설정을 수행합니다.
        /// </summary>
        public VoiceRecognitionView()
        {
            InitializeComponent();
            
            try
            {
                // 서비스 초기화
                _apiService = new ApiService();
                _loggingService = new LoggingService();
                _voiceService = new VoiceRecognitionWrapperService();
                
                // 컬렉션 초기화
                VoiceResults = new ObservableCollection<VoiceAnalysisResult>();
                
                // 데이터 컨텍스트 설정
                DataContext = this;
                
                // 이벤트 핸들러 등록
                Loaded += VoiceRecognitionView_Loaded;
                
                _loggingService.LogInfo("음성 인식 View 초기화 완료");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"음성 인식 시스템 초기화에 실패했습니다.\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// View가 로드될 때 실행되는 이벤트 핸들러
        /// </summary>
        private async void VoiceRecognitionView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeVoiceSystemAsync();
        }
        
        /// <summary>
        /// 음성 인식 시스템을 초기화하는 비동기 함수
        /// </summary>
        private async Task InitializeVoiceSystemAsync()
        {
            try
            {
                _loggingService.LogInfo("음성 인식 시스템 초기화 시작");
                
                // 마이크 장치 목록 로드
                await LoadMicrophoneDevicesAsync();
                
                // 상태 업데이트
                await UpdateVoiceStatusAsync();
                
                _loggingService.LogInfo("음성 인식 시스템 초기화 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 시스템 초기화 실패: {ex.Message}");
                UIHelper.ShowError($"음성 인식 시스템 초기화에 실패했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 사용 가능한 마이크 장치 목록을 로드하는 함수
        /// </summary>
        private async Task LoadMicrophoneDevicesAsync()
        {
            try
            {
                var devices = await _voiceService.GetAvailableDevicesAsync();
                
                // ComboBox에 장치 목록 바인딩 (UI에 ComboBox가 있다면)
                if (devices.Count > 0)
                {
                    _loggingService.LogInfo($"마이크 장치 {devices.Count}개 발견");
                }
                else
                {
                    _loggingService.LogWarning("사용 가능한 마이크 장치가 없습니다.");
                    UIHelper.ShowWarning("사용 가능한 마이크 장치가 없습니다.\n음성 인식 기능을 사용할 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 장치 로드 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 음성 인식 상태를 업데이트하는 함수
        /// </summary>
        private async Task UpdateVoiceStatusAsync()
        {
            try
            {
                var status = await _voiceService.GetRecordingStatusAsync();
                if (status != null)
                {
                    IsRecording = status.IsRecording;
                    InputLevel = status.AudioLevel;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 상태 업데이트 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 테스트 버튼 클릭 이벤트 핸들러
        /// 음성 인식 테스트를 수행합니다.
        /// </summary>
        private async void TestVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("음성 인식 테스트 시작");
                
                // 테스트 상태 UI 표시
                TestVoiceButton.IsEnabled = false;
                TestVoiceButton.Content = "테스트 중...";
                
                // 마이크 테스트 수행
                var testResult = await _voiceService.TestMicrophoneAsync();
                
                if (testResult != null && testResult.Success)
                {
                    UIHelper.ShowSuccess($"마이크 테스트 성공!\n\n장치 사용 가능: {(testResult.DeviceAvailable ? "예" : "아니오")}\n녹음 테스트: {(testResult.RecordingTest ? "통과" : "실패")}\n오디오 레벨 감지: {(testResult.AudioLevelDetected ? "정상" : "감지되지 않음")}");
                    
                    // 실제 음성 인식 테스트
                    await PerformVoiceRecognitionTestAsync();
                }
                else
                {
                    var errorMsg = testResult?.ErrorMessage ?? "알 수 없는 오류";
                    UIHelper.ShowError($"마이크 테스트 실패:\n{errorMsg}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 테스트 실패: {ex.Message}");
                UIHelper.ShowError($"음성 인식 테스트 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                // UI 상태 복원
                TestVoiceButton.IsEnabled = true;
                TestVoiceButton.Content = "테스트 시작";
            }
        }
        
        /// <summary>
        /// 실제 음성 인식 테스트를 수행하는 함수
        /// </summary>
        private async Task PerformVoiceRecognitionTestAsync()
        {
            try
            {
                UIHelper.ShowInfo("3초간 음성을 녹음하여 인식을 테스트합니다.\n'확인'을 누른 후 마이크에 대고 말해보세요.");
                
                // 3초간 음성 분석 및 매크로 매칭
                var results = await _voiceService.AnalyzeVoiceAndMatchMacrosAsync(3.0);
                
                if (results != null && results.Count > 0)
                {
                    // 결과를 UI에 추가
                    foreach (var result in results)
                    {
                        // VoiceMatchResult는 이미 올바른 형식이므로 직접 추가
                        VoiceResults.Add(new VoiceAnalysisResult
                        {
                            RecognizedText = result.RecognizedText,
                            Confidence = result.Confidence * 100, // 퍼센트로 변환
                            Language = "ko-KR", // 기본 언어 설정
                            ProcessingTime = 0.0, // VoiceMatchResult에는 처리 시간이 없음
                            MatchedMacros = new List<MacroMatchInfo>
                            {
                                new MacroMatchInfo
                                {
                                    Id = result.MacroId,
                                    Name = result.MacroName,
                                    VoiceCommand = result.VoiceCommand,
                                    ActionType = "combo", // 기본값
                                    KeySequence = result.ActionDescription,
                                    MatchConfidence = result.Confidence * 100
                                }
                            }
                        });
                    }
                    
                    // 가장 높은 신뢰도의 결과 표시
                    var bestResult = results.OrderByDescending(r => r.Confidence).First();
                    UIHelper.ShowSuccess($"음성 인식 성공!\n\n인식된 텍스트: '{bestResult.RecognizedText}'\n신뢰도: {bestResult.Confidence * 100:F1}%\n매칭된 매크로: {bestResult.MacroName}");
                }
                else
                {
                    UIHelper.ShowWarning("음성이 인식되지 않았습니다.\n마이크 설정과 주변 소음을 확인해주세요.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 수행 실패: {ex.Message}");
                UIHelper.ShowError($"음성 인식 수행 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 음성 인식 시작/중지 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void StartRecognitionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsRecording)
                {
                    // 녹음 시작
                    _loggingService.LogInfo("연속 음성 인식 시작");
                    
                    var success = await _voiceService.StartRecordingAsync();
                    if (success)
                    {
                        IsRecording = true;
                        
                        // 연속 인식 모니터링 시작
                        _cancellationTokenSource = new CancellationTokenSource();
                        _ = Task.Run(() => ContinuousRecognitionMonitorAsync(_cancellationTokenSource.Token));
                        
                        UIHelper.ShowInfo("연속 음성 인식이 시작되었습니다.");
                    }
                    else
                    {
                        UIHelper.ShowError("음성 인식 시작에 실패했습니다.");
                    }
                }
                else
                {
                    // 녹음 중지
                    await StopContinuousRecognitionAsync();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 제어 실패: {ex.Message}");
                UIHelper.ShowError($"음성 인식 제어 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 연속 음성 인식을 중지하는 함수
        /// </summary>
        private async Task StopContinuousRecognitionAsync()
        {
            try
            {
                _loggingService.LogInfo("연속 음성 인식 중지");
                
                // 취소 토큰 발행
                _cancellationTokenSource?.Cancel();
                
                // 녹음 중지
                var success = await _voiceService.StopRecordingAsync();
                if (success)
                {
                    IsRecording = false;
                    UIHelper.ShowInfo("연속 음성 인식이 중지되었습니다.");
                }
                else
                {
                    UIHelper.ShowWarning("음성 인식 중지에 실패했습니다.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 중지 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 연속 음성 인식을 모니터링하는 비동기 함수
        /// </summary>
        private async Task ContinuousRecognitionMonitorAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsRecording)
                {
                    // 주기적으로 상태 확인 및 결과 처리
                    await UpdateVoiceStatusAsync();
                    
                    // 2초마다 확인
                    await Task.Delay(2000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 정상적인 취소
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"연속 인식 모니터링 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 음성 인식 중지 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void StopRecognitionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsRecording)
                {
                    await StopContinuousRecognitionAsync();
                }
                else
                {
                    UIHelper.ShowInfo("현재 음성 인식이 실행되고 있지 않습니다.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 중지 실패: {ex.Message}");
                UIHelper.ShowError($"음성 인식 중지 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 결과 초기화 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirmResult = UIHelper.ShowConfirm("모든 음성 인식 결과를 삭제하시겠습니까?");
                if (!confirmResult) return;
                
                _loggingService.LogInfo("음성 인식 결과 초기화");
                
                // 결과 목록 초기화
                VoiceResults.Clear();
                
                UIHelper.ShowSuccess("음성 인식 결과가 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"결과 초기화 실패: {ex.Message}");
                UIHelper.ShowError($"결과 초기화에 실패했습니다.\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// PropertyChanged 이벤트 (INotifyPropertyChanged 구현)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// 프로퍼티 변경 알림을 발생시키는 헬퍼 메서드
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 