using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VoiceMacroPro.Models;
using VoiceMacroPro.Services;
using VoiceMacroPro.Utils;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 프리셋 관리 뷰 - 매크로 프리셋 생성, 수정, 삭제, 가져오기/내보내기 기능을 제공합니다
    /// 실제 API 서비스와 연결되어 모든 CRUD 기능을 완전히 지원합니다.
    /// </summary>
    public partial class PresetManagementView : UserControl, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly LoggingService _loggingService;
        private ObservableCollection<PresetModel> _presets;
        private PresetModel _selectedPreset;
        private string _statusMessage;
        private bool _isLoading;

        /// <summary>프리셋 목록 컬렉션</summary>
        public ObservableCollection<PresetModel> Presets
        {
            get => _presets;
            set => SetProperty(ref _presets, value);
        }

        /// <summary>선택된 프리셋</summary>
        public PresetModel SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                SetProperty(ref _selectedPreset, value);
                UpdatePresetDetails();
            }
        }

        /// <summary>상태 메시지</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>로딩 상태</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// PresetManagementView 생성자 - 초기화 및 프리셋 데이터 로드를 수행합니다
        /// </summary>
        public PresetManagementView()
        {
            InitializeComponent();
            DataContext = this;
            
            _apiService = new ApiService();
            _loggingService = LoggingService.Instance;
            _presets = new ObservableCollection<PresetModel>();
            
            _loggingService.LogInfo("PresetManagementView 초기화 완료");
            
            // 프리셋 데이터 로드
            _ = LoadPresetsAsync();
        }

        /// <summary>
        /// API에서 프리셋 목록을 로드하는 비동기 함수
        /// </summary>
        private async Task LoadPresetsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "프리셋 목록을 불러오는 중...";
                _loggingService.LogInfo("프리셋 목록 로드 시작");

                var presets = await _apiService.GetPresetsAsync();
                
                Presets.Clear();
                foreach (var preset in presets)
                {
                    Presets.Add(preset);
                }

                StatusMessage = $"프리셋 {Presets.Count}개 로드 완료";
                _loggingService.LogInfo($"프리셋 {Presets.Count}개 로드 완료");
            }
            catch (Exception ex)
            {
                StatusMessage = "프리셋 목록 로드 실패";
                _loggingService.LogError($"프리셋 목록 로드 실패: {ex.Message}");
                UIHelper.ShowError($"프리셋 목록을 불러오는 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 새 프리셋 생성 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void NewPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.LogInfo("새 프리셋 생성 시작");
                
                // 새 프리셋 다이얼로그 표시 (간단한 입력창)
                var dialog = new CreatePresetDialog();
                if (dialog.ShowDialog() == true)
                {
                    var newPreset = await _apiService.CreatePresetAsync(
                        dialog.PresetName, 
                        dialog.PresetDescription, 
                        new System.Collections.Generic.List<int>()
                    );
                    
                    Presets.Add(newPreset);
                    SelectedPreset = newPreset;
                    StatusMessage = $"프리셋 '{newPreset.Name}' 생성 완료";
                    _loggingService.LogInfo($"프리셋 '{newPreset.Name}' 생성 완료");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 생성 실패: {ex.Message}");
                UIHelper.ShowError($"프리셋 생성 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 수정 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void EditPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPreset == null)
            {
                UIHelper.ShowWarning("수정할 프리셋을 선택해주세요.");
                return;
            }

            try
            {
                _loggingService.LogInfo($"프리셋 '{SelectedPreset.Name}' 수정 시작");
                
                var updatedPreset = await _apiService.UpdatePresetAsync(
                    SelectedPreset.Id,
                    PresetNameTextBox.Text,
                    PresetDescriptionTextBox.Text,
                    null, // 매크로 ID는 별도 관리
                    FavoriteCheckBox.IsChecked
                );
                
                // UI 업데이트
                var index = Presets.IndexOf(SelectedPreset);
                if (index >= 0)
                {
                    Presets[index] = updatedPreset;
                    SelectedPreset = updatedPreset;
                }
                
                StatusMessage = $"프리셋 '{updatedPreset.Name}' 수정 완료";
                _loggingService.LogInfo($"프리셋 '{updatedPreset.Name}' 수정 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 수정 실패: {ex.Message}");
                UIHelper.ShowError($"프리셋 수정 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 복사 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void CopyPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPreset == null)
            {
                UIHelper.ShowWarning("복사할 프리셋을 선택해주세요.");
                return;
            }

            try
            {
                _loggingService.LogInfo($"프리셋 '{SelectedPreset.Name}' 복사 시작");
                
                var copiedPreset = await _apiService.CopyPresetAsync(SelectedPreset.Id);
                Presets.Add(copiedPreset);
                SelectedPreset = copiedPreset;
                
                StatusMessage = $"프리셋 '{copiedPreset.Name}' 복사 완료";
                _loggingService.LogInfo($"프리셋 '{copiedPreset.Name}' 복사 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 복사 실패: {ex.Message}");
                UIHelper.ShowError($"프리셋 복사 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 삭제 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPreset == null)
            {
                UIHelper.ShowWarning("삭제할 프리셋을 선택해주세요.");
                return;
            }

            if (!UIHelper.ShowConfirm($"프리셋 '{SelectedPreset.Name}'을(를) 정말 삭제하시겠습니까?"))
            {
                return;
            }

            try
            {
                _loggingService.LogInfo($"프리셋 '{SelectedPreset.Name}' 삭제 시작");
                
                await _apiService.DeletePresetAsync(SelectedPreset.Id);
                Presets.Remove(SelectedPreset);
                SelectedPreset = null;
                
                StatusMessage = "프리셋 삭제 완료";
                _loggingService.LogInfo("프리셋 삭제 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 삭제 실패: {ex.Message}");
                UIHelper.ShowError($"프리셋 삭제 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 내보내기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ExportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPreset == null)
            {
                UIHelper.ShowWarning("내보낼 프리셋을 선택해주세요.");
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "프리셋 내보내기",
                    Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"{SelectedPreset.Name}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _loggingService.LogInfo($"프리셋 '{SelectedPreset.Name}' 내보내기 시작");
                    
                    await _apiService.ExportPresetAsync(SelectedPreset.Id, saveFileDialog.FileName);
                    
                    StatusMessage = $"프리셋 '{SelectedPreset.Name}' 내보내기 완료";
                    _loggingService.LogInfo($"프리셋 '{SelectedPreset.Name}' 내보내기 완료: {saveFileDialog.FileName}");
                    UIHelper.ShowInfo($"프리셋이 성공적으로 내보내졌습니다.\n경로: {saveFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 내보내기 실패: {ex.Message}");
                UIHelper.ShowError($"프리셋 내보내기 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 가져오기 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void ImportPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "프리셋 가져오기",
                    Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _loggingService.LogInfo($"프리셋 가져오기 시작: {openFileDialog.FileName}");
                    
                    var presetId = await _apiService.ImportPresetAsync(openFileDialog.FileName);
                    await LoadPresetsAsync(); // 목록 새로고침
                    
                    StatusMessage = "프리셋 가져오기 완료";
                    _loggingService.LogInfo($"프리셋 가져오기 완료: {openFileDialog.FileName}");
                    UIHelper.ShowInfo("프리셋이 성공적으로 가져와졌습니다.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 가져오기 실패: {ex.Message}");
                UIHelper.ShowError($"프리셋 가져오기 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        /// <summary>
        /// 프리셋 세부 정보를 업데이트하는 함수
        /// </summary>
        private void UpdatePresetDetails()
        {
            if (SelectedPreset != null)
            {
                PresetNameTextBox.Text = SelectedPreset.Name;
                PresetDescriptionTextBox.Text = SelectedPreset.Description;
                FavoriteCheckBox.IsChecked = SelectedPreset.IsFavorite;
                
                // 프리셋에 속한 매크로 목록 업데이트
                _ = LoadPresetMacrosAsync();
            }
            else
            {
                PresetNameTextBox.Text = "";
                PresetDescriptionTextBox.Text = "";
                FavoriteCheckBox.IsChecked = false;
                PresetMacroListBox.ItemsSource = null;
            }
        }

        /// <summary>
        /// 선택된 프리셋의 매크로 목록을 로드하는 비동기 함수
        /// </summary>
        private async Task LoadPresetMacrosAsync()
        {
            if (SelectedPreset == null) return;

            try
            {
                // 프리셋에 속한 매크로들의 정보를 가져와서 표시
                var macroNames = new System.Collections.Generic.List<string>();
                foreach (var macroId in SelectedPreset.MacroIds)
                {
                    try
                    {
                        var macro = await _apiService.GetMacroByIdAsync(macroId);
                        macroNames.Add($"{macro.Name} ({macro.VoiceCommand})");
                    }
                    catch
                    {
                        macroNames.Add($"매크로 ID: {macroId} (정보 없음)");
                    }
                }
                
                PresetMacroListBox.ItemsSource = macroNames;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 매크로 목록 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// DataGrid 선택 변경 이벤트 핸들러
        /// </summary>
        private void PresetDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetDataGrid.SelectedItem is PresetModel selectedPreset)
            {
                SelectedPreset = selectedPreset;
            }
        }

        #region INotifyPropertyChanged Implementation
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 새 프리셋 생성 다이얼로그 (간단한 구현)
    /// </summary>
    public partial class CreatePresetDialog : Window
    {
        public string PresetName { get; private set; }
        public string PresetDescription { get; private set; }

        public CreatePresetDialog()
        {
            Title = "새 프리셋 생성";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var nameLabel = new Label { Content = "프리셋 이름:", Margin = new Thickness(10) };
            var nameTextBox = new TextBox { Margin = new Thickness(10), Padding = new Thickness(5) };
            var descLabel = new Label { Content = "설명:", Margin = new Thickness(10) };
            var descTextBox = new TextBox { Margin = new Thickness(10), Padding = new Thickness(5), Height = 60, TextWrapping = TextWrapping.Wrap };
            
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(10) };
            var okButton = new Button { Content = "확인", Width = 80, Height = 30, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "취소", Width = 80, Height = 30, Margin = new Thickness(5) };
            
            okButton.Click += (s, e) => 
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    MessageBox.Show("프리셋 이름을 입력해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                PresetName = nameTextBox.Text.Trim();
                PresetDescription = descTextBox.Text.Trim();
                DialogResult = true;
                Close();
            };
            
            cancelButton.Click += (s, e) => 
            {
                DialogResult = false;
                Close();
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            Grid.SetRow(nameLabel, 0);
            Grid.SetRow(nameTextBox, 1);
            Grid.SetRow(descLabel, 2);
            Grid.SetRow(descTextBox, 3);
            Grid.SetRow(buttonPanel, 4);
            
            grid.Children.Add(nameLabel);
            grid.Children.Add(nameTextBox);
            grid.Children.Add(descLabel);
            grid.Children.Add(descTextBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
    }
} 