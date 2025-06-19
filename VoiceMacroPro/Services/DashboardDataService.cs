using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoiceMacroPro.Models;

namespace VoiceMacroPro.Services
{
    /// <summary>
    /// 대시보드 데이터 제공 서비스
    /// 통계 정보, 최근 활동, 시스템 정보 등을 제공합니다.
    /// </summary>
    public class DashboardDataService
    {
        #region Private Fields
        
        private static DashboardDataService? _instance;
        private readonly ApiService _apiService;
        private readonly LoggingService _loggingService;
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static DashboardDataService Instance => _instance ??= new DashboardDataService();
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// DashboardDataService 생성자 (private - 싱글톤 패턴)
        /// </summary>
        private DashboardDataService()
        {
            _apiService = ApiService.Instance;
            _loggingService = LoggingService.Instance;
        }
        
        #endregion
        
        #region Statistics Methods
        
        /// <summary>
        /// 총 매크로 개수를 조회합니다.
        /// </summary>
        /// <returns>매크로 총 개수</returns>
        public async Task<int> GetTotalMacroCountAsync()
        {
            try
            {
                var macros = await _apiService.GetMacrosAsync();
                return macros?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"매크로 개수 조회 실패: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 커스텀 스크립트 개수를 조회합니다.
        /// </summary>
        /// <returns>커스텀 스크립트 개수</returns>
        public async Task<int> GetCustomScriptCountAsync()
        {
            try
            {
                var scripts = await _apiService.GetCustomScriptsAsync();
                return scripts?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"커스텀 스크립트 개수 조회 실패: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 프리셋 개수를 조회합니다.
        /// </summary>
        /// <returns>프리셋 개수</returns>
        public async Task<int> GetPresetCountAsync()
        {
            try
            {
                var presets = await _apiService.GetPresetsAsync();
                return presets?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"프리셋 개수 조회 실패: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 음성 인식 정확도를 조회합니다.
        /// </summary>
        /// <returns>음성 인식 정확도 (0-100)</returns>
        public async Task<double> GetVoiceRecognitionAccuracyAsync()
        {
            try
            {
                var whisperStatus = await _apiService.GetWhisperStatusAsync();
                if (whisperStatus != null && whisperStatus.IsReady)
                {
                    // 실제 정확도 계산 로직을 구현하거나 서버에서 제공하는 값 사용
                    // 현재는 임시로 Whisper 상태에 따라 반환
                    return 92.5; // 임시값
                }
                return 0.0;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 인식 정확도 조회 실패: {ex.Message}");
                return 0.0;
            }
        }
        
        #endregion
        
        #region Recent Activity Methods
        
        /// <summary>
        /// 최근 사용한 매크로 목록을 조회합니다.
        /// </summary>
        /// <param name="limit">조회할 매크로 개수 (기본값: 5)</param>
        /// <returns>최근 매크로 목록</returns>
        public async Task<List<RecentMacroInfo>> GetRecentMacrosAsync(int limit = 5)
        {
            try
            {
                var macros = await _apiService.GetMacrosAsync(sortBy: "updated_at");
                if (macros == null || !macros.Any())
                {
                    return new List<RecentMacroInfo>();
                }
                
                return macros
                    .Take(limit)
                    .Select(m => new RecentMacroInfo
                    {
                        Name = m.Name,
                        VoiceCommand = m.VoiceCommand,
                        LastUsed = CalculateTimeAgo(m.UpdatedAt)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"최근 매크로 목록 조회 실패: {ex.Message}");
                return new List<RecentMacroInfo>();
            }
        }
        
        /// <summary>
        /// 즐겨찾기 프리셋 목록을 조회합니다.
        /// </summary>
        /// <param name="limit">조회할 프리셋 개수 (기본값: 5)</param>
        /// <returns>즐겨찾기 프리셋 목록</returns>
        public async Task<List<FavoritePresetInfo>> GetFavoritePresetsAsync(int limit = 5)
        {
            try
            {
                var presets = await _apiService.GetPresetsAsync(favoritesOnly: true);
                if (presets == null || !presets.Any())
                {
                    return new List<FavoritePresetInfo>();
                }
                
                return presets
                    .Take(limit)
                    .Select(p => new FavoritePresetInfo
                    {
                        Name = p.Name,
                        MacroCount = p.MacroIds?.Count ?? 0
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"즐겨찾기 프리셋 목록 조회 실패: {ex.Message}");
                return new List<FavoritePresetInfo>();
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 날짜로부터 경과 시간을 계산합니다.
        /// </summary>
        /// <param name="dateTime">기준 날짜시간</param>
        /// <returns>경과 시간 문자열</returns>
        private string CalculateTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "방금 전";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}분 전";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}시간 전";
            else if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}일 전";
            else if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}주 전";
            else
                return $"{(int)(timeSpan.TotalDays / 30)}개월 전";
        }
        
        #endregion
    }
    
    #region Data Models
    
    /// <summary>
    /// 최근 매크로 정보 모델
    /// </summary>
    public class RecentMacroInfo
    {
        public string Name { get; set; } = "";
        public string VoiceCommand { get; set; } = "";
        public string LastUsed { get; set; } = "";
    }
    
    /// <summary>
    /// 즐겨찾기 프리셋 정보 모델
    /// </summary>
    public class FavoritePresetInfo
    {
        public string Name { get; set; } = "";
        public int MacroCount { get; set; }
    }
    
    #endregion
} 