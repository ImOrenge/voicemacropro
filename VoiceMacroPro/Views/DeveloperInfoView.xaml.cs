using System;
using System.Windows;
using System.Windows.Controls;

namespace VoiceMacroPro.Views
{
    /// <summary>
    /// 개발자 정보 뷰 - 버전 정보, 개발자 연락처, 업데이트 정보를 제공합니다
    /// </summary>
    public partial class DeveloperInfoView : UserControl
    {
        /// <summary>
        /// DeveloperInfoView 생성자 - 초기화를 수행합니다
        /// </summary>
        public DeveloperInfoView()
        {
            InitializeComponent();
            LoadDeveloperInfo();
        }
        
        /// <summary>
        /// 개발자 정보를 로드하는 함수
        /// </summary>
        private void LoadDeveloperInfo()
        {
            try
            {
                // 개발자 정보 로드 로직
                System.Diagnostics.Debug.WriteLine("개발자 정보 로드 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"개발자 정보 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 