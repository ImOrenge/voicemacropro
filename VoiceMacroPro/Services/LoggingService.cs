using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace VoiceMacroPro.Services
{
    /// <summary>
    /// 로그 레벨 열거형
    /// PRD 3.5.2에 정의된 로그 레벨을 구현합니다.
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// 로그 항목을 나타내는 클래스
    /// UI 바인딩을 위해 INotifyPropertyChanged를 구현합니다.
    /// </summary>
    public class LogEntry : INotifyPropertyChanged
    {
        private string _message = string.Empty;
        private LogLevel _level = LogLevel.Info;
        private DateTime _timestamp = DateTime.Now;
        private int? _macroId;

        /// <summary>
        /// 로그 메시지
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 로그 레벨
        /// </summary>
        public LogLevel Level
        {
            get => _level;
            set
            {
                _level = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LevelText));
                OnPropertyChanged(nameof(LevelColor));
            }
        }

        /// <summary>
        /// 로그 발생 시간
        /// </summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeText));
            }
        }

        /// <summary>
        /// 관련 매크로 ID (선택사항)
        /// </summary>
        public int? MacroId
        {
            get => _macroId;
            set
            {
                _macroId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// UI 표시용 시간 텍스트
        /// </summary>
        public string TimeText => Timestamp.ToString("HH:mm:ss.fff");

        /// <summary>
        /// UI 표시용 레벨 텍스트
        /// </summary>
        public string LevelText => Level.ToString().ToUpper();

        /// <summary>
        /// UI 표시용 레벨 색상
        /// </summary>
        public string LevelColor => Level switch
        {
            LogLevel.Debug => "#6C757D",    // 회색
            LogLevel.Info => "#17A2B8",     // 청색
            LogLevel.Warning => "#FFC107",  // 황색
            LogLevel.Error => "#DC3545",    // 적색
            _ => "#000000"
        };

        /// <summary>
        /// 전체 로그 텍스트 (파일 저장용)
        /// </summary>
        public string FullText => $"[{TimeText}] [{LevelText}] {Message}" + 
                                 (MacroId.HasValue ? $" (Macro ID: {MacroId})" : "");

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 로깅 서비스 클래스
    /// PRD 3.5 로그 및 모니터링 시스템을 구현합니다.
    /// </summary>
    public class LoggingService : INotifyPropertyChanged
    {
        private static LoggingService? _instance;
        private LogLevel _currentLogLevel = LogLevel.Info;
        private bool _isAutoScroll = true;
        private string _logFilePath;

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static LoggingService Instance => _instance ??= new LoggingService();

        /// <summary>
        /// 실시간 로그 항목 컬렉션 (UI 바인딩용)
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries { get; } = new();

        /// <summary>
        /// 현재 로그 레벨
        /// 이 레벨보다 낮은 로그는 무시됩니다.
        /// </summary>
        public LogLevel CurrentLogLevel
        {
            get => _currentLogLevel;
            set
            {
                _currentLogLevel = value;
                OnPropertyChanged();
                LogInfo($"로그 레벨이 {value}로 변경되었습니다.");
            }
        }

        /// <summary>
        /// 자동 스크롤 활성화 여부
        /// </summary>
        public bool IsAutoScroll
        {
            get => _isAutoScroll;
            set
            {
                _isAutoScroll = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 총 로그 항목 수
        /// </summary>
        public int TotalLogCount => LogEntries.Count;

        /// <summary>
        /// 로깅 서비스 생성자
        /// 로그 파일 경로를 설정하고 초기화합니다.
        /// </summary>
        private LoggingService()
        {
            // 로그 폴더 생성
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);

            // 로그 파일 경로 설정 (날짜별)
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(logDirectory, $"VoiceMacroPro_{today}.log");

            // 서비스 시작 로그
            LogInfo("로깅 서비스가 시작되었습니다.", null);
        }

        /// <summary>
        /// Debug 레벨 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="macroId">관련 매크로 ID (선택사항)</param>
        public void LogDebug(string message, int? macroId = null)
        {
            WriteLog(LogLevel.Debug, message, macroId);
        }

        /// <summary>
        /// Info 레벨 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="macroId">관련 매크로 ID (선택사항)</param>
        public void LogInfo(string message, int? macroId = null)
        {
            WriteLog(LogLevel.Info, message, macroId);
        }

        /// <summary>
        /// Warning 레벨 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="macroId">관련 매크로 ID (선택사항)</param>
        public void LogWarning(string message, int? macroId = null)
        {
            WriteLog(LogLevel.Warning, message, macroId);
        }

        /// <summary>
        /// Error 레벨 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="macroId">관련 매크로 ID (선택사항)</param>
        public void LogError(string message, int? macroId = null)
        {
            WriteLog(LogLevel.Error, message, macroId);
        }

        /// <summary>
        /// 예외 정보를 포함한 Error 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="exception">예외 객체</param>
        /// <param name="macroId">관련 매크로 ID (선택사항)</param>
        public void LogError(string message, Exception exception, int? macroId = null)
        {
            var fullMessage = $"{message}\n예외: {exception.Message}\n스택 트레이스: {exception.StackTrace}";
            WriteLog(LogLevel.Error, fullMessage, macroId);
        }

        /// <summary>
        /// 실제 로그를 기록하는 내부 메서드
        /// </summary>
        /// <param name="level">로그 레벨</param>
        /// <param name="message">로그 메시지</param>
        /// <param name="macroId">관련 매크로 ID</param>
        private void WriteLog(LogLevel level, string message, int? macroId = null)
        {
            // 현재 설정된 로그 레벨보다 낮으면 무시
            if (level < _currentLogLevel)
                return;

            var logEntry = new LogEntry
            {
                Level = level,
                Message = message,
                MacroId = macroId,
                Timestamp = DateTime.Now
            };

            // UI 스레드에서 컬렉션 업데이트
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LogEntries.Add(logEntry);

                // 로그 항목이 너무 많으면 오래된 항목 제거 (최대 1000개 유지)
                while (LogEntries.Count > 1000)
                {
                    LogEntries.RemoveAt(0);
                }

                OnPropertyChanged(nameof(TotalLogCount));
            });

            // 파일에 비동기로 기록
            _ = Task.Run(() => WriteLogToFile(logEntry));

            // 콘솔에도 출력 (디버그 모드)
            Console.WriteLine(logEntry.FullText);
        }

        /// <summary>
        /// 로그를 파일에 기록하는 메서드
        /// </summary>
        /// <param name="logEntry">기록할 로그 항목</param>
        private async Task WriteLogToFile(LogEntry logEntry)
        {
            try
            {
                await File.AppendAllTextAsync(_logFilePath, logEntry.FullText + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // 파일 기록 실패 시 콘솔에만 출력
                Console.WriteLine($"로그 파일 기록 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 로그를 파일로 내보내기
        /// </summary>
        /// <param name="filePath">내보낼 파일 경로</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> ExportLogsAsync(string filePath)
        {
            try
            {
                var logLines = new List<string>();
                
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    foreach (var entry in LogEntries)
                    {
                        logLines.Add(entry.FullText);
                    }
                });

                await File.WriteAllLinesAsync(filePath, logLines);
                LogInfo($"로그가 성공적으로 내보내졌습니다: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"로그 내보내기 실패: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 모든 로그 항목을 지우는 메서드
        /// </summary>
        public void ClearLogs()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LogEntries.Clear();
                OnPropertyChanged(nameof(TotalLogCount));
            });
            LogInfo("모든 로그가 지워졌습니다.");
        }

        /// <summary>
        /// 매크로 실행 시작 로그
        /// </summary>
        /// <param name="macroName">매크로 이름</param>
        /// <param name="macroId">매크로 ID</param>
        public void LogMacroStart(string macroName, int macroId)
        {
            LogInfo($"매크로 실행 시작: {macroName}", macroId);
        }

        /// <summary>
        /// 매크로 실행 완료 로그
        /// </summary>
        /// <param name="macroName">매크로 이름</param>
        /// <param name="macroId">매크로 ID</param>
        /// <param name="duration">실행 시간</param>
        public void LogMacroComplete(string macroName, int macroId, TimeSpan duration)
        {
            LogInfo($"매크로 실행 완료: {macroName} (소요시간: {duration.TotalMilliseconds:F0}ms)", macroId);
        }

        /// <summary>
        /// 음성 인식 결과 로그
        /// </summary>
        /// <param name="recognizedText">인식된 텍스트</param>
        /// <param name="confidence">신뢰도</param>
        public void LogVoiceRecognition(string recognizedText, float confidence)
        {
            LogInfo($"음성 인식 결과: '{recognizedText}' (신뢰도: {confidence:P1})");
        }

        /// <summary>
        /// 매크로 매칭 결과 로그
        /// </summary>
        /// <param name="voiceCommand">음성 명령</param>
        /// <param name="matchedMacro">매칭된 매크로 이름</param>
        /// <param name="similarity">유사도</param>
        public void LogMacroMatch(string voiceCommand, string matchedMacro, float similarity)
        {
            LogInfo($"매크로 매칭: '{voiceCommand}' → '{matchedMacro}' (유사도: {similarity:P1})");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 