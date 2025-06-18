using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using VoiceMacroPro.Models;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 대시보드 홈 화면을 표시하는 사용자 컨트롤
    /// 주요 통계, 빠른 액션, 최근 활동 등을 보여줍니다.
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        /// <summary>
        /// 대시보드 데이터를 로드하는 메서드
        /// 통계 정보, 최근 활동 등을 초기화합니다.
        /// </summary>
        private void LoadDashboardData()
        {
            try
            {
                // 임시 데이터로 초기화 (실제로는 서비스에서 가져와야 함)
                UpdateStatistics();
                LoadRecentMacros();
                LoadFavoritePresets();
                UpdateSystemInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"대시보드 데이터 로드 중 오류 발생: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 통계 정보를 업데이트하는 메서드
        /// </summary>
        private void UpdateStatistics()
        {
            // 실제로는 서비스에서 데이터를 가져와야 함
            TotalMacrosText.Text = "12";
            CustomScriptsText.Text = "5";
            PresetsText.Text = "3";
            SystemStatusText.Text = "정상";
            SystemStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#38A169"));
        }

        /// <summary>
        /// 최근 사용한 매크로 목록을 로드하는 메서드
        /// </summary>
        private void LoadRecentMacros()
        {
            var recentMacros = new List<object>
            {
                new { Name = "공격 콤보", VoiceCommand = "공격", LastUsed = "5분 전" },
                new { Name = "힐링", VoiceCommand = "힐", LastUsed = "10분 전" },
                new { Name = "스킬 연속 사용", VoiceCommand = "스킬", LastUsed = "15분 전" },
                new { Name = "인벤토리 정리", VoiceCommand = "정리", LastUsed = "1시간 전" }
            };

            RecentMacrosListBox.ItemsSource = recentMacros;
        }

        /// <summary>
        /// 즐겨찾기 프리셋 목록을 로드하는 메서드
        /// </summary>
        private void LoadFavoritePresets()
        {
            var favoritePresets = new List<object>
            {
                new { Name = "LOL 전투", MacroCount = 8 },
                new { Name = "PUBG 서바이벌", MacroCount = 12 },
                new { Name = "공통 유틸리티", MacroCount = 5 }
            };

            FavoritePresetsListBox.ItemsSource = favoritePresets;
        }

        /// <summary>
        /// 시스템 정보를 업데이트하는 메서드
        /// </summary>
        private void UpdateSystemInfo()
        {
            // 실제로는 시스템 모니터링 서비스에서 가져와야 함
            CpuUsageBar.Value = 25;
            CpuUsageText.Text = "25%";
            
            MemoryUsageBar.Value = 45;
            MemoryUsageText.Text = "45%";
            
            VoiceAccuracyBar.Value = 92;
            VoiceAccuracyText.Text = "92%";
        }

        // 빠른 액션 버튼 이벤트 핸들러들

        /// <summary>
        /// 새 매크로 추가 버튼 클릭 이벤트
        /// </summary>
        private void QuickAddMacroButton_Click(object sender, RoutedEventArgs e)
        {
            // MainWindow의 메서드를 호출하여 매크로 관리 탭으로 이동
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToMacroManagement();
        }

        /// <summary>
        /// 음성 인식 시작 버튼 클릭 이벤트
        /// </summary>
        private void QuickStartVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToVoiceRecognition();
        }

        /// <summary>
        /// 프리셋 관리 버튼 클릭 이벤트
        /// </summary>
        private void QuickPresetButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToPresetManagement();
        }

        /// <summary>
        /// 스크립트 편집 버튼 클릭 이벤트
        /// </summary>
        private void QuickScriptButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToCustomScripting();
        }

        /// <summary>
        /// 로그 보기 버튼 클릭 이벤트
        /// </summary>
        private void QuickLogButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToLogMonitoring();
        }

        /// <summary>
        /// 설정 버튼 클릭 이벤트
        /// </summary>
        private void QuickSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("설정 기능은 추후 구현 예정입니다.", "알림", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 대시보드 데이터를 새로고침하는 공개 메서드
        /// 다른 탭에서 돌아왔을 때 호출될 수 있습니다.
        /// </summary>
        public void RefreshDashboard()
        {
            LoadDashboardData();
        }
    }
} 