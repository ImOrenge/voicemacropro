using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using VoiceMacroPro.Models;

namespace VoiceMacroPro.Utils
{
    /// <summary>
    /// ConnectionStatus 열거형을 UI 표시용 문자열로 변환하는 컨버터
    /// </summary>
    public class ConnectionStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus status)
            {
                return status switch
                {
                    ConnectionStatus.Connected => "연결됨",
                    ConnectionStatus.Connecting => "연결 중...",
                    ConnectionStatus.Disconnected => "연결 해제됨",
                    ConnectionStatus.Error => "연결 오류",
                    _ => "알 수 없음"
                };
            }
            return "알 수 없음";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ConnectionStatus 열거형을 UI 표시용 색상으로 변환하는 컨버터
    /// </summary>
    public class ConnectionStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus status)
            {
                return status switch
                {
                    ConnectionStatus.Connected => new SolidColorBrush(Colors.Green),
                    ConnectionStatus.Connecting => new SolidColorBrush(Colors.Orange),
                    ConnectionStatus.Disconnected => new SolidColorBrush(Colors.Red),
                    ConnectionStatus.Error => new SolidColorBrush(Colors.DarkRed),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 