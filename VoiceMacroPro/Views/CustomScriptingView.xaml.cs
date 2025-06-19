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

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 커스텀 스크립팅 뷰 - MSL 스크립트 편집 및 관리 기능을 제공합니다
    /// MSL (Macro Scripting Language) 기반 고급 매크로 스크립팅 시스템
    /// </summary>
    public partial class CustomScriptingView : UserControl, INotifyPropertyChanged
    {
        // 서비스 의존성
        private readonly ApiService _apiService;
        private readonly LoggingService _loggingService;
        
        // 커스텀 스크립트 컬렉션
        private ObservableCollection<CustomScript> _customScripts;
        public ObservableCollection<CustomScript> CustomScripts
        {
            get => _customScripts;
            set
            {
                _customScripts = value;
                OnPropertyChanged(nameof(CustomScripts));
            }
        }
        
        // 현재 선택된 스크립트
        private CustomScript _selectedScript;
        public CustomScript SelectedScript
        {
            get => _selectedScript;
            set
            {
                _selectedScript = value;
                OnPropertyChanged(nameof(SelectedScript));
                UpdateButtonStates();
                if (value != null)
                {
                    LoadScriptToEditor(value);
                }
            }
        }
        
        // 현재 편집 중인 스크립트 정보
        private string _currentScriptName = "";
        private string _currentScriptDescription = "";
        private string _currentScriptCode = "";
        private string _validationResult = "";
        
        /// <summary>
        /// CustomScriptingView 생성자 - 초기화 및 서비스 설정을 수행합니다
        /// </summary>
        public CustomScriptingView()
        {
            InitializeComponent();
            
            try
            {
                // 서비스 초기화
                _apiService = new ApiService();
                _loggingService = new LoggingService();
                
                // 컬렉션 초기화
                CustomScripts = new ObservableCollection<CustomScript>();
                
                // 데이터 컨텍스트 설정
                DataContext = this;
                
                // 이벤트 핸들러 등록
                Loaded += CustomScriptingView_Loaded;
                CustomScriptDataGrid.SelectionChanged += CustomScriptDataGrid_SelectionChanged;
                
                // 초기 상태 설정
                UpdateButtonStates();
                
                _loggingService.LogInfo("커스텀 스크립팅 View 초기화 완료");
            }
            catch (Exception ex)
            {
                UIHelper.ShowError($"커스텀 스크립팅 시스템 초기화에 실패했습니다.\n{ex.Message}");
            }
        }
        
        /// <summary>
        /// View가 로드될 때 실행되는 이벤트 핸들러
        /// </summary>
        private async void CustomScriptingView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCustomScriptsAsync();
        }
        
        /// <summary>
        /// 서버에서 커스텀 스크립트 목록을 불러오는 비동기 함수
        /// </summary>
        private async Task LoadCustomScriptsAsync()
        {
            try
            {
                _loggingService.LogInfo("커스텀 스크립트 목록 로딩 시작");
                
                // API를 통해 스크립트 목록 가져오기
                var scripts = await _apiService.GetCustomScriptsAsync();
                
                // UI 업데이트
                CustomScripts.Clear();
                foreach (var script in scripts)
                {
                    CustomScripts.Add(script);
                }
                
                // DataGrid에 바인딩
                CustomScriptDataGrid.ItemsSource = CustomScripts;
                
                UpdateStatusText();
                _loggingService.LogInfo($"커스텀 스크립트 {scripts.Count}개 로딩 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"커스텀 스크립트 로딩 실패: {ex.Message}");
                
                // 서버 연결 실패 시 테스트 데이터 로드
                LoadTestData();
                UIHelper.ShowWarning("서버에서 스크립트를 불러올 수 없어 테스트 데이터를 표시합니다.");
            }
        }
        
        /// <summary>
        /// 테스트용 커스텀 스크립트 데이터를 로드하는 함수
        /// </summary>
        private void LoadTestData()
        {
            try
            {
                CustomScripts.Clear();
                
                // MSL 예시 스크립트들
                CustomScripts.Add(new CustomScript 
                { 
                    Id = 1, 
                    Name = "기본 콤보", 
                    Description = "순차적 키 입력 예시 - W, A, S, D 순서대로 실행",
                    Category = "기본",
                    GameTarget = "범용",
                    ScriptCode = "W,A,S,D",
                    CreatedAt = DateTime.Now.AddDays(-7),
                    IsActive = true
                });
                
                CustomScripts.Add(new CustomScript 
                { 
                    Id = 2, 
                    Name = "동시 키 입력", 
                    Description = "여러 키를 동시에 누르는 예시",
                    Category = "기본",
                    GameTarget = "범용",
                    ScriptCode = "Ctrl+Alt+Shift",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    IsActive = true
                });
                
                CustomScripts.Add(new CustomScript 
                { 
                    Id = 3, 
                    Name = "지연이 있는 콤보", 
                    Description = "키 사이에 지연 시간을 두는 예시",
                    Category = "고급",
                    GameTarget = "리그 오브 레전드",
                    ScriptCode = "Q(200)W(300)E(400)R",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    IsActive = true
                });
                
                CustomScripts.Add(new CustomScript 
                { 
                    Id = 4, 
                    Name = "홀드 및 반복", 
                    Description = "키를 홀드하거나 반복하는 예시",
                    Category = "고급",
                    GameTarget = "FPS 게임",
                    ScriptCode = "Space[1000]+(W*5{100})",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    IsActive = true
                });
                
                UpdateStatusText();
                _loggingService.LogInfo("테스트 데이터 로드 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"테스트 데이터 로드 실패: {ex.Message}");
                UIHelper.ShowError($"테스트 데이터 로드 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 상태 텍스트를 업데이트하는 함수
        /// </summary>
        private void UpdateStatusText()
        {
            try
            {
                if (ScriptCountTextBlock != null)
                {
                    ScriptCountTextBlock.Text = $"총 {CustomScripts.Count}개 스크립트";
                }
            }
            catch (Exception) { /* UI 컨트롤이 없을 수 있음 */ }
        }
        
        /// <summary>
        /// 새 스크립트 추가 버튼 클릭 이벤트 핸들러
        /// 스크립트 에디터를 초기화하고 새 스크립트 생성 모드로 전환
        /// </summary>
        private void AddScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("새 스크립트 추가 시작");
                
                // 에디터 초기화
                ClearEditor();
                
                // 기본 템플릿 제공
                ScriptNameTextBox.Text = "새 스크립트";
                ScriptDescriptionTextBox.Text = "스크립트 설명을 입력하세요.";
                ScriptCodeTextBox.Text = "// MSL 스크립트 예시\n// 기본 콤보: W,A,S,D\n// 지연 포함: Q(200)W(300)E\n// 동시 입력: Ctrl+Alt+D\n// 반복: Space*5{100}\n\nW,A,S,D";
                
                // 포커스 설정
                ScriptNameTextBox.Focus();
                ScriptNameTextBox.SelectAll();
                
                UIHelper.ShowInfo("새 스크립트를 생성할 수 있습니다.\n\nMSL 문법:\n• 순차 실행: W,A,S,D\n• 동시 실행: Ctrl+Alt+D\n• 지연: Q(200)W\n• 홀드: W[1000]\n• 반복: Space*5");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"새 스크립트 추가 실패: {ex.Message}");
                UIHelper.ShowError($"새 스크립트 추가 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 에디터를 초기화하는 함수
        /// </summary>
        private void ClearEditor()
        {
            ScriptNameTextBox.Text = "";
            ScriptDescriptionTextBox.Text = "";
            ScriptCodeTextBox.Text = "";
            ValidationResultTextBlock.Text = "";
            SelectedScript = null;
        }
        
        /// <summary>
        /// 스크립트 목록 선택 변경 이벤트 핸들러
        /// </summary>
        private void CustomScriptDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CustomScriptDataGrid.SelectedItem is CustomScript selectedScript)
                {
                    SelectedScript = selectedScript;
                    _loggingService.LogInfo($"스크립트 선택: {selectedScript.Name}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 선택 실패: {ex.Message}");
                UIHelper.ShowError($"스크립트 선택 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 선택된 스크립트를 에디터에 로드하는 함수
        /// </summary>
        private void LoadScriptToEditor(CustomScript script)
        {
            try
            {
                if (script == null) return;
                
                ScriptNameTextBox.Text = script.Name ?? "";
                ScriptDescriptionTextBox.Text = script.Description ?? "";
                ScriptCodeTextBox.Text = script.ScriptCode ?? "";
                ValidationResultTextBlock.Text = "";
                
                _currentScriptName = script.Name;
                _currentScriptDescription = script.Description;
                _currentScriptCode = script.ScriptCode;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 로드 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 버튼 활성화 상태를 업데이트하는 함수
        /// </summary>
        private void UpdateButtonStates()
        {
            bool hasSelection = SelectedScript != null;
            
            if (EditScriptButton != null)
                EditScriptButton.IsEnabled = hasSelection;
            
            if (CopyScriptButton != null)
                CopyScriptButton.IsEnabled = hasSelection;
                
            if (DeleteScriptButton != null)
                DeleteScriptButton.IsEnabled = hasSelection;
                
            if (ExecuteScriptButton != null)
                ExecuteScriptButton.IsEnabled = hasSelection;
        }
        
        /// <summary>
        /// 스크립트 검증 버튼 클릭 이벤트 핸들러
        /// MSL 문법 검사를 수행합니다.
        /// </summary>
        private async void ValidateScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string scriptCode = ScriptCodeTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(scriptCode))
                {
                    ValidationResultTextBlock.Text = "❌ 검증 실패: 스크립트 코드가 비어있습니다.";
                    return;
                }
                
                _loggingService.LogInfo($"스크립트 검증 시작: {scriptCode}");
                
                // API를 통해 MSL 스크립트 검증
                var validationResult = await _apiService.ValidateScriptAsync(scriptCode);
                
                if (validationResult != null && validationResult.IsValid)
                {
                    ValidationResultTextBlock.Text = "✅ 검증 성공: 스크립트 문법이 올바릅니다.";
                    UIHelper.ShowSuccess("스크립트 검증이 완료되었습니다.");
                }
                else
                {
                    var errorText = "스크립트 검증에 실패했습니다.";
                    if (validationResult != null && validationResult.Errors != null && validationResult.Errors.Count > 0)
                    {
                        errorText = string.Join("\n", validationResult.Errors);
                    }
                    ValidationResultTextBlock.Text = $"❌ 검증 실패:\n{errorText}";
                    UIHelper.ShowError($"스크립트 문법 오류:\n{errorText}");
                }
                
                _loggingService.LogInfo($"스크립트 검증 완료: {validationResult.IsValid}");
            }
            catch (Exception ex)
            {
                ValidationResultTextBlock.Text = $"❌ 검증 중 오류: {ex.Message}";
                _loggingService.LogError($"스크립트 검증 실패: {ex.Message}");
                UIHelper.ShowError($"스크립트 검증 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 스크립트 저장 버튼 클릭 이벤트 핸들러
        /// 현재 편집 중인 스크립트를 저장합니다.
        /// </summary>
        private async void SaveScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 입력 유효성 검사
                if (!ValidateScriptInput())
                    return;
                
                string scriptName = ScriptNameTextBox.Text.Trim();
                string scriptDescription = ScriptDescriptionTextBox.Text.Trim();
                string scriptCode = ScriptCodeTextBox.Text.Trim();
                
                _loggingService.LogInfo($"스크립트 저장 시작: {scriptName}");
                
                CustomScript scriptToSave;
                
                if (SelectedScript != null)
                {
                    // 기존 스크립트 수정
                    scriptToSave = SelectedScript;
                    scriptToSave.Name = scriptName;
                    scriptToSave.Description = scriptDescription;
                    scriptToSave.ScriptCode = scriptCode;
                    scriptToSave.UpdatedAt = DateTime.Now;
                    
                    // 서버에 업데이트
                    var success = await _apiService.UpdateCustomScriptAsync(scriptToSave);
                    
                    if (success)
                    {
                        UIHelper.ShowSuccess($"스크립트 '{scriptName}'이(가) 성공적으로 수정되었습니다.");
                    }
                    else
                    {
                        throw new Exception("스크립트 수정에 실패했습니다.");
                    }
                }
                else
                {
                    // 새 스크립트 생성
                    scriptToSave = new CustomScript
                    {
                        Name = scriptName,
                        Description = scriptDescription,
                        ScriptCode = scriptCode,
                        Category = "사용자 정의",
                        GameTarget = "범용",
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };
                    
                    // 서버에 생성
                    var createdScript = await _apiService.CreateCustomScriptAsync(scriptToSave);
                    
                    // ID가 반환된 경우 scriptToSave에 ID 설정
                    if (createdScript > 0)
                    {
                        scriptToSave.Id = createdScript;
                        // UI에 추가
                        CustomScripts.Add(scriptToSave);
                        
                        UIHelper.ShowSuccess($"스크립트 '{scriptName}'이(가) 성공적으로 생성되었습니다.");
                    }
                    else
                    {
                        throw new Exception("스크립트 생성에 실패했습니다.");
                    }
                }
                
                UpdateStatusText();
                _loggingService.LogInfo($"스크립트 저장 완료: {scriptName}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 저장 실패: {ex.Message}");
                UIHelper.ShowError($"스크립트 저장 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 스크립트 입력 유효성을 검사하는 함수
        /// </summary>
        private bool ValidateScriptInput()
        {
            string scriptName = ScriptNameTextBox.Text.Trim();
            string scriptCode = ScriptCodeTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(scriptName))
            {
                UIHelper.ShowWarning("스크립트 이름을 입력해주세요.");
                ScriptNameTextBox.Focus();
                return false;
            }
            
            if (string.IsNullOrEmpty(scriptCode))
            {
                UIHelper.ShowWarning("스크립트 코드를 입력해주세요.");
                ScriptCodeTextBox.Focus();
                return false;
            }
            
            // 중복 이름 검사 (새 스크립트인 경우)
            if (SelectedScript == null)
            {
                foreach (var script in CustomScripts)
                {
                    if (script.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase))
                    {
                        UIHelper.ShowWarning($"'{scriptName}' 이름의 스크립트가 이미 존재합니다.");
                        ScriptNameTextBox.Focus();
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 스크립트 실행 버튼 클릭 이벤트 핸들러
        /// 선택된 스크립트를 즉시 실행합니다.
        /// </summary>
        private async void ExecuteScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedScript == null)
            {
                UIHelper.ShowWarning("실행할 스크립트를 선택해주세요.");
                return;
            }
            
            try
            {
                _loggingService.LogInfo($"스크립트 실행 시작: {SelectedScript.Name}");
                
                var confirmResult = UIHelper.ShowConfirm($"스크립트 '{SelectedScript.Name}'을(를) 실행하시겠습니까?\n\n⚠️ 주의: 스크립트가 즉시 실행되어 키보드/마우스 입력이 발생합니다.");
                
                if (!confirmResult)
                    return;
                
                // 서버에 스크립트 실행 요청
                var success = await _apiService.ExecuteCustomScriptAsync(SelectedScript.Id);
                
                if (success)
                {
                    UIHelper.ShowSuccess($"스크립트 '{SelectedScript.Name}'이(가) 성공적으로 실행되었습니다.");
                    _loggingService.LogInfo($"스크립트 실행 완료: {SelectedScript.Name}");
                }
                else
                {
                    UIHelper.ShowError("스크립트 실행에 실패했습니다.");
                    _loggingService.LogError($"스크립트 실행 실패: {SelectedScript.Name}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"스크립트 실행 실패: {ex.Message}");
                UIHelper.ShowError($"스크립트 실행 중 오류가 발생했습니다: {ex.Message}");
            }
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