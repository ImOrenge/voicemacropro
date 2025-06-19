using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using VoiceMacroPro.Models;
using VoiceMacroPro.Services;
using VoiceMacroPro.Views;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using VoiceMacroPro.Utils;

namespace VoiceMacroPro
{
    /// <summary>
    /// MainWindow - VoiceMacro Pro 메인 윈도우
    /// 대시보드 기반 UI 구조로 설계된 메인 애플리케이션 창입니다.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields
        
        /// <summary>
        /// API 서비스 인스턴스
        /// </summary>
        private readonly ApiService _apiService;
        
        /// <summary>
        /// 로깅 서비스 인스턴스
        /// </summary>
        private readonly LoggingService _loggingService;
        
        /// <summary>
        /// 음성 인식 서비스 인스턴스
        /// </summary>
        private readonly VoiceRecognitionWrapperService _voiceService;
        
        /// <summary>
        /// 변경사항 서비스 인스턴스
        /// </summary>
        private readonly ChangelogService _changelogService;
        
        /// <summary>
        /// 현재 선택된 섹션
        /// </summary>
        private string _currentSection = "Dashboard";
        
        /// <summary>
        /// 대시보드 뷰 인스턴스
        /// </summary>
        private DashboardView? _dashboardView = null;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// MainWindow 생성자
        /// 서비스들을 초기화하고 UI를 설정합니다.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // 서비스 초기화
            _loggingService = new LoggingService();
            _apiService = new ApiService();
            _voiceService = new VoiceRecognitionWrapperService();
            _changelogService = new ChangelogService();
            
            // 이벤트 핸들러 등록
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            
            _loggingService.LogInfo("MainWindow 초기화 완료");
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// 윈도우 로드 완료 이벤트 핸들러
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("MainWindow 로드 시작");
                
                // View 생성 테스트 시작
                _loggingService.LogInfo("=== View 생성 테스트 시작 ===");
                
                try
                {
                    _loggingService.LogInfo("DashboardView 테스트 생성 시도...");
                    var testDashboard = new DashboardView();
                    _loggingService.LogInfo("✅ DashboardView 생성 성공");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"❌ DashboardView 생성 실패: {ex.Message}");
                }
                
                try
                {
                    _loggingService.LogInfo("MacroManagementView 테스트 생성 시도...");
                    var testMacro = new MacroManagementView();
                    _loggingService.LogInfo("✅ MacroManagementView 생성 성공");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"❌ MacroManagementView 생성 실패: {ex.Message}");
                }
                
                try
                {
                    _loggingService.LogInfo("CustomScriptingView 테스트 생성 시도...");
                    var testScript = new CustomScriptingView();
                    _loggingService.LogInfo("✅ CustomScriptingView 생성 성공");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"❌ CustomScriptingView 생성 실패: {ex.Message}");
                }
                
                try
                {
                    _loggingService.LogInfo("VoiceRecognitionView 테스트 생성 시도...");
                    var testVoice = new VoiceRecognitionView();
                    _loggingService.LogInfo("✅ VoiceRecognitionView 생성 성공");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"❌ VoiceRecognitionView 생성 실패: {ex.Message}");
                }
                
                try
                {
                    _loggingService.LogInfo("LogMonitoringView 테스트 생성 시도...");
                    var testLog = new LogMonitoringView();
                    _loggingService.LogInfo("✅ LogMonitoringView 생성 성공");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"❌ LogMonitoringView 생성 실패: {ex.Message}");
                }
                
                try
                {
                    _loggingService.LogInfo("PresetManagementView 테스트 생성 시도...");
                    var testPreset = new PresetManagementView();
                    _loggingService.LogInfo("✅ PresetManagementView 생성 성공");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"❌ PresetManagementView 생성 실패: {ex.Message}");
                }
                
                try
                {
                    _loggingService.LogInfo("DeveloperInfoView 테스트 생성 시도...");
                    var testDev = new DeveloperInfoView();
                    _loggingService.LogInfo("✅ DeveloperInfoView 생성 성공");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"❌ DeveloperInfoView 생성 실패: {ex.Message}");
                }
                
                _loggingService.LogInfo("=== View 생성 테스트 완료 ===");
                
                // 서버 연결 확인
                await CheckServerConnection();
                
                // 대시보드로 네비게이션
                NavigateToSection("Dashboard");
                
                _loggingService.LogInfo("MainWindow 로드 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"MainWindow 로드 중 오류: {ex.Message}");
                MessageBox.Show($"애플리케이션 초기화 중 오류가 발생했습니다:\n{ex.Message}", 
                              "초기화 오류", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 윈도우 종료 이벤트 핸들러
        /// </summary>
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("MainWindow 종료 시작");
                
                // 서비스 정리
                _voiceService?.Dispose();
                
                _loggingService.LogInfo("MainWindow 종료 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"MainWindow 종료 중 오류: {ex.Message}");
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// 서버 연결 상태를 확인합니다.
        /// </summary>
        private async Task CheckServerConnection()
        {
            try
            {
                _loggingService.LogInfo("서버 연결 상태 확인 시작");
                
                var isConnected = await _apiService.TestConnectionAsync();
                
                if (isConnected)
                {
                    _loggingService.LogInfo("서버 연결 성공");
                }
                else
                {
                    _loggingService.LogWarning("서버 연결 실패 - 오프라인 모드로 실행");
                    MessageBox.Show("서버에 연결할 수 없습니다.\n일부 기능이 제한될 수 있습니다.", 
                                  "서버 연결 실패", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"서버 연결 확인 중 오류: {ex.Message}");
            }
        }
        
        #endregion

        #region Navigation Methods
        
        /// <summary>
        /// 네비게이션 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void NavigateToSection(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo($"네비게이션 버튼 클릭됨. Sender: {sender?.GetType().Name}");
                
                if (sender is FrameworkElement element)
                {
                    _loggingService.LogInfo($"FrameworkElement 확인됨. Tag: {element.Tag}");
                    
                    if (element.Tag is string section)
                    {
                        _loggingService.LogInfo($"Tag에서 섹션 추출됨: {section}");
                        NavigateToSection(section);
                    }
                    else
                    {
                        _loggingService.LogError($"Tag가 문자열이 아님: {element.Tag?.GetType().Name ?? "null"}");
                    }
                }
                else
                {
                    _loggingService.LogError($"Sender가 FrameworkElement가 아님: {sender?.GetType().Name ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"네비게이션 버튼 클릭 핸들러 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 지정된 섹션으로 네비게이션을 수행합니다.
        /// </summary>
        /// <param name="section">이동할 섹션 이름</param>
        private void NavigateToSection(string section)
        {
            try
            {
                _loggingService.LogInfo($"섹션으로 네비게이션: {section}");
                
                _currentSection = section;
                UpdatePageHeader(section);
                LoadSectionContent(section);
                UpdateSidebarSelection(section);
                
                _loggingService.LogInfo($"섹션 네비게이션 완료: {section}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"섹션 네비게이션 중 오류: {ex.Message}");
                LoadErrorContent(section, ex.Message);
            }
        }
        
        /// <summary>
        /// 페이지 헤더를 업데이트합니다.
        /// </summary>
        /// <param name="section">현재 섹션</param>
        private void UpdatePageHeader(string section)
        {
            try
            {
                var headerMapping = new Dictionary<string, (string title, string breadcrumb)>
                {
                    { "Dashboard", ("대시보드", "홈 > 대시보드") },
                    { "MacroManagement", ("매크로 관리", "홈 > 매크로 관리") },
                    { "CustomScripting", ("커스텀 스크립팅", "홈 > 커스텀 스크립팅") },
                    { "VoiceRecognition", ("음성 인식", "홈 > 음성 인식") },
                    { "LogMonitoring", ("로그 모니터링", "홈 > 로그 모니터링") },
                    { "PresetManagement", ("프리셋 관리", "홈 > 프리셋 관리") },
                    { "DeveloperInfo", ("개발자 정보", "홈 > 개발자 정보") }
                };

                if (headerMapping.TryGetValue(section, out var header))
                {
                    if (this.FindName("PageTitleText") is System.Windows.Controls.TextBlock titleText)
                    {
                        titleText.Text = header.title;
                    }
                    
                    if (this.FindName("PageBreadcrumbText") is System.Windows.Controls.TextBlock breadcrumbText)
                    {
                        breadcrumbText.Text = header.breadcrumb;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"페이지 헤더 업데이트 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 섹션 내용을 로드합니다.
        /// </summary>
        /// <param name="section">로드할 섹션</param>
        private void LoadSectionContent(string section)
        {
            try
            {
                _loggingService.LogInfo($"LoadSectionContent 시작: {section}");
                
                // ContentArea를 직접 참조
                if (ContentArea == null)
                {
                    _loggingService.LogError("ContentArea를 찾을 수 없습니다.");
                    return;
                }

                _loggingService.LogInfo($"ContentArea 확인됨, 섹션별 콘텐츠 로드 시작: {section}");

                switch (section)
                {
                    case "Dashboard":
                        try
                        {
                            _loggingService.LogInfo("Dashboard View 생성 시도");
                            _dashboardView ??= new DashboardView();
                            ContentArea.Content = _dashboardView;
                            _loggingService.LogInfo("Dashboard View 로드 완료");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"Dashboard View 생성 오류: {ex.Message}");
                            ContentArea.Content = CreatePlaceholderContent("Dashboard 오류", "⚠️", $"Dashboard 로드 실패: {ex.Message}");
                        }
                        break;
                        
                    case "MacroManagement":
                        try
                        {
                            _loggingService.LogInfo("MacroManagement View 생성 시도");
                            ContentArea.Content = new MacroManagementView();
                            _loggingService.LogInfo("MacroManagement View 로드 완료");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"MacroManagement View 생성 오류: {ex.Message}");
                            ContentArea.Content = CreatePlaceholderContent("매크로 관리 오류", "⚠️", $"매크로 관리 로드 실패: {ex.Message}");
                        }
                        break;
                        
                    case "CustomScripting":
                        try
                        {
                            _loggingService.LogInfo("CustomScripting View 생성 시도");
                            ContentArea.Content = new CustomScriptingView();
                            _loggingService.LogInfo("CustomScripting View 로드 완료");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"CustomScripting View 생성 오류: {ex.Message}");
                            ContentArea.Content = CreatePlaceholderContent("커스텀 스크립팅 오류", "⚠️", $"커스텀 스크립팅 로드 실패: {ex.Message}");
                        }
                        break;
                        
                    case "VoiceRecognition":
                        try
                        {
                            _loggingService.LogInfo("VoiceRecognition View 생성 시도");
                            ContentArea.Content = new VoiceRecognitionView();
                            _loggingService.LogInfo("VoiceRecognition View 로드 완료");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"VoiceRecognition View 생성 오류: {ex.Message}");
                            ContentArea.Content = CreatePlaceholderContent("음성 인식 오류", "⚠️", $"음성 인식 로드 실패: {ex.Message}");
                        }
                        break;
                        
                    case "LogMonitoring":
                        try
                        {
                            _loggingService.LogInfo("LogMonitoring View 생성 시도");
                            ContentArea.Content = new LogMonitoringView();
                            _loggingService.LogInfo("LogMonitoring View 로드 완료");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"LogMonitoring View 생성 오류: {ex.Message}");
                            ContentArea.Content = CreatePlaceholderContent("로그 모니터링 오류", "⚠️", $"로그 모니터링 로드 실패: {ex.Message}");
                        }
                        break;
                        
                    case "PresetManagement":
                        try
                        {
                            _loggingService.LogInfo("PresetManagement View 생성 시도");
                            ContentArea.Content = new PresetManagementView();
                            _loggingService.LogInfo("PresetManagement View 로드 완료");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"PresetManagement View 생성 오류: {ex.Message}");
                            ContentArea.Content = CreatePlaceholderContent("프리셋 관리 오류", "⚠️", $"프리셋 관리 로드 실패: {ex.Message}");
                        }
                        break;
                        
                    case "DeveloperInfo":
                        try
                        {
                            _loggingService.LogInfo("DeveloperInfo View 생성 시도");
                            ContentArea.Content = new DeveloperInfoView();
                            _loggingService.LogInfo("DeveloperInfo View 로드 완료");
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"DeveloperInfo View 생성 오류: {ex.Message}");
                            ContentArea.Content = CreatePlaceholderContent("개발자 정보 오류", "⚠️", $"개발자 정보 로드 실패: {ex.Message}");
                        }
                        break;
                        
                    default:
                        _loggingService.LogWarning($"알 수 없는 섹션: {section}");
                        ContentArea.Content = CreatePlaceholderContent(section, "❓", $"{section} 섹션이 구현 중입니다.");
                        break;
                }
                
                _loggingService.LogInfo($"LoadSectionContent 완료: {section}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"섹션 내용 로드 중 치명적 오류: {ex.Message}");
                LoadErrorContent(section, ex.Message);
            }
        }
        
        /// <summary>
        /// 플레이스홀더 내용을 생성합니다.
        /// </summary>
        /// <param name="title">제목</param>
        /// <param name="icon">아이콘</param>
        /// <param name="description">설명</param>
        /// <returns>생성된 UI 요소</returns>
        private UIElement CreatePlaceholderContent(string title, string icon, string description)
        {
            var stackPanel = new System.Windows.Controls.StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconText = new System.Windows.Controls.TextBlock
            {
                Text = icon,
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var titleText = new System.Windows.Controls.TextBlock
            {
                Text = title,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var descText = new System.Windows.Controls.TextBlock
            {
                Text = description,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(descText);

            return stackPanel;
        }
        
        /// <summary>
        /// 오류 내용을 로드합니다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="errorMessage">오류 메시지</param>
        private void LoadErrorContent(string section, string errorMessage)
        {
            try
            {
                if (ContentArea != null)
                {
                    ContentArea.Content = CreatePlaceholderContent($"{section} 오류", "⚠️", $"오류: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"오류 내용 로드 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 사이드바 선택 상태를 업데이트합니다.
        /// </summary>
        /// <param name="section">선택된 섹션</param>
        private void UpdateSidebarSelection(string section)
        {
            try
            {
                // 사이드바 버튼 스타일 리셋
                ResetSidebarButtonStyles();
                
                // 현재 섹션 버튼 활성화
                var buttonName = $"{section}MenuButton";
                if (this.FindName(buttonName) is System.Windows.Controls.Button button)
                {
                    button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x90, 0xE2));
                    button.Foreground = System.Windows.Media.Brushes.White;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"사이드바 선택 업데이트 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 사이드바 버튼 스타일을 리셋합니다.
        /// </summary>
        private void ResetSidebarButtonStyles()
        {
            try
            {
                var sections = new[] { "Dashboard", "MacroManagement", "CustomScripting", "VoiceRecognition", "LogMonitoring", "PresetManagement", "DeveloperInfo" };
                
                foreach (var section in sections)
                {
                    var buttonName = $"{section}MenuButton";
                    if (this.FindName(buttonName) is System.Windows.Controls.Button button)
                    {
                        button.Background = System.Windows.Media.Brushes.Transparent;
                        button.Foreground = System.Windows.Media.Brushes.White;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"사이드바 버튼 스타일 리셋 중 오류: {ex.Message}");
            }
        }
        
        #endregion

        #region Public Navigation Methods
        
        /// <summary>
        /// 매크로 관리 섹션으로 이동합니다.
        /// </summary>
        public void NavigateToMacroManagement()
        {
            NavigateToSection("MacroManagement");
        }

        /// <summary>
        /// 커스텀 스크립팅 섹션으로 이동합니다.
        /// </summary>
        public void NavigateToCustomScripting()
        {
            NavigateToSection("CustomScripting");
        }

        /// <summary>
        /// 음성 인식 섹션으로 이동합니다.
        /// </summary>
        public void NavigateToVoiceRecognition()
        {
            NavigateToSection("VoiceRecognition");
        }

        /// <summary>
        /// 로그 모니터링 섹션으로 이동합니다.
        /// </summary>
        public void NavigateToLogMonitoring()
        {
            NavigateToSection("LogMonitoring");
        }

        /// <summary>
        /// 프리셋 관리 섹션으로 이동합니다.
        /// </summary>
        public void NavigateToPresetManagement()
        {
            NavigateToSection("PresetManagement");
        }
        
        #endregion

        #region Search Functionality
        
        /// <summary>
        /// 전역 검색 박스 포커스 이벤트 핸들러
        /// </summary>
        private void GlobalSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && textBox.Text == "Search macros, scripts, presets...")
            {
                textBox.Text = "";
                textBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        /// <summary>
        /// 전역 검색 박스 포커스 해제 이벤트 핸들러
        /// </summary>
        private void GlobalSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Search macros, scripts, presets...";
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
        
        #endregion

        #region Changelog Popup Functionality
        
        /// <summary>
        /// 알림(종모양) 버튼 클릭 이벤트 핸들러
        /// 변경사항 팝업을 애니메이션과 함께 표시합니다.
        /// </summary>
        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("변경사항 팝업 표시 요청");
                ShowChangelogPopup();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 팝업 표시 실패: {ex.Message}");
                UIHelper.ShowError($"변경사항을 표시하는 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 변경사항 팝업 닫기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void CloseChangelogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("변경사항 팝업 닫기 요청");
                HideChangelogPopup();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 팝업 닫기 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 변경사항 오버레이 클릭 이벤트 핸들러 (배경 클릭 시 팝업 닫기)
        /// </summary>
        private void ChangelogOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 오버레이 배경을 클릭한 경우에만 팝업 닫기
                if (e.Source == ChangelogOverlay)
                {
                    _loggingService.LogInfo("변경사항 팝업 배경 클릭으로 닫기");
                    HideChangelogPopup();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 팝업 배경 클릭 처리 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 변경사항 팝업을 애니메이션과 함께 표시하는 함수
        /// </summary>
        private void ShowChangelogPopup()
        {
            try
            {
                // 변경사항 데이터 로드
                var latestChanges = _changelogService.GetLatestChanges(8);
                
                if (latestChanges == null || !latestChanges.Any())
                {
                    _loggingService.LogWarning("표시할 변경사항이 없습니다.");
                    UIHelper.ShowInfo("현재 표시할 변경사항이 없습니다.");
                    return;
                }

                // 팝업 내용 업데이트
                UpdateChangelogContent(latestChanges);

                // 오버레이 표시
                ChangelogOverlay.Visibility = Visibility.Visible;

                // 애니메이션 실행
                var showAnimation = this.FindResource("PopupShowAnimation") as Storyboard;
                if (showAnimation != null)
                {
                    Storyboard.SetTarget(showAnimation, ChangelogPopup);
                    showAnimation.Begin();
                }

                _loggingService.LogInfo($"변경사항 팝업 표시 완료 ({latestChanges.Count}개 항목)");
                
                // 모든 변경사항을 읽음으로 표시
                _changelogService.MarkAllAsRead();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 팝업 표시 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 변경사항 팝업을 애니메이션과 함께 숨기는 함수
        /// </summary>
        private void HideChangelogPopup()
        {
            try
            {
                var hideAnimation = this.FindResource("PopupHideAnimation") as Storyboard;
                if (hideAnimation != null)
                {
                    Storyboard.SetTarget(hideAnimation, ChangelogPopup);
                    
                    // 애니메이션 완료 후 오버레이 숨기기
                    hideAnimation.Completed += (s, e) =>
                    {
                        ChangelogOverlay.Visibility = Visibility.Collapsed;
                    };
                    
                    hideAnimation.Begin();
                }
                else
                {
                    // 애니메이션이 없는 경우 바로 숨기기
                    ChangelogOverlay.Visibility = Visibility.Collapsed;
                }

                _loggingService.LogInfo("변경사항 팝업 숨김 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 팝업 숨김 중 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 변경사항 팝업의 내용을 업데이트하는 함수
        /// </summary>
        /// <param name="changes">표시할 변경사항 목록</param>
        private void UpdateChangelogContent(List<ChangelogItem> changes)
        {
            try
            {
                // 기존 내용 클리어
                ChangelogItemsPanel.Children.Clear();

                foreach (var change in changes.Take(8)) // 최대 8개만 표시
                {
                    var changeItem = CreateChangelogItemUI(change);
                    ChangelogItemsPanel.Children.Add(changeItem);
                }

                // 마지막 업데이트 날짜 설정
                var latestDate = changes.Max(c => c.Date);
                LastUpdateText.Text = $"마지막 업데이트: {latestDate:yyyy-MM-dd}";

                _loggingService.LogInfo($"변경사항 내용 업데이트 완료 ({changes.Count}개 항목)");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 내용 업데이트 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 개별 변경사항 UI 요소를 생성하는 함수
        /// </summary>
        /// <param name="change">변경사항 데이터</param>
        /// <returns>생성된 UI 요소</returns>
        private UIElement CreateChangelogItemUI(ChangelogItem change)
        {
            try
            {
                var border = new Border
                {
                    Style = this.FindResource("ChangelogItemStyle") as Style,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // 타입 아이콘
                var typeIcon = new TextBlock
                {
                    Text = change.TypeIcon,
                    FontSize = 18,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(typeIcon, 0);

                // 내용 패널
                var contentPanel = new StackPanel
                {
                    Margin = new Thickness(0, 0, 10, 0)
                };

                // 제목
                var titleText = new TextBlock
                {
                    Text = change.Title,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x20, 0x2C)),
                    TextWrapping = TextWrapping.Wrap
                };

                // 설명
                var descText = new TextBlock
                {
                    Text = change.Description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x71, 0x80, 0x96)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 5)
                };

                // 타입 및 우선순위 패널
                var metaPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                // 타입 배지
                var typeBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(change.TypeColor)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(0, 0, 5, 0)
                };

                var typeBadgeText = new TextBlock
                {
                    Text = change.Type.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                };
                typeBadge.Child = typeBadgeText;

                // 우선순위 배지 (높음/긴급만 표시)
                if (change.Priority >= 3)
                {
                    var priorityBadge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(change.PriorityColor)),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 5, 0)
                    };

                    var priorityText = new TextBlock
                    {
                        Text = change.PriorityText,
                        FontSize = 9,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold
                    };
                    priorityBadge.Child = priorityText;
                    metaPanel.Children.Add(priorityBadge);
                }

                metaPanel.Children.Add(typeBadge);

                contentPanel.Children.Add(titleText);
                contentPanel.Children.Add(descText);
                contentPanel.Children.Add(metaPanel);

                Grid.SetColumn(contentPanel, 1);

                // 날짜
                var dateText = new TextBlock
                {
                    Text = change.Date.ToString("MM-dd"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xAE, 0xC0)),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(dateText, 2);

                // 새로운 항목 표시
                if (change.IsNew)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
                    border.BorderThickness = new Thickness(3, 0, 0, 0);
                }

                grid.Children.Add(typeIcon);
                grid.Children.Add(contentPanel);
                grid.Children.Add(dateText);

                border.Child = grid;

                return border;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 UI 생성 실패: {ex.Message}");
                
                // 오류 발생 시 간단한 텍스트 반환
                return new TextBlock
                {
                    Text = $"오류: {change.Title}",
                    FontSize = 12,
                    Foreground = Brushes.Red,
                    Margin = new Thickness(0, 5, 0, 0)
                };
            }
        }
        
        #endregion
    }
} 