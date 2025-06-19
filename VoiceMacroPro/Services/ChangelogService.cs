using System;
using System.Collections.Generic;
using System.Linq;
using VoiceMacroPro.Models;

namespace VoiceMacroPro.Services
{
    /// <summary>
    /// 변경사항 관리 서비스
    /// 소프트웨어 업데이트 변경사항을 관리하고 제공합니다.
    /// </summary>
    public class ChangelogService
    {
        private readonly List<ChangelogVersion> _changelogVersions;
        private readonly LoggingService _loggingService;

        /// <summary>
        /// ChangelogService 생성자
        /// </summary>
        public ChangelogService()
        {
            _loggingService = new LoggingService();
            _changelogVersions = InitializeChangelog();
        }

        /// <summary>
        /// 최신 변경사항 목록을 반환하는 함수
        /// </summary>
        /// <param name="count">반환할 최대 변경사항 수</param>
        /// <returns>최신 변경사항 목록</returns>
        public List<ChangelogItem> GetLatestChanges(int count = 10)
        {
            try
            {
                return _changelogVersions
                    .SelectMany(v => v.Changes)
                    .OrderByDescending(c => c.Date)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"최신 변경사항 조회 실패: {ex.Message}");
                return new List<ChangelogItem>();
            }
        }

        /// <summary>
        /// 특정 버전의 변경사항을 반환하는 함수
        /// </summary>
        /// <param name="version">조회할 버전</param>
        /// <returns>해당 버전의 변경사항</returns>
        public ChangelogVersion? GetVersionChangelog(string version)
        {
            try
            {
                return _changelogVersions.FirstOrDefault(v => v.Version == version);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"버전 변경사항 조회 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 모든 버전 목록을 반환하는 함수
        /// </summary>
        /// <returns>모든 버전 목록</returns>
        public List<ChangelogVersion> GetAllVersions()
        {
            return _changelogVersions.OrderByDescending(v => v.ReleaseDate).ToList();
        }

        /// <summary>
        /// 새로운 변경사항이 있는지 확인하는 함수
        /// </summary>
        /// <returns>새로운 변경사항 여부</returns>
        public bool HasNewChanges()
        {
            return _changelogVersions
                .SelectMany(v => v.Changes)
                .Any(c => c.IsNew);
        }

        /// <summary>
        /// 변경사항을 읽음으로 표시하는 함수
        /// </summary>
        /// <param name="changeId">변경사항 ID</param>
        public void MarkAsRead(string changeId)
        {
            try
            {
                var change = _changelogVersions
                    .SelectMany(v => v.Changes)
                    .FirstOrDefault(c => c.Id == changeId);
                
                if (change != null)
                {
                    change.IsNew = false;
                    _loggingService.LogInfo($"변경사항 '{changeId}' 읽음으로 표시");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"변경사항 읽음 표시 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 변경사항을 읽음으로 표시하는 함수
        /// </summary>
        public void MarkAllAsRead()
        {
            try
            {
                foreach (var version in _changelogVersions)
                {
                    foreach (var change in version.Changes)
                    {
                        change.IsNew = false;
                    }
                }
                _loggingService.LogInfo("모든 변경사항 읽음으로 표시 완료");
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"모든 변경사항 읽음 표시 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 변경사항 데이터를 초기화하는 함수
        /// 실제 운영에서는 서버나 파일에서 데이터를 로드할 수 있습니다.
        /// </summary>
        /// <returns>초기화된 변경사항 목록</returns>
        private List<ChangelogVersion> InitializeChangelog()
        {
            var versions = new List<ChangelogVersion>();

            // 버전 1.2.0 (최신)
            var v120 = new ChangelogVersion
            {
                Version = "1.2.0",
                ReleaseDate = DateTime.Now.AddDays(-1),
                Description = "대시보드 UI 개선 및 프리셋 관리 기능 강화",
                IsImportant = true,
                Changes = new List<ChangelogItem>
                {
                    new ChangelogItem
                    {
                        Id = "1.2.0-001",
                        Title = "대시보드 UI 완전 리뉴얼",
                        Description = "실시간 시스템 모니터링과 통계 대시보드로 완전히 새롭게 설계되었습니다. CPU/메모리 사용량 실시간 표시, 매크로 사용 통계, 최근 활동 내역 등이 추가되었습니다.",
                        Type = ChangeType.UIUpdate,
                        Date = DateTime.Now.AddDays(-1),
                        Version = "1.2.0",
                        Priority = 3,
                        IsNew = true
                    },
                    new ChangelogItem
                    {
                        Id = "1.2.0-002",
                        Title = "프리셋 관리 시스템 완전 구현",
                        Description = "프리셋 생성, 수정, 삭제, 복사 기능이 모두 구현되었습니다. 실제 API 서버와 연동하여 데이터 영속성을 보장하며, 프리셋 가져오기/내보내기 기능도 지원합니다.",
                        Type = ChangeType.Feature,
                        Date = DateTime.Now.AddDays(-1),
                        Version = "1.2.0",
                        Priority = 4,
                        IsNew = true
                    },
                    new ChangelogItem
                    {
                        Id = "1.2.0-003",
                        Title = "로그 모니터링 시스템 개선",
                        Description = "실시간 로그 출력, 로그 레벨별 필터링, 자동 스크롤 기능이 추가되었습니다. 로그 내보내기 및 성능 로그 분석 기능도 강화되었습니다.",
                        Type = ChangeType.Improvement,
                        Date = DateTime.Now.AddDays(-1),
                        Version = "1.2.0",
                        Priority = 2,
                        IsNew = true
                    },
                    new ChangelogItem
                    {
                        Id = "1.2.0-004",
                        Title = "메모리 누수 문제 해결",
                        Description = "장시간 사용 시 발생하던 메모리 누수 문제를 해결했습니다. 특히 음성 인식 및 매크로 실행 과정에서의 메모리 관리가 개선되었습니다.",
                        Type = ChangeType.Bugfix,
                        Date = DateTime.Now.AddDays(-1),
                        Version = "1.2.0",
                        Priority = 3,
                        IsNew = true
                    }
                }
            };

            // 버전 1.1.5
            var v115 = new ChangelogVersion
            {
                Version = "1.1.5",
                ReleaseDate = DateTime.Now.AddDays(-7),
                Description = "보안 패치 및 안정성 개선",
                IsImportant = false,
                Changes = new List<ChangelogItem>
                {
                    new ChangelogItem
                    {
                        Id = "1.1.5-001",
                        Title = "보안 취약점 패치",
                        Description = "음성 데이터 전송 과정에서의 보안 취약점을 수정했습니다. 이제 모든 음성 데이터는 AES-256으로 암호화되어 전송됩니다.",
                        Type = ChangeType.Security,
                        Date = DateTime.Now.AddDays(-7),
                        Version = "1.1.5",
                        Priority = 4,
                        IsNew = false
                    },
                    new ChangelogItem
                    {
                        Id = "1.1.5-002",
                        Title = "음성 인식 성능 향상",
                        Description = "Whisper AI 모델 최적화로 음성 인식 속도가 30% 향상되었습니다. 특히 한국어 인식 정확도가 크게 개선되었습니다.",
                        Type = ChangeType.Performance,
                        Date = DateTime.Now.AddDays(-7),
                        Version = "1.1.5",
                        Priority = 2,
                        IsNew = false
                    }
                }
            };

            // 버전 1.1.0
            var v110 = new ChangelogVersion
            {
                Version = "1.1.0",
                ReleaseDate = DateTime.Now.AddDays(-14),
                Description = "커스텀 스크립팅 시스템 도입",
                IsImportant = true,
                Changes = new List<ChangelogItem>
                {
                    new ChangelogItem
                    {
                        Id = "1.1.0-001",
                        Title = "MSL 스크립팅 언어 지원",
                        Description = "Macro Scripting Language (MSL)을 도입하여 복잡한 매크로를 직관적으로 작성할 수 있게 되었습니다. 순차 실행, 동시 실행, 반복 등 다양한 패턴을 지원합니다.",
                        Type = ChangeType.Feature,
                        Date = DateTime.Now.AddDays(-14),
                        Version = "1.1.0",
                        Priority = 4,
                        IsNew = false
                    },
                    new ChangelogItem
                    {
                        Id = "1.1.0-002",
                        Title = "스크립트 에디터 구현",
                        Description = "구문 강조, 자동 완성, 실시간 오류 검사 기능이 있는 고급 스크립트 에디터가 추가되었습니다.",
                        Type = ChangeType.Feature,
                        Date = DateTime.Now.AddDays(-14),
                        Version = "1.1.0",
                        Priority = 3,
                        IsNew = false
                    }
                }
            };

            versions.Add(v120);
            versions.Add(v115);
            versions.Add(v110);

            return versions;
        }
    }
} 