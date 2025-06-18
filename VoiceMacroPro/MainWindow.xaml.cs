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
using System.Windows.Input;
using VoiceMacroPro.Utils;

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

        // ==================== 프리셋 관리 관련 필드 및 메서드 ====================
        private List<PresetModel> _allPresets = new List<PresetModel>();
        private PresetModel? _selectedPreset = null;
        private string _currentPresetSearchTerm = string.Empty;
        private bool _favoritesOnly = false;

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
        /// 매크로 추가 버튼 클릭 이벤트 핸들러
        /// 새로운 매크로를 생성하기 위한 다이얼로그를 표시합니다.
        /// </summary>
        private async void AddMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new MacroEditWindow();
                
                if (editWindow.ShowDialog() == true && editWindow.MacroResult != null)
                {
                    UpdateStatusText("매크로 추가 중...");
                    _loggingService.LogInfo($"새 매크로 추가 시도: {editWindow.MacroResult.Name}");
                    
                    var macroId = await _apiService.CreateMacroAsync(editWindow.MacroResult);
                    
                    MessageBox.Show($"매크로 '{editWindow.MacroResult.Name}'이(가) 성공적으로 추가되었습니다!\nID: {macroId}", 
                                  "매크로 추가", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    UpdateStatusText("매크로 추가 완료");
                    _loggingService.LogInfo($"매크로 추가 성공: ID {macroId}, 이름 '{editWindow.MacroResult.Name}'");
                    
                    // 모든 관련 데이터 동기화
                    await RefreshMacroRelatedData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 추가 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                UpdateStatusText("매크로 추가 실패");
                _loggingService.LogError($"매크로 추가 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 수정 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 수정하기 위한 다이얼로그를 표시합니다.
        /// </summary>
        private async void EditMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MacroDataGrid?.SelectedItem is not Macro selectedMacro)
                {
                    MessageBox.Show("수정할 매크로를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var editWindow = new MacroEditWindow(selectedMacro);
                
                if (editWindow.ShowDialog() == true && editWindow.MacroResult != null)
                {
                    UpdateStatusText("매크로 수정 중...");
                    _loggingService.LogInfo($"매크로 수정 시도: ID {selectedMacro.Id}, 이름 '{selectedMacro.Name}'");
                    
                    var success = await _apiService.UpdateMacroAsync(editWindow.MacroResult);
                    
                    if (success)
                    {
                        MessageBox.Show($"매크로 '{editWindow.MacroResult.Name}'이(가) 성공적으로 수정되었습니다!", 
                                      "매크로 수정", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                        
                        UpdateStatusText("매크로 수정 완료");
                        _loggingService.LogInfo($"매크로 수정 성공: ID {selectedMacro.Id}, 새 이름 '{editWindow.MacroResult.Name}'");
                        
                        // 모든 관련 데이터 동기화
                        await RefreshMacroRelatedData();
                    }
                    else
                    {
                        MessageBox.Show("매크로 수정에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        UpdateStatusText("매크로 수정 실패");
                        _loggingService.LogError("매크로 수정 실패: 서버에서 false 응답");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 수정 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                UpdateStatusText("매크로 수정 실패");
                _loggingService.LogError($"매크로 수정 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 복사 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 복사하여 새로운 매크로를 생성합니다.
        /// </summary>
        private async void CopyMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MacroDataGrid?.SelectedItem is not Macro selectedMacro)
                {
                    MessageBox.Show("복사할 매크로를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                UpdateStatusText("매크로 복사 중...");
                _loggingService.LogInfo($"매크로 복사 시도: ID {selectedMacro.Id}, 이름 '{selectedMacro.Name}'");
                
                var newMacroId = await _apiService.CopyMacroAsync(selectedMacro.Id);
                
                MessageBox.Show($"매크로 '{selectedMacro.Name}'이(가) 성공적으로 복사되었습니다!\n새 매크로 ID: {newMacroId}", 
                              "매크로 복사", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
                
                UpdateStatusText("매크로 복사 완료");
                _loggingService.LogInfo($"매크로 복사 성공: 원본 ID {selectedMacro.Id}, 새 ID {newMacroId}");
                
                // 모든 관련 데이터 동기화
                await RefreshMacroRelatedData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 복사 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                UpdateStatusText("매크로 복사 실패");
                _loggingService.LogError($"매크로 복사 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 삭제 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 삭제합니다.
        /// </summary>
        private async void DeleteMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MacroDataGrid?.SelectedItem is not Macro selectedMacro)
                {
                    MessageBox.Show("삭제할 매크로를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show($"'{selectedMacro.Name}' 매크로를 정말 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.", 
                                           "매크로 삭제 확인", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    UpdateStatusText("매크로 삭제 중...");
                    _loggingService.LogInfo($"매크로 삭제 시도: ID {selectedMacro.Id}, 이름 '{selectedMacro.Name}'");
                    
                    var success = await _apiService.DeleteMacroAsync(selectedMacro.Id);
                    
                    if (success)
                    {
                        MessageBox.Show($"매크로 '{selectedMacro.Name}'이(가) 성공적으로 삭제되었습니다.", 
                                      "매크로 삭제", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                        
                        UpdateStatusText("매크로 삭제 완료");
                        _loggingService.LogInfo($"매크로 삭제 성공: ID {selectedMacro.Id}, 이름 '{selectedMacro.Name}'");
                        
                        // 모든 관련 데이터 동기화
                        await RefreshMacroRelatedData();
                    }
                    else
                    {
                        MessageBox.Show("매크로 삭제에 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        UpdateStatusText("매크로 삭제 실패");
                        _loggingService.LogError("매크로 삭제 실패: 서버에서 false 응답");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 삭제 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                UpdateStatusText("매크로 삭제 실패");
                _loggingService.LogError($"매크로 삭제 실패: {ex.Message}");
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
        /// 프리셋 관련 데이터 변경 시 관련된 모든 탭을 갱신하는 메서드
        /// 프리셋 추가/수정/삭제/적용 후에 호출합니다.
        /// </summary>
        private async Task RefreshPresetRelatedData()
        {
            try
            {
                _loggingService.LogDebug("프리셋 관련 데이터 새로고침 시작");
                
                // 프리셋 목록 새로고침
                await LoadPresets();
                
                // 프리셋 통계 업데이트
                await UpdatePresetStatistics();
                
                // 프리셋 미리보기 갱신
                await UpdatePresetPreview();
                
                // 프리셋 적용으로 매크로가 변경되었을 수 있으므로 매크로 목록도 새로고침
                await LoadMacros();
                
                _loggingService.LogDebug("프리셋 관련 데이터 새로고침 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 관련 데이터 새로고침 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 음성 인식 매칭 캐시를 무효화하는 메서드
        /// 매크로나 프리셋 변경 시 호출합니다.
        /// </summary>
        private async Task InvalidateVoiceMatchingCache()
        {
            try
            {
                // 현재 매칭 결과 초기화
                _currentMatchResults.Clear();
                
                if (MatchedMacrosDataGrid != null)
                {
                    MatchedMacrosDataGrid.ItemsSource = null;
                }
                
                // 인식된 텍스트가 있다면 새로운 매크로 목록으로 다시 매칭
                if (RecognizedTextBlock != null && !string.IsNullOrWhiteSpace(RecognizedTextBlock.Text) && 
                    RecognizedTextBlock.Text != "음성 인식을 시작하세요...")
                {
                    await AnalyzeVoiceAndShowResults();
                }
                
                _loggingService.LogDebug("음성 인식 매칭 캐시 무효화 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"음성 인식 매칭 캐시 무효화 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 로깅 UI를 초기화하는 메서드
        /// </summary>
        private void InitializeLoggingUI()
        {
            try
            {
                // CollectionViewSource를 통한 필터링 설정
                _logViewSource = new CollectionViewSource();
                _logViewSource.Source = _loggingService.LogEntries;
                
                if (LogDataGrid != null)
                {
                    LogDataGrid.ItemsSource = _logViewSource.View;
                }
                
                _loggingService.LogInfo("로깅 UI 초기화 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로깅 UI 초기화 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                
                _loggingService.LogInfo($"프리셋 목록 로드 완료: {_allPresets.Count}개 항목");
                UpdateStatusText($"프리셋 {_allPresets.Count}개 로드 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프리셋 목록을 불러오는 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("프리셋 로드 실패");
                _loggingService.LogError($"프리셋 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 통계 정보를 업데이트하는 메서드
        /// </summary>
        private async Task UpdatePresetStatistics()
        {
            try
            {
                var totalCount = _allPresets.Count;
                var favoriteCount = _allPresets.Count(p => p.IsFavorite);
                var favoritePercentage = totalCount > 0 ? (favoriteCount * 100.0 / totalCount) : 0;
                var recentPreset = _allPresets.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

                // UI 업데이트
                if (TotalPresetsText != null) TotalPresetsText.Text = $"{totalCount}개";
                if (FavoritePresetsText != null) FavoritePresetsText.Text = $"{favoriteCount}개";
                if (FavoritePercentageText != null) FavoritePercentageText.Text = $"{favoritePercentage:F0}%";
                if (RecentPresetText != null) RecentPresetText.Text = recentPreset?.Name ?? "없음";
                if (PresetCountTextBlock != null) PresetCountTextBlock.Text = $"총 {totalCount}개 프리셋";
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 통계 업데이트 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택된 프리셋의 미리보기를 업데이트하는 메서드
        /// 백엔드 API를 호출하여 매크로 상세 정보까지 포함해서 표시합니다.
        /// </summary>
        private async Task UpdatePresetPreview()
        {
            try
            {
                if (_selectedPreset == null)
                {
                    // 선택된 프리셋이 없을 때 미리보기 초기화
                    if (NoPresetSelectedText != null)
                    {
                        NoPresetSelectedText.Text = "프리셋을 선택하면 포함된 매크로 목록이 여기에 표시됩니다.";
                        NoPresetSelectedText.Visibility = Visibility.Visible;
                    }
                    if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                // 로딩 상태 표시
                if (NoPresetSelectedText != null)
                {
                    NoPresetSelectedText.Text = "📡 프리셋 정보를 불러오는 중...";
                    NoPresetSelectedText.Visibility = Visibility.Visible;
                }
                if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Collapsed;

                _loggingService.LogInfo($"프리셋 미리보기 업데이트 시작: {_selectedPreset.Name} (ID: {_selectedPreset.Id})");

                // 백엔드 API를 통해 프리셋 상세 정보 조회 (매크로 상세 정보 포함)
                var presetDetail = await _apiService.GetPresetAsync(_selectedPreset.Id);
                
                if (presetDetail != null && presetDetail.Macros != null && presetDetail.Macros.Any())
                {
                    // 매크로 상세 정보로 미리보기 텍스트 생성
                    var previewText = new StringBuilder();
                    previewText.AppendLine($"🎯 프리셋: {presetDetail.Name}");
                    previewText.AppendLine($"📝 설명: {presetDetail.Description}");
                    previewText.AppendLine($"⭐ 즐겨찾기: {(presetDetail.IsFavorite ? "예" : "아니오")}");
                    previewText.AppendLine($"📅 생성일: {presetDetail.CreatedAt:yyyy-MM-dd HH:mm}");
                    previewText.AppendLine($"🔄 수정일: {presetDetail.UpdatedAt:yyyy-MM-dd HH:mm}");
                    previewText.AppendLine();
                    previewText.AppendLine($"🎮 포함된 매크로 ({presetDetail.Macros.Count}개):");
                    previewText.AppendLine(new string('=', 50));

                    foreach (var macro in presetDetail.Macros.Take(10)) // 최대 10개까지 표시
                    {
                        previewText.AppendLine($"📌 {macro.Name}");
                        previewText.AppendLine($"   🗣️ 음성명령: \"{macro.VoiceCommand}\"");
                        previewText.AppendLine($"   ⚡ 동작타입: {macro.ActionType}");
                        previewText.AppendLine($"   ⌨️ 키시퀀스: {macro.KeySequence}");
                        // IsActive 속성이 없을 수 있으므로 체크
                        var activeStatus = macro.GetType().GetProperty("IsActive") != null ? 
                            (bool?)macro.GetType().GetProperty("IsActive")?.GetValue(macro) : null;
                        previewText.AppendLine($"   ✅ 활성상태: {(activeStatus?.ToString() ?? "확인불가")}");
                        previewText.AppendLine();
                    }

                    if (presetDetail.Macros.Count > 10)
                    {
                        previewText.AppendLine($"... 외 {presetDetail.Macros.Count - 10}개 매크로가 더 있습니다.");
                    }

                    // UI 업데이트
                    if (NoPresetSelectedText != null)
                    {
                        NoPresetSelectedText.Text = previewText.ToString();
                        NoPresetSelectedText.Visibility = Visibility.Visible;
                    }
                    if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Visible;

                    _loggingService.LogInfo($"프리셋 미리보기 업데이트 완료: {presetDetail.Macros.Count}개 매크로 표시");
                }
                else if (presetDetail != null)
                {
                    // 매크로가 없는 프리셋
                    var previewText = new StringBuilder();
                    previewText.AppendLine($"🎯 프리셋: {presetDetail.Name}");
                    previewText.AppendLine($"📝 설명: {presetDetail.Description}");
                    previewText.AppendLine($"⭐ 즐겨찾기: {(presetDetail.IsFavorite ? "예" : "아니오")}");
                    previewText.AppendLine($"📅 생성일: {presetDetail.CreatedAt:yyyy-MM-dd HH:mm}");
                    previewText.AppendLine();
                    previewText.AppendLine("⚠️ 이 프리셋에는 매크로가 포함되어 있지 않습니다.");
                    previewText.AppendLine("매크로를 추가한 후 프리셋을 업데이트하세요.");

                    if (NoPresetSelectedText != null)
                    {
                        NoPresetSelectedText.Text = previewText.ToString();
                        NoPresetSelectedText.Visibility = Visibility.Visible;
                    }
                    if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Collapsed;

                    _loggingService.LogWarning($"프리셋 '{presetDetail.Name}'에 매크로가 없습니다.");
                }
                else
                {
                    // 프리셋 조회 실패
                    if (NoPresetSelectedText != null)
                    {
                        NoPresetSelectedText.Text = $"❌ 프리셋 정보를 불러올 수 없습니다.\n프리셋 ID: {_selectedPreset.Id}";
                        NoPresetSelectedText.Visibility = Visibility.Visible;
                    }
                    if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Collapsed;
                    
                    _loggingService.LogError($"프리셋 상세 조회 실패: ID {_selectedPreset.Id}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 미리보기 업데이트 중 오류: {ex.Message}");
                
                if (NoPresetSelectedText != null)
                {
                    NoPresetSelectedText.Text = $"❌ 오류 발생: {ex.Message}\n\n서버가 실행 중인지 확인하세요.";
                    NoPresetSelectedText.Visibility = Visibility.Visible;
                }
                if (PresetInfoPanel != null) PresetInfoPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 음성 인식 UI를 초기화하는 메서드
        /// </summary>
        private void InitializeVoiceRecognitionUI()
        {
            try
            {
                // 마이크 디바이스 목록 로드
                _ = LoadMicrophoneDevices();
                
                // 상태 업데이트 타이머 시작
                _statusUpdateTimer = new System.Windows.Threading.DispatcherTimer();
                _statusUpdateTimer.Interval = TimeSpan.FromMilliseconds(100);
                _statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
                _statusUpdateTimer.Start();
                
                _loggingService.LogInfo("음성 인식 UI 초기화 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 UI 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 마이크 디바이스 목록을 로드하는 메서드
        /// </summary>
        private async Task LoadMicrophoneDevices()
        {
            try
            {
                // 마이크 디바이스 목록 조회 로직 (구현 필요)
                if (MicrophoneComboBox != null)
                {
                    MicrophoneComboBox.Items.Clear();
                    MicrophoneComboBox.Items.Add("기본 마이크");
                    MicrophoneComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 디바이스 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 상태 업데이트 타이머 이벤트 핸들러
        /// </summary>
        private async void StatusUpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 마이크 레벨 업데이트 로직 (구현 필요)
                if (MicLevelProgressBar != null && MicLevelTextBlock != null)
                {
                    // 임시 값 (실제로는 마이크 레벨을 읽어와야 함)
                    var level = 0.0;
                    MicLevelProgressBar.Value = level;
                    MicLevelTextBlock.Text = $"{level * 100:F0}%";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"상태 업데이트 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 음성 분석 및 매크로 매칭 결과 표시
        /// </summary>
        private async Task AnalyzeVoiceAndShowResults()
        {
            try
            {
                if (RecognizedTextBlock == null || string.IsNullOrWhiteSpace(RecognizedTextBlock.Text))
                    return;

                var recognizedText = RecognizedTextBlock.Text;
                _loggingService.LogInfo($"음성 텍스트 분석 시작: '{recognizedText}'");

                // API를 통해 매크로 매칭 수행
                var matchResults = await _apiService.AnalyzeVoiceCommandAsync(recognizedText);
                
                if (matchResults != null && matchResults.Any())
                {
                    _currentMatchResults = matchResults;
                    
                    if (MatchedMacrosDataGrid != null)
                    {
                        MatchedMacrosDataGrid.ItemsSource = _currentMatchResults;
                    }
                    
                    _loggingService.LogInfo($"매크로 매칭 완료: {matchResults.Count}개 결과");
                }
                else
                {
                    _currentMatchResults.Clear();
                    if (MatchedMacrosDataGrid != null)
                    {
                        MatchedMacrosDataGrid.ItemsSource = null;
                    }
                    
                    _loggingService.LogInfo("매칭된 매크로가 없습니다");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 분석 실패: {ex.Message}");
            }
        }

        // ==================== 로그 관련 이벤트 핸들러들 ====================
        
        private void LogLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (LogLevelComboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    var selectedLevel = selectedItem.Tag?.ToString() ?? "Info";
                    _loggingService.SetMinimumLevel(selectedLevel);
                    _loggingService.LogInfo($"로그 레벨이 {selectedLevel}로 변경되었습니다");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"로그 레벨 변경 실패: {ex.Message}");
            }
        }

        private void LogFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filterText = LogFilterTextBox?.Text?.Trim().ToLower() ?? "";
                
                if (_logViewSource?.View != null)
                {
                    if (string.IsNullOrEmpty(filterText))
                    {
                        _logViewSource.View.Filter = null;
                    }
                    else
                    {
                        _logViewSource.View.Filter = obj =>
                        {
                            if (obj is LogEntry log)
                            {
                                return log.Message.ToLower().Contains(filterText) ||
                                       log.LevelText.ToLower().Contains(filterText);
                            }
                            return false;
                        };
                    }
                    _logViewSource.View.Refresh();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"로그 필터링 실패: {ex.Message}");
            }
        }

        private void AutoScrollCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _loggingService.SetAutoScroll(true);
        }

        private void AutoScrollCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _loggingService.SetAutoScroll(false);
        }

        private async void ExportLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "로그 파일 저장",
                    Filter = "텍스트 파일 (*.txt)|*.txt|CSV 파일 (*.csv)|*.csv",
                    DefaultExt = "txt",
                    FileName = $"VoiceMacroLog_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await _loggingService.ExportLogsAsync(saveDialog.FileName);
                    MessageBox.Show($"로그가 성공적으로 저장되었습니다:\n{saveDialog.FileName}", 
                                  "로그 저장", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 저장 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("모든 로그를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", 
                                           "로그 삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _loggingService.ClearLogs();
                    _loggingService.LogInfo("로그가 사용자에 의해 삭제되었습니다");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 삭제 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 음성 인식 이벤트 핸들러들 ====================
        
        private async void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isRecording = true;
                UpdateRecordingUI();
                
                // 음성 인식 시작 로직 (구현 필요)
                _loggingService.LogInfo("음성 녹음이 시작되었습니다");
                
                if (RecognizedTextBlock != null)
                {
                    RecognizedTextBlock.Text = "음성을 인식하고 있습니다...";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 녹음 시작 실패: {ex.Message}");
                _isRecording = false;
                UpdateRecordingUI();
            }
        }

        private async void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isRecording = false;
                UpdateRecordingUI();
                
                // 음성 인식 중지 로직 (구현 필요)
                _loggingService.LogInfo("음성 녹음이 중지되었습니다");
                
                // 임시 테스트 텍스트
                if (RecognizedTextBlock != null)
                {
                    RecognizedTextBlock.Text = "테스트 음성 명령어";
                    await AnalyzeVoiceAndShowResults();
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 녹음 중지 실패: {ex.Message}");
            }
        }

        private void UpdateRecordingUI()
        {
            try
            {
                if (StartRecordingButton != null)
                    StartRecordingButton.IsEnabled = !_isRecording;
                
                if (StopRecordingButton != null)
                    StopRecordingButton.IsEnabled = _isRecording;
                
                if (RecordingStatusIndicator != null)
                {
                    RecordingStatusIndicator.Fill = new SolidColorBrush(_isRecording ? Colors.Green : Colors.Red);
                }
                
                if (RecordingStatusText != null)
                {
                    RecordingStatusText.Text = _isRecording ? "녹음 중" : "중지";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogWarning($"녹음 UI 업데이트 실패: {ex.Message}");
            }
        }

        private async void MicrophoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (MicrophoneComboBox?.SelectedItem != null)
                {
                    var selectedMicrophone = MicrophoneComboBox.SelectedItem.ToString();
                    _loggingService.LogInfo($"마이크가 변경되었습니다: {selectedMicrophone}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 변경 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 감도 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 초기화 중에는 무시
            if (SensitivityValueText == null || _loggingService == null)
                return;

            try
            {
                SensitivityValueText.Text = $"{e.NewValue:F1}x";
                _loggingService.LogDebug($"음성 인식 감도 변경: {e.NewValue:F1}x");
            }
            catch (Exception ex)
            {
                // 로깅 서비스가 없으면 콘솔에 출력
                System.Diagnostics.Debug.WriteLine($"SensitivitySlider_ValueChanged 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 콤보 기본 딜레이 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void ComboDefaultDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 초기화 중에는 무시
            if (ComboDefaultDelayText == null || _loggingService == null)
                return;

            try
            {
                ComboDefaultDelayText.Text = $"{(int)e.NewValue}ms";
                _loggingService.LogDebug($"콤보 기본 딜레이 변경: {e.NewValue}ms");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ComboDefaultDelaySlider_ValueChanged 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 연속 클릭 CPS 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RapidCpsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 초기화 중에는 무시
            if (RapidCpsText == null || _loggingService == null)
                return;

            try
            {
                RapidCpsText.Text = $"{e.NewValue:F1} CPS";
                _loggingService.LogDebug($"연속 클릭 CPS 변경: {e.NewValue}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RapidCpsSlider_ValueChanged 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 연속 클릭 지속시간 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RapidDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 초기화 중에는 무시
            if (RapidDurationText == null || _loggingService == null)
                return;

            try
            {
                RapidDurationText.Text = $"{e.NewValue:F1}초";
                _loggingService.LogDebug($"연속 클릭 지속시간 변경: {e.NewValue}초");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RapidDurationSlider_ValueChanged 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 홀드 지속시간 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void HoldDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 초기화 중에는 무시
            if (HoldDurationText == null || _loggingService == null)
                return;

            try
            {
                HoldDurationText.Text = $"{e.NewValue:F1}초";
                _loggingService.LogDebug($"홀드 지속시간 변경: {e.NewValue}초");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HoldDurationSlider_ValueChanged 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 반복 횟수 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RepeatCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 초기화 중에는 무시
            if (RepeatCountText == null || _loggingService == null)
                return;

            try
            {
                RepeatCountText.Text = $"{(int)e.NewValue}회";
                _loggingService.LogDebug($"반복 횟수 변경: {e.NewValue}회");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RepeatCountSlider_ValueChanged 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 반복 간격 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private void RepeatIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 초기화 중에는 무시
            if (RepeatIntervalText == null || _loggingService == null)
                return;

            try
            {
                RepeatIntervalText.Text = $"{e.NewValue:F1}초";
                _loggingService.LogDebug($"반복 간격 변경: {e.NewValue}초");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RepeatIntervalSlider_ValueChanged 오류: {ex.Message}");
            }
        }

        // ==================== 누락된 이벤트 핸들러들 ====================
        
        /// <summary>
        /// 매크로 타입 라디오 버튼 체크 이벤트 핸들러
        /// </summary>
        private void MacroTypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // 매크로 타입 변경시 처리 로직 (현재는 기본 구현)
            _loggingService.LogDebug("매크로 타입 변경됨");
        }

        /// <summary>
        /// 콤보 스텝 추가 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void AddComboStepButton_Click(object sender, RoutedEventArgs e)
        {
            // 콤보 스텝 추가 로직
            _loggingService.LogDebug("콤보 스텝 추가 버튼 클릭");
        }

        /// <summary>
        /// 콤보 스텝 제거 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void RemoveComboStepButton_Click(object sender, RoutedEventArgs e)
        {
            // 콤보 스텝 제거 로직
            _loggingService.LogDebug("콤보 스텝 제거 버튼 클릭");
        }

        /// <summary>
        /// 콤보 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void TestComboButton_Click(object sender, RoutedEventArgs e)
        {
            // 콤보 테스트 로직
            _loggingService.LogDebug("콤보 테스트 버튼 클릭");
        }

        /// <summary>
        /// 연속 클릭 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void TestRapidButton_Click(object sender, RoutedEventArgs e)
        {
            // 연속 클릭 테스트 로직
            _loggingService.LogDebug("연속 클릭 테스트 버튼 클릭");
        }

        /// <summary>
        /// 홀드 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void TestHoldButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("홀드 테스트 버튼 클릭");
        }

        /// <summary>
        /// 토글 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void TestToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("토글 테스트 버튼 클릭");
        }

        /// <summary>
        /// 반복 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void TestRepeatButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("반복 테스트 버튼 클릭");
        }

        /// <summary>
        /// 매크로 설정 저장 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void SaveMacroSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("매크로 설정 저장 버튼 클릭");
        }

        /// <summary>
        /// 매크로 설정 취소 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void CancelMacroSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("매크로 설정 취소 버튼 클릭");
        }

        /// <summary>
        /// 매크로 미리보기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void PreviewMacroButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("매크로 미리보기 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 검색 텍스트박스 텍스트 변경 이벤트 핸들러
        /// </summary>
        private void PresetSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 검색 텍스트 변경");
        }

        /// <summary>
        /// 프리셋 검색 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void PresetSearchButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 검색 버튼 클릭");
        }

        /// <summary>
        /// 즐겨찾기만 보기 체크박스 변경 이벤트 핸들러
        /// </summary>
        private void FavoritesOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("즐겨찾기 전용 필터 변경");
        }

        /// <summary>
        /// 프리셋 새로고침 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void RefreshPresetsButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 새로고침 버튼 클릭");
        }

        /// <summary>
        /// 새 프리셋 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void NewPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("새 프리셋 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 편집 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void EditPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 편집 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 복사 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void CopyPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 복사 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 삭제 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 삭제 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 즐겨찾기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 즐겨찾기 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 가져오기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ImportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 가져오기 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 내보내기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ExportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 내보내기 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 적용 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ApplyPresetButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 적용 버튼 클릭");
        }

        /// <summary>
        /// 즐겨찾기 토글 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void ToggleFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("즐겨찾기 토글 버튼 클릭");
        }

        /// <summary>
        /// 프리셋 데이터그리드 선택 변경 이벤트 핸들러
        /// </summary>
        private void PresetDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _loggingService.LogDebug("프리셋 선택 변경됨");
        }

        /// <summary>
        /// 프리셋 데이터그리드 더블클릭 이벤트 핸들러
        /// </summary>
        private void PresetDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _loggingService.LogDebug("프리셋 더블클릭됨");
        }

        /// <summary>
        /// 빠른 적용 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void QuickApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _loggingService.LogDebug("빠른 적용 버튼 클릭");
        }

        /// <summary>
        /// 매크로 관련 데이터를 모든 관련 탭에서 새로고침하는 메서드
        /// </summary>
        private async Task RefreshMacroRelatedData()
        {
            try
            {
                _loggingService.LogDebug("매크로 관련 데이터 새로고침 시작");
                
                // 매크로 목록 새로고침
                await LoadMacros();
                
                // 현재 선택된 프리셋의 미리보기 갱신
                await UpdatePresetPreview();
                
                // 음성 인식 매칭 캐시 무효화 (새로운 매크로가 추가되었을 수 있음)
                try
                {
                    await InvalidateVoiceMatchingCache();
                }
                catch (Exception ex)
                {
                    _loggingService.LogWarning($"음성 인식 매칭 캐시 무효화 중 오류 발생: {ex.Message}");
                }
                
                _loggingService.LogDebug("매크로 관련 데이터 새로고침 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 관련 데이터 새로고침 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 언어 선택 콤보박스 변경 이벤트 핸들러
        /// </summary>
        private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loggingService == null) return;

            try
            {
                if (LanguageComboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    var language = selectedItem.Tag?.ToString() ?? "ko";
                    _loggingService.LogInfo($"언어가 {language}로 변경되었습니다");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"언어 변경 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 마이크 테스트 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void TestMicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loggingService == null) return;

            try
            {
                _loggingService.LogInfo("마이크 테스트를 시작합니다");
                MessageBox.Show("마이크 테스트가 시작되었습니다.\n5초간 소리를 내보세요.", 
                              "마이크 테스트", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 마이크 테스트 로직 (구현 필요)
                await Task.Delay(5000);
                
                MessageBox.Show("마이크 테스트가 완료되었습니다.", 
                              "마이크 테스트", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 테스트 실패: {ex.Message}");
                MessageBox.Show($"마이크 테스트 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 마이크 새로고침 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void RefreshMicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loggingService == null) return;

            try
            {
                await LoadMicrophoneDevices();
                _loggingService.LogInfo("마이크 디바이스 목록이 새로고침되었습니다");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 새로고침 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 매칭 결과 더블클릭 이벤트 핸들러
        /// </summary>
        private async void MatchedMacrosDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_loggingService == null) return;

            try
            {
                if (MatchedMacrosDataGrid?.SelectedItem is VoiceMatchResult selectedResult)
                {
                    await ExecuteMacroById(selectedResult.MacroId);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 실행 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 실행 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ExecuteMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loggingService == null) return;

            try
            {
                if (sender is Button button && button.Tag is int macroId)
                {
                    await ExecuteMacroById(macroId);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 실행 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 ID로 매크로 실행
        /// </summary>
        private async Task ExecuteMacroById(int macroId)
        {
            try
            {
                _loggingService.LogInfo($"매크로 실행 시작: ID {macroId}");
                
                var result = await _apiService.ExecuteMacroAsync(macroId);
                if (result)
                {
                    _loggingService.LogInfo($"매크로 실행 성공: ID {macroId}");
                    UpdateStatusText("매크로 실행 완료");
                }
                else
                {
                    _loggingService.LogError($"매크로 실행 실패: ID {macroId}");
                    UpdateStatusText("매크로 실행 실패");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 실행 중 오류: {ex.Message}");
                UpdateStatusText("매크로 실행 오류");
            }
        }

        /// <summary>
        /// 홀드 고정 지속시간 체크박스 체크 이벤트 핸들러
        /// </summary>
        private void HoldUseFixedDurationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_loggingService == null) return;
            _loggingService.LogDebug("홀드 고정 지속시간 체크됨");
        }

        /// <summary>
        /// 홀드 고정 지속시간 체크박스 체크 해제 이벤트 핸들러
        /// </summary>
        private void HoldUseFixedDurationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_loggingService == null) return;
            _loggingService.LogDebug("홀드 고정 지속시간 체크 해제됨");
        }
    }
} 