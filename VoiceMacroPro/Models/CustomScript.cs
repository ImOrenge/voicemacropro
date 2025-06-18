using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// 커스텀 스크립트 모델 클래스
    /// MSL(Macro Scripting Language)로 작성된 사용자 정의 스크립트를 나타냅니다.
    /// </summary>
    public class CustomScript : INotifyPropertyChanged
    {
        private int _id;
        private string _name = "";
        private string _description = "";
        private string _scriptCode = "";
        private string _scriptLanguage = "MSL";
        private bool _isActive = true;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private int _usageCount;
        private string _category = "";
        private string _gameTarget = "";
        private int _scriptVersion = 1;
        private double _executionTimeAvg;
        private double _successRate;

        /// <summary>
        /// 스크립트 고유 ID
        /// </summary>
        [JsonProperty("id")]
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        /// <summary>
        /// 스크립트 이름
        /// </summary>
        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value ?? "";
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// 스크립트 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description
        {
            get => _description;
            set
            {
                _description = value ?? "";
                OnPropertyChanged(nameof(Description));
            }
        }

        /// <summary>
        /// MSL 스크립트 코드
        /// </summary>
        [JsonProperty("script_code")]
        public string ScriptCode
        {
            get => _scriptCode;
            set
            {
                _scriptCode = value ?? "";
                OnPropertyChanged(nameof(ScriptCode));
            }
        }

        /// <summary>
        /// 스크립트 언어 (기본값: MSL)
        /// </summary>
        [JsonProperty("script_language")]
        public string ScriptLanguage
        {
            get => _scriptLanguage;
            set
            {
                _scriptLanguage = value ?? "MSL";
                OnPropertyChanged(nameof(ScriptLanguage));
            }
        }

        /// <summary>
        /// 스크립트 활성화 상태
        /// </summary>
        [JsonProperty("is_active")]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        /// <summary>
        /// 생성일시
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                _createdAt = value;
                OnPropertyChanged(nameof(CreatedAt));
            }
        }

        /// <summary>
        /// 수정일시
        /// </summary>
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set
            {
                _updatedAt = value;
                OnPropertyChanged(nameof(UpdatedAt));
            }
        }

        /// <summary>
        /// 사용 횟수
        /// </summary>
        [JsonProperty("usage_count")]
        public int UsageCount
        {
            get => _usageCount;
            set
            {
                _usageCount = value;
                OnPropertyChanged(nameof(UsageCount));
            }
        }

        /// <summary>
        /// 스크립트 카테고리 (예: 전투, 이동, 상호작용 등)
        /// </summary>
        [JsonProperty("category")]
        public string Category
        {
            get => _category;
            set
            {
                _category = value ?? "";
                OnPropertyChanged(nameof(Category));
            }
        }

        /// <summary>
        /// 대상 게임 (예: LOL, PUBG, VALORANT 등)
        /// </summary>
        [JsonProperty("game_target")]
        public string GameTarget
        {
            get => _gameTarget;
            set
            {
                _gameTarget = value ?? "";
                OnPropertyChanged(nameof(GameTarget));
            }
        }

        /// <summary>
        /// 스크립트 버전
        /// </summary>
        [JsonProperty("script_version")]
        public int ScriptVersion
        {
            get => _scriptVersion;
            set
            {
                _scriptVersion = value;
                OnPropertyChanged(nameof(ScriptVersion));
            }
        }

        /// <summary>
        /// 평균 실행 시간 (밀리초)
        /// </summary>
        [JsonProperty("execution_time_avg")]
        public double ExecutionTimeAvg
        {
            get => _executionTimeAvg;
            set
            {
                _executionTimeAvg = value;
                OnPropertyChanged(nameof(ExecutionTimeAvg));
                OnPropertyChanged(nameof(ExecutionTimeText));
            }
        }

        /// <summary>
        /// 성공률 (0.0 ~ 1.0)
        /// </summary>
        [JsonProperty("success_rate")]
        public double SuccessRate
        {
            get => _successRate;
            set
            {
                _successRate = value;
                OnPropertyChanged(nameof(SuccessRate));
                OnPropertyChanged(nameof(SuccessRateText));
                OnPropertyChanged(nameof(SuccessRateColor));
            }
        }

        // UI 바인딩용 읽기 전용 프로퍼티들

        /// <summary>
        /// 상태 텍스트 (활성/비활성)
        /// </summary>
        public string StatusText => IsActive ? "활성" : "비활성";

        /// <summary>
        /// 상태 색상 (활성: 초록, 비활성: 빨강)
        /// </summary>
        public string StatusColor => IsActive ? "#4CAF50" : "#F44336";

        /// <summary>
        /// 실행 시간 텍스트 (ms 단위)
        /// </summary>
        public string ExecutionTimeText => $"{ExecutionTimeAvg:F1}ms";

        /// <summary>
        /// 성공률 텍스트 (퍼센트)
        /// </summary>
        public string SuccessRateText => $"{SuccessRate * 100:F1}%";

        /// <summary>
        /// 성공률 색상 (높음: 초록, 중간: 주황, 낮음: 빨강)
        /// </summary>
        public string SuccessRateColor
        {
            get
            {
                if (SuccessRate >= 0.8) return "#4CAF50"; // 초록 (80% 이상)
                if (SuccessRate >= 0.6) return "#FF9800"; // 주황 (60-80%)
                return "#F44336"; // 빨강 (60% 미만)
            }
        }

        /// <summary>
        /// 스크립트 코드 미리보기 (첫 50자)
        /// </summary>
        public string CodePreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ScriptCode))
                    return "코드 없음";

                var preview = ScriptCode.Replace("\n", " ").Replace("\r", "").Trim();
                return preview.Length > 50 ? preview.Substring(0, 50) + "..." : preview;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 스크립트 템플릿 모델 클래스
    /// 미리 정의된 MSL 스크립트 템플릿을 나타냅니다.
    /// </summary>
    public class ScriptTemplate : INotifyPropertyChanged
    {
        private int _id;
        private string _name = "";
        private string _description = "";
        private string _category = "";
        private string _gameTarget = "";
        private string _templateCode = "";
        private string _difficultyLevel = "초급";
        private int _usageCount;
        private DateTime _createdAt;

        /// <summary>
        /// 템플릿 고유 ID
        /// </summary>
        [JsonProperty("id")]
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        /// <summary>
        /// 템플릿 이름
        /// </summary>
        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                _name = value ?? "";
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// 템플릿 설명
        /// </summary>
        [JsonProperty("description")]
        public string Description
        {
            get => _description;
            set
            {
                _description = value ?? "";
                OnPropertyChanged(nameof(Description));
            }
        }

        /// <summary>
        /// 템플릿 카테고리
        /// </summary>
        [JsonProperty("category")]
        public string Category
        {
            get => _category;
            set
            {
                _category = value ?? "";
                OnPropertyChanged(nameof(Category));
            }
        }

        /// <summary>
        /// 대상 게임
        /// </summary>
        [JsonProperty("game_target")]
        public string GameTarget
        {
            get => _gameTarget;
            set
            {
                _gameTarget = value ?? "";
                OnPropertyChanged(nameof(GameTarget));
            }
        }

        /// <summary>
        /// 템플릿 MSL 코드
        /// </summary>
        [JsonProperty("template_code")]
        public string TemplateCode
        {
            get => _templateCode;
            set
            {
                _templateCode = value ?? "";
                OnPropertyChanged(nameof(TemplateCode));
            }
        }

        /// <summary>
        /// 난이도 (초급, 중급, 고급)
        /// </summary>
        [JsonProperty("difficulty_level")]
        public string DifficultyLevel
        {
            get => _difficultyLevel;
            set
            {
                _difficultyLevel = value ?? "초급";
                OnPropertyChanged(nameof(DifficultyLevel));
                OnPropertyChanged(nameof(DifficultyColor));
            }
        }

        /// <summary>
        /// 사용 횟수
        /// </summary>
        [JsonProperty("usage_count")]
        public int UsageCount
        {
            get => _usageCount;
            set
            {
                _usageCount = value;
                OnPropertyChanged(nameof(UsageCount));
            }
        }

        /// <summary>
        /// 생성일시
        /// </summary>
        [JsonProperty("created_at")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                _createdAt = value;
                OnPropertyChanged(nameof(CreatedAt));
            }
        }

        // UI 바인딩용 읽기 전용 프로퍼티들

        /// <summary>
        /// 난이도별 색상 (초급: 초록, 중급: 주황, 고급: 빨강)
        /// </summary>
        public string DifficultyColor
        {
            get
            {
                return DifficultyLevel switch
                {
                    "초급" => "#4CAF50",  // 초록
                    "중급" => "#FF9800",  // 주황
                    "고급" => "#F44336",  // 빨강
                    _ => "#9E9E9E"        // 회색
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 스크립트 실행 로그 모델 클래스
    /// </summary>
    public class ScriptExecutionLog
    {
        /// <summary>
        /// 로그 고유 ID
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// 스크립트 ID
        /// </summary>
        [JsonProperty("script_id")]
        public int ScriptId { get; set; }

        /// <summary>
        /// 스크립트 이름
        /// </summary>
        [JsonProperty("script_name")]
        public string ScriptName { get; set; } = "";

        /// <summary>
        /// 실행 시간 (밀리초)
        /// </summary>
        [JsonProperty("execution_time_ms")]
        public double ExecutionTimeMs { get; set; }

        /// <summary>
        /// 실행 성공 여부
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 오류 메시지 (실패시)
        /// </summary>
        [JsonProperty("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 실행일시
        /// </summary>
        [JsonProperty("executed_at")]
        public DateTime ExecutedAt { get; set; }

        // UI 바인딩용 읽기 전용 프로퍼티들

        /// <summary>
        /// 성공/실패 텍스트
        /// </summary>
        public string StatusText => Success ? "성공" : "실패";

        /// <summary>
        /// 상태 색상
        /// </summary>
        public string StatusColor => Success ? "#4CAF50" : "#F44336";

        /// <summary>
        /// 실행 시간 텍스트
        /// </summary>
        public string ExecutionTimeText => $"{ExecutionTimeMs:F1}ms";
    }

    /// <summary>
    /// 스크립트 성능 분석 모델 클래스
    /// </summary>
    public class ScriptPerformanceStats
    {
        /// <summary>
        /// 스크립트 ID
        /// </summary>
        [JsonProperty("script_id")]
        public int ScriptId { get; set; }

        /// <summary>
        /// 스크립트 이름
        /// </summary>
        [JsonProperty("script_name")]
        public string ScriptName { get; set; } = "";

        /// <summary>
        /// 총 실행 횟수
        /// </summary>
        [JsonProperty("total_executions")]
        public int TotalExecutions { get; set; }

        /// <summary>
        /// 성공한 실행 횟수
        /// </summary>
        [JsonProperty("successful_executions")]
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// 평균 실행 시간 (밀리초)
        /// </summary>
        [JsonProperty("avg_execution_time")]
        public double AvgExecutionTime { get; set; }

        /// <summary>
        /// 최소 실행 시간 (밀리초)
        /// </summary>
        [JsonProperty("min_execution_time")]
        public double MinExecutionTime { get; set; }

        /// <summary>
        /// 최대 실행 시간 (밀리초)
        /// </summary>
        [JsonProperty("max_execution_time")]
        public double MaxExecutionTime { get; set; }

        /// <summary>
        /// 성공률 (0.0 ~ 1.0)
        /// </summary>
        [JsonProperty("success_rate")]
        public double SuccessRate { get; set; }

        /// <summary>
        /// 마지막 업데이트 일시
        /// </summary>
        [JsonProperty("last_updated")]
        public DateTime LastUpdated { get; set; }

        // UI 바인딩용 읽기 전용 프로퍼티들

        /// <summary>
        /// 성공률 텍스트 (퍼센트)
        /// </summary>
        public string SuccessRateText => $"{SuccessRate * 100:F1}%";

        /// <summary>
        /// 평균 실행 시간 텍스트
        /// </summary>
        public string AvgExecutionTimeText => $"{AvgExecutionTime:F1}ms";

        /// <summary>
        /// 실행 범위 텍스트 (최소~최대)
        /// </summary>
        public string ExecutionRangeText => $"{MinExecutionTime:F1}ms ~ {MaxExecutionTime:F1}ms";
    }

    /// <summary>
    /// 스크립트 검증 결과 모델 클래스
    /// </summary>
    public class ScriptValidationResult
    {
        /// <summary>
        /// 검증 성공 여부
        /// </summary>
        [JsonProperty("is_valid")]
        public bool IsValid { get; set; }

        /// <summary>
        /// 오류 메시지 목록
        /// </summary>
        [JsonProperty("errors")]
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 경고 메시지 목록
        /// </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 검증된 토큰 정보
        /// </summary>
        [JsonProperty("tokens")]
        public List<object> Tokens { get; set; } = new List<object>();

        /// <summary>
        /// AST (추상 구문 트리) 정보
        /// </summary>
        [JsonProperty("ast")]
        public object? Ast { get; set; }

        /// <summary>
        /// 검증 시간 (밀리초)
        /// </summary>
        [JsonProperty("validation_time_ms")]
        public double ValidationTimeMs { get; set; }

        // UI 바인딩용 읽기 전용 프로퍼티들

        /// <summary>
        /// 상태 텍스트
        /// </summary>
        public string StatusText => IsValid ? "유효함" : "오류 있음";

        /// <summary>
        /// 상태 색상
        /// </summary>
        public string StatusColor => IsValid ? "#4CAF50" : "#F44336";

        /// <summary>
        /// 오류/경고 개수 텍스트
        /// </summary>
        public string IssueCountText
        {
            get
            {
                var errorCount = Errors?.Count ?? 0;
                var warningCount = Warnings?.Count ?? 0;
                
                if (errorCount == 0 && warningCount == 0)
                    return "문제 없음";
                
                var parts = new List<string>();
                if (errorCount > 0) parts.Add($"오류 {errorCount}개");
                if (warningCount > 0) parts.Add($"경고 {warningCount}개");
                
                return string.Join(", ", parts);
            }
        }
    }
} 