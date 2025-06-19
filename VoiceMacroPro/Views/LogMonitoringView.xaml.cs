using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using VoiceMacroPro.Services;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 로그 및 모니터링 뷰 - 실시간 로그 출력 및 성능 모니터링 기능을 제공합니다
    /// </summary>
    public partial class LogMonitoringView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields
        
        private readonly LoggingService _loggingService;
        private ObservableCollection<string> _displayLogs;
        private LogLevel _selectedLogLevel = LogLevel.Info;
        private bool _isAutoScroll = true;
        private string _statusText = "로그 모니터링 시스템 준비 중...";
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// UI에 표시할 로그 항목들
        /// </summary>
        public ObservableCollection<string> DisplayLogs
        {
            get => _displayLogs;
            set
            {
                _displayLogs = value;
                OnPropertyChanged(nameof(DisplayLogs));
            }
        }
        
        /// <summary>
        /// 선택된 로그 레벨
        /// </summary>
        public LogLevel SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                _selectedLogLevel = value;
                OnPropertyChanged(nameof(SelectedLogLevel));
                FilterLogs();
            }
        }
        
        /// <summary>
        /// 자동 스크롤 활성화 여부
        /// </summary>
        public bool IsAutoScroll
        {
            get => _isAutoScroll;
            set
            {
                _isAutoScroll = value;
                OnPropertyChanged(nameof(IsAutoScroll));
                _loggingService.SetAutoScroll(value);
            }
        }
        
        /// <summary>
        /// 상태 텍스트
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// LogMonitoringView 생성자 - 초기화 및 로그 시스템 설정을 수행합니다
        /// </summary>
        public LogMonitoringView()
        {
            try
            {
                InitializeComponent();
                
                // 로깅 서비스 인스턴스 가져오기
                _loggingService = LoggingService.Instance;
                
                // 컬렉션 초기화
                _displayLogs = new ObservableCollection<string>();
                
                // 데이터 컨텍스트 설정
                DataContext = this;
                
                // LogListBox에 ItemsSource 바인딩
                LogListBox.ItemsSource = DisplayLogs;
                
                // 로깅 서비스 이벤트 구독 - CollectionChanged 이벤트 사용
                _loggingService.LogEntries.CollectionChanged += LogEntries_CollectionChanged;
                _loggingService.PropertyChanged += LoggingService_PropertyChanged;
                
                // 초기 로그 로드
                LoadExistingLogs();
                
                // 테스트 로그 추가 (로깅 시스템 동작 확인용)
                _loggingService.LogInfo("📊 로그 모니터링 시스템이 활성화되었습니다");
                _loggingService.LogDebug("디버그 모드로 실행 중입니다");
                _loggingService.LogWarning("테스트 경고 메시지입니다");
                _loggingService.LogError("테스트 오류 메시지입니다");
                
                StatusText = "로그 모니터링 시스템 활성화됨";
                
                _loggingService.LogInfo("LogMonitoringView 초기화 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 모니터링 뷰 초기화 중 오류: {ex.Message}", 
                              "초기화 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// LogEntries 컬렉션 변경 이벤트 핸들러
        /// </summary>
        private void LogEntries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                // UI 스레드에서 처리
                Dispatcher.Invoke(() => 
                {
                    if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
                    {
                        // 새로 추가된 로그 엔트리들 처리
                        foreach (LogEntry newLogEntry in e.NewItems)
                        {
                            if (ShouldDisplayLog(newLogEntry))
                            {
                                DisplayLogs.Add(FormatLogEntry(newLogEntry));
                                
                                // 자동 스크롤
                                if (IsAutoScroll)
                                {
                                    ScrollToBottom();
                                }
                            }
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        // 컬렉션이 지워진 경우
                        DisplayLogs.Clear();
                    }
                    
                    // 최대 로그 개수 제한 (UI 성능을 위해)
                    while (DisplayLogs.Count > 500)
                    {
                        DisplayLogs.RemoveAt(0);
                    }
                    
                    // 상태 업데이트
                    StatusText = $"활성 로그: {DisplayLogs.Count}개 | 전체: {_loggingService.LogEntries.Count}개";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogEntries_CollectionChanged 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 로깅 서비스의 속성 변경 이벤트 핸들러
        /// </summary>
        private void LoggingService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoggingService.TotalLogCount))
            {
                // UI 스레드에서 상태 업데이트
                Dispatcher.Invoke(() => {
                    StatusText = $"활성 로그: {DisplayLogs.Count}개 | 전체: {_loggingService.LogEntries.Count}개";
                });
            }
        }
        
        /// <summary>
        /// 로그 레벨 변경 이벤트 핸들러
        /// </summary>
        private void LogLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
            {
                if (Enum.TryParse<LogLevel>(comboBox.SelectedItem.ToString(), out var level))
                {
                    SelectedLogLevel = level;
                    _loggingService.CurrentLogLevel = level;
                }
            }
        }
        
        /// <summary>
        /// 로그 지우기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("모든 로그를 지우시겠습니까?", 
                                          "로그 삭제 확인", 
                                          MessageBoxButton.YesNo, 
                                          MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _loggingService.ClearLogs();
                    DisplayLogs.Clear();
                    StatusText = "로그가 모두 삭제되었습니다";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 삭제 중 오류: {ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 로그 내보내기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ExportLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "텍스트 파일 (*.txt)|*.txt|모든 파일 (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"VoiceMacro_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    await _loggingService.ExportLogsAsync(saveFileDialog.FileName);
                    StatusText = $"로그가 내보내졌습니다: {saveFileDialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 내보내기 중 오류: {ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 자동 스크롤 체크박스 변경 이벤트 핸들러
        /// </summary>
        private void AutoScrollCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsAutoScroll = true;
            ScrollToBottom();
        }
        
        private void AutoScrollCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsAutoScroll = false;
        }
        
        /// <summary>
        /// 새로고침 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadExistingLogs();
                _loggingService.LogInfo("로그 모니터링 새로고침 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 새로고침 중 오류: {ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 기존 로그를 로드하는 함수
        /// </summary>
        private void LoadExistingLogs()
        {
            try
            {
                DisplayLogs.Clear();
                
                foreach (var logEntry in _loggingService.LogEntries)
                {
                    if (ShouldDisplayLog(logEntry))
                    {
                        DisplayLogs.Add(FormatLogEntry(logEntry));
                    }
                }
                
                StatusText = $"총 {DisplayLogs.Count}개의 로그가 로드됨";
                
                // 자동 스크롤
                if (IsAutoScroll)
                {
                    ScrollToBottom();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"기존 로그 로드 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 로그 필터링 함수
        /// </summary>
        private void FilterLogs()
        {
            try
            {
                DisplayLogs.Clear();
                
                foreach (var logEntry in _loggingService.LogEntries)
                {
                    if (ShouldDisplayLog(logEntry))
                    {
                        DisplayLogs.Add(FormatLogEntry(logEntry));
                    }
                }
                
                StatusText = $"필터링된 로그: {DisplayLogs.Count}개";
                
                if (IsAutoScroll)
                {
                    ScrollToBottom();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"로그 필터링 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 로그가 현재 필터 조건에 맞는지 확인하는 함수
        /// </summary>
        private bool ShouldDisplayLog(LogEntry logEntry)
        {
            return logEntry.Level >= SelectedLogLevel;
        }
        
        /// <summary>
        /// 로그 엔트리를 UI 표시용 문자열로 포맷하는 함수
        /// </summary>
        private string FormatLogEntry(LogEntry logEntry)
        {
            var macroInfo = logEntry.MacroId.HasValue ? $" [Macro:{logEntry.MacroId}]" : "";
            return $"[{logEntry.TimeText}] [{logEntry.LevelText}]{macroInfo} {logEntry.Message}";
        }
        
        /// <summary>
        /// 로그 목록을 맨 아래로 스크롤하는 함수
        /// </summary>
        private void ScrollToBottom()
        {
            try
            {
                if (LogListBox.Items.Count > 0)
                {
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
            }
            catch (Exception)
            {
                // 스크롤 실패는 무시 (UI 관련 예외)
            }
        }
        
        #endregion
        
        #region INotifyPropertyChanged Implementation
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 이벤트 구독 해제
                if (_loggingService != null)
                {
                    _loggingService.LogEntries.CollectionChanged -= LogEntries_CollectionChanged;
                    _loggingService.PropertyChanged -= LoggingService_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogMonitoringView Dispose 오류: {ex.Message}");
            }
        }
        
        #endregion
    }
} 