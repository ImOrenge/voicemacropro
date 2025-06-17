using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VoiceMacroPro.Models;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 매크로 편집 윈도우의 상호작용 로직을 담당하는 클래스
    /// 새로운 매크로 생성 또는 기존 매크로 수정을 위한 UI를 제공합니다.
    /// </summary>
    public partial class MacroEditWindow : Window
    {
        /// <summary>
        /// 편집 완료 후 반환할 매크로 객체
        /// </summary>
        public Macro MacroResult { get; private set; } = new Macro();

        /// <summary>
        /// 편집 모드 여부 (true: 수정, false: 새로 생성)
        /// </summary>
        private bool _isEditMode = false;

        /// <summary>
        /// 새 매크로 생성을 위한 생성자
        /// </summary>
        public MacroEditWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            TitleTextBlock.Text = "새 매크로 추가";
            
            // 새 매크로용 MacroResult 초기화
            MacroResult = new Macro();
            
            InitializeDefaultValues();
        }

        /// <summary>
        /// 기존 매크로 수정을 위한 생성자
        /// </summary>
        /// <param name="macro">수정할 매크로 객체</param>
        public MacroEditWindow(Macro macro)
        {
            InitializeComponent();
            _isEditMode = true;
            TitleTextBlock.Text = "매크로 수정";
            LoadMacroData(macro);
        }

        /// <summary>
        /// 기본값으로 UI를 초기화하는 함수
        /// </summary>
        private void InitializeDefaultValues()
        {
            // 동작 타입 기본값 설정 (콤보)
            ActionTypeComboBox.SelectedIndex = 0;
            
            // 추가 설정 기본값
            DelayTextBox.Text = "100";
            RepeatCountTextBox.Text = "1";
        }

        /// <summary>
        /// 기존 매크로 데이터를 UI에 로드하는 함수
        /// </summary>
        /// <param name="macro">로드할 매크로 객체</param>
        private void LoadMacroData(Macro macro)
        {
            // 기본 정보 설정
            NameTextBox.Text = macro.Name;
            VoiceCommandTextBox.Text = macro.VoiceCommand;
            KeySequenceTextBox.Text = macro.KeySequence;

            // 동작 타입 설정
            SetActionTypeComboBox(macro.ActionType);

            // 추가 설정 로드
            LoadAdditionalSettings(macro.Settings);

            // 결과 객체 초기화 (ID 포함)
            MacroResult = new Macro
            {
                Id = macro.Id,
                CreatedAt = macro.CreatedAt,
                UsageCount = macro.UsageCount
            };
        }

        /// <summary>
        /// 동작 타입 콤보박스를 설정하는 함수
        /// </summary>
        /// <param name="actionType">설정할 동작 타입</param>
        private void SetActionTypeComboBox(string actionType)
        {
            for (int i = 0; i < ActionTypeComboBox.Items.Count; i++)
            {
                if (ActionTypeComboBox.Items[i] is ComboBoxItem item)
                {
                    if (item.Tag?.ToString() == actionType)
                    {
                        ActionTypeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 추가 설정을 UI에 로드하는 함수
        /// </summary>
        /// <param name="settings">설정 딕셔너리</param>
        private void LoadAdditionalSettings(Dictionary<string, object> settings)
        {
            if (settings == null) return;

            // 지연 시간 설정
            if (settings.ContainsKey("delay"))
            {
                DelayTextBox.Text = settings["delay"]?.ToString() ?? "100";
            }

            // 반복 횟수 설정
            if (settings.ContainsKey("repeat_count"))
            {
                RepeatCountTextBox.Text = settings["repeat_count"]?.ToString() ?? "1";
            }
        }

        /// <summary>
        /// 입력 유효성을 검사하는 함수
        /// </summary>
        /// <returns>유효성 검사 통과 여부</returns>
        private bool ValidateInput()
        {
            var errors = new List<string>();

            // 필수 필드 검사
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                errors.Add("매크로 이름을 입력해주세요.");
            }

            if (string.IsNullOrWhiteSpace(VoiceCommandTextBox.Text))
            {
                errors.Add("음성 명령어를 입력해주세요.");
            }

            if (string.IsNullOrWhiteSpace(KeySequenceTextBox.Text))
            {
                errors.Add("키 시퀀스를 입력해주세요.");
            }

            if (ActionTypeComboBox.SelectedItem == null)
            {
                errors.Add("동작 타입을 선택해주세요.");
            }

            // 숫자 필드 유효성 검사
            if (!int.TryParse(DelayTextBox.Text, out int delay) || delay < 0)
            {
                errors.Add("지연 시간은 0 이상의 숫자를 입력해주세요.");
            }

            if (!int.TryParse(RepeatCountTextBox.Text, out int repeatCount) || repeatCount < 1)
            {
                errors.Add("반복 횟수는 1 이상의 숫자를 입력해주세요.");
            }

            // 오류가 있으면 메시지 표시
            if (errors.Any())
            {
                string errorMessage = "다음 오류를 수정해주세요:\n\n" + string.Join("\n", errors);
                MessageBox.Show(errorMessage, "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 현재 입력값으로 매크로 객체를 생성하는 함수
        /// </summary>
        /// <returns>생성된 매크로 객체</returns>
        private Macro CreateMacroFromInput()
        {
            // 동작 타입 추출
            string actionType = ((ComboBoxItem)ActionTypeComboBox.SelectedItem).Tag?.ToString() ?? "combo";

            // 추가 설정 구성
            var settings = new Dictionary<string, object>
            {
                ["delay"] = int.Parse(DelayTextBox.Text),
                ["repeat_count"] = int.Parse(RepeatCountTextBox.Text)
            };

            // 매크로 객체 생성
            var macro = new Macro
            {
                Name = NameTextBox.Text.Trim(),
                VoiceCommand = VoiceCommandTextBox.Text.Trim(),
                ActionType = actionType,
                KeySequence = KeySequenceTextBox.Text.Trim(),
                Settings = settings
            };

            // 수정 모드인 경우 기존 정보 유지
            if (_isEditMode && MacroResult != null)
            {
                macro.Id = MacroResult.Id;
                macro.CreatedAt = MacroResult.CreatedAt;
                macro.UsageCount = MacroResult.UsageCount;
            }

            return macro;
        }

        /// <summary>
        /// 테스트 버튼 클릭 이벤트 핸들러
        /// 입력된 매크로 설정을 검증하고 미리보기를 제공합니다.
        /// </summary>
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            var testMacro = CreateMacroFromInput();
            
            string testMessage = $"매크로 테스트 정보:\n\n" +
                               $"이름: {testMacro.Name}\n" +
                               $"음성 명령어: {testMacro.VoiceCommand}\n" +
                               $"동작 타입: {testMacro.GetActionTypeDisplayName()}\n" +
                               $"키 시퀀스: {testMacro.KeySequence}\n" +
                               $"지연 시간: {testMacro.Settings["delay"]}ms\n" +
                               $"반복 횟수: {testMacro.Settings["repeat_count"]}회\n\n" +
                               "실제 키 입력 테스트는 향후 구현 예정입니다.";

            MessageBox.Show(testMessage, "매크로 테스트", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 저장 버튼 클릭 이벤트 핸들러
        /// 입력 유효성을 검사하고 매크로를 저장합니다.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
            {
                return;
            }

            try
            {
                // 매크로 객체 생성
                MacroResult = CreateMacroFromInput();

                // 매크로 유효성 추가 검증
                if (!MacroResult.IsValid())
                {
                    MessageBox.Show("매크로 정보가 올바르지 않습니다.", "유효성 오류", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 성공 메시지 표시
                string successMessage = _isEditMode ? "매크로가 성공적으로 수정되었습니다." 
                                                   : "새 매크로가 성공적으로 생성되었습니다.";
                
                // 다이얼로그 결과를 OK로 설정하고 창 닫기
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"매크로 저장 중 오류가 발생했습니다:\n{ex.Message}", 
                              "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 취소 버튼 클릭 이벤트 핸들러
        /// 변경사항을 저장하지 않고 창을 닫습니다.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 변경사항이 있는지 확인
            if (HasUnsavedChanges())
            {
                var result = MessageBox.Show("변경사항이 저장되지 않습니다.\n정말로 취소하시겠습니까?", 
                                           "취소 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // 다이얼로그 결과를 Cancel로 설정하고 창 닫기
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 저장되지 않은 변경사항이 있는지 확인하는 함수
        /// </summary>
        /// <returns>변경사항 존재 여부</returns>
        private bool HasUnsavedChanges()
        {
            // 새 매크로 생성 모드인 경우
            if (!_isEditMode)
            {
                return !string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                       !string.IsNullOrWhiteSpace(VoiceCommandTextBox.Text) ||
                       !string.IsNullOrWhiteSpace(KeySequenceTextBox.Text);
            }

            // 수정 모드인 경우 - 원본과 비교
            if (MacroResult != null)
            {
                return NameTextBox.Text.Trim() != MacroResult.Name ||
                       VoiceCommandTextBox.Text.Trim() != MacroResult.VoiceCommand ||
                       KeySequenceTextBox.Text.Trim() != MacroResult.KeySequence;
            }

            return false;
        }

        /// <summary>
        /// 윈도우가 닫힐 때 추가 처리를 위한 오버라이드 함수
        /// </summary>
        /// <param name="e">이벤트 인수</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // ESC 키나 X 버튼으로 창을 닫는 경우에도 취소 확인
            if (DialogResult == null && HasUnsavedChanges())
            {
                var result = MessageBox.Show("변경사항이 저장되지 않습니다.\n정말로 창을 닫으시겠습니까?", 
                                           "종료 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }
    }
} 