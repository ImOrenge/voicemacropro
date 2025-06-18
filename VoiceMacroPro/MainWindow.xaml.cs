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

        // ==================== 프리셋 관리 관련 필드 및 메서드 ====================
        private List<PresetModel> _allPresets = new List<PresetModel>();
        private PresetModel? _selectedPreset = null;
        private string _currentPresetSearchTerm = string.Empty;
        private bool _favoritesOnly = false;

        // ==================== 커스텀 스크립팅 관련 필드 ====================
        private List<CustomScript> _allCustomScripts = new List<CustomScript>();
        private CustomScript? _selectedCustomScript = null;
        private string _currentScriptSearchTerm = string.Empty;
        private string _currentScriptCategory = string.Empty;
        private string _currentScriptGame = string.Empty;
        private string _currentScriptSortBy = "name";
        private bool _isEditingScript = false;
        private int _editingScriptId = 0;

        // ==================== 대시보드 네비게이션 관련 필드 ====================
        private string _currentSection = "Dashboard";
        private DashboardView? _dashboardView = null;

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
                
                // 커스텀 스크립트 목록 로드
                await LoadCustomScripts();
                System.Diagnostics.Debug.WriteLine("커스텀 스크립트 로드 완료");
                
                // 음성 인식 UI 초기화 (UI 요소들이 모두 로드된 후)
                InitializeVoiceRecognitionUI();
                System.Diagnostics.Debug.WriteLine("음성 인식 UI 초기화 완료");
                
                // 대시보드 초기 로드
                NavigateToSection("Dashboard");
                System.Diagnostics.Debug.WriteLine("대시보드 초기 로드 완료");
                
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
        /// 커스텀 스크립트는 제외하고 기본 매크로만 로드합니다.
        /// </summary>
        private async Task LoadMacros()
        {
            try
            {
                // 서비스가 초기화되지 않은 경우 대기
                if (_apiService == null)
                    return;
                
                UpdateStatusText("매크로 목록 로딩 중...");
                
                // API를 통해 매크로 목록 조회
                var allMacros = await _apiService.GetMacrosAsync(_currentSearchTerm, _currentSortBy);
                
                // 커스텀 스크립트가 아닌 기본 매크로만 필터링
                _allMacros = allMacros.Where(m => !m.IsScript).ToList();
                
                // DataGrid에 바인딩
                if (MacroDataGrid != null)
                {
                    MacroDataGrid.ItemsSource = _allMacros;
                }
                
                _loggingService?.LogInfo($"기본 매크로 목록 로드 완료: {_allMacros.Count}개 항목 (전체 {allMacros.Count}개 중)");
                UpdateStatusText($"기본 매크로 {_allMacros.Count}개 로드 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 목록을 불러오는 중 오류가 발생했습니다:\n{ex.Message}", 
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("매크로 로드 실패");
                _loggingService?.LogError($"매크로 로드 실패: {ex.Message}");
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
            // 초기화가 완료되지 않은 상태에서는 이벤트를 무시
            if (_loggingService == null || _apiService == null)
                return;
                
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
        /// 매크로 DataGrid에서 선택이 변경될 때 실행되는 이벤트 핸들러
        /// 커스텀 스크립트는 수정 불가능하도록 처리합니다.
        /// </summary>
        private void MacroDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = MacroDataGrid.SelectedItem != null;
            
            if (hasSelection && MacroDataGrid.SelectedItem is Macro selectedMacro)
            {
                // 커스텀 스크립트인 경우 버튼 텍스트 변경
                bool isCustomScript = selectedMacro.IsScript;
                
                // 모든 버튼 활성화
                EditMacroButton.IsEnabled = true;
                CopyMacroButton.IsEnabled = true;
                DeleteMacroButton.IsEnabled = true;
                
                // 커스텀 스크립트 여부에 따라 버튼 텍스트 및 상태 메시지 변경
                if (isCustomScript)
                {
                    EditMacroButton.Content = "🔧 스크립트 편집";
                    UpdateStatusText($"커스텀 스크립트 선택됨: {selectedMacro.Name} (스크립트 편집 버튼 클릭 시 커스텀 스크립팅 탭으로 이동)");
                }
                else
                {
                    EditMacroButton.Content = "✏️ 수정";
                    UpdateStatusText($"일반 매크로 선택됨: {selectedMacro.Name}");
                }
            }
            else
            {
                // 선택된 항목이 없을 때 모든 버튼 비활성화 및 기본 텍스트 복원
                EditMacroButton.IsEnabled = false;
                EditMacroButton.Content = "✏️ 수정";
                CopyMacroButton.IsEnabled = false;
                DeleteMacroButton.IsEnabled = false;
                UpdateStatusText("준비");
            }
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
        /// 일반 매크로인 경우 수정 다이얼로그를 표시하고,
        /// 커스텀 스크립트인 경우 커스텀 스크립팅 탭으로 이동하여 해당 스크립트를 로드합니다.
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

                // 커스텀 스크립트인 경우 커스텀 스크립팅 탭으로 이동
                if (selectedMacro.IsScript)
                {
                    _loggingService.LogInfo($"커스텀 스크립트 편집 요청: ID {selectedMacro.Id}, 이름 '{selectedMacro.Name}'");
                    
                    // 커스텀 스크립팅 탭으로 전환
                    if (this.FindName("MainTabControl") is TabControl mainTabControl)
                    {
                        // 커스텀 스크립팅 탭 찾기 (인덱스 1)
                        mainTabControl.SelectedIndex = 1;
                        
                        // 해당 매크로의 커스텀 스크립트 정보 로드
                        await LoadCustomScriptByMacroId(selectedMacro.Id);
                        
                        UpdateStatusText($"커스텀 스크립트 편집 모드: {selectedMacro.Name}");
                        
                        MessageBox.Show($"'{selectedMacro.Name}' 커스텀 스크립트를 편집할 수 있도록\n커스텀 스크립팅 탭으로 이동했습니다.", 
                                      "커스텀 스크립트 편집", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                    return;
                }

                // 일반 매크로인 경우 기존 편집 다이얼로그 표시
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
            // 초기화 중에는 무시
            if (_loggingService == null)
                return;

            try
            {
                if (LogLevelComboBox?.SelectedItem is ComboBoxItem selectedItem)
                {
                    var selectedLevel = selectedItem.Tag?.ToString() ?? "Info";
                    _loggingService.SetMinimumLevel(selectedLevel);
                    _loggingService.LogInfo($"로그 레벨이 {selectedLevel}로 변경되었습니다");
                    
                    // 필터 적용
                    ApplyLogFilter();
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"로그 레벨 변경 실패: {ex.Message}");
            }
        }

        private void LogFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 초기화 중에는 무시
            if (_loggingService == null)
                return;

            try
            {
                ApplyLogFilter();
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"로그 필터링 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 로그 필터를 적용하는 메서드
        /// </summary>
        private void ApplyLogFilter()
        {
            try
            {
                if (LogDataGrid?.ItemsSource is not ObservableCollection<LogEntry> logEntries)
                    return;

                var filterText = LogFilterTextBox?.Text?.Trim().ToLower() ?? "";
                var currentLevel = (LogLevelComboBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Info";

                // 컬렉션 뷰를 사용하여 필터링
                var view = CollectionViewSource.GetDefaultView(logEntries);
                if (view != null)
                {
                    view.Filter = obj =>
                    {
                        if (obj is not LogEntry log)
                            return false;

                        // 레벨 필터 적용
                        if (!PassesLevelFilter(log.LevelText, currentLevel))
                            return false;

                        // 텍스트 필터 적용
                        if (!string.IsNullOrEmpty(filterText))
                        {
                            return log.Message.ToLower().Contains(filterText) ||
                                   log.LevelText.ToLower().Contains(filterText);
                        }

                        return true;
                    };
                    view.Refresh();
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"로그 필터 적용 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 로그 레벨 필터를 확인하는 메서드
        /// </summary>
        private static bool PassesLevelFilter(string logLevel, string minimumLevel)
        {
            var levels = new Dictionary<string, int>
            {
                { "Debug", 0 },
                { "Info", 1 },
                { "Warning", 2 },
                { "Error", 3 }
            };

            if (!levels.TryGetValue(logLevel, out int logLevelValue))
                logLevelValue = 1; // 기본값은 Info

            if (!levels.TryGetValue(minimumLevel, out int minLevelValue))
                minLevelValue = 1; // 기본값은 Info

            return logLevelValue >= minLevelValue;
        }

        private void AutoScrollCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // 초기화 중에는 무시
            if (_loggingService == null)
                return;

            _loggingService.SetAutoScroll(true);
            
            // 체크된 즉시 마지막 로그로 스크롤
            try
            {
                if (LogDataGrid != null && _loggingService.LogEntries.Count > 0)
                {
                    LogDataGrid.ScrollIntoView(_loggingService.LogEntries.Last());
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogWarning($"자동 스크롤 활성화 실패: {ex.Message}");
            }
        }

        private void AutoScrollCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // 초기화 중에는 무시
            if (_loggingService == null)
                return;

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
            // 초기화 중에는 무시
            if (_loggingService == null)
                return;

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
                _loggingService?.LogError($"마이크 변경 실패: {ex.Message}");
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

        // ==================== 커스텀 스크립팅 탭 관련 메서드들 ====================

        /// <summary>
        /// 커스텀 스크립트 목록을 서버에서 불러와 DataGrid에 표시하는 함수
        /// </summary>
        private async Task LoadCustomScripts()
        {
            try
            {
                UpdateStatusText("커스텀 스크립트 목록 로딩 중...");

                // API를 통해 커스텀 스크립트 목록 조회
                _allCustomScripts = await _apiService.GetCustomScriptsAsync(
                    _currentScriptSearchTerm, 
                    _currentScriptCategory, 
                    _currentScriptGame, 
                    _currentScriptSortBy);

                // DataGrid에 바인딩
                if (CustomScriptDataGrid != null)
                {
                    CustomScriptDataGrid.ItemsSource = _allCustomScripts;
                }

                // 스크립트 개수 표시 업데이트
                if (ScriptCountTextBlock != null)
                {
                    ScriptCountTextBlock.Text = $"스크립트: {_allCustomScripts.Count}개";
                }

                _loggingService.LogInfo($"커스텀 스크립트 목록 로드 완료: {_allCustomScripts.Count}개 항목");
                UpdateStatusText($"커스텀 스크립트 {_allCustomScripts.Count}개 로드 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"커스텀 스크립트 목록을 불러오는 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusText("커스텀 스크립트 로드 실패");
                _loggingService.LogError($"커스텀 스크립트 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 커스텀 스크립트 검색/필터링 및 새로고침 버튼 클릭 이벤트
        /// </summary>
        private async void RefreshScriptsButton_Click(object sender, RoutedEventArgs e)
        {
            // 검색어 업데이트
            _currentScriptSearchTerm = ScriptSearchTextBox?.Text?.Trim() ?? string.Empty;

            // 카테고리 필터 업데이트
            if (ScriptCategoryComboBox?.SelectedItem is ComboBoxItem categoryItem)
            {
                _currentScriptCategory = categoryItem.Tag?.ToString() ?? string.Empty;
            }

            // 게임 필터 업데이트
            if (ScriptGameComboBox?.SelectedItem is ComboBoxItem gameItem)
            {
                _currentScriptGame = gameItem.Tag?.ToString() ?? string.Empty;
            }

            // 정렬 기준 업데이트
            if (ScriptSortComboBox?.SelectedItem is ComboBoxItem sortItem)
            {
                _currentScriptSortBy = sortItem.Tag?.ToString() ?? "name";
            }

            await LoadCustomScripts();
        }

        /// <summary>
        /// 매크로 이름으로 커스텀 스크립트를 찾아서 에디터에 로드하는 메서드
        /// 매크로 관리 탭에서 커스텀 스크립트 편집 버튼을 클릭했을 때 호출됩니다.
        /// 커스텀 스크립트의 이름이 매크로 이름과 일치하는 것을 찾습니다.
        /// </summary>
        /// <param name="macroId">참고용 매크로 ID</param>
        private async Task LoadCustomScriptByMacroId(int macroId)
        {
            try
            {
                // 선택된 매크로 정보 가져오기
                var selectedMacro = MacroDataGrid?.SelectedItem as Macro;
                if (selectedMacro == null)
                {
                    _loggingService.LogWarning("선택된 매크로가 없어서 커스텀 스크립트를 찾을 수 없음");
                    return;
                }

                _loggingService.LogDebug($"매크로 '{selectedMacro.Name}' (ID: {macroId})에 해당하는 커스텀 스크립트 검색 시작");
                
                // 현재 로드된 스크립트 목록에서 매크로 이름과 일치하는 스크립트 찾기
                var targetScript = _allCustomScripts.FirstOrDefault(s => 
                    s.Name.Equals(selectedMacro.Name, StringComparison.OrdinalIgnoreCase));
                
                // 로드된 목록에 없으면 서버에서 다시 가져오기
                if (targetScript == null)
                {
                    await LoadCustomScripts();
                    targetScript = _allCustomScripts.FirstOrDefault(s => 
                        s.Name.Equals(selectedMacro.Name, StringComparison.OrdinalIgnoreCase));
                }
                
                if (targetScript != null)
                {
                    // 스크립트 목록에서 해당 스크립트 선택
                    Dispatcher.Invoke(() =>
                    {
                        if (CustomScriptDataGrid != null)
                        {
                            CustomScriptDataGrid.SelectedItem = targetScript;
                            CustomScriptDataGrid.ScrollIntoView(targetScript);
                        }
                    });
                    
                    // 에디터에 스크립트 로드
                    LoadScriptToEditor(targetScript);
                    
                    _loggingService.LogInfo($"매크로 '{selectedMacro.Name}'에 해당하는 커스텀 스크립트 '{targetScript.Name}' 로드 완료");
                }
                else
                {
                    _loggingService.LogWarning($"매크로 '{selectedMacro.Name}'과 일치하는 커스텀 스크립트를 찾을 수 없음");
                    
                    // 스크립트를 찾을 수 없는 경우 에디터 초기화
                    ClearScriptEditor();
                    
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"'{selectedMacro.Name}' 매크로에 해당하는 커스텀 스크립트를 찾을 수 없습니다.\n\n" +
                                      "• 커스텀 스크립트 이름이 매크로 이름과 정확히 일치해야 합니다.\n" +
                                      "• 스크립트가 삭제되었거나 아직 생성되지 않았을 수 있습니다.\n\n" +
                                      "커스텀 스크립팅 탭에서 새로운 스크립트를 생성하거나 기존 스크립트를 수정하세요.", 
                                      "커스텀 스크립트 없음", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 ID {macroId}에 대한 커스텀 스크립트 로드 실패: {ex.Message}");
                
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"커스텀 스크립트 로드 중 오류가 발생했습니다:\n{ex.Message}", 
                                  "오류", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// 새 스크립트 버튼 클릭 이벤트
        /// 새 커스텀 스크립트 작성 모드로 전환
        /// </summary>
        private void NewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 편집 모드 초기화
                _isEditingScript = false;
                _editingScriptId = 0;
                _selectedCustomScript = null;

                // 에디터 타이틀 변경
                if (ScriptEditorTitle != null)
                {
                    ScriptEditorTitle.Text = "🔧 새 MSL 스크립트 작성";
                }

                // 입력 필드 초기화
                ClearScriptEditor();

                // 상태 업데이트
                if (ScriptStatusTextBlock != null)
                {
                    ScriptStatusTextBlock.Text = "새 스크립트 작성 모드";
                }

                _loggingService.LogInfo("새 커스텀 스크립트 작성 모드로 전환되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"새 스크립트 모드 전환 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"새 스크립트 모드 전환 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 목록에서 선택이 변경될 때 실행되는 이벤트 핸들러
        /// </summary>
        private void CustomScriptDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CustomScriptDataGrid.SelectedItem is CustomScript selectedScript)
                {
                    _selectedCustomScript = selectedScript;

                    // 선택된 스크립트 정보를 에디터에 로드
                    LoadScriptToEditor(selectedScript);

                    // 버튼 활성화
                    EnableScriptButtons(true);

                    // 상태 업데이트
                    if (ScriptStatusTextBlock != null)
                    {
                        ScriptStatusTextBlock.Text = $"선택됨: {selectedScript.Name}";
                    }
                }
                else
                {
                    _selectedCustomScript = null;
                    EnableScriptButtons(false);

                    if (ScriptStatusTextBlock != null)
                    {
                        ScriptStatusTextBlock.Text = "준비됨";
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 선택 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택된 스크립트를 에디터에 로드하는 함수
        /// </summary>
        private void LoadScriptToEditor(CustomScript script)
        {
            try
            {
                _isEditingScript = true;
                _editingScriptId = script.Id;

                // 에디터 타이틀 변경
                if (ScriptEditorTitle != null)
                {
                    ScriptEditorTitle.Text = $"🔧 MSL 스크립트 에디터 - {script.Name}";
                }

                // 스크립트 정보 입력 필드 채우기
                if (ScriptNameTextBox != null)
                {
                    ScriptNameTextBox.Text = script.Name;
                }

                if (ScriptDescriptionTextBox != null)
                {
                    ScriptDescriptionTextBox.Text = script.Description;
                }

                if (ScriptCategoryEditComboBox != null)
                {
                    ScriptCategoryEditComboBox.Text = script.Category;
                }

                if (ScriptGameEditComboBox != null)
                {
                    ScriptGameEditComboBox.Text = script.GameTarget;
                }

                if (ScriptCodeTextBox != null)
                {
                    ScriptCodeTextBox.Text = script.ScriptCode;
                }

                // 검증 결과 및 로그 초기화
                if (ValidationResultTextBlock != null)
                {
                    ValidationResultTextBlock.Text = "스크립트가 로드되었습니다. 검증 버튼을 클릭하여 확인하세요.";
                    ValidationResultTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
                }

                if (ExecutionLogTextBlock != null)
                {
                    ExecutionLogTextBlock.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스크립트를 에디터에 로드하는 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 에디터 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 에디터 필드를 초기화하는 함수
        /// </summary>
        private void ClearScriptEditor()
        {
            try
            {
                if (ScriptNameTextBox != null)
                {
                    ScriptNameTextBox.Text = "";
                }

                if (ScriptDescriptionTextBox != null)
                {
                    ScriptDescriptionTextBox.Text = "";
                }

                if (ScriptCategoryEditComboBox != null)
                {
                    ScriptCategoryEditComboBox.SelectedIndex = 0;
                }

                if (ScriptGameEditComboBox != null)
                {
                    ScriptGameEditComboBox.SelectedIndex = 0;
                }

                if (ScriptCodeTextBox != null)
                {
                    ScriptCodeTextBox.Text = @"// MSL 스크립트를 여기에 작성하세요
// 예시:
action ""기본 공격"" {
    press Q
    wait 100ms
    press W
}";
                }

                if (ValidationResultTextBlock != null)
                {
                    ValidationResultTextBlock.Text = "스크립트를 작성하고 검증 버튼을 클릭하세요.";
                    ValidationResultTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                }

                if (ExecutionLogTextBlock != null)
                {
                    ExecutionLogTextBlock.Text = "스크립트 실행 로그가 여기에 표시됩니다.";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 에디터 초기화 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 관련 버튼들의 활성화 상태를 설정하는 함수
        /// </summary>
        private void EnableScriptButtons(bool enabled)
        {
            try
            {
                if (EditScriptButton != null)
                {
                    EditScriptButton.IsEnabled = enabled;
                }

                if (CopyScriptButton != null)
                {
                    CopyScriptButton.IsEnabled = enabled;
                }

                if (DeleteScriptButton != null)
                {
                    DeleteScriptButton.IsEnabled = enabled;
                }

                if (ExecuteScriptButton != null)
                {
                    ExecuteScriptButton.IsEnabled = enabled;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 버튼 상태 설정 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 검증 버튼 클릭 이벤트
        /// MSL 스크립트 코드를 서버에서 검증
        /// </summary>
        private async void ValidateScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ScriptCodeTextBox == null)
                {
                    return;
                }

                string scriptCode = ScriptCodeTextBox.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(scriptCode))
                {
                                         if (ValidationResultTextBlock != null)
                     {
                         ValidationResultTextBlock.Text = "스크립트 코드를 입력해주세요.";
                         ValidationResultTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                     }
                    return;
                }

                // 버튼 비활성화 및 상태 표시
                ValidateScriptButton.IsEnabled = false;
                if (ScriptStatusTextBlock != null)
                {
                    ScriptStatusTextBlock.Text = "스크립트 검증 중...";
                }

                if (ValidationResultTextBlock != null)
                {
                    ValidationResultTextBlock.Text = "검증 중... 잠시만 기다려주세요.";
                    ValidationResultTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
                }

                // 서버에 검증 요청
                var validationResult = await _apiService.ValidateScriptAsync(scriptCode);

                if (validationResult != null)
                {
                    // 검증 결과 표시
                    DisplayValidationResult(validationResult);
                    _loggingService.LogInfo($"스크립트 검증 완료: {validationResult.StatusText}");
                }
                else
                {
                    if (ValidationResultTextBlock != null)
                    {
                        ValidationResultTextBlock.Text = "검증 결과를 받을 수 없습니다. 서버 연결을 확인해주세요.";
                        ValidationResultTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ValidationResultTextBlock != null)
                {
                    ValidationResultTextBlock.Text = $"검증 중 오류 발생: {ex.Message}";
                    ValidationResultTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }

                MessageBox.Show($"스크립트 검증 중 오류가 발생했습니다:\n{ex.Message}",
                              "검증 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 검증 오류: {ex.Message}");
            }
            finally
            {
                // 버튼 다시 활성화
                ValidateScriptButton.IsEnabled = true;
                if (ScriptStatusTextBlock != null)
                {
                    ScriptStatusTextBlock.Text = _isEditingScript ? $"편집 중: {_selectedCustomScript?.Name}" : "준비됨";
                }
            }
        }

        /// <summary>
        /// 검증 결과를 UI에 표시하는 함수
        /// </summary>
        private void DisplayValidationResult(ScriptValidationResult result)
        {
            try
            {
                if (ValidationResultTextBlock == null)
                {
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"검증 결과: {result.StatusText}");
                sb.AppendLine($"검증 시간: {result.ValidationTimeMs:F1}ms");
                sb.AppendLine();

                if (result.IsValid)
                {
                    sb.AppendLine("✅ 스크립트가 유효합니다!");
                    ValidationResultTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    sb.AppendLine("❌ 스크립트에 문제가 있습니다:");
                    ValidationResultTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }

                // 오류 표시
                if (result.Errors != null && result.Errors.Count > 0)
                {
                    sb.AppendLine("\n🚫 오류:");
                    foreach (var error in result.Errors)
                    {
                        sb.AppendLine($"  • {error}");
                    }
                }

                // 경고 표시
                if (result.Warnings != null && result.Warnings.Count > 0)
                {
                    sb.AppendLine("\n⚠️ 경고:");
                    foreach (var warning in result.Warnings)
                    {
                        sb.AppendLine($"  • {warning}");
                    }
                }

                ValidationResultTextBlock.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"검증 결과 표시 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 저장 버튼 클릭 이벤트
        /// 새 스크립트 생성 또는 기존 스크립트 수정
        /// </summary>
        private async void SaveScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 입력 검증
                if (!ValidateScriptInput())
                {
                    return;
                }

                // 스크립트 객체 생성
                var script = CreateScriptFromEditor();
                if (script == null)
                {
                    return;
                }

                // 버튼 비활성화 및 상태 표시
                SaveScriptButton.IsEnabled = false;
                if (ScriptStatusTextBlock != null)
                {
                    ScriptStatusTextBlock.Text = _isEditingScript ? "스크립트 수정 중..." : "스크립트 저장 중...";
                }

                if (_isEditingScript && _editingScriptId > 0)
                {
                    // 기존 스크립트 수정
                    script.Id = _editingScriptId;
                    bool success = await _apiService.UpdateCustomScriptAsync(script);

                    if (success)
                    {
                        MessageBox.Show("스크립트가 성공적으로 수정되었습니다!", "저장 완료", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        _loggingService.LogInfo($"커스텀 스크립트 수정 완료: {script.Name}");
                    }
                    else
                    {
                        MessageBox.Show("스크립트 수정에 실패했습니다.", "저장 실패", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // 새 스크립트 생성
                    int newId = await _apiService.CreateCustomScriptAsync(script);

                    if (newId > 0)
                    {
                        MessageBox.Show($"새 스크립트가 성공적으로 생성되었습니다! (ID: {newId})", "저장 완료", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        _loggingService.LogInfo($"새 커스텀 스크립트 생성 완료: {script.Name} (ID: {newId})");

                        // 편집 모드로 전환
                        _isEditingScript = true;
                        _editingScriptId = newId;
                    }
                    else
                    {
                        MessageBox.Show("스크립트 생성에 실패했습니다.", "저장 실패", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // 스크립트 목록 새로고침
                await LoadCustomScripts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스크립트 저장 중 오류가 발생했습니다:\n{ex.Message}",
                              "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 저장 오류: {ex.Message}");
            }
            finally
            {
                // 버튼 다시 활성화
                SaveScriptButton.IsEnabled = true;
                if (ScriptStatusTextBlock != null)
                {
                    ScriptStatusTextBlock.Text = _isEditingScript ? $"편집 중: {ScriptNameTextBox?.Text}" : "준비됨";
                }
            }
        }

        /// <summary>
        /// 스크립트 입력 값들을 검증하는 함수
        /// </summary>
        private bool ValidateScriptInput()
        {
            try
            {
                // 이름 검증
                if (ScriptNameTextBox == null || string.IsNullOrWhiteSpace(ScriptNameTextBox.Text))
                {
                    MessageBox.Show("스크립트 이름을 입력해주세요.", "입력 오류", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    ScriptNameTextBox?.Focus();
                    return false;
                }

                // 코드 검증
                if (ScriptCodeTextBox == null || string.IsNullOrWhiteSpace(ScriptCodeTextBox.Text))
                {
                    MessageBox.Show("스크립트 코드를 입력해주세요.", "입력 오류", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    ScriptCodeTextBox?.Focus();
                    return false;
                }

                // 카테고리 검증
                if (ScriptCategoryEditComboBox == null || string.IsNullOrWhiteSpace(ScriptCategoryEditComboBox.Text))
                {
                    MessageBox.Show("카테고리를 선택하거나 입력해주세요.", "입력 오류", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    ScriptCategoryEditComboBox?.Focus();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 입력 검증 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 에디터의 입력값들로부터 CustomScript 객체를 생성하는 함수
        /// </summary>
        private CustomScript? CreateScriptFromEditor()
        {
            try
            {
                return new CustomScript
                {
                    Name = ScriptNameTextBox?.Text?.Trim() ?? "",
                    Description = ScriptDescriptionTextBox?.Text?.Trim() ?? "",
                    ScriptCode = ScriptCodeTextBox?.Text?.Trim() ?? "",
                    Category = ScriptCategoryEditComboBox?.Text?.Trim() ?? "",
                    GameTarget = ScriptGameEditComboBox?.Text?.Trim() ?? "",
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스크립트 객체 생성 중 오류가 발생했습니다:\n{ex.Message}",
                              "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 객체 생성 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 스크립트 수정 버튼 클릭 이벤트
        /// </summary>
        private void EditScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomScript == null)
            {
                MessageBox.Show("수정할 스크립트를 선택해주세요.", "선택 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 이미 에디터에 로드되어 있으므로 별도 작업 불필요
            MessageBox.Show("선택한 스크립트가 에디터에 로드되었습니다. 수정 후 저장 버튼을 클릭하세요.", 
                          "편집 모드", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 스크립트 복사 버튼 클릭 이벤트
        /// </summary>
        private void CopyScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomScript == null)
            {
                MessageBox.Show("복사할 스크립트를 선택해주세요.", "선택 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 선택된 스크립트를 에디터에 로드 (복사본으로)
                LoadScriptToEditor(_selectedCustomScript);

                // 새 스크립트 모드로 전환 (ID 초기화)
                _isEditingScript = false;
                _editingScriptId = 0;

                // 이름에 "복사본" 추가
                if (ScriptNameTextBox != null)
                {
                    ScriptNameTextBox.Text = $"{_selectedCustomScript.Name} - 복사본";
                }

                // 에디터 타이틀 변경
                if (ScriptEditorTitle != null)
                {
                    ScriptEditorTitle.Text = "🔧 MSL 스크립트 에디터 - 복사본 작성";
                }

                MessageBox.Show("스크립트가 복사되었습니다. 필요에 따라 수정 후 저장하세요.", 
                              "복사 완료", MessageBoxButton.OK, MessageBoxImage.Information);

                _loggingService.LogInfo($"스크립트 복사: {_selectedCustomScript.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스크립트 복사 중 오류가 발생했습니다:\n{ex.Message}",
                              "복사 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 복사 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 삭제 버튼 클릭 이벤트
        /// </summary>
        private async void DeleteScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomScript == null)
            {
                MessageBox.Show("삭제할 스크립트를 선택해주세요.", "선택 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 삭제 확인
                var result = MessageBox.Show(
                    $"정말로 '{_selectedCustomScript.Name}' 스크립트를 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.",
                    "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // 삭제 실행
                bool success = await _apiService.DeleteCustomScriptAsync(_selectedCustomScript.Id);

                if (success)
                {
                    MessageBox.Show("스크립트가 성공적으로 삭제되었습니다.", "삭제 완료", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    _loggingService.LogInfo($"커스텀 스크립트 삭제 완료: {_selectedCustomScript.Name}");

                    // 에디터 초기화
                    ClearScriptEditor();
                    _isEditingScript = false;
                    _editingScriptId = 0;
                    _selectedCustomScript = null;

                    // 에디터 타이틀 변경
                    if (ScriptEditorTitle != null)
                    {
                        ScriptEditorTitle.Text = "🔧 MSL 스크립트 에디터";
                    }

                    // 버튼 비활성화
                    EnableScriptButtons(false);

                    // 스크립트 목록 새로고침
                    await LoadCustomScripts();
                }
                else
                {
                    MessageBox.Show("스크립트 삭제에 실패했습니다.", "삭제 실패", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스크립트 삭제 중 오류가 발생했습니다:\n{ex.Message}",
                              "삭제 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 삭제 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 실행 버튼 클릭 이벤트
        /// </summary>
        private async void ExecuteScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomScript == null)
            {
                MessageBox.Show("실행할 스크립트를 선택해주세요.", "선택 오류", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 버튼 비활성화 및 상태 표시
                ExecuteScriptButton.IsEnabled = false;
                if (ScriptStatusTextBlock != null)
                {
                    ScriptStatusTextBlock.Text = $"실행 중: {_selectedCustomScript.Name}";
                }

                if (ExecutionLogTextBlock != null)
                {
                    ExecutionLogTextBlock.Text = "스크립트 실행 중... 잠시만 기다려주세요.";
                    ExecutionLogTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
                }

                // 스크립트 실행
                bool success = await _apiService.ExecuteCustomScriptAsync(_selectedCustomScript.Id);

                if (success)
                {
                    if (ExecutionLogTextBlock != null)
                    {
                        ExecutionLogTextBlock.Text = $"✅ 스크립트 실행 완료!\n실행 시간: {DateTime.Now:HH:mm:ss}\n" +
                                                   $"스크립트: {_selectedCustomScript.Name}";
                        ExecutionLogTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }

                    _loggingService.LogInfo($"커스텀 스크립트 실행 완료: {_selectedCustomScript.Name}");
                }
                else
                {
                    if (ExecutionLogTextBlock != null)
                    {
                        ExecutionLogTextBlock.Text = $"❌ 스크립트 실행 실패\n실행 시간: {DateTime.Now:HH:mm:ss}\n" +
                                                   $"스크립트: {_selectedCustomScript.Name}\n오류: 서버에서 실행을 거부했습니다.";
                        ExecutionLogTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    }

                    MessageBox.Show("스크립트 실행에 실패했습니다.", "실행 실패", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // 스크립트 목록 새로고침 (사용 횟수 업데이트 등)
                await LoadCustomScripts();
            }
            catch (Exception ex)
            {
                if (ExecutionLogTextBlock != null)
                {
                    ExecutionLogTextBlock.Text = $"❌ 스크립트 실행 오류\n실행 시간: {DateTime.Now:HH:mm:ss}\n" +
                                               $"오류 메시지: {ex.Message}";
                    ExecutionLogTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }

                MessageBox.Show($"스크립트 실행 중 오류가 발생했습니다:\n{ex.Message}",
                              "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 실행 오류: {ex.Message}");
            }
            finally
            {
                // 버튼 다시 활성화
                ExecuteScriptButton.IsEnabled = true;
                if (ScriptStatusTextBlock != null)
                {
                    ScriptStatusTextBlock.Text = _selectedCustomScript != null ? $"선택됨: {_selectedCustomScript.Name}" : "준비됨";
                }
            }
        }

        /// <summary>
        /// 스크립트 템플릿 보기 버튼 클릭 이벤트
        /// </summary>
        private async void ScriptTemplatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 템플릿 목록 조회
                var templates = await _apiService.GetScriptTemplatesAsync();

                if (templates == null || templates.Count == 0)
                {
                    MessageBox.Show("사용 가능한 스크립트 템플릿이 없습니다.", "템플릿 없음", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 템플릿 선택 대화상자 (간단한 구현)
                var templateNames = templates.Select(t => $"{t.Name} ({t.Category} - {t.DifficultyLevel})").ToArray();
                
                // TODO: 더 sophisticated한 템플릿 선택 윈도우 구현
                MessageBox.Show($"사용 가능한 템플릿 {templates.Count}개:\n\n" + 
                              string.Join("\n", templateNames.Take(10)) + 
                              (templateNames.Length > 10 ? "\n... 등" : ""), 
                              "스크립트 템플릿", MessageBoxButton.OK, MessageBoxImage.Information);

                _loggingService.LogInfo($"스크립트 템플릿 조회: {templates.Count}개 발견");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 조회 중 오류가 발생했습니다:\n{ex.Message}",
                              "템플릿 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService.LogError($"스크립트 템플릿 조회 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스크립트 코드 텍스트 변경 이벤트
        /// 실시간으로 간단한 구문 확인 가능
        /// </summary>
        private void ScriptCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: 실시간 문법 하이라이팅 구현 (향후 개선사항)
            // 현재는 단순히 변경을 감지만 함
        }

        // ==================== 대시보드 네비게이션 메서드들 ====================

        /// <summary>
        /// 사이드바 메뉴 버튼 클릭 시 해당 섹션으로 네비게이션하는 메서드
        /// </summary>
        private void NavigateToSection(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button)
                {
                    string section = button.Tag?.ToString() ?? "Dashboard";
                    NavigateToSection(section);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"네비게이션 오류: {ex.Message}");
                MessageBox.Show($"페이지 이동 중 오류가 발생했습니다: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 지정된 섹션으로 네비게이션하는 메서드
        /// </summary>
        /// <param name="section">이동할 섹션 이름</param>
        private void NavigateToSection(string section)
        {
            try
            {
                _currentSection = section;
                
                // 페이지 제목과 경로 업데이트
                UpdatePageHeader(section);
                
                // 메인 콘텐츠 영역에 해당 뷰 로드
                LoadSectionContent(section);
                
                // 사이드바 메뉴 활성 상태 업데이트
                UpdateSidebarSelection(section);
                
                _loggingService.LogInfo($"페이지 이동: {section}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"섹션 네비게이션 오류: {ex.Message}");
                MessageBox.Show($"페이지 로드 중 오류가 발생했습니다: {ex.Message}", "오류", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 페이지 헤더 정보를 업데이트하는 메서드
        /// </summary>
        private void UpdatePageHeader(string section)
        {
            if (PageTitleText != null && PageBreadcrumbText != null)
            {
                switch (section)
                {
                    case "Dashboard":
                        PageTitleText.Text = "대시보드";
                        PageBreadcrumbText.Text = "홈 > 대시보드";
                        break;
                    case "MacroManagement":
                        PageTitleText.Text = "매크로 관리";
                        PageBreadcrumbText.Text = "홈 > 매크로 관리";
                        break;
                    case "CustomScripting":
                        PageTitleText.Text = "커스텀 스크립팅";
                        PageBreadcrumbText.Text = "홈 > 커스텀 스크립팅";
                        break;
                    case "VoiceRecognition":
                        PageTitleText.Text = "음성 인식";
                        PageBreadcrumbText.Text = "홈 > 음성 인식";
                        break;
                    case "LogMonitoring":
                        PageTitleText.Text = "로그 및 모니터링";
                        PageBreadcrumbText.Text = "홈 > 로그 및 모니터링";
                        break;
                    case "PresetManagement":
                        PageTitleText.Text = "프리셋 관리";
                        PageBreadcrumbText.Text = "홈 > 프리셋 관리";
                        break;
                    case "DeveloperInfo":
                        PageTitleText.Text = "개발자 정보";
                        PageBreadcrumbText.Text = "홈 > 개발자 정보";
                        break;
                    default:
                        PageTitleText.Text = "대시보드";
                        PageBreadcrumbText.Text = "홈 > 대시보드";
                        break;
                }
            }
        }

        /// <summary>
        /// 선택된 섹션에 해당하는 콘텐츠를 로드하는 메서드
        /// </summary>
        private void LoadSectionContent(string section)
        {
            if (MainContentPresenter == null) return;

            try
            {
                switch (section)
                {
                    case "Dashboard":
                        if (_dashboardView == null)
                        {
                            _dashboardView = new DashboardView();
                        }
                        else
                        {
                            _dashboardView.RefreshDashboard();
                        }
                        MainContentPresenter.Content = _dashboardView;
                        break;
                        
                    case "MacroManagement":
                        // 기존 매크로 관리 UI를 UserControl로 만들어야 함 (추후 구현)
                        MainContentPresenter.Content = CreatePlaceholderContent("매크로 관리", "📋", "매크로 CRUD 기능이 여기에 표시됩니다.");
                        break;
                        
                    case "CustomScripting":
                        // 기존 커스텀 스크립팅 UI를 UserControl로 만들어야 함 (추후 구현)
                        MainContentPresenter.Content = CreatePlaceholderContent("커스텀 스크립팅", "🔧", "MSL 스크립트 에디터가 여기에 표시됩니다.");
                        break;
                        
                    case "VoiceRecognition":
                        // 기존 음성 인식 UI를 UserControl로 만들어야 함 (추후 구현)
                        MainContentPresenter.Content = CreatePlaceholderContent("음성 인식", "🎤", "음성 인식 및 매크로 매칭 기능이 여기에 표시됩니다.");
                        break;
                        
                    case "LogMonitoring":
                        // 기존 로그 UI를 UserControl로 만들어야 함 (추후 구현)
                        MainContentPresenter.Content = CreatePlaceholderContent("로그 및 모니터링", "📊", "실시간 로그 및 시스템 모니터링이 여기에 표시됩니다.");
                        break;
                        
                    case "PresetManagement":
                        // 기존 프리셋 관리 UI를 UserControl로 만들어야 함 (추후 구현)
                        MainContentPresenter.Content = CreatePlaceholderContent("프리셋 관리", "📁", "프리셋 관리 기능이 여기에 표시됩니다.");
                        break;
                        
                    case "DeveloperInfo":
                        // 기존 개발자 정보 UI를 UserControl로 만들어야 함 (추후 구현)
                        MainContentPresenter.Content = CreatePlaceholderContent("개발자 정보", "💻", "개발자 정보 및 라이선스가 여기에 표시됩니다.");
                        break;
                        
                    default:
                        if (_dashboardView == null)
                        {
                            _dashboardView = new DashboardView();
                        }
                        MainContentPresenter.Content = _dashboardView;
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"콘텐츠 로드 오류 ({section}): {ex.Message}");
                MainContentPresenter.Content = CreateErrorContent(section, ex.Message);
            }
        }

        /// <summary>
        /// 임시 플레이스홀더 콘텐츠를 생성하는 메서드
        /// </summary>
        private UIElement CreatePlaceholderContent(string title, string icon, string description)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(50),
                Margin = new Thickness(0)
            };
            
            border.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 3,
                Color = (Color)ColorConverter.ConvertFromString("#E0E6ED"),
                Opacity = 0.2
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 64,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var descText = new TextBlock
            {
                Text = description,
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400
            };

            var comingSoonText = new TextBlock
            {
                Text = "🚧 개발 중인 기능입니다 🚧",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F56565")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0),
                FontWeight = FontWeights.Medium
            };

            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(descText);
            stackPanel.Children.Add(comingSoonText);
            
            border.Child = stackPanel;
            return border;
        }

        /// <summary>
        /// 오류 콘텐츠를 생성하는 메서드
        /// </summary>
        private UIElement CreateErrorContent(string section, string errorMessage)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(50),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FED7D7")),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconText = new TextBlock
            {
                Text = "⚠️",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var titleText = new TextBlock
            {
                Text = $"{section} 로드 오류",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53E3E")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var errorText = new TextBlock
            {
                Text = errorMessage,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500
            };

            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(titleText);
            stackPanel.Children.Add(errorText);
            
            border.Child = stackPanel;
            return border;
        }

        /// <summary>
        /// 사이드바 메뉴의 선택 상태를 업데이트하는 메서드
        /// </summary>
        private void UpdateSidebarSelection(string section)
        {
            // 모든 메뉴 버튼의 스타일을 기본으로 리셋
            ResetSidebarButtonStyles();
            
            // 선택된 메뉴 버튼을 활성 상태로 변경
            Button? activeButton = section switch
            {
                "Dashboard" => DashboardMenuButton,
                "MacroManagement" => MacroManagementMenuButton,
                "CustomScripting" => CustomScriptingMenuButton,
                "VoiceRecognition" => VoiceRecognitionMenuButton,
                "LogMonitoring" => LogMonitoringMenuButton,
                "PresetManagement" => PresetManagementMenuButton,
                "DeveloperInfo" => DeveloperInfoMenuButton,
                _ => DashboardMenuButton
            };

            if (activeButton != null)
            {
                activeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            }
        }

        /// <summary>
        /// 사이드바 버튼 스타일을 기본 상태로 리셋하는 메서드
        /// </summary>
        private void ResetSidebarButtonStyles()
        {
            var transparentBrush = new SolidColorBrush(Colors.Transparent);
            
            if (DashboardMenuButton != null) DashboardMenuButton.Background = transparentBrush;
            if (MacroManagementMenuButton != null) MacroManagementMenuButton.Background = transparentBrush;
            if (CustomScriptingMenuButton != null) CustomScriptingMenuButton.Background = transparentBrush;
            if (VoiceRecognitionMenuButton != null) VoiceRecognitionMenuButton.Background = transparentBrush;
            if (LogMonitoringMenuButton != null) LogMonitoringMenuButton.Background = transparentBrush;
            if (PresetManagementMenuButton != null) PresetManagementMenuButton.Background = transparentBrush;
            if (DeveloperInfoMenuButton != null) DeveloperInfoMenuButton.Background = transparentBrush;
        }

        // ==================== 대시보드에서 호출할 수 있는 공개 네비게이션 메서드들 ====================

        /// <summary>
        /// 매크로 관리 페이지로 이동하는 공개 메서드
        /// </summary>
        public void NavigateToMacroManagement()
        {
            NavigateToSection("MacroManagement");
        }

        /// <summary>
        /// 커스텀 스크립팅 페이지로 이동하는 공개 메서드
        /// </summary>
        public void NavigateToCustomScripting()
        {
            NavigateToSection("CustomScripting");
        }

        /// <summary>
        /// 음성 인식 페이지로 이동하는 공개 메서드
        /// </summary>
        public void NavigateToVoiceRecognition()
        {
            NavigateToSection("VoiceRecognition");
        }

        /// <summary>
        /// 로그 및 모니터링 페이지로 이동하는 공개 메서드
        /// </summary>
        public void NavigateToLogMonitoring()
        {
            NavigateToSection("LogMonitoring");
        }

        /// <summary>
        /// 프리셋 관리 페이지로 이동하는 공개 메서드
        /// </summary>
        public void NavigateToPresetManagement()
        {
            NavigateToSection("PresetManagement");
        }

        // ==================== 헤더 검색 및 기타 UI 이벤트 핸들러들 ====================

        /// <summary>
        /// 글로벌 검색 박스 포커스 이벤트 핸들러
        /// </summary>
        private void GlobalSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == "검색...")
            {
                textBox.Text = "";
                textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748"));
            }
        }

        /// <summary>
        /// 글로벌 검색 박스 포커스 해제 이벤트 핸들러
        /// </summary>
        private void GlobalSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "검색...";
                textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0AEC0"));
            }
        }
    }
} 