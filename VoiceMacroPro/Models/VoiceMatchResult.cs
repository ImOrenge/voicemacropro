using System.ComponentModel;
using System.Windows.Media;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// 음성 인식 매칭 결과를 나타내는 모델 클래스
    /// UI의 매칭 결과 DataGrid에 바인딩되는 데이터를 담습니다.
    /// </summary>
    public class VoiceMatchResult : INotifyPropertyChanged
    {
        private int _rank;
        private string _macroName = string.Empty;
        private string _voiceCommand = string.Empty;
        private double _confidence;
        private string _actionDescription = string.Empty;
        private int _macroId;
        private string _recognizedText = string.Empty;
        private bool _isExecuted;

        /// <summary>
        /// 매칭 순위 (1, 2, 3, ...)
        /// </summary>
        public int Rank
        {
            get => _rank;
            set
            {
                _rank = value;
                OnPropertyChanged(nameof(Rank));
            }
        }

        /// <summary>
        /// 매크로 이름
        /// </summary>
        public string MacroName
        {
            get => _macroName;
            set
            {
                _macroName = value;
                OnPropertyChanged(nameof(MacroName));
            }
        }

        /// <summary>
        /// 음성 명령어
        /// </summary>
        public string VoiceCommand
        {
            get => _voiceCommand;
            set
            {
                _voiceCommand = value;
                OnPropertyChanged(nameof(VoiceCommand));
            }
        }

        /// <summary>
        /// 확신도 (0.0 ~ 1.0)
        /// </summary>
        public double Confidence
        {
            get => _confidence;
            set
            {
                _confidence = value;
                OnPropertyChanged(nameof(Confidence));
                OnPropertyChanged(nameof(ConfidenceText));
                OnPropertyChanged(nameof(ConfidenceColor));
            }
        }

        /// <summary>
        /// 확신도를 백분율 텍스트로 표시 (예: "85%")
        /// </summary>
        public string ConfidenceText => $"{Confidence * 100:F0}%";

        /// <summary>
        /// 확신도에 따른 색상 (높음: 초록, 중간: 노랑, 낮음: 빨강)
        /// </summary>
        public Brush ConfidenceColor
        {
            get
            {
                if (Confidence >= 0.8)
                    return new SolidColorBrush(Colors.Green);
                else if (Confidence >= 0.5)
                    return new SolidColorBrush(Colors.Orange);
                else
                    return new SolidColorBrush(Colors.Red);
            }
        }

        /// <summary>
        /// 동작 설명
        /// </summary>
        public string ActionDescription
        {
            get => _actionDescription;
            set
            {
                _actionDescription = value;
                OnPropertyChanged(nameof(ActionDescription));
            }
        }

        /// <summary>
        /// 매크로 ID
        /// </summary>
        public int MacroId
        {
            get => _macroId;
            set
            {
                _macroId = value;
                OnPropertyChanged(nameof(MacroId));
            }
        }

        /// <summary>
        /// 인식된 텍스트 (음성 인식 결과)
        /// </summary>
        public string RecognizedText
        {
            get => _recognizedText;
            set
            {
                _recognizedText = value;
                OnPropertyChanged(nameof(RecognizedText));
            }
        }

        /// <summary>
        /// 매크로 실행 여부
        /// </summary>
        public bool IsExecuted
        {
            get => _isExecuted;
            set
            {
                _isExecuted = value;
                OnPropertyChanged(nameof(IsExecuted));
                OnPropertyChanged(nameof(ExecutionStatusText));
                OnPropertyChanged(nameof(ExecutionStatusColor));
            }
        }

        /// <summary>
        /// 실행 상태 텍스트 (UI 표시용)
        /// </summary>
        public string ExecutionStatusText => IsExecuted ? "실행됨" : "대기중";

        /// <summary>
        /// 실행 상태 색상 (UI 표시용)
        /// </summary>
        public string ExecutionStatusColor => IsExecuted ? "#4CAF50" : "#FF9800";

        /// <summary>
        /// 프로퍼티 변경 이벤트
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 프로퍼티 변경 알림을 발생시키는 헬퍼 메서드
        /// </summary>
        /// <param name="propertyName">변경된 프로퍼티 이름</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 