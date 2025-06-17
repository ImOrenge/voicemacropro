using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// 매크로 동작 타입을 정의하는 열거형
    /// PRD 3.3.2에서 정의된 5가지 매크로 타입
    /// </summary>
    public enum MacroActionType
    {
        /// <summary>
        /// 콤보: 여러 키를 순차적으로 입력
        /// </summary>
        Combo,

        /// <summary>
        /// 연사: 특정 키를 빠르게 반복 입력
        /// </summary>
        Rapid,

        /// <summary>
        /// 홀드: 키를 눌러서 유지하는 동작
        /// </summary>
        Hold,

        /// <summary>
        /// 토글: 키를 한 번 누르면 ON, 다시 누르면 OFF
        /// </summary>
        Toggle,

        /// <summary>
        /// 반복: 특정 동작을 지정된 횟수만큼 반복
        /// </summary>
        Repeat
    }

    /// <summary>
    /// 매크로 설정의 기본 인터페이스
    /// 모든 매크로 타입별 설정이 구현해야 하는 공통 인터페이스
    /// </summary>
    public interface IMacroActionSettings : INotifyPropertyChanged
    {
        /// <summary>
        /// 매크로 동작 타입
        /// </summary>
        MacroActionType ActionType { get; }

        /// <summary>
        /// 설정을 JSON 문자열로 변환
        /// </summary>
        string ToJsonString();

        /// <summary>
        /// JSON 문자열에서 설정을 복원
        /// </summary>
        void FromJsonString(string json);

        /// <summary>
        /// 설정 유효성 검증
        /// </summary>
        bool IsValid(out string errorMessage);
    }

    /// <summary>
    /// PropertyChanged 이벤트를 위한 기본 클래스
    /// </summary>
    public abstract class MacroActionSettingsBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// 콤보 매크로 설정
    /// 여러 키를 순차적으로 입력하는 매크로
    /// </summary>
    public class ComboActionSettings : MacroActionSettingsBase, IMacroActionSettings
    {
        private List<ComboStep> _steps = new();
        private int _defaultDelayMs = 100;

        /// <summary>
        /// 매크로 동작 타입
        /// </summary>
        public MacroActionType ActionType => MacroActionType.Combo;

        /// <summary>
        /// 콤보 단계 목록
        /// </summary>
        public List<ComboStep> Steps
        {
            get => _steps;
            set => SetProperty(ref _steps, value);
        }

        /// <summary>
        /// 기본 딜레이 (밀리초)
        /// </summary>
        public int DefaultDelayMs
        {
            get => _defaultDelayMs;
            set => SetProperty(ref _defaultDelayMs, Math.Max(1, value));
        }

        public string ToJsonString()
        {
            var data = new { Steps, DefaultDelayMs };
            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        public void FromJsonString(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                // JSON 파싱 구현 (추후 상세 구현)
            }
            catch (Exception)
            {
                // 기본값으로 복원
                Steps = new List<ComboStep>();
                DefaultDelayMs = 100;
            }
        }

        public bool IsValid(out string errorMessage)
        {
            if (Steps.Count == 0)
            {
                errorMessage = "콤보 단계가 하나도 설정되지 않았습니다.";
                return false;
            }

            if (DefaultDelayMs < 1)
            {
                errorMessage = "기본 딜레이는 1ms 이상이어야 합니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// 콤보의 개별 단계를 나타내는 클래스
    /// </summary>
    public class ComboStep : MacroActionSettingsBase
    {
        private string _keySequence = string.Empty;
        private int _delayAfterMs = 100;
        private string _description = string.Empty;

        /// <summary>
        /// 키 시퀀스 (예: "Ctrl+C", "Alt+Tab", "Enter")
        /// </summary>
        public string KeySequence
        {
            get => _keySequence;
            set => SetProperty(ref _keySequence, value ?? string.Empty);
        }

        /// <summary>
        /// 이 단계 후 대기 시간 (밀리초)
        /// </summary>
        public int DelayAfterMs
        {
            get => _delayAfterMs;
            set => SetProperty(ref _delayAfterMs, Math.Max(0, value));
        }

        /// <summary>
        /// 단계 설명
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }
    }

    /// <summary>
    /// 연사 매크로 설정
    /// 특정 키를 빠르게 반복 입력하는 매크로
    /// </summary>
    public class RapidActionSettings : MacroActionSettingsBase, IMacroActionSettings
    {
        private string _keySequence = string.Empty;
        private double _clicksPerSecond = 10.0;
        private double _durationSeconds = 1.0;
        private bool _useFixedDuration = true;

        /// <summary>
        /// 매크로 동작 타입
        /// </summary>
        public MacroActionType ActionType => MacroActionType.Rapid;

        /// <summary>
        /// 연사할 키 시퀀스
        /// </summary>
        public string KeySequence
        {
            get => _keySequence;
            set => SetProperty(ref _keySequence, value ?? string.Empty);
        }

        /// <summary>
        /// 연사 속도 (CPS - Clicks Per Second)
        /// </summary>
        public double ClicksPerSecond
        {
            get => _clicksPerSecond;
            set => SetProperty(ref _clicksPerSecond, Math.Max(0.1, Math.Min(100.0, value)));
        }

        /// <summary>
        /// 연사 지속 시간 (초)
        /// </summary>
        public double DurationSeconds
        {
            get => _durationSeconds;
            set => SetProperty(ref _durationSeconds, Math.Max(0.1, value));
        }

        /// <summary>
        /// 고정 지속 시간 사용 여부
        /// false인 경우 다시 명령어를 말할 때까지 계속 연사
        /// </summary>
        public bool UseFixedDuration
        {
            get => _useFixedDuration;
            set => SetProperty(ref _useFixedDuration, value);
        }

        public string ToJsonString()
        {
            var data = new { KeySequence, ClicksPerSecond, DurationSeconds, UseFixedDuration };
            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        public void FromJsonString(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                // JSON 파싱 구현 (추후 상세 구현)
            }
            catch (Exception)
            {
                // 기본값으로 복원
                KeySequence = string.Empty;
                ClicksPerSecond = 10.0;
                DurationSeconds = 1.0;
                UseFixedDuration = true;
            }
        }

        public bool IsValid(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(KeySequence))
            {
                errorMessage = "연사할 키 시퀀스가 설정되지 않았습니다.";
                return false;
            }

            if (ClicksPerSecond < 0.1 || ClicksPerSecond > 100.0)
            {
                errorMessage = "연사 속도는 0.1~100 CPS 범위여야 합니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// 홀드 매크로 설정
    /// 키를 눌러서 유지하는 매크로
    /// </summary>
    public class HoldActionSettings : MacroActionSettingsBase, IMacroActionSettings
    {
        private string _keySequence = string.Empty;
        private double _holdDurationSeconds = 1.0;
        private bool _useFixedDuration = true;
        private string _releaseCommand = string.Empty;

        /// <summary>
        /// 매크로 동작 타입
        /// </summary>
        public MacroActionType ActionType => MacroActionType.Hold;

        /// <summary>
        /// 홀드할 키 시퀀스
        /// </summary>
        public string KeySequence
        {
            get => _keySequence;
            set => SetProperty(ref _keySequence, value ?? string.Empty);
        }

        /// <summary>
        /// 홀드 지속 시간 (초)
        /// </summary>
        public double HoldDurationSeconds
        {
            get => _holdDurationSeconds;
            set => SetProperty(ref _holdDurationSeconds, Math.Max(0.1, value));
        }

        /// <summary>
        /// 고정 지속 시간 사용 여부
        /// false인 경우 해제 명령어를 말할 때까지 계속 홀드
        /// </summary>
        public bool UseFixedDuration
        {
            get => _useFixedDuration;
            set => SetProperty(ref _useFixedDuration, value);
        }

        /// <summary>
        /// 해제 명령어 (UseFixedDuration이 false일 때 사용)
        /// </summary>
        public string ReleaseCommand
        {
            get => _releaseCommand;
            set => SetProperty(ref _releaseCommand, value ?? string.Empty);
        }

        public string ToJsonString()
        {
            var data = new { KeySequence, HoldDurationSeconds, UseFixedDuration, ReleaseCommand };
            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        public void FromJsonString(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                // JSON 파싱 구현 (추후 상세 구현)
            }
            catch (Exception)
            {
                // 기본값으로 복원
                KeySequence = string.Empty;
                HoldDurationSeconds = 1.0;
                UseFixedDuration = true;
                ReleaseCommand = string.Empty;
            }
        }

        public bool IsValid(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(KeySequence))
            {
                errorMessage = "홀드할 키 시퀀스가 설정되지 않았습니다.";
                return false;
            }

            if (!UseFixedDuration && string.IsNullOrWhiteSpace(ReleaseCommand))
            {
                errorMessage = "수동 해제 모드에서는 해제 명령어가 필요합니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// 토글 매크로 설정
    /// 키를 한 번 누르면 ON, 다시 누르면 OFF
    /// </summary>
    public class ToggleActionSettings : MacroActionSettingsBase, IMacroActionSettings
    {
        private string _keySequence = string.Empty;
        private string _toggleOffCommand = string.Empty;
        private bool _showStatusIndicator = true;
        private bool _isCurrentlyOn = false;

        /// <summary>
        /// 매크로 동작 타입
        /// </summary>
        public MacroActionType ActionType => MacroActionType.Toggle;

        /// <summary>
        /// 토글할 키 시퀀스
        /// </summary>
        public string KeySequence
        {
            get => _keySequence;
            set => SetProperty(ref _keySequence, value ?? string.Empty);
        }

        /// <summary>
        /// 토글 해제 명령어 (선택사항, 기본은 동일 명령어)
        /// </summary>
        public string ToggleOffCommand
        {
            get => _toggleOffCommand;
            set => SetProperty(ref _toggleOffCommand, value ?? string.Empty);
        }

        /// <summary>
        /// 상태 표시기 사용 여부
        /// </summary>
        public bool ShowStatusIndicator
        {
            get => _showStatusIndicator;
            set => SetProperty(ref _showStatusIndicator, value);
        }

        /// <summary>
        /// 현재 토글 상태 (ON/OFF)
        /// </summary>
        public bool IsCurrentlyOn
        {
            get => _isCurrentlyOn;
            set => SetProperty(ref _isCurrentlyOn, value);
        }

        public string ToJsonString()
        {
            var data = new { KeySequence, ToggleOffCommand, ShowStatusIndicator, IsCurrentlyOn };
            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        public void FromJsonString(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                // JSON 파싱 구현 (추후 상세 구현)
            }
            catch (Exception)
            {
                // 기본값으로 복원
                KeySequence = string.Empty;
                ToggleOffCommand = string.Empty;
                ShowStatusIndicator = true;
                IsCurrentlyOn = false;
            }
        }

        public bool IsValid(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(KeySequence))
            {
                errorMessage = "토글할 키 시퀀스가 설정되지 않았습니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// 반복 매크로 설정
    /// 특정 동작을 지정된 횟수만큼 반복
    /// </summary>
    public class RepeatActionSettings : MacroActionSettingsBase, IMacroActionSettings
    {
        private string _keySequence = string.Empty;
        private int _repeatCount = 3;
        private double _intervalSeconds = 0.5;
        private bool _stopOnNextCommand = false;

        /// <summary>
        /// 매크로 동작 타입
        /// </summary>
        public MacroActionType ActionType => MacroActionType.Repeat;

        /// <summary>
        /// 반복할 키 시퀀스
        /// </summary>
        public string KeySequence
        {
            get => _keySequence;
            set => SetProperty(ref _keySequence, value ?? string.Empty);
        }

        /// <summary>
        /// 반복 횟수
        /// </summary>
        public int RepeatCount
        {
            get => _repeatCount;
            set => SetProperty(ref _repeatCount, Math.Max(1, value));
        }

        /// <summary>
        /// 반복 간격 (초)
        /// </summary>
        public double IntervalSeconds
        {
            get => _intervalSeconds;
            set => SetProperty(ref _intervalSeconds, Math.Max(0.1, value));
        }

        /// <summary>
        /// 다음 음성 명령 시 중단 여부
        /// </summary>
        public bool StopOnNextCommand
        {
            get => _stopOnNextCommand;
            set => SetProperty(ref _stopOnNextCommand, value);
        }

        public string ToJsonString()
        {
            var data = new { KeySequence, RepeatCount, IntervalSeconds, StopOnNextCommand };
            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        public void FromJsonString(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                // JSON 파싱 구현 (추후 상세 구현)
            }
            catch (Exception)
            {
                // 기본값으로 복원
                KeySequence = string.Empty;
                RepeatCount = 3;
                IntervalSeconds = 0.5;
                StopOnNextCommand = false;
            }
        }

        public bool IsValid(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(KeySequence))
            {
                errorMessage = "반복할 키 시퀀스가 설정되지 않았습니다.";
                return false;
            }

            if (RepeatCount < 1)
            {
                errorMessage = "반복 횟수는 1회 이상이어야 합니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// 키 입력 정보를 나타내는 클래스
    /// </summary>
    public class KeyInputInfo : MacroActionSettingsBase
    {
        private string _displayName = string.Empty;
        private string _virtualKeyCode = string.Empty;
        private bool _isModifier = false;

        /// <summary>
        /// 표시 이름 (예: "Ctrl", "Alt", "Enter")
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value ?? string.Empty);
        }

        /// <summary>
        /// 가상 키 코드 (PyAutoGUI에서 사용)
        /// </summary>
        public string VirtualKeyCode
        {
            get => _virtualKeyCode;
            set => SetProperty(ref _virtualKeyCode, value ?? string.Empty);
        }

        /// <summary>
        /// 수정키 여부 (Ctrl, Alt, Shift 등)
        /// </summary>
        public bool IsModifier
        {
            get => _isModifier;
            set => SetProperty(ref _isModifier, value);
        }
    }
} 