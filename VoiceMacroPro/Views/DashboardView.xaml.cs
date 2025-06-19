using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VoiceMacroPro.Models;
using VoiceMacroPro.Services;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 대시보드 홈 화면을 표시하는 사용자 컨트롤
    /// 주요 통계, 빠른 액션, 최근 활동 등을 보여줍니다.
    /// </summary>
    public partial class DashboardView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields
        
        private readonly DashboardDataService _dashboardService;
        private readonly SystemMonitoringService _systemMonitoringService;
        private readonly DispatcherTimer _refreshTimer;
        
        #endregion
        
        #region Constructor
        
        public DashboardView()
        {
            InitializeComponent();
            
            // 서비스 초기화
            _dashboardService = DashboardDataService.Instance;
            _systemMonitoringService = SystemMonitoringService.Instance;
            
            // 자동 새로고침 타이머 설정 (30초마다)
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            
            // 시스템 모니터링 이벤트 구독
            _systemMonitoringService.PropertyChanged += SystemMonitoringService_PropertyChanged;
            
            // 초기 데이터 로드
            LoadDashboardData();
            
            // 시스템 모니터링 시작
            _systemMonitoringService.StartMonitoring();
            _refreshTimer.Start();
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// 자동 새로고침 타이머 이벤트 핸들러
        /// </summary>
        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            await RefreshDashboardDataAsync();
        }
        
        /// <summary>
        /// 시스템 모니터링 서비스 속성 변경 이벤트 핸들러
        /// </summary>
        private void SystemMonitoringService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(SystemMonitoringService.CpuUsage):
                        UpdateCpuUsage();
                        break;
                    case nameof(SystemMonitoringService.MemoryUsage):
                        UpdateMemoryUsage();
                        break;
                    case nameof(SystemMonitoringService.SystemStatus):
                        UpdateSystemStatus();
                        break;
                }
            });
        }
        
        #endregion

        #region Data Loading Methods

        /// <summary>
        /// 대시보드 데이터를 비동기로 로드하는 메서드
        /// 통계 정보, 최근 활동 등을 초기화합니다.
        /// </summary>
        private async void LoadDashboardData()
        {
            try
            {
                LoggingService.Instance.LogInfo("대시보드 데이터 로드 시작");
                
                // 실제 데이터로 초기화
                await UpdateStatisticsAsync();
                await LoadRecentMacrosAsync();
                await LoadFavoritePresetsAsync();
                UpdateSystemInfo();
                
                LoggingService.Instance.LogInfo("대시보드 데이터 로드 완료");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"대시보드 데이터 로드 실패: {ex.Message}");
                MessageBox.Show($"대시보드 데이터 로드 중 오류 발생: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 통계 정보를 비동기로 업데이트하는 메서드
        /// 실제 API에서 데이터를 가져와서 UI에 반영합니다.
        /// </summary>
        private async Task UpdateStatisticsAsync()
        {
            try
            {
                // 병렬로 데이터 가져오기
                var totalMacrosTask = _dashboardService.GetTotalMacroCountAsync();
                var customScriptsTask = _dashboardService.GetCustomScriptCountAsync();
                var presetsTask = _dashboardService.GetPresetCountAsync();
                var voiceAccuracyTask = _dashboardService.GetVoiceRecognitionAccuracyAsync();

                await Task.WhenAll(totalMacrosTask, customScriptsTask, presetsTask, voiceAccuracyTask);

                // UI 스레드에서 업데이트
                Dispatcher.Invoke(() =>
                {
                    TotalMacrosText.Text = totalMacrosTask.Result.ToString();
                    CustomScriptsText.Text = customScriptsTask.Result.ToString();
                    PresetsText.Text = presetsTask.Result.ToString();
                    
                    VoiceAccuracyBar.Value = voiceAccuracyTask.Result;
                    VoiceAccuracyText.Text = $"{voiceAccuracyTask.Result:F1}%";
                    
                    UpdateSystemStatus();
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"통계 정보 업데이트 실패: {ex.Message}");
                
                // 기본값으로 설정
                Dispatcher.Invoke(() =>
                {
                    TotalMacrosText.Text = "0";
                    CustomScriptsText.Text = "0";
                    PresetsText.Text = "0";
                });
            }
        }

        /// <summary>
        /// 최근 사용한 매크로 목록을 비동기로 로드하는 메서드
        /// </summary>
        private async Task LoadRecentMacrosAsync()
        {
            try
            {
                var recentMacros = await _dashboardService.GetRecentMacrosAsync();
                
                Dispatcher.Invoke(() =>
                {
                    RecentMacrosListBox.ItemsSource = recentMacros;
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"최근 매크로 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 즐겨찾기 프리셋 목록을 비동기로 로드하는 메서드
        /// </summary>
        private async Task LoadFavoritePresetsAsync()
        {
            try
            {
                var favoritePresets = await _dashboardService.GetFavoritePresetsAsync();
                
                Dispatcher.Invoke(() =>
                {
                    FavoritePresetsListBox.ItemsSource = favoritePresets;
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"즐겨찾기 프리셋 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 시스템 정보를 업데이트하는 메서드
        /// 실시간 시스템 모니터링 데이터를 UI에 반영합니다.
        /// </summary>
        private void UpdateSystemInfo()
        {
            try
            {
                UpdateCpuUsage();
                UpdateMemoryUsage();
                UpdateSystemStatus();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"시스템 정보 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// CPU 사용률 UI 업데이트
        /// </summary>
        private void UpdateCpuUsage()
        {
            var cpuUsage = _systemMonitoringService.CpuUsage;
            CpuUsageBar.Value = cpuUsage;
            CpuUsageText.Text = $"{cpuUsage:F1}%";
            
            // CPU 사용률에 따른 색상 변경
            if (cpuUsage > 80)
                CpuUsageBar.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 빨강
            else if (cpuUsage > 50)
                CpuUsageBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 주황
            else
                CpuUsageBar.Foreground = new SolidColorBrush(Color.FromRgb(66, 133, 244)); // 파랑
        }

        /// <summary>
        /// 메모리 사용률 UI 업데이트
        /// </summary>
        private void UpdateMemoryUsage()
        {
            var memoryUsage = _systemMonitoringService.MemoryUsage;
            MemoryUsageBar.Value = memoryUsage;
            MemoryUsageText.Text = $"{memoryUsage:F1}%";
            
            // 메모리 사용률에 따른 색상 변경
            if (memoryUsage > 80)
                MemoryUsageBar.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 빨강
            else if (memoryUsage > 60)
                MemoryUsageBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 주황
            else
                MemoryUsageBar.Foreground = new SolidColorBrush(Color.FromRgb(56, 161, 105)); // 초록
        }

        /// <summary>
        /// 시스템 상태 UI 업데이트
        /// </summary>
        private void UpdateSystemStatus()
        {
            var systemStatus = _systemMonitoringService.SystemStatus;
            SystemStatusText.Text = systemStatus;
            
            // 시스템 상태에 따른 색상 변경
            switch (systemStatus)
            {
                case "정상":
                    SystemStatusText.Foreground = new SolidColorBrush(Color.FromRgb(56, 161, 105)); // 초록
                    break;
                case "보통":
                    SystemStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 주황
                    break;
                case "높은 사용률":
                case "오류":
                    SystemStatusText.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 빨강
                    break;
                default:
                    SystemStatusText.Foreground = new SolidColorBrush(Color.FromRgb(113, 128, 150)); // 회색
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 대시보드 데이터를 수동으로 새로고침하는 메서드
        /// </summary>
        public async Task RefreshDashboardDataAsync()
        {
            try
            {
                LoggingService.Instance.LogInfo("대시보드 수동 새로고침 시작");
                
                await UpdateStatisticsAsync();
                await LoadRecentMacrosAsync();
                await LoadFavoritePresetsAsync();
                
                LoggingService.Instance.LogInfo("대시보드 수동 새로고침 완료");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"대시보드 새로고침 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 이전 호환성을 위한 동기식 새로고침 메서드
        /// </summary>
        public void RefreshDashboard()
        {
            _ = Task.Run(RefreshDashboardDataAsync);
        }

        #endregion

        #region Quick Action Button Event Handlers

        /// <summary>
        /// 새 매크로 추가 빠른 액션 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickAddMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.NavigateToMacroManagement();
                LoggingService.Instance.LogInfo("매크로 관리 화면으로 이동");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"매크로 관리 이동 실패: {ex.Message}");
                MessageBox.Show($"매크로 관리로 이동 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 음성 인식 시작 빠른 액션 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickStartVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.NavigateToVoiceRecognition();
                LoggingService.Instance.LogInfo("음성 인식 화면으로 이동");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"음성 인식 이동 실패: {ex.Message}");
                MessageBox.Show($"음성 인식으로 이동 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 프리셋 관리 빠른 액션 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.NavigateToPresetManagement();
                LoggingService.Instance.LogInfo("프리셋 관리 화면으로 이동");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"프리셋 관리 이동 실패: {ex.Message}");
                MessageBox.Show($"프리셋 관리로 이동 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 커스텀 스크립팅 빠른 액션 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.NavigateToCustomScripting();
                LoggingService.Instance.LogInfo("커스텀 스크립팅 화면으로 이동");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"커스텀 스크립팅 이동 실패: {ex.Message}");
                MessageBox.Show($"커스텀 스크립팅으로 이동 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 로그 모니터링 빠른 액션 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.NavigateToLogMonitoring();
                LoggingService.Instance.LogInfo("로그 모니터링 화면으로 이동");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"로그 모니터링 이동 실패: {ex.Message}");
                MessageBox.Show($"로그 모니터링으로 이동 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 설정 빠른 액션 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo("설정 기능 요청 (미구현)");
                MessageBox.Show("설정 기능은 아직 구현되지 않았습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"설정 기능 실행 실패: {ex.Message}");
                MessageBox.Show($"설정 기능 실행 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Disposal

        /// <summary>
        /// 리소스 정리
        /// </summary>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _refreshTimer?.Stop();
                _systemMonitoringService.PropertyChanged -= SystemMonitoringService_PropertyChanged;
                LoggingService.Instance.LogInfo("대시보드 뷰 리소스 정리 완료");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"대시보드 뷰 리소스 정리 실패: {ex.Message}");
            }
        }

        #endregion
    }
} 