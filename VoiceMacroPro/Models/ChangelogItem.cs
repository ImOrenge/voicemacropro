using System;
using System.Collections.Generic;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// 변경사항 타입 열거형
    /// 각 변경사항의 유형을 분류합니다.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>새로운 기능 추가</summary>
        Feature,
        /// <summary>기존 기능 개선</summary>
        Improvement,
        /// <summary>버그 수정</summary>
        Bugfix,
        /// <summary>보안 업데이트</summary>
        Security,
        /// <summary>성능 개선</summary>
        Performance,
        /// <summary>UI/UX 개선</summary>
        UIUpdate
    }

    /// <summary>
    /// 변경사항 아이템 모델
    /// 소프트웨어 업데이트의 개별 변경사항을 나타냅니다.
    /// </summary>
    public class ChangelogItem
    {
        /// <summary>
        /// 변경사항 ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 변경사항 제목
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 변경사항 상세 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 변경사항 타입
        /// </summary>
        public ChangeType Type { get; set; }

        /// <summary>
        /// 변경 날짜
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 버전 정보
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 중요도 (1: 낮음, 2: 보통, 3: 높음, 4: 긴급)
        /// </summary>
        public int Priority { get; set; } = 2;

        /// <summary>
        /// 사용자에게 새로운 변경사항인지 여부
        /// </summary>
        public bool IsNew { get; set; } = true;

        /// <summary>
        /// 변경사항 타입에 따른 아이콘 반환
        /// </summary>
        public string TypeIcon
        {
            get
            {
                return Type switch
                {
                    ChangeType.Feature => "✨",
                    ChangeType.Improvement => "🔧",
                    ChangeType.Bugfix => "🐛",
                    ChangeType.Security => "🔒",
                    ChangeType.Performance => "⚡",
                    ChangeType.UIUpdate => "🎨",
                    _ => "📋"
                };
            }
        }

        /// <summary>
        /// 변경사항 타입에 따른 색상 반환
        /// </summary>
        public string TypeColor
        {
            get
            {
                return Type switch
                {
                    ChangeType.Feature => "#22C55E",     // 초록
                    ChangeType.Improvement => "#3B82F6", // 파랑
                    ChangeType.Bugfix => "#EF4444",      // 빨강
                    ChangeType.Security => "#F59E0B",    // 주황
                    ChangeType.Performance => "#8B5CF6", // 보라
                    ChangeType.UIUpdate => "#EC4899",    // 핑크
                    _ => "#6B7280"                        // 회색
                };
            }
        }

        /// <summary>
        /// 우선순위에 따른 배지 색상 반환
        /// </summary>
        public string PriorityColor
        {
            get
            {
                return Priority switch
                {
                    1 => "#10B981", // 낮음 - 초록
                    2 => "#3B82F6", // 보통 - 파랑
                    3 => "#F59E0B", // 높음 - 주황
                    4 => "#EF4444", // 긴급 - 빨강
                    _ => "#6B7280"  // 기본 - 회색
                };
            }
        }

        /// <summary>
        /// 우선순위 텍스트 반환
        /// </summary>
        public string PriorityText
        {
            get
            {
                return Priority switch
                {
                    1 => "낮음",
                    2 => "보통",
                    3 => "높음",
                    4 => "긴급",
                    _ => "알 수 없음"
                };
            }
        }
    }

    /// <summary>
    /// 변경사항 버전 정보
    /// 특정 버전의 모든 변경사항을 그룹핑합니다.
    /// </summary>
    public class ChangelogVersion
    {
        /// <summary>
        /// 버전 번호
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 릴리즈 날짜
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// 버전 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 이 버전의 변경사항 목록
        /// </summary>
        public List<ChangelogItem> Changes { get; set; } = new List<ChangelogItem>();

        /// <summary>
        /// 중요한 업데이트인지 여부
        /// </summary>
        public bool IsImportant { get; set; } = false;
    }
} 