using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VoiceMacroPro.Utils
{
    /// <summary>
    /// UI 관련 공통 기능을 제공하는 헬퍼 클래스
    /// 메시지 박스, 확인 대화상자, 상태 표시 등의 기능을 통합 관리합니다.
    /// </summary>
    public static class UIHelper
    {
        // ============================================================================
        // 메시지 박스 관련 기능
        // ============================================================================

        /// <summary>
        /// 오류 메시지를 표시하는 함수
        /// 일관된 스타일의 오류 대화상자를 제공합니다.
        /// </summary>
        /// <param name="message">표시할 오류 메시지</param>
        /// <param name="title">대화상자 제목 (기본값: "오류")</param>
        public static void ShowError(string message, string title = "오류")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 성공 메시지를 표시하는 함수
        /// 작업 완료 시 사용자에게 피드백을 제공합니다.
        /// </summary>
        /// <param name="message">표시할 성공 메시지</param>
        /// <param name="title">대화상자 제목 (기본값: "성공")</param>
        public static void ShowSuccess(string message, string title = "성공")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 경고 메시지를 표시하는 함수
        /// 주의가 필요한 상황에서 사용합니다.
        /// </summary>
        /// <param name="message">표시할 경고 메시지</param>
        /// <param name="title">대화상자 제목 (기본값: "경고")</param>
        public static void ShowWarning(string message, string title = "경고")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// 정보 메시지를 표시하는 함수
        /// 일반적인 정보 전달 시 사용합니다.
        /// </summary>
        /// <param name="message">표시할 정보 메시지</param>
        /// <param name="title">대화상자 제목 (기본값: "정보")</param>
        public static void ShowInfo(string message, string title = "정보")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ============================================================================
        // 확인 대화상자 관련 기능
        // ============================================================================

        /// <summary>
        /// 예/아니오 확인 대화상자를 표시하는 함수
        /// 사용자의 선택이 필요한 상황에서 사용합니다.
        /// </summary>
        /// <param name="message">확인할 메시지</param>
        /// <param name="title">대화상자 제목 (기본값: "확인")</param>
        /// <returns>사용자가 '예'를 선택했으면 true, 그렇지 않으면 false</returns>
        public static bool ShowConfirm(string message, string title = "확인")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, 
                                 MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 삭제 확인 대화상자를 표시하는 함수
        /// 삭제 작업 전 사용자 확인을 받습니다.
        /// </summary>
        /// <param name="itemName">삭제할 항목 이름</param>
        /// <returns>사용자가 삭제를 확인했으면 true, 그렇지 않으면 false</returns>
        public static bool ShowDeleteConfirm(string itemName)
        {
            string message = $"'{itemName}'을(를) 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.";
            return MessageBox.Show(message, "삭제 확인", MessageBoxButton.YesNo, 
                                 MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 저장 확인 대화상자를 표시하는 함수
        /// 변경사항 저장 여부를 확인합니다.
        /// </summary>
        /// <returns>사용자가 저장을 선택했으면 true, 그렇지 않으면 false</returns>
        public static bool ShowSaveConfirm()
        {
            string message = "변경사항이 있습니다. 저장하시겠습니까?";
            return MessageBox.Show(message, "저장 확인", MessageBoxButton.YesNo, 
                                 MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        // ============================================================================
        // 상태 표시 관련 기능
        // ============================================================================

        /// <summary>
        /// 서버 연결 상태에 따른 색상을 반환하는 함수
        /// UI에서 연결 상태를 시각적으로 표시할 때 사용합니다.
        /// </summary>
        /// <param name="isConnected">연결 상태</param>
        /// <returns>연결 상태에 맞는 색상 브러시</returns>
        public static Brush GetConnectionStatusColor(bool isConnected)
        {
            return isConnected ? Brushes.Green : Brushes.Red;
        }

        /// <summary>
        /// 서버 연결 상태에 따른 텍스트를 반환하는 함수
        /// 상태 표시 레이블에서 사용합니다.
        /// </summary>
        /// <param name="isConnected">연결 상태</param>
        /// <returns>연결 상태를 나타내는 텍스트</returns>
        public static string GetConnectionStatusText(bool isConnected)
        {
            return isConnected ? "🟢 서버 연결됨" : "🔴 서버 연결 안됨";
        }

        // ============================================================================
        // 데이터 유효성 검증 관련 기능
        // ============================================================================

        /// <summary>
        /// 필수 입력 필드가 비어있는지 검증하는 함수
        /// 사용자 입력 검증 시 사용합니다.
        /// </summary>
        /// <param name="value">검증할 값</param>
        /// <param name="fieldName">필드 이름</param>
        /// <returns>유효하면 true, 그렇지 않으면 false</returns>
        public static bool ValidateRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ShowError($"{fieldName}은(는) 필수 입력 항목입니다.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 텍스트 길이를 검증하는 함수
        /// 입력값의 길이 제한을 확인합니다.
        /// </summary>
        /// <param name="value">검증할 값</param>
        /// <param name="fieldName">필드 이름</param>
        /// <param name="maxLength">최대 길이</param>
        /// <returns>유효하면 true, 그렇지 않으면 false</returns>
        public static bool ValidateLength(string value, string fieldName, int maxLength)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            {
                ShowError($"{fieldName}은(는) {maxLength}자를 초과할 수 없습니다.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 음성 명령어 유효성을 검증하는 함수
        /// 특수문자나 길이 제한을 확인합니다.
        /// </summary>
        /// <param name="command">검증할 음성 명령어</param>
        /// <returns>유효하면 true, 그렇지 않으면 false</returns>
        public static bool ValidateVoiceCommand(string command)
        {
            if (!ValidateRequired(command, "음성 명령어"))
                return false;

            if (!ValidateLength(command, "음성 명령어", 50))
                return false;

            // 특수문자 검증 (한글, 영문, 숫자, 공백만 허용)
            if (!System.Text.RegularExpressions.Regex.IsMatch(command.Trim(), @"^[가-힣a-zA-Z0-9\s]+$"))
            {
                ShowError("음성 명령어는 한글, 영문, 숫자, 공백만 포함할 수 있습니다.");
                return false;
            }

            return true;
        }

        // ============================================================================
        // 날짜/시간 포맷팅 관련 기능
        // ============================================================================

        /// <summary>
        /// DateTime을 한국어 형식으로 포맷팅하는 함수
        /// UI에서 날짜 표시 시 일관된 형식을 제공합니다.
        /// </summary>
        /// <param name="dateTime">포맷할 DateTime</param>
        /// <param name="includeTime">시간 포함 여부 (기본값: true)</param>
        /// <returns>포맷된 날짜 문자열</returns>
        public static string FormatDateTime(DateTime dateTime, bool includeTime = true)
        {
            if (includeTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            else
                return dateTime.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 상대적 시간을 표시하는 함수 (예: "3분 전", "1시간 전")
        /// 최근 활동 표시 시 사용합니다.
        /// </summary>
        /// <param name="dateTime">기준 DateTime</param>
        /// <returns>상대적 시간 문자열</returns>
        public static string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "방금 전";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}분 전";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}시간 전";
            else if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays}일 전";
            else
                return FormatDateTime(dateTime, false);
        }

        // ============================================================================
        // UI 상태 관리 관련 기능
        // ============================================================================

        /// <summary>
        /// 컨트롤의 활성화 상태를 안전하게 변경하는 함수
        /// UI 스레드 안전성을 보장합니다.
        /// </summary>
        /// <param name="control">상태를 변경할 컨트롤</param>
        /// <param name="isEnabled">활성화 여부</param>
        public static void SetControlEnabled(FrameworkElement control, bool isEnabled)
        {
            if (control.Dispatcher.CheckAccess())
            {
                control.IsEnabled = isEnabled;
            }
            else
            {
                control.Dispatcher.Invoke(() => control.IsEnabled = isEnabled);
            }
        }

        /// <summary>
        /// 로딩 상태를 표시하는 함수
        /// 비동기 작업 중 사용자에게 피드백을 제공합니다.
        /// </summary>
        /// <param name="isLoading">로딩 상태</param>
        /// <param name="loadingMessage">로딩 메시지 (기본값: "처리 중...")</param>
        public static void ShowLoading(bool isLoading, string loadingMessage = "처리 중...")
        {
            // 실제 구현은 메인 윈도우의 로딩 패널을 제어
            // 여기서는 기본 구조만 제시
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 로딩 상태에 따른 UI 업데이트 로직
                // 예: 로딩 스피너 표시/숨김, 버튼 비활성화 등
            });
        }
    }

    // ============================================================================
    // WPF 바인딩용 컨버터 클래스들
    // ============================================================================

    /// <summary>
    /// 불린 값을 반전시키는 컨버터
    /// IsEnabled 바인딩에서 반대 값이 필요할 때 사용합니다.
    /// </summary>
    public class BooleanToInvertedBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    /// <summary>
    /// 불린 값을 가시성으로 변환하는 컨버터
    /// Visibility 바인딩에서 사용합니다.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;
            return false;
        }
    }

    /// <summary>
    /// DateTime을 상대적 시간 문자열로 변환하는 컨버터
    /// DataGrid에서 "3분 전" 형식으로 표시할 때 사용합니다.
    /// </summary>
    public class DateTimeToRelativeTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
                return UIHelper.GetRelativeTime(dateTime);
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 