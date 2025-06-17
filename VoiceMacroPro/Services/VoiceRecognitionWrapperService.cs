using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using VoiceMacroPro.Models;
using System.Text.Json.Serialization;

namespace VoiceMacroPro.Services
{
    /// <summary>
    /// Python 백엔드의 음성 인식 서비스와 통신하는 C# 래퍼 클래스
    /// 음성 인식, 매크로 매칭, 마이크 관리 기능을 제공합니다.
    /// </summary>
    public class VoiceRecognitionWrapperService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly LoggingService _loggingService;

        /// <summary>
        /// 음성 인식 래퍼 서비스 생성자
        /// </summary>
        /// <param name="baseUrl">백엔드 API 기본 URL (기본값: http://localhost:5000)</param>
        public VoiceRecognitionWrapperService(string baseUrl = "http://localhost:5000")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // 30초 타임아웃
            _loggingService = LoggingService.Instance;
        }

        /// <summary>
        /// 사용 가능한 마이크 장치 목록을 가져오는 함수
        /// </summary>
        /// <returns>마이크 장치 정보 목록</returns>
        public async Task<List<MicrophoneDevice>> GetAvailableDevicesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/voice/devices");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<MicrophoneDevice>>>(jsonContent);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _loggingService.LogInfo($"마이크 장치 목록 조회 성공: {apiResponse.Data.Count}개 장치");
                        return apiResponse.Data;
                    }
                }
                
                _loggingService.LogWarning("마이크 장치 목록 조회 실패");
                return new List<MicrophoneDevice>();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 장치 목록 조회 중 오류: {ex.Message}");
                return new List<MicrophoneDevice>();
            }
        }

        /// <summary>
        /// 마이크 장치를 설정하는 함수
        /// </summary>
        /// <param name="deviceId">설정할 장치 ID</param>
        /// <returns>설정 성공 여부</returns>
        public async Task<bool> SetMicrophoneDeviceAsync(int deviceId)
        {
            try
            {
                var requestData = new { device_id = deviceId };
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/voice/device", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(responseContent);
                    
                    if (apiResponse?.Success == true)
                    {
                        _loggingService.LogInfo($"마이크 장치 설정 성공: 장치 ID {deviceId}");
                        return true;
                    }
                }
                
                _loggingService.LogWarning($"마이크 장치 설정 실패: 장치 ID {deviceId}");
                return false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 장치 설정 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 음성 인식 녹음을 시작하는 함수
        /// </summary>
        /// <returns>녹음 시작 성공 여부</returns>
        public async Task<bool> StartRecordingAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/voice/recording/start", null);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(jsonContent);
                    
                    if (apiResponse?.Success == true)
                    {
                        _loggingService.LogInfo("음성 녹음 시작 성공");
                        return true;
                    }
                }
                
                _loggingService.LogWarning("음성 녹음 시작 실패");
                return false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 녹음 시작 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 음성 인식 녹음을 중지하는 함수
        /// </summary>
        /// <returns>녹음 중지 성공 여부</returns>
        public async Task<bool> StopRecordingAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/voice/recording/stop", null);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(jsonContent);
                    
                    if (apiResponse?.Success == true)
                    {
                        _loggingService.LogInfo("음성 녹음 중지 성공");
                        return true;
                    }
                }
                
                _loggingService.LogWarning("음성 녹음 중지 실패");
                return false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 녹음 중지 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 녹음 상태를 가져오는 함수
        /// </summary>
        /// <returns>녹음 상태 정보</returns>
        public async Task<VoiceRecognitionStatus?> GetRecordingStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/voice/status");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<VoiceRecognitionStatus>>(jsonContent);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"녹음 상태 조회 중 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 음성을 분석하고 매크로 매칭 결과를 가져오는 함수
        /// 실시간 녹음된 오디오를 가져와서 Whisper로 처리합니다.
        /// </summary>
        /// <param name="duration">녹음할 시간 (초)</param>
        /// <returns>매크로 매칭 결과 목록</returns>
        public async Task<List<VoiceMatchResult>> AnalyzeVoiceAndMatchMacrosAsync(double duration = 2.0)
        {
            try
            {
                var requestData = new { duration = duration };
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/voice/record-and-process", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<VoiceProcessingResult>>(responseContent);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        var result = apiResponse.Data;
                        _loggingService.LogInfo($"음성 분석 성공: '{result.RecognizedText}' (처리시간: {result.ProcessingTime:F2}초)");
                        
                        // 매칭 결과를 VoiceMatchResult 객체로 변환
                        var matchResults = new List<VoiceMatchResult>();
                        
                        for (int i = 0; i < result.MatchedMacros.Count; i++)
                        {
                            var match = result.MatchedMacros[i];
                            matchResults.Add(new VoiceMatchResult
                            {
                                Rank = i + 1,
                                MacroName = match.Name,
                                VoiceCommand = match.VoiceCommand,
                                Confidence = match.MatchConfidence * 100, // 백분율로 변환
                                ActionDescription = $"{match.ActionType}: {match.KeySequence}",
                                MacroId = match.Id
                            });
                        }
                        
                        return matchResults;
                    }
                    else
                    {
                        _loggingService.LogWarning($"음성 분석 실패: {apiResponse?.Message ?? "알 수 없는 오류"}");
                    }
                }
                
                return new List<VoiceMatchResult>();
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"음성 분석 및 매크로 매칭 중 오류: {ex.Message}");
                return new List<VoiceMatchResult>();
            }
        }

        /// <summary>
        /// 마이크 테스트를 수행하는 함수
        /// </summary>
        /// <returns>테스트 결과</returns>
        public async Task<MicrophoneTestResult?> TestMicrophoneAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/voice/test", null);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<MicrophoneTestResult>>(jsonContent);
                    
                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _loggingService.LogInfo($"마이크 테스트 완료: {(apiResponse.Data.Success ? "성공" : "실패")}");
                        return apiResponse.Data;
                    }
                }
                
                _loggingService.LogWarning("마이크 테스트 실패");
                return null;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"마이크 테스트 중 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 언어 설정을 변경하는 함수
        /// 현재 Python API에 언어 설정 엔드포인트가 없으므로 로컬에서만 저장합니다.
        /// </summary>
        /// <param name="language">언어 코드 (ko, en)</param>
        /// <returns>설정 성공 여부</returns>
        public async Task<bool> SetLanguageAsync(string language)
        {
            try
            {
                // TODO: Python API에 언어 설정 엔드포인트 추가 필요
                // 현재는 로컬에서만 언어 설정을 저장
                _loggingService.LogInfo($"언어 설정 변경: {language} (로컬 저장)");
                return true;
                
                /*
                // 향후 Python API에 구현될 언어 설정 엔드포인트
                var requestData = new { language = language };
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/voice/language", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(responseContent);
                    
                    if (apiResponse?.Success == true)
                    {
                        _loggingService.LogInfo($"언어 설정 변경 성공: {language}");
                        return true;
                    }
                }
                
                _loggingService.LogWarning($"언어 설정 변경 실패: {language}");
                return false;
                */
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"언어 설정 변경 중 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // 관련 데이터 모델들

    /// <summary>
    /// 마이크 장치 정보를 나타내는 클래스
    /// Python API에서 반환되는 마이크 장치 정보와 매핑됩니다.
    /// </summary>
    public class MicrophoneDevice
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("max_input_channels")]
        public int MaxInputChannels { get; set; }
        
        [JsonPropertyName("default_samplerate")]
        public double DefaultSampleRate { get; set; }
    }

    /// <summary>
    /// 음성 인식 상태 정보
    /// </summary>
    public class VoiceRecognitionStatus
    {
        public bool IsRecording { get; set; }
        public int CurrentDevice { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int AvailableDevicesCount { get; set; }
        public int QueueSize { get; set; }
        public string Mode { get; set; } = string.Empty;
        public double AudioLevel { get; set; }
    }

    /// <summary>
    /// 음성 분석 결과
    /// </summary>
    public class VoiceAnalysisResult
    {
        public string RecognizedText { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Language { get; set; } = string.Empty;
        public double ProcessingTime { get; set; }
        public List<MacroMatchInfo> MatchedMacros { get; set; } = new();
    }

    /// <summary>
    /// 음성 처리 결과 (record-and-process API 응답)
    /// </summary>
    public class VoiceProcessingResult
    {
        [JsonPropertyName("recognized_text")]
        public string RecognizedText { get; set; } = string.Empty;
        
        [JsonPropertyName("matched_macros")]
        public List<MacroMatchInfo> MatchedMacros { get; set; } = new();
        
        [JsonPropertyName("processing_time")]
        public double ProcessingTime { get; set; }
        
        [JsonPropertyName("audio_duration")]
        public double AudioDuration { get; set; }
    }

    /// <summary>
    /// 매크로 매칭 정보
    /// </summary>
    public class MacroMatchInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VoiceCommand { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string KeySequence { get; set; } = string.Empty;
        public double MatchConfidence { get; set; }
    }

    /// <summary>
    /// 마이크 테스트 결과를 나타내는 클래스
    /// </summary>
    public class MicrophoneTestResult
    {
        public bool Success { get; set; }
        public bool DeviceAvailable { get; set; }
        public bool RecordingTest { get; set; }
        public bool AudioLevelDetected { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
    }
} 