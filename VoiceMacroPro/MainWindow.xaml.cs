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
        private List<Macro> _allMacros = new List<Macro>();
        private string _currentSearchTerm = string.Empty;
        private string _currentSortBy = "name";
        private CollectionViewSource? _logViewSource;

        /// <summary>
        /// 메인 윈도우 생성자
        /// API 서비스를 초기화하고 UI를 설정합니다.
        /// </summary>
        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow 생성자 시작");
                
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent 완료");
                
                _apiService = new ApiService();
                System.Diagnostics.Debug.WriteLine("ApiService 초기화 완료");
                
                // 로깅 서비스 초기화
                _loggingService = LoggingService.Instance;
                InitializeLoggingUI();
                System.Diagnostics.Debug.WriteLine("LoggingService 초기화 완료");
                
                // 윈도우가 로드된 후 초기화 작업 수행
                Loaded += MainWindow_Loaded;
                System.Diagnostics.Debug.WriteLine("MainWindow 생성자 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow 생성자 오류: {ex}");
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
        /// 윈도우가 닫힐 때 리소스를 정리하는 함수
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // API 서비스 리소스 정리
            _apiService?.Dispose();
            
            // 로깅 서비스 종료 로그
            _loggingService?.LogInfo("애플리케이션이 종료됩니다.");
            
            base.OnClosed(e);
        }
    }
} 