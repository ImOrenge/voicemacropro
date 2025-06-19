using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VoiceMacroPro.Models;
using VoiceMacroPro.Services;
using VoiceMacroPro.Utils;
using VoiceMacroPro.Views;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 매크로 관리를 위한 View 클래스
    /// 매크로 CRUD 기능과 목록 표시 기능을 제공합니다.
    /// </summary>
    public partial class MacroManagementView : UserControl, INotifyPropertyChanged
    {
        // API 서비스 (백엔드 통신용)
        private readonly ApiService _apiService;
        
        // 로깅 서비스 (로그 기록용)
        private readonly LoggingService _loggingService;
        
        // 매크로 목록을 위한 Observable Collection (실시간 UI 업데이트)
        public ObservableCollection<Macro> Macros { get; set; }
        
        // 현재 선택된 매크로
        private Macro _selectedMacro;
        public Macro SelectedMacro
        {
            get => _selectedMacro;
            set
            {
                _selectedMacro = value;
                OnPropertyChanged(nameof(SelectedMacro));
                UpdateButtonStates(); // 선택된 매크로에 따라 버튼 활성화 상태 변경
            }
        }
        
        /// <summary>
        /// MacroManagementView 생성자
        /// 서비스 의존성 주입 및 초기화 수행
        /// </summary>
        public MacroManagementView()
        {
            InitializeComponent();
            
            // 서비스 초기화
            _apiService = new ApiService();
            _loggingService = new LoggingService();
            
            // 매크로 컬렉션 초기화
            Macros = new ObservableCollection<Macro>();
            
            // 데이터 컨텍스트 설정 (XAML 바인딩용)
            DataContext = this;
            
            // 이벤트 핸들러 등록
            Loaded += MacroManagementView_Loaded;
            MacroDataGrid.SelectionChanged += MacroDataGrid_SelectionChanged;
            
            // 로그 기록
            _loggingService.LogInfo("매크로 관리 View 초기화 완료");
        }
        
        /// <summary>
        /// View가 로드될 때 실행되는 이벤트 핸들러
        /// 매크로 목록을 서버에서 불러옵니다.
        /// </summary>
        private async void MacroManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMacrosAsync();
        }
        
        /// <summary>
        /// DataGrid에서 매크로 선택이 변경될 때 실행되는 이벤트 핸들러
        /// </summary>
        private void MacroDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MacroDataGrid.SelectedItem is Macro selectedMacro)
            {
                SelectedMacro = selectedMacro;
                _loggingService.LogInfo($"매크로 선택됨: {selectedMacro.Name}");
            }
            else
            {
                SelectedMacro = null;
            }
        }
        
        /// <summary>
        /// 서버에서 매크로 목록을 불러와 DataGrid에 표시하는 함수
        /// </summary>
        private async Task LoadMacrosAsync()
        {
            try
            {
                StatusTextBlock.Text = "매크로 목록 로딩 중...";
                _loggingService.LogInfo("매크로 목록 로딩 시작");
                
                // API를 통해 매크로 목록 가져오기
                var macros = await _apiService.GetMacrosAsync();
                
                // UI 스레드에서 컬렉션 업데이트
                Dispatcher.Invoke(() =>
                {
                    Macros.Clear();
                    foreach (var macro in macros)
                    {
                        Macros.Add(macro);
                    }
                    
                    // DataGrid에 바인딩
                    MacroDataGrid.ItemsSource = Macros;
                    
                    // 상태 업데이트
                    MacroCountText.Text = $"총 {Macros.Count}개 매크로";
                    StatusTextBlock.Text = "매크로 로드 완료";
                    LastUpdateText.Text = $"마지막 업데이트: {DateTime.Now:HH:mm:ss}";
                });
                
                _loggingService.LogInfo($"매크로 {macros.Count}개 로딩 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 로딩 실패: {ex.Message}");
                UIHelper.ShowError($"매크로 목록을 불러오는데 실패했습니다.\n{ex.Message}");
                StatusTextBlock.Text = "매크로 로딩 실패";
            }
        }
        
        /// <summary>
        /// 새 매크로 추가 버튼 클릭 이벤트 핸들러
        /// 매크로 편집 창을 열어 새 매크로 생성
        /// </summary>
        private async void AddMacroButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("새 매크로 추가 시작");
                
                // 매크로 편집 창 열기 (새 매크로 모드)
                var editWindow = new MacroEditWindow();
                var result = editWindow.ShowDialog();
                
                // 매크로가 성공적으로 생성된 경우
                if (result == true && editWindow.MacroResult != null)
                {
                                    // 서버에 매크로 저장
                var createdMacroId = await _apiService.CreateMacroAsync(editWindow.MacroResult);
                
                if (createdMacroId > 0)
                {
                    // 생성된 ID를 설정
                    editWindow.MacroResult.Id = createdMacroId;
                    
                    // UI 목록에 추가
                    Macros.Add(editWindow.MacroResult);
                    
                    // 상태 업데이트
                    MacroCountText.Text = $"총 {Macros.Count}개 매크로";
                    StatusTextBlock.Text = $"매크로 '{editWindow.MacroResult.Name}' 추가 완료";
                    
                    _loggingService.LogInfo($"새 매크로 추가 완료: {editWindow.MacroResult.Name}");
                    UIHelper.ShowSuccess($"매크로 '{editWindow.MacroResult.Name}'이(가) 성공적으로 추가되었습니다.");
                }
                else
                {
                    throw new Exception("매크로 생성에 실패했습니다.");
                }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 추가 실패: {ex.Message}");
                UIHelper.ShowError($"매크로 추가에 실패했습니다.\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// 매크로 수정 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 편집합니다.
        /// </summary>
        private async void EditMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMacro == null)
            {
                UIHelper.ShowWarning("수정할 매크로를 선택해주세요.");
                return;
            }
            
            try
            {
                _loggingService.LogInfo($"매크로 수정 시작: {SelectedMacro.Name}");
                
                // 매크로 편집 창 열기 (수정 모드)
                var editWindow = new MacroEditWindow(SelectedMacro);
                var result = editWindow.ShowDialog();
                
                // 매크로가 성공적으로 수정된 경우
                if (result == true && editWindow.MacroResult != null)
                {
                                    // 서버에 매크로 업데이트
                var updateSuccess = await _apiService.UpdateMacroAsync(editWindow.MacroResult);
                
                if (updateSuccess)
                {
                    // UI에서 매크로 정보 업데이트
                    var index = Macros.IndexOf(SelectedMacro);
                    if (index >= 0)
                    {
                        Macros[index] = editWindow.MacroResult;
                    }
                    
                    StatusTextBlock.Text = $"매크로 '{editWindow.MacroResult.Name}' 수정 완료";
                    _loggingService.LogInfo($"매크로 수정 완료: {editWindow.MacroResult.Name}");
                    UIHelper.ShowSuccess($"매크로 '{editWindow.MacroResult.Name}'이(가) 성공적으로 수정되었습니다.");
                }
                else
                {
                    throw new Exception("매크로 수정에 실패했습니다.");
                }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 수정 실패: {ex.Message}");
                UIHelper.ShowError($"매크로 수정에 실패했습니다.\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// 매크로 복사 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 복제합니다.
        /// </summary>
        private async void CopyMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMacro == null)
            {
                UIHelper.ShowWarning("복사할 매크로를 선택해주세요.");
                return;
            }
            
            try
            {
                _loggingService.LogInfo($"매크로 복사 시작: {SelectedMacro.Name}");
                
                // 새로운 매크로 객체 생성 (복사본)
                var copiedMacro = new Macro
                {
                    Name = $"{SelectedMacro.Name} (복사본)",
                    VoiceCommand = $"{SelectedMacro.VoiceCommand}_copy",
                    ActionType = SelectedMacro.ActionType,
                    KeySequence = SelectedMacro.KeySequence,
                    Settings = SelectedMacro.Settings
                };
                
                // 서버에 복사된 매크로 저장
                var createdMacroId = await _apiService.CreateMacroAsync(copiedMacro);
                
                if (createdMacroId > 0)
                {
                    // 생성된 ID를 설정
                    copiedMacro.Id = createdMacroId;
                    
                    // UI 목록에 추가
                    Macros.Add(copiedMacro);
                    
                    // 상태 업데이트
                    MacroCountText.Text = $"총 {Macros.Count}개 매크로";
                    StatusTextBlock.Text = $"매크로 '{copiedMacro.Name}' 복사 완료";
                    
                    _loggingService.LogInfo($"매크로 복사 완료: {copiedMacro.Name}");
                    UIHelper.ShowSuccess($"매크로 '{copiedMacro.Name}'이(가) 성공적으로 복사되었습니다.");
                }
                else
                {
                    throw new Exception("매크로 복사에 실패했습니다.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 복사 실패: {ex.Message}");
                UIHelper.ShowError($"매크로 복사에 실패했습니다.\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// 매크로 삭제 버튼 클릭 이벤트 핸들러
        /// 선택된 매크로를 삭제합니다.
        /// </summary>
        private async void DeleteMacroButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMacro == null)
            {
                UIHelper.ShowWarning("삭제할 매크로를 선택해주세요.");
                return;
            }
            
            // 삭제 확인 대화상자
            var confirmResult = UIHelper.ShowConfirm(
                $"매크로 '{SelectedMacro.Name}'을(를) 정말 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.");
            
            if (!confirmResult)
                return;
            
            try
            {
                _loggingService.LogInfo($"매크로 삭제 시작: {SelectedMacro.Name}");
                
                // 서버에서 매크로 삭제
                await _apiService.DeleteMacroAsync(SelectedMacro.Id);
                
                // UI에서 매크로 제거
                var macroToDelete = SelectedMacro;
                Macros.Remove(macroToDelete);
                SelectedMacro = null;
                
                // 상태 업데이트
                MacroCountText.Text = $"총 {Macros.Count}개 매크로";
                StatusTextBlock.Text = $"매크로 '{macroToDelete.Name}' 삭제 완료";
                
                _loggingService.LogInfo($"매크로 삭제 완료: {macroToDelete.Name}");
                UIHelper.ShowSuccess($"매크로 '{macroToDelete.Name}'이(가) 성공적으로 삭제되었습니다.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 삭제 실패: {ex.Message}");
                UIHelper.ShowError($"매크로 삭제에 실패했습니다.\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// 선택된 매크로에 따라 버튼 활성화 상태를 업데이트하는 함수
        /// </summary>
        private void UpdateButtonStates()
        {
            bool hasMacroSelected = SelectedMacro != null;
            
            // 수정, 복사, 삭제 버튼은 매크로가 선택되었을 때만 활성화
            EditMacroButton.IsEnabled = hasMacroSelected;
            CopyMacroButton.IsEnabled = hasMacroSelected;
            DeleteMacroButton.IsEnabled = hasMacroSelected;
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
