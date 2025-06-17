using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using VoiceMacroPro.Models;
using VoiceMacroPro.Services;
using VoiceMacroPro.Views;
using System.Collections.ObjectModel;
using System.Text;

namespace VoiceMacroPro
{
    /// <summary>
    /// 메인 윈도우의 상호작용 로직을 담당하는 클래스
    /// 매크로 관리 UI의 이벤트 처리와 데이터 바인딩을 수행합니다.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly LoggingService _loggingService;
        private readonly VoiceRecognitionWrapperService _voiceService;
        private List<Macro> _allMacros = new List<Macro>();
        private string _currentSearchTerm = string.Empty;
        private string _currentSortBy = "name";
        private CollectionViewSource? _logViewSource;
        
        // 음성 인식 관련 필드
        private List<VoiceMatchResult> _currentMatchResults = new List<VoiceMatchResult>();
        private bool _isRecording = false;
        private System.Windows.Threading.DispatcherTimer? _statusUpdateTimer;

        // ==================== 매크로 설정 관련 필드 ====================
        private MacroActionType _currentMacroType = MacroActionType.Combo;
        private ComboActionSettings _comboSettings = new();
        private RapidActionSettings _rapidSettings = new();
        private HoldActionSettings _holdSettings = new();
        private ToggleActionSettings _toggleSettings = new();
        private RepeatActionSettings _repeatSettings = new();
        private ObservableCollection<ComboStep> _comboSteps = new();

        /// <summary>
        /// 메인 윈도우 생성자
        /// API 서비스를 초기화하고 UI를 설정합니다.
        /// </summary>
        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow 생성자 시작");
                Console.WriteLine("MainWindow 생성자 시작");
                
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent 완료");
                Console.WriteLine("InitializeComponent 완료");
                
                _apiService = new ApiService();
                System.Diagnostics.Debug.WriteLine("ApiService 초기화 완료");
                Console.WriteLine("ApiService 초기화 완료");
                
                // 로깅 서비스 초기화
                _loggingService = LoggingService.Instance;
                System.Diagnostics.Debug.WriteLine("LoggingService 인스턴스 획득 완료");
                Console.WriteLine("LoggingService 인스턴스 획득 완료");
                
                InitializeLoggingUI();
                System.Diagnostics.Debug.WriteLine("LoggingService UI 초기화 완료");
                Console.WriteLine("LoggingService UI 초기화 완료");
                
                // 음성 인식 서비스 초기화
                _voiceService = new VoiceRecognitionWrapperService();
                System.Diagnostics.Debug.WriteLine("VoiceRecognitionService 초기화 완료");
                Console.WriteLine("VoiceRecognitionService 초기화 완료");
                
                // 윈도우가 로드된 후 초기화 작업 수행
                Loaded += MainWindow_Loaded;
                System.Diagnostics.Debug.WriteLine("MainWindow 생성자 완료");
                Console.WriteLine("MainWindow 생성자 완료");
            }
            catch (Exception ex)
            {
                var errorMsg = $"MainWindow 생성자 오류: {ex.Message}\n\n스택 트레이스:\n{ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                Console.WriteLine(errorMsg);
                MessageBox.Show($"MainWindow 초기화 중 오류가 발생했습니다:\n{ex.Message}\n\n스택 트레이스:\n{ex.StackTrace}", 
                              "초기화 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        /// <summary>
        /// 윈도우 로드 완료 시 실행되는 이벤트 핸들러
        /// 서버 연결 상태 확인 및 초기 데이터 로드를 수행합니다.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded 시작");
                _loggingService.LogInfo("메인 윈도우 로드가 시작되었습니다.");
                
                UpdateStatusText("애플리케이션 초기화 중...");
                System.Diagnostics.Debug.WriteLine("상태 텍스트 업데이트 완료");
                
                // 서버 연결 상태 확인
                await CheckServerConnection();
                System.Diagnostics.Debug.WriteLine("서버 연결 확인 완료");
                
                // 매크로 목록 로드
                await LoadMacros();
                System.Diagnostics.Debug.WriteLine("매크로 로드 완료");
                
                // 프리셋 목록 로드
                await LoadPresets();
                System.Diagnostics.Debug.WriteLine("프리셋 로드 완료");
                
                // 음성 인식 UI 초기화 (UI 요소들이 모두 로드된 후)
                InitializeVoiceRecognitionUI();
                System.Diagnostics.Debug.WriteLine("음성 인식 UI 초기화 완료");
                
                UpdateStatusText("준비 완료");
                _loggingService.LogInfo("애플리케이션 초기화가 완료되었습니다.");
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded 오류: {ex}");
                MessageBox.Show($"윈도우 로드 중 오류가 발생했습니다:\n{ex.Message}\n\n스택 트레이스:\n{ex.StackTrace}", 
                              "로드 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 서버 연결 상태를 확인하고 UI에 표시하는 함수
        /// </summary>
        private async Task CheckServerConnection()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("서버 연결 확인 시작");
                
                bool isConnected = await _apiService.CheckServerHealthAsync();
                
                if (isConnected)
                {
                    // 연결 성공 - 초록색 표시
                    if (ServerStatusIndicator != null)
                    {
                        ServerStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                    }
                    if (ServerStatusText != null)
                    {
                        ServerStatusText.Text = "서버 연결됨";
                    }
                    _loggingService.LogInfo("백엔드 API 서버에 성공적으로 연결되었습니다.");
                    System.Diagnostics.Debug.WriteLine("서버 연결 성공");
                }
                else
                {
                    // 연결 실패 - 빨간색 표시
                    if (ServerStatusIndicator != null)
                    {
                        ServerStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                    }
                    if (ServerStatusText != null)
                    {
                        ServerStatusText.Text = "서버 연결 실패";
                    }
                    
                    _loggingService.LogWarning("백엔드 API 서버에 연결할 수 없습니다. (http://localhost:5000)");
                    
                    MessageBox.Show("백엔드 서버에 연결할 수 없습니다.\n" +
                                  "Python API 서버가 실행 중인지 확인해주세요.\n" +
                                  "(주소: http://localhost:5000)", 
                                  "서버 연결 오류", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"서버 연결 확인 오류: {ex}");
                
                if (ServerStatusIndicator != null)
                {
                    ServerStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                }
                if (ServerStatusText != null)
                {
                    ServerStatusText.Text = "연결 오류";
                }
                
                MessageBox.Show($"서버 연결 확인 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 매크로 목록을 서버에서 불러와 DataGrid에 표시하는 함수
        /// </summary>
        private async Task LoadMacros()
        {
            try
            {
                UpdateStatusText("매크로 목록 로딩 중...");
                
                // API를 통해 매크로 목록 조회
                _allMacros = await _apiService.GetMacrosAsync(_currentSearchTerm, _currentSortBy);
                
                // DataGrid에 바인딩
                if (MacroDataGrid != null)
                {
                    MacroDataGrid.ItemsSource = _allMacros;
                }
                
                _loggingService.LogInfo($"매크로 목록 로드 완료: {_allMacros.Count}개 항목");
                UpdateStatusText($"매크로 {_allMacros.Count}개 로드 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 목록을 불러오는 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("매크로 로드 실패");
            }
        }

        /// <summary>
        /// 검색 버튼 클릭 이벤트 핸들러
        /// 입력된 검색어로 매크로를 검색합니다.
        /// </summary>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentSearchTerm = SearchTextBox.Text?.Trim() ?? string.Empty;
            await LoadMacros();
        }

        /// <summary>
        /// 정렬 방식 변경 이벤트 핸들러
        /// 선택된 정렬 기준으로 매크로 목록을 다시 로드합니다.
        /// </summary>
        private async void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentSortBy = selectedItem.Tag?.ToString() ?? "name";
                await LoadMacros();
            }
        }

        /// <summary>
        /// 새로고침 버튼 클릭 이벤트 핸들러
        /// 매크로 목록을 다시 로드합니다.
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMacros();
        }

        /// <summary>
        /// 매크로 목록에서 선택이 변경될 때 실행되는 이벤트 핸들러
        /// 선택된 항목에 따라 버튼 활성화 상태를 변경합니다.
        /// </summary>
        private void MacroDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = MacroDataGrid.SelectedItem != null;
            
            // 선택된 항목이 있을 때만 수정/복사/삭제 버튼 활성화
            EditMacroButton.IsEnabled = hasSelection;
            CopyMacroButton.IsEnabled = hasSelection;
            DeleteMacroButton.IsEnabled = hasSelection;
        }

        /// <summary>
        /// 새 매크로 추가 버튼 클릭 이벤트 핸들러
        /// 매크로 추가/편집 윈도우를 표시합니다.
        /// </summary>
        private async void AddMacroButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new MacroEditWindow();
            
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    UpdateStatusText("새 매크로 생성 중...");
                    
                    // 새 매크로 생성
                    var newMacroId = await _apiService.CreateMacroAsync(editWindow.MacroResult);
                    _loggingService.LogInfo($"새 매크로가 생성되었습니다: '{editWindow.MacroResult.Name}' (ID: {newMacroId})");
                    
                    // 목록 새로고침
                    await LoadMacros();
                    
                    UpdateStatusText("새 매크로가 성공적으로 추가되었습니다.");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("매크로 추가 중 오류 발생", ex);
                    MessageBox.Show($"매크로 추가 중 오류가 발생했습니다:\n{ex.Message}", 
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("매크로 추가 실패");
                }
            }
        }

        /// <summary>
        /// 매크로 수정 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 편집합니다.
        /// </summary>
        private async void EditMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (MacroDataGrid.SelectedItem is not Macro selectedMacro)
            {
                MessageBox.Show("수정할 매크로를 선택해주세요.", "선택 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editWindow = new MacroEditWindow(selectedMacro);
            
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    UpdateStatusText("매크로 수정 중...");
                    
                    // 매크로 수정
                    await _apiService.UpdateMacroAsync(editWindow.MacroResult);
                    _loggingService.LogInfo($"매크로가 수정되었습니다: '{editWindow.MacroResult.Name}' (ID: {editWindow.MacroResult.Id})");
                    
                    // 목록 새로고침
                    await LoadMacros();
                    
                    UpdateStatusText("매크로가 성공적으로 수정되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"매크로 수정 중 오류가 발생했습니다:\n{ex.Message}", 
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("매크로 수정 실패");
                }
            }
        }

        /// <summary>
        /// 매크로 복사 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 복사하여 새로운 매크로를 생성합니다.
        /// </summary>
        private async void CopyMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (MacroDataGrid.SelectedItem is not Macro selectedMacro)
            {
                MessageBox.Show("복사할 매크로를 선택해주세요.", "선택 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                UpdateStatusText("매크로 복사 중...");
                
                // 새로운 이름으로 복사
                string newName = $"{selectedMacro.Name}_복사";
                await _apiService.CopyMacroAsync(selectedMacro.Id, newName);
                
                // 목록 새로고침
                await LoadMacros();
                
                UpdateStatusText("매크로가 성공적으로 복사되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 복사 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("매크로 복사 실패");
            }
        }

        /// <summary>
        /// 매크로 삭제 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 삭제합니다.
        /// </summary>
        private async void DeleteMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (MacroDataGrid.SelectedItem is not Macro selectedMacro)
            {
                MessageBox.Show("삭제할 매크로를 선택해주세요.", "선택 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 삭제 확인 대화상자
            var result = MessageBox.Show(
                $"정말로 '{selectedMacro.Name}' 매크로를 삭제하시겠습니까?\n\n" +
                "이 작업은 되돌릴 수 없습니다.", 
                "삭제 확인", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                                    UpdateStatusText("매크로 삭제 중...");
                
                // 매크로 삭제
                await _apiService.DeleteMacroAsync(selectedMacro.Id);
                _loggingService.LogInfo($"매크로가 삭제되었습니다: '{selectedMacro.Name}' (ID: {selectedMacro.Id})");
                
                // 목록 새로고침
                await LoadMacros();
                
                UpdateStatusText("매크로가 성공적으로 삭제되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"매크로 삭제 중 오류가 발생했습니다:\n{ex.Message}", 
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("매크로 삭제 실패");
                }
            }
        }

        /// <summary>
        /// 상태 텍스트를 업데이트하는 헬퍼 함수
        /// </summary>
        /// <param name="message">표시할 상태 메시지</param>
        private void UpdateStatusText(string message)
        {
            try
            {
                if (StatusTextBlock != null)
                {
                    StatusTextBlock.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
                }
                System.Diagnostics.Debug.WriteLine($"상태 업데이트: {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"상태 텍스트 업데이트 오류: {ex}");
            }
        }

        /// <summary>
        /// 로깅 UI를 초기화하는 함수
        /// </summary>
        private void InitializeLoggingUI()
        {
            try
            {
                // 로그 데이터 바인딩 설정
                _logViewSource = new CollectionViewSource
                {
                    Source = _loggingService.LogEntries
                };
                
                if (LogDataGrid != null)
                {
                    LogDataGrid.ItemsSource = _logViewSource.View;
                }
                
                // 로그 카운트 바인딩
                if (LogCountTextBlock != null)
                {
                    var binding = new Binding("TotalLogCount")
                    {
                        Source = _loggingService,
                        StringFormat = "총 {0}개 로그"
                    };
                    LogCountTextBlock.SetBinding(TextBlock.TextProperty, binding);
                }
                
                _loggingService.LogInfo("로그 UI가 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 UI 초기화 오류: {ex}");
            }
        }

        /// <summary>
        /// 로그 레벨 콤보박스 선택 변경 이벤트 핸들러
        /// </summary>
        private void LogLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (LogLevelComboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    var levelText = selectedItem.Tag?.ToString();
                    if (Enum.TryParse<LogLevel>(levelText, out var logLevel))
                    {
                        _loggingService.CurrentLogLevel = logLevel;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("로그 레벨 변경 중 오류 발생", ex);
            }
        }

        /// <summary>
        /// 로그 필터 텍스트 변경 이벤트 핸들러
        /// </summary>
        private void LogFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_logViewSource?.View != null)
                {
                    var filterText = LogFilterTextBox?.Text?.Trim().ToLower();
                    
                    if (string.IsNullOrEmpty(filterText))
                    {
                        _logViewSource.View.Filter = null;
                    }
                    else
                    {
                        _logViewSource.View.Filter = obj =>
                        {
                            if (obj is LogEntry logEntry)
                            {
                                return logEntry.Message.ToLower().Contains(filterText) ||
                                       logEntry.LevelText.ToLower().Contains(filterText);
                            }
                            return false;
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("로그 필터링 중 오류 발생", ex);
            }
        }

        /// <summary>
        /// 자동 스크롤 체크박스 체크 이벤트 핸들러
        /// </summary>
        private void AutoScrollCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_loggingService != null)
            {
                _loggingService.IsAutoScroll = true;
            }
        }

        /// <summary>
        /// 자동 스크롤 체크박스 체크 해제 이벤트 핸들러
        /// </summary>
        private void AutoScrollCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_loggingService != null)
            {
                _loggingService.IsAutoScroll = false;
            }
        }

        /// <summary>
        /// 로그 내보내기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ExportLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "로그 파일 (*.log)|*.log|텍스트 파일 (*.txt)|*.txt|모든 파일 (*.*)|*.*",
                    DefaultExt = "log",
                    FileName = $"VoiceMacro_Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    UpdateStatusText("로그 내보내기 중...");
                    
                    var success = await _loggingService.ExportLogsAsync(saveFileDialog.FileName);
                    
                    if (success)
                    {
                        UpdateStatusText("로그 내보내기 완료");
                        MessageBox.Show($"로그가 성공적으로 저장되었습니다:\n{saveFileDialog.FileName}", 
                                      "내보내기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        UpdateStatusText("로그 내보내기 실패");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("로그 내보내기 중 오류 발생", ex);
                MessageBox.Show($"로그 내보내기 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 로그 지우기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("모든 로그를 지우시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.", 
                                           "로그 지우기 확인", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _loggingService.ClearLogs();
                    UpdateStatusText("로그가 모두 지워졌습니다");
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError("로그 지우기 중 오류 발생", ex);
            }
        }

        /// <summary>
        /// 음성 인식 UI 초기화
        /// </summary>
        private void InitializeVoiceRecognitionUI()
        {
            try
            {
                // 매칭 결과 DataGrid 초기화
                if (MatchedMacrosDataGrid != null)
                {
                    MatchedMacrosDataGrid.ItemsSource = _currentMatchResults;
                }
                
                // 상태 업데이트 타이머 설정 (1초마다)
                _statusUpdateTimer = new System.Windows.Threading.DispatcherTimer();
                _statusUpdateTimer.Interval = TimeSpan.FromSeconds(1);
                _statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
                _statusUpdateTimer.Start();
                
                // 초기 UI 상태 설정
                UpdateRecordingUI();
                
                // 마이크 장치 목록 로드 (서버 연결 확인 후 비동기적으로)
                Task.Run(async () => 
                {
                    try
                    {
                        // 서버 연결 대기 (최대 10초)
                        bool serverConnected = false;
                        for (int i = 0; i < 10; i++)
                        {
                            try
                            {
                                using var httpClient = new System.Net.Http.HttpClient();
                                var testResponse = await httpClient.GetAsync("http://localhost:5000/api/health");
                                if (testResponse.IsSuccessStatusCode)
                                {
                                    serverConnected = true;
                                    break;
                                }
                            }
                            catch
                            {
                                // 연결 실패, 1초 대기 후 재시도
                            }
                            
                            await Task.Delay(1000);
                        }
                        
                        if (serverConnected)
                        {
                            _loggingService.LogInfo("서버 연결 확인됨. 마이크 장치 목록 로드 시작...");
                            await LoadMicrophoneDevices();
                        }
                        else
                        {
                            _loggingService.LogError("서버에 연결할 수 없습니다. 마이크 목록을 로드할 수 없습니다.");
                            
                            // UI에 오류 메시지 표시
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (MicrophoneComboBox != null)
                                {
                                    MicrophoneComboBox.Items.Clear();
                                    var errorItem = new ComboBoxItem
                                    {
                                        Content = "서버 연결 실패 - API 서버 시작 필요",
                                        Tag = -1,
                                        IsEnabled = false
                                    };
                                    MicrophoneComboBox.Items.Add(errorItem);
                                    MicrophoneComboBox.SelectedIndex = 0;
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"마이크 장치 로드 중 오류: {ex.Message}");
                    }
                });
                
                _loggingService.LogInfo("음성 인식 UI 초기화 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 UI 초기화 중 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"음성 인식 UI 초기화 오류: {ex}");
            }
        }

        /// <summary>
        /// 마이크 장치 목록을 로드하여 ComboBox에 표시
        /// </summary>
        private async Task LoadMicrophoneDevices()
        {
            try
            {
                _loggingService.LogInfo("마이크 장치 목록 로드 시작...");
                System.Diagnostics.Debug.WriteLine("마이크 장치 목록 로드 시작...");
                
                var devices = await _voiceService.GetAvailableDevicesAsync();
                
                _loggingService.LogInfo($"API에서 {devices?.Count ?? 0}개의 마이크 장치를 반환했습니다.");
                System.Diagnostics.Debug.WriteLine($"API에서 {devices?.Count ?? 0}개의 마이크 장치를 반환했습니다.");
                
                // UI 스레드에서 ComboBox 업데이트
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (MicrophoneComboBox != null)
                        {
                            _loggingService.LogInfo("ComboBox 업데이트 시작...");
                            System.Diagnostics.Debug.WriteLine("ComboBox 업데이트 시작...");
                            
                            MicrophoneComboBox.Items.Clear();
                            
                            if (devices != null && devices.Count > 0)
                            {
                                foreach (var device in devices)
                                {
                                    var item = new ComboBoxItem
                                    {
                                        Content = device.Name,
                                        Tag = device.Id
                                    };
                                    MicrophoneComboBox.Items.Add(item);
                                    
                                    _loggingService.LogInfo($"마이크 추가: [{device.Id}] {device.Name}");
                                    System.Diagnostics.Debug.WriteLine($"마이크 추가: [{device.Id}] {device.Name}");
                                }
                                
                                // 첫 번째 장치를 기본으로 선택
                                MicrophoneComboBox.SelectedIndex = 0;
                                
                                _loggingService.LogInfo($"ComboBox에 {devices.Count}개 장치 추가 완료. 첫 번째 장치 선택됨.");
                                System.Diagnostics.Debug.WriteLine($"ComboBox에 {devices.Count}개 장치 추가 완료. 첫 번째 장치 선택됨.");
                            }
                            else
                            {
                                _loggingService.LogWarning("API에서 반환된 마이크 장치가 없습니다.");
                                System.Diagnostics.Debug.WriteLine("API에서 반환된 마이크 장치가 없습니다.");
                                
                                // 기본 메시지 추가
                                var defaultItem = new ComboBoxItem
                                {
                                    Content = "마이크 장치를 찾을 수 없습니다",
                                    Tag = -1,
                                    IsEnabled = false
                                };
                                MicrophoneComboBox.Items.Add(defaultItem);
                                MicrophoneComboBox.SelectedIndex = 0;
                            }
                        }
                        else
                        {
                            _loggingService.LogError("MicrophoneComboBox가 null입니다!");
                            System.Diagnostics.Debug.WriteLine("MicrophoneComboBox가 null입니다!");
                        }
                    }
                    catch (Exception uiEx)
                    {
                        _loggingService.LogError($"UI 업데이트 중 오류: {uiEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"UI 업데이트 중 오류: {uiEx}");
                    }
                });
                
                _loggingService.LogInfo($"마이크 장치 목록 로드 완료: {devices?.Count ?? 0}개 장치");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 장치 목록 로드 중 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"마이크 장치 로드 오류: {ex}");
                
                // 오류 발생 시에도 기본 메시지 표시
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (MicrophoneComboBox != null)
                        {
                            MicrophoneComboBox.Items.Clear();
                            var errorItem = new ComboBoxItem
                            {
                                Content = "마이크 로드 실패 - 서버 연결 확인",
                                Tag = -1,
                                IsEnabled = false
                            };
                            MicrophoneComboBox.Items.Add(errorItem);
                            MicrophoneComboBox.SelectedIndex = 0;
                        }
                    });
                }
                catch (Exception dispatcherEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Dispatcher 오류: {dispatcherEx}");
                }
            }
        }

        /// <summary>
        /// 상태 업데이트 타이머 이벤트 핸들러
        /// </summary>
        private async void StatusUpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var status = await _voiceService.GetRecordingStatusAsync();
                
                if (status != null)
                {
                    // 마이크 레벨 업데이트
                    if (MicLevelProgressBar != null)
                    {
                        MicLevelProgressBar.Value = status.AudioLevel;
                    }
                    
                    if (MicLevelTextBlock != null)
                    {
                        MicLevelTextBlock.Text = $"{status.AudioLevel * 100:F0}%";
                    }
                    
                    // 녹음 상태 업데이트
                    _isRecording = status.IsRecording;
                    UpdateRecordingUI();
                }
            }
            catch (Exception ex)
            {
                // 타이머에서는 에러를 조용히 처리
                System.Diagnostics.Debug.WriteLine($"상태 업데이트 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 녹음 상태에 따라 UI를 업데이트
        /// </summary>
        private void UpdateRecordingUI()
        {
            try
            {
                if (RecordingStatusIndicator != null)
                {
                    RecordingStatusIndicator.Fill = _isRecording ? 
                        new SolidColorBrush(Colors.Green) : 
                        new SolidColorBrush(Colors.Red);
                }
                
                if (RecordingStatusText != null)
                {
                    RecordingStatusText.Text = _isRecording ? "녹음 중" : "중지";
                }
                
                if (StartRecordingButton != null)
                {
                    StartRecordingButton.IsEnabled = !_isRecording;
                }
                
                if (StopRecordingButton != null)
                {
                    StopRecordingButton.IsEnabled = _isRecording;
                }
                
                // 초기 텍스트 설정
                if (!_isRecording && RecognizedTextBlock != null && string.IsNullOrEmpty(RecognizedTextBlock.Text))
                {
                    RecognizedTextBlock.Text = "음성 인식을 시작하세요...";
                    RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI 업데이트 오류: {ex.Message}");
            }
        }

        #region 음성 인식 이벤트 핸들러들

        /// <summary>
        /// 녹음 시작 버튼 클릭 이벤트 핸들러
        /// 녹음 시작 전에 자동으로 마이크 장치를 설정합니다.
        /// </summary>
        private async void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 먼저 마이크 장치가 설정되어 있는지 확인하고 자동 설정
                await EnsureMicrophoneDeviceSet();
                
                // 2. 녹음 시작
                var success = await _voiceService.StartRecordingAsync();
                
                if (success)
                {
                    _isRecording = true;
                    UpdateRecordingUI();
                    
                    if (RecognizedTextBlock != null)
                    {
                        RecognizedTextBlock.Text = "🎤 음성을 인식하고 있습니다...";
                        RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
                    }
                    
                    _loggingService.LogInfo("✅ 음성 녹음이 시작되었습니다.");
                    
                    // 2초 후 자동으로 음성 분석 시작
                    await Task.Delay(2000);
                    if (_isRecording)
                    {
                        await AnalyzeVoiceAndShowResults();
                    }
                }
                else
                {
                    _loggingService.LogWarning("❌ 음성 녹음 시작 실패");
                    MessageBox.Show("❌ 음성 녹음을 시작할 수 없습니다.\n\n" +
                                  "💡 해결 방법:\n" +
                                  "• 마이크 장치가 올바르게 설정되었는지 확인\n" +
                                  "• 다른 프로그램에서 마이크를 사용하고 있지 않은지 확인\n" +
                                  "• Windows 마이크 권한 설정 확인", 
                                  "녹음 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 녹음 시작 중 오류: {ex.Message}");
                MessageBox.Show($"❌ 녹음 시작 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 녹음 중지 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var success = await _voiceService.StopRecordingAsync();
                
                if (success)
                {
                    _isRecording = false;
                    UpdateRecordingUI();
                    
                    if (RecognizedTextBlock != null)
                    {
                        RecognizedTextBlock.Text = "음성 인식을 시작하세요...";
                        RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
                    }
                    
                    _loggingService.LogInfo("음성 녹음이 중지되었습니다.");
                }
                else
                {
                    MessageBox.Show("음성 녹음을 중지할 수 없습니다.", "오류", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"녹음 중지 중 오류: {ex.Message}");
                MessageBox.Show($"녹음 중지 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 마이크 선택 변경 이벤트 핸들러
        /// </summary>
        private async void MicrophoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 서비스들이 초기화된 경우에만 처리
                if (_voiceService != null && _loggingService != null && 
                    MicrophoneComboBox?.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is int deviceId)
                {
                    var success = await _voiceService.SetMicrophoneDeviceAsync(deviceId);
                    
                    if (success)
                    {
                        _loggingService.LogInfo($"마이크 장치 변경: {selectedItem.Content}");
                    }
                    else
                    {
                        _loggingService.LogWarning($"마이크 장치 변경 실패: {selectedItem.Content}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_loggingService != null)
                {
                    _loggingService.LogError($"마이크 장치 변경 중 오류: {ex.Message}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"마이크 장치 변경 중 오류 (초기화 전): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 감도 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                // UI 업데이트
                if (SensitivityValueText != null)
                {
                    SensitivityValueText.Text = $"{e.NewValue:F1}x";
                }
                
                // 로깅 서비스가 초기화된 경우에만 로그 기록
                if (_loggingService != null)
                {
                    _loggingService.LogDebug($"마이크 감도 변경: {e.NewValue:F1}x");
                }
            }
            catch (Exception ex)
            {
                // 로깅 서비스가 초기화된 경우에만 오류 로그 기록
                if (_loggingService != null)
                {
                    _loggingService.LogError($"감도 설정 중 오류: {ex.Message}");
                }
                // 초기화되지 않은 경우 콘솔 출력
                else
                {
                    System.Diagnostics.Debug.WriteLine($"감도 설정 중 오류 (초기화 전): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 언어 선택 변경 이벤트 핸들러
        /// </summary>
        private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 서비스들이 초기화된 경우에만 처리
                if (_voiceService != null && _loggingService != null && 
                    LanguageComboBox?.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string language)
                {
                    var success = await _voiceService.SetLanguageAsync(language);
                    
                    if (success)
                    {
                        _loggingService.LogInfo($"언어 설정 변경: {language}");
                    }
                    else
                    {
                        _loggingService.LogWarning($"언어 설정 변경 실패: {language}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_loggingService != null)
                {
                    _loggingService.LogError($"언어 설정 변경 중 오류: {ex.Message}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"언어 설정 변경 중 오류 (초기화 전): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 마이크 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void TestMicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TestMicrophoneButton != null)
                {
                    TestMicrophoneButton.IsEnabled = false;
                    TestMicrophoneButton.Content = "🔄 테스트 중...";
                }
                
                // 1. 먼저 마이크 장치가 설정되어 있는지 확인하고 자동 설정
                await EnsureMicrophoneDeviceSet();
                
                // 2. 마이크 테스트 수행
                var result = await _voiceService.TestMicrophoneAsync();
                
                if (result != null)
                {
                    string message;
                    if (result.Success)
                    {
                        message = "✅ 마이크 테스트 성공!\n\n" +
                                $"• 장치 사용 가능: {(result.DeviceAvailable ? "✅" : "❌")}\n" +
                                $"• 녹음 테스트: {(result.RecordingTest ? "✅" : "❌")}\n" +
                                $"• 오디오 레벨 감지: {(result.AudioLevelDetected ? "✅" : "❌")}\n" +
                                $"• 모드: {result.Mode}";
                        
                        MessageBox.Show(message, "마이크 테스트 완료", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        _loggingService.LogInfo("✅ 마이크 테스트 성공: 모든 항목 통과");
                    }
                    else
                    {
                        message = $"❌ 마이크 테스트 실패\n\n" +
                                $"오류: {result.ErrorMessage}\n\n" +
                                $"💡 해결 방법:\n" +
                                $"• 마이크가 다른 프로그램에서 사용 중인지 확인\n" +
                                $"• Windows 마이크 권한 설정 확인\n" +
                                $"• 마이크 드라이버 업데이트 시도";
                        
                        MessageBox.Show(message, "마이크 테스트 실패", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        
                        _loggingService.LogWarning($"❌ 마이크 테스트 실패: {result.ErrorMessage}");
                    }
                }
                else
                {
                    MessageBox.Show("❌ 마이크 테스트를 수행할 수 없습니다.\n\n" +
                                  "서버 연결을 확인해주세요.", 
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    _loggingService.LogError("마이크 테스트 실패: 응답이 null");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 테스트 중 오류: {ex.Message}");
                MessageBox.Show($"❌ 마이크 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (TestMicrophoneButton != null)
                {
                    TestMicrophoneButton.IsEnabled = true;
                    TestMicrophoneButton.Content = "🧪 마이크 테스트";
                }
            }
        }

        /// <summary>
        /// 매칭된 매크로 DataGrid 더블클릭 이벤트 핸들러
        /// </summary>
        private async void MatchedMacrosDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (MatchedMacrosDataGrid?.SelectedItem is VoiceMatchResult selectedMatch)
                {
                    await ExecuteMacroById(selectedMatch.MacroId);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 실행 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 실행 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ExecuteMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int macroId)
                {
                    await ExecuteMacroById(macroId);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 실행 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 음성을 분석하고 매크로 매칭 결과를 표시 (OpenAI Whisper 사용)
        /// </summary>
        private async Task AnalyzeVoiceAndShowResults()
        {
            try
            {
                if (RecognizedTextBlock != null)
                {
                    RecognizedTextBlock.Text = "🤖 Whisper AI로 음성을 분석 중...";
                    RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                }
                
                _loggingService.LogInfo("Whisper AI 음성 분석 시작");
                
                // OpenAI Whisper API를 사용하여 음성 명령 처리
                var result = await _apiService.RecordAndProcessVoiceAsync(3.0); // 3초간 녹음 후 처리
                
                if (result != null && !string.IsNullOrEmpty(result.RecognizedText))
                {
                    // 인식된 텍스트 표시
                    if (RecognizedTextBlock != null)
                    {
                        RecognizedTextBlock.Text = $"🎯 인식됨: \"{result.RecognizedText}\"";
                        RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    
                    // 매칭된 매크로가 있는 경우
                    if (result.MatchedMacros != null && result.MatchedMacros.Count > 0)
                    {
                                                 // MacroMatch를 VoiceMatchResult로 변환
                         _currentMatchResults.Clear();
                         
                         for (int i = 0; i < result.MatchedMacros.Count; i++)
                         {
                             var macro = result.MatchedMacros[i];
                             var matchResult = new VoiceMatchResult
                             {
                                 Rank = i + 1,
                                 MacroId = macro.MacroId,
                                 MacroName = macro.MacroName,
                                 VoiceCommand = macro.VoiceCommand,
                                 ActionDescription = macro.ActionDescription,
                                 Confidence = macro.Confidence / 100.0 // 백분율을 0.0-1.0 범위로 변환
                             };
                             _currentMatchResults.Add(matchResult);
                         }
                        
                        // DataGrid 새로고침
                        if (MatchedMacrosDataGrid != null)
                        {
                            MatchedMacrosDataGrid.Items.Refresh();
                        }
                        
                        _loggingService.LogInfo($"✅ Whisper 음성 인식 성공! 텍스트: '{result.RecognizedText}', " +
                                             $"매칭된 매크로: {result.MatchedMacros.Count}개, " +
                                             $"처리 시간: {result.ProcessingTime:F2}초");
                        
                        // 가장 높은 확신도의 매크로가 90% 이상이면 자동 실행 옵션 제공
                        if (result.MatchedMacros[0].Confidence >= 90)
                        {
                            var topMacro = result.MatchedMacros[0];
                            var autoExecuteResult = MessageBox.Show(
                                $"높은 확신도({topMacro.Confidence:F1}%)로 매크로를 찾았습니다!\n\n" +
                                $"매크로: {topMacro.MacroName}\n" +
                                $"명령어: {topMacro.VoiceCommand}\n" +
                                $"동작: {topMacro.ActionDescription}\n\n" +
                                $"지금 실행하시겠습니까?",
                                "자동 실행 확인", 
                                MessageBoxButton.YesNo, 
                                MessageBoxImage.Question);
                            
                            if (autoExecuteResult == MessageBoxResult.Yes)
                            {
                                await ExecuteMacroById(topMacro.MacroId);
                            }
                        }
                    }
                    else
                    {
                        // 음성은 인식되었지만 매칭되는 매크로가 없음
                        _currentMatchResults.Clear();
                        if (MatchedMacrosDataGrid != null)
                        {
                            MatchedMacrosDataGrid.Items.Refresh();
                        }
                        
                        _loggingService.LogWarning($"⚠️ 음성 인식 성공하였으나 매칭되는 매크로가 없습니다. " +
                                                 $"인식된 텍스트: '{result.RecognizedText}'");
                        
                        MessageBox.Show(
                            $"음성이 \"{result.RecognizedText}\"로 인식되었지만,\n" +
                            $"매칭되는 매크로가 없습니다.\n\n" +
                            $"매크로 관리 탭에서 이 명령어로 새 매크로를 생성해보세요.",
                            "매크로 없음", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    // 음성 인식 실패
                    if (RecognizedTextBlock != null)
                    {
                        RecognizedTextBlock.Text = "❌ 음성을 인식할 수 없습니다";
                        RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    
                    _currentMatchResults.Clear();
                    if (MatchedMacrosDataGrid != null)
                    {
                        MatchedMacrosDataGrid.Items.Refresh();
                    }
                    
                    _loggingService.LogWarning("❌ Whisper 음성 인식 실패 - 명확한 음성이나 소음이 없음");
                    
                    MessageBox.Show(
                        "음성을 인식할 수 없습니다.\n\n" +
                        "다음을 확인해주세요:\n" +
                        "• 마이크가 올바르게 연결되어 있는지\n" +
                        "• 주변 소음이 너무 크지 않은지\n" +
                        "• 명령어를 명확하게 발음했는지\n" +
                        "• OpenAI API 키가 설정되어 있는지",
                        "음성 인식 실패", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                if (RecognizedTextBlock != null)
                {
                    RecognizedTextBlock.Text = "💥 음성 분석 오류 발생";
                    RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                
                _currentMatchResults.Clear();
                if (MatchedMacrosDataGrid != null)
                {
                    MatchedMacrosDataGrid.Items.Refresh();
                }
                
                _loggingService.LogError($"💥 Whisper 음성 분석 중 오류: {ex.Message}");
                
                MessageBox.Show(
                    $"음성 분석 중 오류가 발생했습니다:\n\n{ex.Message}\n\n" +
                    $"다음을 확인해주세요:\n" +
                    $"• 백엔드 서버가 실행 중인지 (Python API 서버)\n" +
                    $"• OpenAI API 키가 올바르게 설정되어 있는지\n" +
                    $"• 인터넷 연결이 정상인지",
                    "오류", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 매크로 ID로 매크로를 실행
        /// </summary>
        private async Task ExecuteMacroById(int macroId)
        {
            try
            {
                var success = await _apiService.ExecuteMacroAsync(macroId);
                
                if (success)
                {
                    _loggingService.LogInfo($"매크로 실행 성공: ID {macroId}");
                    UpdateStatusText($"매크로 ID {macroId} 실행 완료");
                }
                else
                {
                    _loggingService.LogWarning($"매크로 실행 실패: ID {macroId}");
                    MessageBox.Show($"매크로 실행에 실패했습니다. (ID: {macroId})", 
                                  "실행 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 실행 중 오류: {ex.Message}");
                MessageBox.Show($"매크로 실행 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        /// <summary>
        /// 윈도우가 닫힐 때 리소스를 정리하는 함수
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _loggingService.LogInfo("애플리케이션이 종료됩니다.");
                
                // 타이머 정리
                _statusUpdateTimer?.Stop();
                _statusUpdateTimer = null;
                
                // 리소스 정리
                _logViewSource = null;
                _voiceService?.Dispose();
                _apiService?.Dispose();
                
                base.OnClosed(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"윈도우 종료 중 오류: {ex}");
            }
        }

        /// <summary>
        /// 마이크 목록 수동 새로고침 (디버깅용)
        /// </summary>
        public async Task RefreshMicrophoneDevicesManually()
        {
            try
            {
                _loggingService.LogInfo("🔄 마이크 목록 수동 새로고침 시작...");
                await LoadMicrophoneDevices();
                _loggingService.LogInfo("✅ 마이크 목록 수동 새로고침 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 마이크 목록 수동 새로고침 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 마이크 새로고침 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void RefreshMicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("🔄 사용자가 마이크 목록 새로고침을 요청했습니다.");
                await RefreshMicrophoneDevicesManually();
                
                MessageBox.Show("마이크 목록이 새로고침되었습니다.\n로그 탭에서 자세한 정보를 확인하세요.", 
                              "정보", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 새로고침 버튼 오류: {ex.Message}");
                MessageBox.Show($"마이크 목록 새로고침 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 마이크 장치가 설정되어 있는지 확인하고 자동으로 설정하는 함수
        /// </summary>
        private async Task EnsureMicrophoneDeviceSet()
        {
            try
            {
                _loggingService.LogInfo("🔍 마이크 장치 설정 상태 확인 중...");
                
                // 현재 음성 상태 확인
                var status = await _voiceService.GetRecordingStatusAsync();
                
                if (status == null || status.CurrentDevice < 0)
                {
                    _loggingService.LogWarning("⚠️ 마이크 장치가 설정되지 않음. 자동 설정 시도...");
                    
                    // 사용 가능한 마이크 장치 목록 가져오기
                    var devices = await _voiceService.GetAvailableDevicesAsync();
                    
                    if (devices != null && devices.Count > 0)
                    {
                        // GM50U 마이크를 우선적으로 찾기
                        var preferredDevice = devices.FirstOrDefault(d => d.Name.Contains("GM50U"));
                        
                        // GM50U가 없으면 첫 번째 사용 가능한 장치 선택
                        if (preferredDevice == null)
                        {
                            preferredDevice = devices.FirstOrDefault(d => d.Id > 0); // Microsoft 사운드 매퍼 제외
                        }
                        
                        if (preferredDevice != null)
                        {
                            var success = await _voiceService.SetMicrophoneDeviceAsync(preferredDevice.Id);
                            
                            if (success)
                            {
                                _loggingService.LogInfo($"✅ 마이크 장치 자동 설정 성공: [{preferredDevice.Id}] {preferredDevice.Name}");
                                
                                // UI 업데이트
                                if (MicrophoneComboBox != null)
                                {
                                    MicrophoneComboBox.SelectedValue = preferredDevice.Id;
                                }
                            }
                            else
                            {
                                _loggingService.LogWarning($"❌ 마이크 장치 설정 실패: [{preferredDevice.Id}] {preferredDevice.Name}");
                            }
                        }
                        else
                        {
                            _loggingService.LogWarning("⚠️ 설정할 수 있는 마이크 장치를 찾을 수 없음");
                        }
                    }
                    else
                    {
                        _loggingService.LogError("❌ 사용 가능한 마이크 장치가 없음");
                    }
                }
                else
                {
                    _loggingService.LogInfo($"✅ 마이크 장치가 이미 설정됨: 장치 ID {status.CurrentDevice}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"❌ 마이크 장치 설정 확인 중 오류: {ex.Message}");
            }
        }

        // ==================== 매크로 설정 이벤트 핸들러 ====================

        /// <summary>
        /// 매크로 타입 라디오 버튼 선택 시 호출되는 이벤트 핸들러
        /// 선택된 타입에 따라 해당 설정 패널을 표시합니다.
        /// </summary>
        private void MacroTypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is RadioButton radioButton && radioButton.Tag is string macroTypeStr)
                {
                    if (Enum.TryParse<MacroActionType>(macroTypeStr, out var macroType))
                    {
                        _currentMacroType = macroType;
                        ShowMacroSettingsPanel(macroType);
                        _loggingService.LogInfo($"매크로 타입이 '{macroType}'으로 변경되었습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 타입 선택 중 오류 발생: {ex.Message}");
                MessageBox.Show($"매크로 타입 선택 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 선택된 매크로 타입에 따라 해당 설정 패널을 표시하는 함수
        /// </summary>
        private void ShowMacroSettingsPanel(MacroActionType macroType)
        {
            try
            {
                // 모든 패널 숨기기
                if (ComboSettingsPanel != null) ComboSettingsPanel.Visibility = Visibility.Collapsed;
                if (RapidSettingsPanel != null) RapidSettingsPanel.Visibility = Visibility.Collapsed;
                if (HoldSettingsPanel != null) HoldSettingsPanel.Visibility = Visibility.Collapsed;
                if (ToggleSettingsPanel != null) ToggleSettingsPanel.Visibility = Visibility.Collapsed;
                if (RepeatSettingsPanel != null) RepeatSettingsPanel.Visibility = Visibility.Collapsed;

                // 선택된 타입에 따라 해당 패널 표시
                switch (macroType)
                {
                    case MacroActionType.Combo:
                        if (ComboSettingsPanel != null) ComboSettingsPanel.Visibility = Visibility.Visible;
                        InitializeComboSettings();
                        break;
                    case MacroActionType.Rapid:
                        if (RapidSettingsPanel != null) RapidSettingsPanel.Visibility = Visibility.Visible;
                        break;
                    case MacroActionType.Hold:
                        if (HoldSettingsPanel != null) HoldSettingsPanel.Visibility = Visibility.Visible;
                        break;
                    case MacroActionType.Toggle:
                        if (ToggleSettingsPanel != null) ToggleSettingsPanel.Visibility = Visibility.Visible;
                        break;
                    case MacroActionType.Repeat:
                        if (RepeatSettingsPanel != null) RepeatSettingsPanel.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 설정 패널 표시 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 콤보 설정을 초기화하는 함수
        /// </summary>
        private void InitializeComboSettings()
        {
            try
            {
                if (ComboStepsDataGrid != null)
                {
                    ComboStepsDataGrid.ItemsSource = _comboSteps;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"콤보 설정 초기화 중 오류 발생: {ex.Message}");
            }
        }

        // ==================== 콤보 설정 이벤트 핸들러 ====================

        /// <summary>
        /// 콤보 기본 딜레이 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void ComboDefaultDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var delayMs = (int)e.NewValue;
                _comboSettings.DefaultDelayMs = delayMs;
                
                if (ComboDefaultDelayText != null)
                {
                    ComboDefaultDelayText.Text = $"{delayMs}ms";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"콤보 딜레이 설정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 콤보 단계 추가 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void AddComboStepButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newStep = new ComboStep
                {
                    KeySequence = "키를 입력하세요",
                    DelayAfterMs = _comboSettings.DefaultDelayMs,
                    Description = "단계 설명"
                };
                
                _comboSteps.Add(newStep);
                _loggingService.LogInfo($"새 콤보 단계가 추가되었습니다. 총 {_comboSteps.Count}개 단계");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"콤보 단계 추가 중 오류 발생: {ex.Message}");
                MessageBox.Show($"콤보 단계 추가 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 콤보 단계 삭제 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void RemoveComboStepButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ComboStepsDataGrid?.SelectedItem is ComboStep selectedStep)
                {
                    _comboSteps.Remove(selectedStep);
                    _loggingService.LogInfo($"콤보 단계가 삭제되었습니다. 총 {_comboSteps.Count}개 단계");
                }
                else
                {
                    MessageBox.Show("삭제할 단계를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"콤보 단계 삭제 중 오류 발생: {ex.Message}");
                MessageBox.Show($"콤보 단계 삭제 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 콤보 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void TestComboButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_comboSteps.Count == 0)
                {
                    MessageBox.Show("테스트할 콤보 단계가 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show("콤보를 테스트하시겠습니까?\n3초 후 콤보가 실행됩니다.", 
                                           "콤보 테스트", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _loggingService.LogInfo("콤보 테스트 시작");
                    
                    // 3초 대기
                    await Task.Delay(3000);
                    
                    // TODO: 실제 콤보 실행 로직 구현 (PyAutoGUI 호출)
                    foreach (var step in _comboSteps)
                    {
                        _loggingService.LogInfo($"콤보 단계 실행: {step.KeySequence}");
                        // 실제 키 입력 실행
                        await Task.Delay(step.DelayAfterMs);
                    }
                    
                    _loggingService.LogInfo("콤보 테스트 완료");
                    MessageBox.Show("콤보 테스트가 완료되었습니다.", "테스트 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"콤보 테스트 중 오류 발생: {ex.Message}");
                MessageBox.Show($"콤보 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 연사 설정 이벤트 핸들러 ====================

        /// <summary>
        /// 연사 속도 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RapidCpsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var cps = Math.Round(e.NewValue, 1);
                _rapidSettings.ClicksPerSecond = cps;
                
                if (RapidCpsText != null)
                {
                    RapidCpsText.Text = $"{cps} CPS";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"연사 속도 설정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 연사 지속시간 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RapidDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var duration = Math.Round(e.NewValue, 1);
                _rapidSettings.DurationSeconds = duration;
                
                if (RapidDurationText != null)
                {
                    RapidDurationText.Text = $"{duration}초";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"연사 지속시간 설정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 연사 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void TestRapidButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(RapidKeySequenceTextBox?.Text))
                {
                    MessageBox.Show("연사할 키를 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"연사를 테스트하시겠습니까?\n" +
                                           $"키: {RapidKeySequenceTextBox.Text}\n" +
                                           $"속도: {_rapidSettings.ClicksPerSecond} CPS\n" +
                                           $"지속시간: {_rapidSettings.DurationSeconds}초", 
                                           "연사 테스트", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _loggingService.LogInfo($"연사 테스트 시작: {RapidKeySequenceTextBox.Text}");
                    
                    // 3초 대기
                    await Task.Delay(3000);
                    
                    // TODO: 실제 연사 실행 로직 구현
                    _loggingService.LogInfo("연사 테스트 완료");
                    MessageBox.Show("연사 테스트가 완료되었습니다.", "테스트 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"연사 테스트 중 오류 발생: {ex.Message}");
                MessageBox.Show($"연사 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 홀드 설정 이벤트 핸들러 ====================

        /// <summary>
        /// 홀드 지속시간 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void HoldDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var duration = Math.Round(e.NewValue, 1);
                _holdSettings.HoldDurationSeconds = duration;
                
                if (HoldDurationText != null)
                {
                    HoldDurationText.Text = $"{duration}초";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"홀드 지속시간 설정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 홀드 고정시간 체크박스 체크 이벤트 핸들러
        /// </summary>
        private void HoldUseFixedDurationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                _holdSettings.UseFixedDuration = true;
                if (HoldReleaseCommandPanel != null)
                {
                    HoldReleaseCommandPanel.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"홀드 고정시간 설정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 홀드 고정시간 체크박스 언체크 이벤트 핸들러
        /// </summary>
        private void HoldUseFixedDurationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                _holdSettings.UseFixedDuration = false;
                if (HoldReleaseCommandPanel != null)
                {
                    HoldReleaseCommandPanel.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"홀드 고정시간 해제 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 홀드 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void TestHoldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(HoldKeySequenceTextBox?.Text))
                {
                    MessageBox.Show("홀드할 키를 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"홀드를 테스트하시겠습니까?\n" +
                                           $"키: {HoldKeySequenceTextBox.Text}\n" +
                                           $"지속시간: {_holdSettings.HoldDurationSeconds}초", 
                                           "홀드 테스트", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _loggingService.LogInfo($"홀드 테스트 시작: {HoldKeySequenceTextBox.Text}");
                    
                    // 3초 대기
                    await Task.Delay(3000);
                    
                    // TODO: 실제 홀드 실행 로직 구현
                    _loggingService.LogInfo("홀드 테스트 완료");
                    MessageBox.Show("홀드 테스트가 완료되었습니다.", "테스트 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"홀드 테스트 중 오류 발생: {ex.Message}");
                MessageBox.Show($"홀드 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 토글 설정 이벤트 핸들러 ====================

        /// <summary>
        /// 토글 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void TestToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ToggleKeySequenceTextBox?.Text))
                {
                    MessageBox.Show("토글할 키를 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 토글 상태 변경
                _toggleSettings.IsCurrentlyOn = !_toggleSettings.IsCurrentlyOn;
                
                // UI 업데이트
                UpdateToggleStatusUI();
                
                _loggingService.LogInfo($"토글 테스트: {ToggleKeySequenceTextBox.Text} - 상태: {(_toggleSettings.IsCurrentlyOn ? "ON" : "OFF")}");
                
                // TODO: 실제 토글 실행 로직 구현
                MessageBox.Show($"토글 상태가 {(_toggleSettings.IsCurrentlyOn ? "ON" : "OFF")}으로 변경되었습니다.", 
                              "토글 테스트", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"토글 테스트 중 오류 발생: {ex.Message}");
                MessageBox.Show($"토글 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 토글 상태 UI를 업데이트하는 함수
        /// </summary>
        private void UpdateToggleStatusUI()
        {
            try
            {
                if (ToggleStatusIndicator != null && ToggleStatusText != null)
                {
                    if (_toggleSettings.IsCurrentlyOn)
                    {
                        ToggleStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                        ToggleStatusText.Text = "ON";
                    }
                    else
                    {
                        ToggleStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                        ToggleStatusText.Text = "OFF";
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"토글 상태 UI 업데이트 중 오류 발생: {ex.Message}");
            }
        }

        // ==================== 반복 설정 이벤트 핸들러 ====================

        /// <summary>
        /// 반복 횟수 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RepeatCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var count = (int)e.NewValue;
                _repeatSettings.RepeatCount = count;
                
                if (RepeatCountText != null)
                {
                    RepeatCountText.Text = $"{count}회";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"반복 횟수 설정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 반복 간격 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RepeatIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var interval = Math.Round(e.NewValue, 1);
                _repeatSettings.IntervalSeconds = interval;
                
                if (RepeatIntervalText != null)
                {
                    RepeatIntervalText.Text = $"{interval}초";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"반복 간격 설정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 반복 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void TestRepeatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(RepeatKeySequenceTextBox?.Text))
                {
                    MessageBox.Show("반복할 키를 입력해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"반복을 테스트하시겠습니까?\n" +
                                           $"키: {RepeatKeySequenceTextBox.Text}\n" +
                                           $"횟수: {_repeatSettings.RepeatCount}회\n" +
                                           $"간격: {_repeatSettings.IntervalSeconds}초", 
                                           "반복 테스트", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _loggingService.LogInfo($"반복 테스트 시작: {RepeatKeySequenceTextBox.Text}");
                    
                    // 3초 대기
                    await Task.Delay(3000);
                    
                    // TODO: 실제 반복 실행 로직 구현
                    for (int i = 0; i < _repeatSettings.RepeatCount; i++)
                    {
                        _loggingService.LogInfo($"반복 실행 {i + 1}/{_repeatSettings.RepeatCount}");
                        await Task.Delay((int)(_repeatSettings.IntervalSeconds * 1000));
                    }
                    
                    _loggingService.LogInfo("반복 테스트 완료");
                    MessageBox.Show("반복 테스트가 완료되었습니다.", "테스트 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"반복 테스트 중 오류 발생: {ex.Message}");
                MessageBox.Show($"반복 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 매크로 저장/취소/미리보기 ====================

        /// <summary>
        /// 매크로 저장 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void SaveMacroSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 기본 정보 검증
                if (string.IsNullOrWhiteSpace(MacroNameTextBox?.Text))
                {
                    MessageBox.Show("매크로 이름을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MacroNameTextBox?.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(VoiceCommandTextBox?.Text))
                {
                    MessageBox.Show("음성 명령어를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    VoiceCommandTextBox?.Focus();
                    return;
                }

                // 현재 설정 유효성 검증
                IMacroActionSettings currentSettings = GetCurrentMacroSettings();
                if (!currentSettings.IsValid(out string errorMessage))
                {
                    MessageBox.Show($"매크로 설정이 올바르지 않습니다:\n{errorMessage}", 
                                  "설정 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 새 매크로 객체 생성
                var newMacro = new Macro
                {
                    Name = MacroNameTextBox.Text.Trim(),
                    VoiceCommand = VoiceCommandTextBox.Text.Trim(),
                    ActionType = _currentMacroType.ToString().ToLower(),
                    KeySequence = GetKeySequenceFromCurrentSettings(),
                    Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        currentSettings.ToJsonString()) ?? new Dictionary<string, object>(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UsageCount = 0
                };

                // API를 통해 매크로 저장
                _loggingService.LogInfo($"새 매크로 저장 시도: {newMacro.Name}");
                int macroId = await _apiService.CreateMacroAsync(newMacro);
                bool success = macroId > 0;

                if (success)
                {
                    _loggingService.LogInfo($"매크로 저장 성공: {newMacro.Name}");
                    MessageBox.Show("매크로가 성공적으로 저장되었습니다!", "저장 완료", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // 매크로 목록 새로고침
                    await LoadMacros();
                    
                    // 입력 필드 초기화
                    ClearMacroSettings();
                }
                else
                {
                    _loggingService.LogError($"매크로 저장 실패: {newMacro.Name}");
                    MessageBox.Show("매크로 저장에 실패했습니다. 다시 시도해주세요.", "저장 실패", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 저장 중 오류 발생: {ex.Message}");
                MessageBox.Show($"매크로 저장 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 매크로 설정 취소 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void CancelMacroSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("변경사항을 취소하시겠습니까?", "취소 확인", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    ClearMacroSettings();
                    _loggingService.LogInfo("매크로 설정이 취소되었습니다.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 설정 취소 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 미리보기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void PreviewMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IMacroActionSettings currentSettings = GetCurrentMacroSettings();
                string previewText = GenerateMacroPreviewText(currentSettings);
                
                MessageBox.Show(previewText, "매크로 미리보기", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 미리보기 중 오류 발생: {ex.Message}");
                MessageBox.Show($"매크로 미리보기 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 헬퍼 함수들 ====================

        /// <summary>
        /// 현재 선택된 매크로 타입의 설정 객체를 반환하는 함수
        /// </summary>
        private IMacroActionSettings GetCurrentMacroSettings()
        {
            return _currentMacroType switch
            {
                MacroActionType.Combo => _comboSettings,
                MacroActionType.Rapid => _rapidSettings,
                MacroActionType.Hold => _holdSettings,
                MacroActionType.Toggle => _toggleSettings,
                MacroActionType.Repeat => _repeatSettings,
                _ => _comboSettings
            };
        }

        /// <summary>
        /// 현재 설정에서 키 시퀀스 문자열을 생성하는 함수
        /// </summary>
        private string GetKeySequenceFromCurrentSettings()
        {
            return _currentMacroType switch
            {
                MacroActionType.Combo => string.Join(" -> ", _comboSteps.Select(s => s.KeySequence)),
                MacroActionType.Rapid => RapidKeySequenceTextBox?.Text ?? "",
                MacroActionType.Hold => HoldKeySequenceTextBox?.Text ?? "",
                MacroActionType.Toggle => ToggleKeySequenceTextBox?.Text ?? "",
                MacroActionType.Repeat => RepeatKeySequenceTextBox?.Text ?? "",
                _ => ""
            };
        }

        /// <summary>
        /// 매크로 설정을 초기화하는 함수
        /// </summary>
        private void ClearMacroSettings()
        {
            try
            {
                // 기본 정보 초기화
                if (MacroNameTextBox != null) MacroNameTextBox.Text = "";
                if (VoiceCommandTextBox != null) VoiceCommandTextBox.Text = "";
                if (MacroDescriptionTextBox != null) MacroDescriptionTextBox.Text = "";

                // 콤보 설정 초기화
                _comboSteps.Clear();
                _comboSettings = new ComboActionSettings();

                // 연사 설정 초기화
                _rapidSettings = new RapidActionSettings();
                if (RapidKeySequenceTextBox != null) RapidKeySequenceTextBox.Text = "";

                // 홀드 설정 초기화
                _holdSettings = new HoldActionSettings();
                if (HoldKeySequenceTextBox != null) HoldKeySequenceTextBox.Text = "";
                if (HoldReleaseCommandTextBox != null) HoldReleaseCommandTextBox.Text = "";

                // 토글 설정 초기화
                _toggleSettings = new ToggleActionSettings();
                if (ToggleKeySequenceTextBox != null) ToggleKeySequenceTextBox.Text = "";
                if (ToggleOffCommandTextBox != null) ToggleOffCommandTextBox.Text = "";
                UpdateToggleStatusUI();

                // 반복 설정 초기화
                _repeatSettings = new RepeatActionSettings();
                if (RepeatKeySequenceTextBox != null) RepeatKeySequenceTextBox.Text = "";

                // 콤보 타입으로 초기화
                if (ComboRadioButton != null) ComboRadioButton.IsChecked = true;
                _currentMacroType = MacroActionType.Combo;
                ShowMacroSettingsPanel(MacroActionType.Combo);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 설정 초기화 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 미리보기 텍스트를 생성하는 함수
        /// </summary>
        private string GenerateMacroPreviewText(IMacroActionSettings settings)
        {
            var preview = $"매크로 이름: {MacroNameTextBox?.Text ?? "미입력"}\n";
            preview += $"음성 명령어: {VoiceCommandTextBox?.Text ?? "미입력"}\n";
            preview += $"매크로 타입: {_currentMacroType}\n\n";

            switch (_currentMacroType)
            {
                case MacroActionType.Combo:
                    preview += "=== 콤보 설정 ===\n";
                    preview += $"기본 딜레이: {_comboSettings.DefaultDelayMs}ms\n";
                    preview += $"단계 수: {_comboSteps.Count}개\n";
                    for (int i = 0; i < _comboSteps.Count; i++)
                    {
                        var step = _comboSteps[i];
                        preview += $"  {i + 1}. {step.KeySequence} (딜레이: {step.DelayAfterMs}ms)\n";
                    }
                    break;

                case MacroActionType.Rapid:
                    preview += "=== 연사 설정 ===\n";
                    preview += $"키: {RapidKeySequenceTextBox?.Text ?? "미입력"}\n";
                    preview += $"속도: {_rapidSettings.ClicksPerSecond} CPS\n";
                    preview += $"지속시간: {_rapidSettings.DurationSeconds}초\n";
                    break;

                case MacroActionType.Hold:
                    preview += "=== 홀드 설정 ===\n";
                    preview += $"키: {HoldKeySequenceTextBox?.Text ?? "미입력"}\n";
                    preview += $"지속시간: {_holdSettings.HoldDurationSeconds}초\n";
                    preview += $"고정 지속시간: {(_holdSettings.UseFixedDuration ? "예" : "아니오")}\n";
                    if (!_holdSettings.UseFixedDuration)
                    {
                        preview += $"해제 명령어: {HoldReleaseCommandTextBox?.Text ?? "미입력"}\n";
                    }
                    break;

                case MacroActionType.Toggle:
                    preview += "=== 토글 설정 ===\n";
                    preview += $"키: {ToggleKeySequenceTextBox?.Text ?? "미입력"}\n";
                    preview += $"해제 명령어: {ToggleOffCommandTextBox?.Text ?? "동일 명령어"}\n";
                    preview += $"상태 표시: {(_toggleSettings.ShowStatusIndicator ? "사용" : "사용 안함")}\n";
                    break;

                case MacroActionType.Repeat:
                    preview += "=== 반복 설정 ===\n";
                    preview += $"키: {RepeatKeySequenceTextBox?.Text ?? "미입력"}\n";
                    preview += $"횟수: {_repeatSettings.RepeatCount}회\n";
                    preview += $"간격: {_repeatSettings.IntervalSeconds}초\n";
                    preview += $"다음 명령 시 중단: {(_repeatSettings.StopOnNextCommand ? "예" : "아니오")}\n";
                    break;
            }

            return preview;
        }

        // ==================== 프리셋 관리 관련 필드 및 메서드 ====================
        private List<PresetModel> _allPresets = new List<PresetModel>();
        private PresetModel? _selectedPreset = null;
        private string _currentPresetSearchTerm = string.Empty;
        private bool _favoritesOnly = false;

        /// <summary>
        /// 프리셋 목록을 서버에서 불러와 DataGrid에 표시하는 함수
        /// </summary>
        private async Task LoadPresets()
        {
            try
            {
                UpdateStatusText("프리셋 목록 로딩 중...");

                // API를 통해 프리셋 목록 조회
                _allPresets = await _apiService.GetPresetsAsync(_currentPresetSearchTerm, _favoritesOnly);

                // DataGrid에 바인딩
                if (PresetDataGrid != null)
                {
                    PresetDataGrid.ItemsSource = _allPresets;
                }

                // 통계 정보 업데이트
                await UpdatePresetStatistics();

                _loggingService.LogInfo($"프리셋 목록 로드 완료: {_allPresets.Count}개 항목");
                UpdateStatusText($"프리셋 {_allPresets.Count}개 로드 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프리셋 목록을 불러오는 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("프리셋 로드 실패");
                _loggingService.LogError("프리셋 목록 로드 실패", ex);
            }
        }

        /// <summary>
        /// 프리셋 통계 정보를 업데이트하는 함수
        /// </summary>
        private async Task UpdatePresetStatistics()
        {
            try
            {
                var stats = await _apiService.GetPresetStatisticsAsync();
                if (stats != null)
                {
                    // UI 요소들 업데이트
                    if (TotalPresetsText != null)
                        TotalPresetsText.Text = $"{stats.TotalPresets}개";

                    if (FavoritePresetsText != null)
                        FavoritePresetsText.Text = $"{stats.FavoritePresets}개";

                    if (FavoritePercentageText != null)
                        FavoritePercentageText.Text = stats.FavoritePercentageText;

                    if (RecentPresetText != null)
                    {
                        RecentPresetText.Text = stats.MostRecentPreset?.Name ?? "없음";
                    }

                    if (PresetCountTextBlock != null)
                        PresetCountTextBlock.Text = $"총 {stats.TotalPresets}개 프리셋";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("프리셋 통계 업데이트 실패", ex);
            }
        }

        /// <summary>
        /// 프리셋 검색 텍스트박스 변경 이벤트 핸들러
        /// </summary>
        private async void PresetSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 검색어 변경 후 잠깐 대기 (연속 입력 방지)
            if (sender is TextBox textBox)
            {
                _currentPresetSearchTerm = textBox.Text?.Trim() ?? string.Empty;
                await Task.Delay(300); // 300ms 대기
                
                // 텍스트가 변경되지 않았으면 검색 실행
                if (textBox.Text?.Trim() == _currentPresetSearchTerm)
                {
                    await LoadPresets();
                }
            }
        }

        /// <summary>
        /// 프리셋 검색 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void PresetSearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPresetSearchTerm = PresetSearchTextBox?.Text?.Trim() ?? string.Empty;
            await LoadPresets();
        }

        /// <summary>
        /// 즐겨찾기 필터 체크박스 변경 이벤트 핸들러
        /// </summary>
        private async void FavoritesOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _favoritesOnly = FavoritesOnlyCheckBox?.IsChecked == true;
            await LoadPresets();
        }

        /// <summary>
        /// 프리셋 새로고침 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void RefreshPresetsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPresets();
        }

        /// <summary>
        /// 새 프리셋 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void NewPresetButton_Click(object sender, RoutedEventArgs e)
        {
            // 간단한 프리셋 생성 다이얼로그
            string result = ShowInputDialog("새 프리셋", "새 프리셋 이름을 입력하세요:", "새 프리셋");
            
            if (!string.IsNullOrWhiteSpace(result))
            {
                try
                {
                    UpdateStatusText("새 프리셋 생성 중...");

                    var request = new CreatePresetRequest
                    {
                        Name = result,
                        Description = "새로 생성된 프리셋",
                        MacroIds = new List<int>(), // 빈 프리셋으로 시작
                        IsFavorite = false
                    };

                    var newPresetId = await _apiService.CreatePresetAsync(request);
                    _loggingService.LogInfo($"새 프리셋이 생성되었습니다: '{request.Name}' (ID: {newPresetId})");

                    await LoadPresets();
                    UpdateStatusText("새 프리셋이 성공적으로 추가되었습니다.");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("프리셋 추가 중 오류 발생", ex);
                    MessageBox.Show($"프리셋 추가 중 오류가 발생했습니다:\n{ex.Message}",
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("프리셋 추가 실패");
                }
            }
        }

        /// <summary>
        /// 프리셋 수정 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void EditPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset == null)
            {
                MessageBox.Show("수정할 프리셋을 선택해주세요.", "선택 오류",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 간단한 프리셋 수정 다이얼로그
            string result = ShowInputDialog("프리셋 수정", "프리셋 이름을 수정하세요:", _selectedPreset.Name);
            
            if (!string.IsNullOrWhiteSpace(result) && result != _selectedPreset.Name)
            {
                try
                {
                    UpdateStatusText("프리셋 수정 중...");

                    var request = new UpdatePresetRequest
                    {
                        Name = result,
                        Description = _selectedPreset.Description,
                        MacroIds = _selectedPreset.MacroIds,
                        IsFavorite = _selectedPreset.IsFavorite
                    };

                    await _apiService.UpdatePresetAsync(_selectedPreset.Id, request);
                    _loggingService.LogInfo($"프리셋이 수정되었습니다: '{request.Name}' (ID: {_selectedPreset.Id})");

                    await LoadPresets();
                    UpdateStatusText("프리셋이 성공적으로 수정되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프리셋 수정 중 오류가 발생했습니다:\n{ex.Message}",
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("프리셋 수정 실패");
                }
            }
        }

        /// <summary>
        /// 프리셋 복사 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void CopyPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset == null)
            {
                MessageBox.Show("복사할 프리셋을 선택해주세요.", "선택 오류",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                UpdateStatusText("프리셋 복사 중...");

                string newName = $"{_selectedPreset.Name} - 복사본";
                var newPresetId = await _apiService.CopyPresetAsync(_selectedPreset.Id, newName);

                await LoadPresets();
                UpdateStatusText("프리셋이 성공적으로 복사되었습니다.");
                _loggingService.LogInfo($"프리셋이 복사되었습니다: '{newName}' (새 ID: {newPresetId})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프리셋 복사 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("프리셋 복사 실패");
            }
        }

        /// <summary>
        /// 프리셋 삭제 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset == null)
            {
                MessageBox.Show("삭제할 프리셋을 선택해주세요.", "선택 오류",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"정말로 '{_selectedPreset.Name}' 프리셋을 삭제하시겠습니까?\n\n" +
                "이 작업은 되돌릴 수 없습니다.",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    UpdateStatusText("프리셋 삭제 중...");

                    await _apiService.DeletePresetAsync(_selectedPreset.Id);
                    _loggingService.LogInfo($"프리셋이 삭제되었습니다: '{_selectedPreset.Name}' (ID: {_selectedPreset.Id})");

                    await LoadPresets();
                    UpdateStatusText("프리셋이 성공적으로 삭제되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프리셋 삭제 중 오류가 발생했습니다:\n{ex.Message}",
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("프리셋 삭제 실패");
                }
            }
        }

        /// <summary>
        /// 프리셋 가져오기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ImportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "프리셋 파일 선택",
                Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    UpdateStatusText("프리셋 가져오는 중...");

                    var newPresetId = await _apiService.ImportPresetAsync(openFileDialog.FileName);
                    _loggingService.LogInfo($"프리셋이 가져와졌습니다. 새 ID: {newPresetId}");

                    await LoadPresets();
                    UpdateStatusText("프리셋이 성공적으로 가져와졌습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프리셋 가져오기 중 오류가 발생했습니다:\n{ex.Message}",
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("프리셋 가져오기 실패");
                }
            }
        }

        /// <summary>
        /// 프리셋 내보내기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ExportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset == null)
            {
                MessageBox.Show("내보낼 프리셋을 선택해주세요.", "선택 오류",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "프리셋 내보내기",
                Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"{_selectedPreset.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    UpdateStatusText("프리셋 내보내는 중...");

                    var result = await _apiService.ExportPresetAsync(_selectedPreset.Id, saveFileDialog.FileName);
                    _loggingService.LogInfo($"프리셋이 내보내졌습니다: {result?.FilePath}");

                    UpdateStatusText("프리셋이 성공적으로 내보내졌습니다.");
                    MessageBox.Show($"프리셋이 다음 위치에 저장되었습니다:\n{result?.FilePath}",
                                  "내보내기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프리셋 내보내기 중 오류가 발생했습니다:\n{ex.Message}",
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("프리셋 내보내기 실패");
                }
            }
        }

        /// <summary>
        /// 프리셋 적용 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ApplyPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset == null)
            {
                MessageBox.Show("적용할 프리셋을 선택해주세요.", "선택 오류",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                UpdateStatusText("프리셋 적용 중...");

                var result = await _apiService.ApplyPresetAsync(_selectedPreset.Id);
                _loggingService.LogInfo($"프리셋이 적용되었습니다: '{result?.PresetName}' ({result?.MacroCount}개 매크로)");

                UpdateStatusText($"프리셋 '{result?.PresetName}'이 성공적으로 적용되었습니다.");
                MessageBox.Show($"프리셋 '{result?.PresetName}'이 적용되었습니다.\n" +
                              $"포함된 매크로: {result?.MacroCount}개",
                              "적용 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프리셋 적용 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("프리셋 적용 실패");
            }
        }

        /// <summary>
        /// 즐겨찾기 토글 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ToggleFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset == null)
            {
                MessageBox.Show("프리셋을 선택해주세요.", "선택 오류",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                UpdateStatusText("즐겨찾기 상태 변경 중...");

                var newFavoriteStatus = await _apiService.TogglePresetFavoriteAsync(_selectedPreset.Id);
                _loggingService.LogInfo($"프리셋 즐겨찾기 상태 변경: '{_selectedPreset.Name}' -> {(newFavoriteStatus ? "ON" : "OFF")}");

                await LoadPresets();
                UpdateStatusText($"즐겨찾기 상태가 {(newFavoriteStatus ? "추가" : "제거")}되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"즐겨찾기 상태 변경 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("즐겨찾기 상태 변경 실패");
            }
        }

        /// <summary>
        /// 프리셋 DataGrid 선택 변경 이벤트 핸들러
        /// </summary>
        private void PresetDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPreset = PresetDataGrid?.SelectedItem as PresetModel;
            bool hasSelection = _selectedPreset != null;

            // 선택된 항목이 있을 때만 관련 버튼들 활성화
            EditPresetButton.IsEnabled = hasSelection;
            CopyPresetButton.IsEnabled = hasSelection;
            DeletePresetButton.IsEnabled = hasSelection;
            ExportPresetButton.IsEnabled = hasSelection;
            ApplyPresetButton.IsEnabled = hasSelection;
            ToggleFavoriteButton.IsEnabled = hasSelection;

            // 미리보기 패널 업데이트
            UpdatePresetPreview();
        }

        /// <summary>
        /// 프리셋 DataGrid 더블클릭 이벤트 핸들러 (프리셋 적용)
        /// </summary>
        private async void PresetDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_selectedPreset != null)
            {
                await ApplyPreset(_selectedPreset.Id);
            }
        }

        /// <summary>
        /// 프리셋 미리보기 패널을 업데이트하는 함수
        /// </summary>
        private void UpdatePresetPreview()
        {
            try
            {
                if (_selectedPreset == null)
                {
                    // 선택된 프리셋이 없을 때
                    if (NoPresetSelectedText != null) NoPresetSelectedText.Visibility = Visibility.Visible;
                    if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Collapsed;
                    if (PresetPreviewActions != null) PresetPreviewActions.Visibility = Visibility.Collapsed;
                    return;
                }

                // 선택된 프리셋이 있을 때
                if (NoPresetSelectedText != null) NoPresetSelectedText.Visibility = Visibility.Collapsed;
                if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Visible;
                if (PresetPreviewActions != null) PresetPreviewActions.Visibility = Visibility.Visible;

                // 기본 정보 표시
                if (PresetNameText != null) PresetNameText.Text = _selectedPreset.Name;
                if (PresetDescriptionText != null) PresetDescriptionText.Text = _selectedPreset.DisplayDescription;
                if (PresetCreatedText != null) PresetCreatedText.Text = $"생성일: {_selectedPreset.CreatedAtText}";

                // 포함된 매크로 목록 로드 (비동기로 실행)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var detailedPreset = await _apiService.GetPresetAsync(_selectedPreset.Id);
                        if (detailedPreset?.Macros != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (PresetMacroListBox != null)
                                    PresetMacroListBox.ItemsSource = detailedPreset.Macros;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError("매크로 목록 로드 실패", ex);
                    }
                });

                // 즐겨찾기 버튼 텍스트 업데이트
                if (QuickFavoriteButton != null)
                    QuickFavoriteButton.Content = _selectedPreset.IsFavorite ? "⭐ 즐겨찾기 제거" : "⭐ 즐겨찾기 추가";
            }
            catch (Exception ex)
            {
                _loggingService.LogError("프리셋 미리보기 업데이트 실패", ex);
            }
        }

        /// <summary>
        /// 빠른 적용 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void QuickApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset != null)
            {
                await ApplyPreset(_selectedPreset.Id);
            }
        }

        /// <summary>
        /// 빠른 즐겨찾기 토글 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void QuickFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPreset != null)
            {
                await TogglePresetFavorite(_selectedPreset.Id);
            }
        }

        /// <summary>
        /// 프리셋을 적용하는 헬퍼 메서드
        /// </summary>
        private async Task ApplyPreset(int presetId)
        {
            try
            {
                UpdateStatusText("프리셋 적용 중...");
                var result = await _apiService.ApplyPresetAsync(presetId);
                
                if (result != null)
                {
                    MessageBox.Show($"프리셋이 성공적으로 적용되었습니다!\n프리셋명: {result.PresetName}\n적용된 매크로 수: {result.MacroCount}",
                                  "프리셋 적용", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateStatusText($"프리셋 적용 완료 ({result.MacroCount}개 매크로)");
                    _loggingService.LogInfo($"프리셋 적용 성공: ID {presetId}, 매크로 {result.MacroCount}개");
                    
                    // 매크로 목록 새로고침
                    await LoadMacros();
                }
                else
                {
                    MessageBox.Show("프리셋 적용에 실패했습니다.",
                                  "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatusText("프리셋 적용 실패");
                    _loggingService.LogError("프리셋 적용 실패: 서버에서 null 응답");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프리셋 적용 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("프리셋 적용 실패");
                _loggingService.LogError("프리셋 적용 중 오류 발생", ex);
            }
        }

        /// <summary>
        /// 프리셋 즐겨찾기 상태를 토글하는 헬퍼 메서드
        /// </summary>
        private async Task TogglePresetFavorite(int presetId)
        {
            try
            {
                UpdateStatusText("즐겨찾기 상태 변경 중...");
                var newFavoriteStatus = await _apiService.TogglePresetFavoriteAsync(presetId);
                
                var statusText = newFavoriteStatus ? "즐겨찾기에 추가" : "즐겨찾기에서 제거";
                UpdateStatusText($"즐겨찾기 상태 변경 완료 ({statusText})");
                _loggingService.LogInfo($"프리셋 즐겨찾기 상태 변경: ID {presetId}, {statusText}");
                
                // 프리셋 목록 새로고침
                await LoadPresets();
                await UpdatePresetStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"즐겨찾기 상태 변경 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("즐겨찾기 상태 변경 실패");
                _loggingService.LogError("즐겨찾기 상태 변경 중 오류 발생", ex);
            }
        }

        /// <summary>
        /// 입력 다이얼로그를 표시하는 헬퍼 메서드
        /// </summary>
        /// <param name="title">다이얼로그 제목</param>
        /// <param name="prompt">입력 안내 메시지</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns>입력된 텍스트 (취소시 빈 문자열)</returns>
        private string ShowInputDialog(string title, string prompt, string defaultValue = "")
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var promptLabel = new TextBlock
            {
                Text = prompt,
                Margin = new Thickness(20, 20, 20, 10),
                FontSize = 14
            };
            Grid.SetRow(promptLabel, 0);

            var inputTextBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(20, 0, 20, 20),
                FontSize = 14,
                Height = 30
            };
            Grid.SetRow(inputTextBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };
            
            var okButton = new Button
            {
                Content = "확인",
                Width = 70,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "취소",
                Width = 70,
                Height = 30,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(promptLabel);
            grid.Children.Add(inputTextBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            bool? result = null;
            okButton.Click += (s, e) => { result = true; dialog.Close(); };
            cancelButton.Click += (s, e) => { result = false; dialog.Close(); };

            inputTextBox.Focus();
            inputTextBox.SelectAll();

            dialog.ShowDialog();

            return result == true ? inputTextBox.Text : "";
        }
    }
} 