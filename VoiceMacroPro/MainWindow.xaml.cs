using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private List<Macro> _allMacros = new List<Macro>();
        private string _currentSearchTerm = string.Empty;
        private string _currentSortBy = "name";

        /// <summary>
        /// 메인 윈도우 생성자
        /// API 서비스를 초기화하고 UI를 설정합니다.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            
            // 윈도우가 로드된 후 초기화 작업 수행
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// 윈도우 로드 완료 시 실행되는 이벤트 핸들러
        /// 서버 연결 상태 확인 및 초기 데이터 로드를 수행합니다.
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatusText("애플리케이션 초기화 중...");
            
            // 서버 연결 상태 확인
            await CheckServerConnection();
            
            // 매크로 목록 로드
            await LoadMacros();
            
            UpdateStatusText("준비 완료");
        }

        /// <summary>
        /// 서버 연결 상태를 확인하고 UI에 표시하는 함수
        /// </summary>
        private async Task CheckServerConnection()
        {
            try
            {
                bool isConnected = await _apiService.CheckServerHealthAsync();
                
                if (isConnected)
                {
                    // 연결 성공 - 초록색 표시
                    ServerStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                    ServerStatusText.Text = "서버 연결됨";
                }
                else
                {
                    // 연결 실패 - 빨간색 표시
                    ServerStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                    ServerStatusText.Text = "서버 연결 실패";
                    
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
                ServerStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                ServerStatusText.Text = "연결 오류";
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
                MacroDataGrid.ItemsSource = _allMacros;
                
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
                    await _apiService.CreateMacroAsync(editWindow.MacroResult);
                    
                    // 목록 새로고침
                    await LoadMacros();
                    
                    UpdateStatusText("새 매크로가 성공적으로 추가되었습니다.");
                }
                catch (Exception ex)
                {
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
            StatusTextBlock.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        /// <summary>
        /// 윈도우가 닫힐 때 리소스를 정리하는 함수
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // API 서비스 리소스 정리
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
} 