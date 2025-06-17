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
                
                // 마이크 장치 목록 로드 (비동기적으로)
                Task.Run(async () => 
                {
                    try
                    {
                        await LoadMicrophoneDevices();
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
                var devices = await _voiceService.GetAvailableDevicesAsync();
                
                // UI 스레드에서 ComboBox 업데이트
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (MicrophoneComboBox != null)
                    {
                        MicrophoneComboBox.Items.Clear();
                        
                        foreach (var device in devices)
                        {
                            var item = new ComboBoxItem
                            {
                                Content = device.Name,
                                Tag = device.Id
                            };
                            MicrophoneComboBox.Items.Add(item);
                        }
                        
                        // 첫 번째 장치를 기본으로 선택
                        if (devices.Count > 0)
                        {
                            MicrophoneComboBox.SelectedIndex = 0;
                        }
                    }
                });
                
                _loggingService.LogInfo($"마이크 장치 목록 로드 완료: {devices.Count}개 장치");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 장치 목록 로드 중 오류: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"마이크 장치 로드 오류: {ex}");
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
        /// </summary>
        private async void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var success = await _voiceService.StartRecordingAsync();
                
                if (success)
                {
                    _isRecording = true;
                    UpdateRecordingUI();
                    
                    if (RecognizedTextBlock != null)
                    {
                        RecognizedTextBlock.Text = "음성을 인식하고 있습니다...";
                        RecognizedTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
                    }
                    
                    _loggingService.LogInfo("음성 녹음이 시작되었습니다.");
                    
                    // 2초 후 자동으로 음성 분석 시작
                    await Task.Delay(2000);
                    if (_isRecording)
                    {
                        await AnalyzeVoiceAndShowResults();
                    }
                }
                else
                {
                    MessageBox.Show("음성 녹음을 시작할 수 없습니다.", "오류", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"녹음 시작 중 오류: {ex.Message}");
                MessageBox.Show($"녹음 시작 중 오류가 발생했습니다:\n{ex.Message}", 
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
                
                var result = await _voiceService.TestMicrophoneAsync();
                
                if (result != null)
                {
                    string message;
                    if (result.Success)
                    {
                        message = "마이크 테스트 성공!\n\n" +
                                $"• 장치 사용 가능: {(result.DeviceAvailable ? "✅" : "❌")}\n" +
                                $"• 녹음 테스트: {(result.RecordingTest ? "✅" : "❌")}\n" +
                                $"• 오디오 레벨 감지: {(result.AudioLevelDetected ? "✅" : "❌")}\n" +
                                $"• 모드: {result.Mode}";
                        
                        MessageBox.Show(message, "마이크 테스트 완료", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        _loggingService.LogInfo("마이크 테스트 성공");
                    }
                    else
                    {
                        message = $"마이크 테스트 실패\n\n오류: {result.ErrorMessage}";
                        MessageBox.Show(message, "마이크 테스트 실패", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        
                        _loggingService.LogWarning($"마이크 테스트 실패: {result.ErrorMessage}");
                    }
                }
                else
                {
                    MessageBox.Show("마이크 테스트를 수행할 수 없습니다.", "오류", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 테스트 중 오류: {ex.Message}");
                MessageBox.Show($"마이크 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
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
    }
} 