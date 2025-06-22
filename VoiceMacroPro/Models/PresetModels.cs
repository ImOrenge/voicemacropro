using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// 프리셋 정보를 나타내는 기본 모델 클래스
    /// 매크로 집합을 그룹화하여 게임별/상황별로 관리할 수 있습니다.
    /// </summary>
    public class PresetModel : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _description;
        private List<int> _macroIds;
        private int _macroCount;
        private bool _isFavorite;
        private bool _isActive;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private List<Macro> _macros;

        /// <summary>프리셋 고유 ID</summary>
        [JsonProperty("id")]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>프리셋 이름</summary>
        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>프리셋 설명</summary>
        [JsonProperty("description")]
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>포함된 매크로 ID 목록</summary>
        [JsonProperty("macro_ids")]
        public List<int> MacroIds
        {
            get => _macroIds ?? (_macroIds = new List<int>());
            set => SetProperty(ref _macroIds, value);
        }

        /// <summary>포함된 매크로 개수</summary>
        [JsonProperty("macro_count")]
        public int MacroCount
        {
            get => _macroCount;
            set => SetProperty(ref _macroCount, value);
        }

        /// <summary>즐겨찾기 여부</summary>
        [JsonProperty("is_favorite")]
        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        /// <summary>활성 상태 여부</summary>
        [JsonProperty("is_active")]
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>생성일시</summary>
        [JsonProperty("created_at")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        /// <summary>수정일시</summary>
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        /// <summary>포함된 매크로들의 상세 정보 (서버에서 조회시 포함)</summary>
        [JsonProperty("macros")]
        public List<Macro> Macros
        {
            get => _macros ?? (_macros = new List<Macro>());
            set => SetProperty(ref _macros, value);
        }

        /// <summary>UI 표시용 즐겨찾기 아이콘</summary>
        [JsonIgnore]
        public string FavoriteIcon => IsFavorite ? "⭐" : "☆";

        /// <summary>UI 표시용 생성일 문자열</summary>
        [JsonIgnore]
        public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd");

        /// <summary>UI 표시용 매크로 개수 문자열</summary>
        [JsonIgnore]
        public string MacroCountText => $"{MacroCount}개 매크로";

        /// <summary>UI 표시용 설명 (빈 값일 경우 기본 메시지)</summary>
        [JsonIgnore]
        public string DisplayDescription => string.IsNullOrWhiteSpace(Description) ? "설명 없음" : Description;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = default)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = default)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// 프리셋 생성용 요청 모델
    /// </summary>
    public class CreatePresetRequest
    {
        /// <summary>프리셋 이름 (필수)</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>프리셋 설명 (선택사항)</summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>포함할 매크로 ID 목록</summary>
        [JsonProperty("macro_ids")]
        public List<int> MacroIds { get; set; } = new List<int>();

        /// <summary>즐겨찾기 여부 (기본값: false)</summary>
        [JsonProperty("is_favorite")]
        public bool IsFavorite { get; set; } = false;
    }

    /// <summary>
    /// 프리셋 수정용 요청 모델
    /// </summary>
    public class UpdatePresetRequest
    {
        /// <summary>프리셋 이름 (선택사항)</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>프리셋 설명 (선택사항)</summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>포함할 매크로 ID 목록 (선택사항)</summary>
        [JsonProperty("macro_ids")]
        public List<int> MacroIds { get; set; }

        /// <summary>즐겨찾기 여부 (선택사항)</summary>
        [JsonProperty("is_favorite")]
        public bool? IsFavorite { get; set; }
    }

    /// <summary>
    /// 프리셋 복사용 요청 모델
    /// </summary>
    public class CopyPresetRequest
    {
        /// <summary>새로운 프리셋 이름 (선택사항)</summary>
        [JsonProperty("new_name")]
        public string NewName { get; set; }
    }

    /// <summary>
    /// 프리셋 내보내기용 요청 모델
    /// </summary>
    public class ExportPresetRequest
    {
        /// <summary>저장할 파일 경로 (선택사항)</summary>
        [JsonProperty("file_path")]
        public string FilePath { get; set; }
    }

    /// <summary>
    /// 프리셋 가져오기용 요청 모델
    /// </summary>
    public class ImportPresetRequest
    {
        /// <summary>가져올 JSON 파일 경로 (필수)</summary>
        [JsonProperty("file_path")]
        public string FilePath { get; set; }

        /// <summary>새로운 프리셋 이름 (선택사항)</summary>
        [JsonProperty("preset_name")]
        public string PresetName { get; set; }
    }

    /// <summary>
    /// 프리셋 통계 정보 모델
    /// </summary>
    public class PresetStatisticsModel
    {
        /// <summary>전체 프리셋 수</summary>
        [JsonProperty("total_presets")]
        public int TotalPresets { get; set; }

        /// <summary>즐겨찾기 프리셋 수</summary>
        [JsonProperty("favorite_presets")]
        public int FavoritePresets { get; set; }

        /// <summary>최근 생성된 프리셋 정보</summary>
        [JsonProperty("most_recent_preset")]
        public RecentPresetInfo MostRecentPreset { get; set; }

        /// <summary>UI 표시용 즐겨찾기 비율</summary>
        [JsonIgnore]
        public double FavoritePercentage =>
            TotalPresets > 0 ? (double)FavoritePresets / TotalPresets * 100 : 0;

        /// <summary>UI 표시용 즐겨찾기 비율 문자열</summary>
        [JsonIgnore]
        public string FavoritePercentageText => $"{FavoritePercentage:F1}%";
    }

    /// <summary>
    /// 최근 프리셋 정보
    /// </summary>
    public class RecentPresetInfo
    {
        /// <summary>프리셋 이름</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>생성일시</summary>
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>UI 표시용 생성일 문자열</summary>
        [JsonIgnore]
        public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");
    }

    /// <summary>
    /// 프리셋 목록용 뷰모델
    /// DataGrid에서 사용하기 위한 추가 프로퍼티를 포함합니다.
    /// </summary>
    public class PresetListItemViewModel : PresetModel
    {
        private bool _isSelected;

        /// <summary>목록에서 선택 여부</summary>
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>UI 표시용 상태 텍스트</summary>
        [JsonIgnore]
        public string StatusText => IsActive ? "활성" : "비활성";

        /// <summary>UI 표시용 상태 색상</summary>
        [JsonIgnore]
        public string StatusColor => IsActive ? "#28A745" : "#6C757D";

        /// <summary>UI 표시용 프리셋 요약</summary>
        [JsonIgnore]
        public string PresetSummary
        {
            get
            {
                var summary = $"{MacroCount}개 매크로";
                if (IsFavorite)
                    summary += " ⭐";
                return summary;
            }
        }
    }

    /// <summary>
    /// 프리셋 내보내기/가져오기 결과 모델
    /// </summary>
    public class PresetFileOperationResult
    {
        /// <summary>파일 경로</summary>
        [JsonProperty("file_path")]
        public string FilePath { get; set; }

        /// <summary>파일명</summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; }

        /// <summary>프리셋 ID (가져오기 시)</summary>
        [JsonProperty("id")]
        public int? Id { get; set; }
    }

    /// <summary>
    /// 프리셋 적용 결과 모델
    /// </summary>
    public class PresetApplicationResult
    {
        /// <summary>적용된 프리셋 ID</summary>
        [JsonProperty("preset_id")]
        public int PresetId { get; set; }

        /// <summary>적용된 프리셋 이름</summary>
        [JsonProperty("preset_name")]
        public string PresetName { get; set; }

        /// <summary>포함된 매크로 개수</summary>
        [JsonProperty("macro_count")]
        public int MacroCount { get; set; }
    }

    /// <summary>
    /// 즐겨찾기 토글 결과 모델
    /// </summary>
    public class FavoriteToggleResult
    {
        /// <summary>새로운 즐겨찾기 상태</summary>
        [JsonProperty("is_favorite")]
        public bool IsFavorite { get; set; }
    }

    /// <summary>
    /// 프리셋 관련 작업 결과를 나타내는 일반적인 모델
    /// </summary>
    public class PresetOperationResult<T>
    {
        /// <summary>작업 성공 여부</summary>
        public bool Success { get; set; }

        /// <summary>결과 데이터</summary>
        public T Data { get; set; }

        /// <summary>메시지</summary>
        public string Message { get; set; }

        /// <summary>오류 정보 (실패시)</summary>
        public string Error { get; set; }

        /// <summary>성공 결과 생성</summary>
        public static PresetOperationResult<T> SuccessResult(T data, string message = "")
        {
            return new PresetOperationResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>실패 결과 생성</summary>
        public static PresetOperationResult<T> FailureResult(string error, string message = "")
        {
            return new PresetOperationResult<T>
            {
                Success = false,
                Error = error,
                Message = message
            };
        }
    }

    /// <summary>
    /// 프리셋 검색/필터링 옵션
    /// </summary>
    public class PresetSearchOptions
    {
        /// <summary>검색어</summary>
        public string SearchTerm { get; set; }

        /// <summary>즐겨찾기만 조회</summary>
        public bool FavoritesOnly { get; set; }

        /// <summary>비활성 프리셋 포함</summary>
        public bool IncludeInactive { get; set; }

        /// <summary>정렬 기준 (name, created_at, macro_count, favorite)</summary>
        public string SortBy { get; set; } = "name";

        /// <summary>정렬 순서 (asc, desc)</summary>
        public string SortOrder { get; set; } = "asc";
    }
} 