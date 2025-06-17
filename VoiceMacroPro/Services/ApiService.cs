using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VoiceMacroPro.Models;
using System.Text.Json.Serialization;

namespace VoiceMacroPro.Services
{
    /// <summary>
    /// Python 백엔드 API와 통신을 담당하는 서비스 클래스
    /// HTTP 요청을 통해 매크로 데이터를 주고받습니다.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        /// <summary>
        /// API 서비스 생성자
        /// HTTP 클라이언트를 초기화하고 기본 설정을 구성합니다.
        /// </summary>
        /// <param name="baseUrl">백엔드 API의 기본 URL</param>
        public ApiService(string baseUrl = "http://localhost:5000")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // 30초 타임아웃 설정
        }

        /// <summary>
        /// 모든 매크로를 조회하는 함수
        /// 검색어와 정렬 조건을 포함하여 서버에 요청합니다.
        /// </summary>
        /// <param name="searchTerm">검색어 (선택사항)</param>
        /// <param name="sortBy">정렬 기준 (name, created_at, usage_count)</param>
        /// <returns>매크로 목록</returns>
        public async Task<List<Macro>> GetMacrosAsync(string? searchTerm = null, string sortBy = "name")
        {
            try
            {
                // URL 쿼리 파라미터 구성
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    queryParams.Add($"search={Uri.EscapeDataString(searchTerm)}");
                }
                queryParams.Add($"sort={sortBy}");

                var queryString = string.Join("&", queryParams);
                var url = $"{_baseUrl}/api/macros?{queryString}";

                // HTTP GET 요청 전송
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // JSON 응답을 파싱하여 매크로 목록 반환
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<Macro>>>(content);
                    return apiResponse?.Data ?? new List<Macro>();
                }
                else
                {
                    throw new Exception($"API 요청 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 목록 조회 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 ID의 매크로를 조회하는 함수
        /// </summary>
        /// <param name="macroId">조회할 매크로 ID</param>
        /// <returns>매크로 객체 (없으면 null)</returns>
        public async Task<Macro?> GetMacroAsync(int macroId)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macroId}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<Macro>>(content);
                    return apiResponse?.Data;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null; // 매크로를 찾지 못함
                }
                else
                {
                    throw new Exception($"API 요청 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 조회 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 새로운 매크로를 생성하는 함수
        /// </summary>
        /// <param name="macro">생성할 매크로 객체</param>
        /// <returns>생성된 매크로의 ID</returns>
        public async Task<int> CreateMacroAsync(Macro macro)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros";
                
                // 매크로 객체를 JSON으로 변환
                var jsonContent = JsonConvert.SerializeObject(new
                {
                    name = macro.Name,
                    voice_command = macro.VoiceCommand,
                    action_type = macro.ActionType,
                    key_sequence = macro.KeySequence,
                    settings = macro.Settings
                });

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    return (int)apiResponse?.Data?.id;
                }
                else
                {
                    throw new Exception($"API 요청 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 생성 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 기존 매크로를 수정하는 함수
        /// </summary>
        /// <param name="macro">수정할 매크로 객체</param>
        /// <returns>수정 성공 여부</returns>
        public async Task<bool> UpdateMacroAsync(Macro macro)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macro.Id}";
                
                var jsonContent = JsonConvert.SerializeObject(new
                {
                    name = macro.Name,
                    voice_command = macro.VoiceCommand,
                    action_type = macro.ActionType,
                    key_sequence = macro.KeySequence,
                    settings = macro.Settings
                });

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, httpContent);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 수정 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로를 복사하는 함수
        /// </summary>
        /// <param name="macroId">복사할 매크로 ID</param>
        /// <param name="newName">새로운 매크로 이름 (선택사항)</param>
        /// <returns>복사된 매크로의 ID</returns>
        public async Task<int> CopyMacroAsync(int macroId, string? newName = null)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macroId}/copy";
                
                var jsonContent = string.Empty;
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    jsonContent = JsonConvert.SerializeObject(new { new_name = newName });
                }

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    return (int)apiResponse?.Data?.id;
                }
                else
                {
                    throw new Exception($"API 요청 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 복사 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로를 삭제하는 함수
        /// </summary>
        /// <param name="macroId">삭제할 매크로 ID</param>
        /// <returns>삭제 성공 여부</returns>
        public async Task<bool> DeleteMacroAsync(int macroId)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macroId}";
                var response = await _httpClient.DeleteAsync(url);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 삭제 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로를 실행하는 함수
        /// </summary>
        /// <param name="macroId">실행할 매크로 ID</param>
        /// <returns>실행 성공 여부</returns>
        public async Task<bool> ExecuteMacroAsync(int macroId)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macroId}/execute";
                var response = await _httpClient.PostAsync(url, null);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    return apiResponse?.Success ?? false;
                }
                else
                {
                    throw new Exception($"API 요청 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 실행 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 실행 중인 매크로를 중지하는 함수
        /// </summary>
        /// <param name="macroId">중지할 매크로 ID</param>
        /// <returns>중지 성공 여부</returns>
        public async Task<bool> StopMacroAsync(int macroId)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macroId}/stop";
                var httpContent = new StringContent("", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 중지 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 실행 중인 매크로를 중지하는 함수 (비상 정지)
        /// </summary>
        /// <returns>중지된 매크로 개수</returns>
        public async Task<int> StopAllMacrosAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/execution/stop-all";
                var httpContent = new StringContent("", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    return apiResponse?.Data?.stopped_count ?? 0;
                }
                else
                {
                    throw new Exception($"매크로 중지 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 중지 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 매크로 실행 상태를 조회하는 함수
        /// </summary>
        /// <returns>실행 상태 정보</returns>
        public async Task<MacroExecutionStatus?> GetExecutionStatusAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/execution/status";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<MacroExecutionStatus>>(content);
                    return apiResponse?.Data;
                }
                else
                {
                    throw new Exception($"실행 상태 조회 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"실행 상태 조회 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 토글 매크로의 현재 상태를 조회하는 함수
        /// </summary>
        /// <param name="macroId">조회할 매크로 ID</param>
        /// <returns>토글 상태 정보</returns>
        public async Task<ToggleState?> GetToggleStateAsync(int macroId)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macroId}/toggle-state";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ToggleState>>(content);
                    return apiResponse?.Data;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                         response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return null; // 매크로를 찾을 수 없거나 토글 타입이 아님
                }
                else
                {
                    throw new Exception($"토글 상태 조회 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"토글 상태 조회 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 토글 매크로의 상태를 직접 설정하는 함수
        /// </summary>
        /// <param name="macroId">설정할 매크로 ID</param>
        /// <param name="state">설정할 상태 (true=ON, false=OFF)</param>
        /// <returns>설정 성공 여부</returns>
        public async Task<bool> SetToggleStateAsync(int macroId, bool state)
        {
            try
            {
                var url = $"{_baseUrl}/api/macros/{macroId}/toggle-state";
                var jsonContent = JsonConvert.SerializeObject(new { state = state });
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"토글 상태 설정 중 오류 발생: {ex.Message}");
            }
        }

        // ==================== OpenAI Whisper 관련 API ====================

        /// <summary>
        /// 오디오 데이터를 OpenAI Whisper로 텍스트 변환하는 함수
        /// </summary>
        /// <param name="audioData">변환할 오디오 데이터</param>
        /// <returns>인식된 텍스트</returns>
        public async Task<string?> TranscribeAudioAsync(byte[] audioData)
        {
            try
            {
                var url = $"{_baseUrl}/api/whisper/transcribe";
                
                // 오디오 데이터를 Base64로 인코딩
                var base64Audio = Convert.ToBase64String(audioData);
                var jsonContent = JsonConvert.SerializeObject(new { audio_data = base64Audio });

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    return apiResponse?.Data?.recognized_text?.ToString();
                }
                else
                {
                    throw new Exception($"음성 인식 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"음성 인식 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 음성 명령 전체 처리 파이프라인 (음성 인식 + 매크로 매칭)
        /// </summary>
        /// <param name="audioData">처리할 오디오 데이터</param>
        /// <returns>음성 인식 결과 및 매칭된 매크로 목록</returns>
        public async Task<VoiceProcessResult?> ProcessVoiceCommandAsync(byte[] audioData)
        {
            try
            {
                var url = $"{_baseUrl}/api/whisper/process";
                
                // 오디오 데이터를 Base64로 인코딩
                var base64Audio = Convert.ToBase64String(audioData);
                var jsonContent = JsonConvert.SerializeObject(new { audio_data = base64Audio });

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    if (apiResponse?.Data != null)
                    {
                        return new VoiceProcessResult
                        {
                            RecognizedText = apiResponse.Data.recognized_text?.ToString() ?? "",
                            MatchedMacros = JsonConvert.DeserializeObject<List<MacroMatch>>(
                                apiResponse.Data.matched_macros?.ToString() ?? "[]") ?? new List<MacroMatch>(),
                            ProcessingTime = (double)(apiResponse.Data.processing_time ?? 0.0)
                        };
                    }
                }
                else
                {
                    throw new Exception($"음성 명령 처리 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"음성 명령 처리 중 오류 발생: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 텍스트와 매크로 명령어 매칭
        /// </summary>
        /// <param name="text">매칭할 텍스트</param>
        /// <returns>매칭된 매크로 목록</returns>
        public async Task<List<MacroMatch>> MatchMacrosAsync(string text)
        {
            try
            {
                var url = $"{_baseUrl}/api/whisper/match";
                
                var jsonContent = JsonConvert.SerializeObject(new { text = text });
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    if (apiResponse?.Data?.matched_macros != null)
                    {
                        return JsonConvert.DeserializeObject<List<MacroMatch>>(
                            apiResponse.Data.matched_macros.ToString()) ?? new List<MacroMatch>();
                    }
                }
                else
                {
                    throw new Exception($"매크로 매칭 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"매크로 매칭 중 오류 발생: {ex.Message}");
            }

            return new List<MacroMatch>();
        }

        /// <summary>
        /// 실시간 녹음된 오디오를 처리하는 함수
        /// </summary>
        /// <param name="duration">녹음할 시간(초)</param>
        /// <returns>음성 인식 및 매크로 매칭 결과</returns>
        public async Task<VoiceProcessResult?> RecordAndProcessVoiceAsync(double duration = 3.0)
        {
            try
            {
                var url = $"{_baseUrl}/api/voice/record-and-process";
                
                var jsonContent = JsonConvert.SerializeObject(new { duration = duration });
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    if (apiResponse?.Data != null)
                    {
                        return new VoiceProcessResult
                        {
                            RecognizedText = apiResponse.Data.recognized_text?.ToString() ?? "",
                            MatchedMacros = JsonConvert.DeserializeObject<List<MacroMatch>>(
                                apiResponse.Data.matched_macros?.ToString() ?? "[]") ?? new List<MacroMatch>(),
                            ProcessingTime = (double)(apiResponse.Data.processing_time ?? 0.0),
                            AudioDuration = (double)(apiResponse.Data.audio_duration ?? duration)
                        };
                    }
                }
                else
                {
                    throw new Exception($"음성 녹음 및 처리 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"음성 녹음 및 처리 중 오류 발생: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Whisper 서비스 상태 조회
        /// </summary>
        /// <returns>Whisper 서비스 상태 정보</returns>
        public async Task<WhisperStatus?> GetWhisperStatusAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/whisper/status";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(content);
                    if (apiResponse?.Data != null)
                    {
                        return JsonConvert.DeserializeObject<WhisperStatus>(apiResponse.Data.ToString());
                    }
                }
                else
                {
                    throw new Exception($"Whisper 상태 조회 실패: {response.StatusCode} - {content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Whisper 상태 조회 중 오류 발생: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 서버 상태를 확인하는 함수
        /// 애플리케이션 시작 시 백엔드 서버가 실행 중인지 확인합니다.
        /// </summary>
        /// <returns>서버 연결 상태</returns>
        public async Task<bool> CheckServerHealthAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/health";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false; // 서버에 연결할 수 없음
            }
        }

        /// <summary>
        /// 리소스 정리 함수
        /// HTTP 클라이언트를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// API 응답을 위한 제네릭 클래스
    /// 백엔드에서 반환하는 JSON 형태와 일치합니다.
    /// </summary>
    /// <typeparam name="T">응답 데이터의 타입</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// 요청 성공 여부
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 응답 데이터
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>
        /// 응답 메시지
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// 오류 정보 (오류 발생 시)
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// 음성 처리 결과 클래스
    /// Whisper 음성 인식 및 매크로 매칭 결과를 담습니다.
    /// </summary>
    public class VoiceProcessResult
    {
        /// <summary>
        /// 인식된 텍스트
        /// </summary>
        public string RecognizedText { get; set; } = "";

        /// <summary>
        /// 매칭된 매크로 목록
        /// </summary>
        public List<MacroMatch> MatchedMacros { get; set; } = new List<MacroMatch>();

        /// <summary>
        /// 처리 시간 (초)
        /// </summary>
        public double ProcessingTime { get; set; }

        /// <summary>
        /// 오디오 녹음 시간 (초)
        /// </summary>
        public double AudioDuration { get; set; }
    }

    /// <summary>
    /// 매크로 매칭 결과 클래스
    /// 음성 명령어와 매칭된 매크로 정보를 담습니다.
    /// </summary>
    public class MacroMatch
    {
        /// <summary>
        /// 매크로 ID
        /// </summary>
        [JsonProperty("macro_id")]
        public int MacroId { get; set; }

        /// <summary>
        /// 매크로 이름
        /// </summary>
        [JsonProperty("macro_name")]
        public string MacroName { get; set; } = "";

        /// <summary>
        /// 음성 명령어
        /// </summary>
        [JsonProperty("voice_command")]
        public string VoiceCommand { get; set; } = "";

        /// <summary>
        /// 동작 타입
        /// </summary>
        [JsonProperty("action_type")]
        public string ActionType { get; set; } = "";

        /// <summary>
        /// 키 시퀀스
        /// </summary>
        [JsonProperty("key_sequence")]
        public string KeySequence { get; set; } = "";

        /// <summary>
        /// 유사도 (0.0 ~ 1.0)
        /// </summary>
        [JsonProperty("similarity")]
        public double Similarity { get; set; }

        /// <summary>
        /// 확신도 (퍼센트)
        /// </summary>
        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        /// <summary>
        /// 매크로 설정
        /// </summary>
        [JsonProperty("settings")]
        public Dictionary<string, object>? Settings { get; set; }

        /// <summary>
        /// UI 표시용 확신도 텍스트
        /// </summary>
        public string ConfidenceText => $"{Confidence:F1}%";

        /// <summary>
        /// UI 표시용 확신도 색상
        /// </summary>
        public string ConfidenceColor
        {
            get
            {
                if (Confidence >= 90) return "#4CAF50"; // 초록색
                if (Confidence >= 70) return "#FF9800"; // 주황색
                return "#F44336"; // 빨간색
            }
        }

        /// <summary>
        /// UI 표시용 동작 설명
        /// </summary>
        public string ActionDescription => $"[{ActionType}] {KeySequence}";

        /// <summary>
        /// UI 표시용 순위
        /// </summary>
        public int Rank { get; set; }
    }

    /// <summary>
    /// Whisper 서비스 상태 클래스
    /// OpenAI Whisper API 서비스의 상태 정보를 담습니다.
    /// </summary>
    public class WhisperStatus
    {
        /// <summary>
        /// 클라이언트 초기화 여부
        /// </summary>
        [JsonProperty("client_initialized")]
        public bool ClientInitialized { get; set; }

        /// <summary>
        /// API 키 설정 여부
        /// </summary>
        [JsonProperty("api_key_configured")]
        public bool ApiKeyConfigured { get; set; }

        /// <summary>
        /// 사용 중인 Whisper 모델
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        /// <summary>
        /// 설정된 언어
        /// </summary>
        [JsonProperty("language")]
        public string Language { get; set; } = "";

        /// <summary>
        /// 샘플레이트
        /// </summary>
        [JsonProperty("sample_rate")]
        public int SampleRate { get; set; }

        /// <summary>
        /// 임시 디렉토리 경로
        /// </summary>
        [JsonProperty("temp_dir")]
        public string TempDir { get; set; } = "";

        /// <summary>
        /// 임시 디렉토리 존재 여부
        /// </summary>
        [JsonProperty("temp_dir_exists")]
        public bool TempDirExists { get; set; }

        /// <summary>
        /// 매크로 캐시 크기
        /// </summary>
        [JsonProperty("macro_cache_size")]
        public int MacroCacheSize { get; set; }

        /// <summary>
        /// 캐시 마지막 업데이트 시간
        /// </summary>
        [JsonProperty("cache_last_updated")]
        public string? CacheLastUpdated { get; set; }

        /// <summary>
        /// 서비스 전체 상태 (사용 가능 여부)
        /// </summary>
        public bool IsReady => ClientInitialized && ApiKeyConfigured && TempDirExists;

        /// <summary>
        /// 상태 설명 텍스트
        /// </summary>
        public string StatusDescription
        {
            get
            {
                if (!ApiKeyConfigured) return "OpenAI API 키가 설정되지 않음";
                if (!ClientInitialized) return "Whisper 클라이언트 초기화 실패";
                if (!TempDirExists) return "임시 디렉토리 생성 실패";
                return "정상 작동";
            }
        }
    }

    /// <summary>
    /// 매크로 실행 상태 클래스
    /// 현재 실행 중인 매크로들의 상태 정보를 담습니다.
    /// </summary>
    public class MacroExecutionStatus
    {
        /// <summary>
        /// 서비스 활성화 여부
        /// </summary>
        [JsonProperty("service_active")]
        public bool ServiceActive { get; set; }

        /// <summary>
        /// 실행 중인 매크로 개수
        /// </summary>
        [JsonProperty("running_macros_count")]
        public int RunningMacrosCount { get; set; }

        /// <summary>
        /// 실행 중인 매크로 상세 목록
        /// </summary>
        [JsonProperty("running_macros")]
        public List<RunningMacroInfo> RunningMacros { get; set; } = new List<RunningMacroInfo>();

        /// <summary>
        /// 토글 매크로들의 상태
        /// </summary>
        [JsonProperty("toggle_states")]
        public Dictionary<int, bool> ToggleStates { get; set; } = new Dictionary<int, bool>();

        /// <summary>
        /// PyAutoGUI FAILSAFE 활성화 여부
        /// </summary>
        [JsonProperty("failsafe_enabled")]
        public bool FailsafeEnabled { get; set; }

        /// <summary>
        /// 상태 조회 시간
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = "";
    }

    /// <summary>
    /// 실행 중인 매크로 정보 클래스
    /// </summary>
    public class RunningMacroInfo
    {
        /// <summary>
        /// 매크로 ID
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// 매크로 이름
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// 동작 타입
        /// </summary>
        [JsonProperty("action_type")]
        public string ActionType { get; set; } = "";
    }

    /// <summary>
    /// 토글 상태 클래스
    /// 토글 매크로의 현재 상태 정보를 담습니다.
    /// </summary>
    public class ToggleState
    {
        /// <summary>
        /// 매크로 ID
        /// </summary>
        [JsonProperty("macro_id")]
        public int MacroId { get; set; }

        /// <summary>
        /// 매크로 이름
        /// </summary>
        [JsonProperty("macro_name")]
        public string MacroName { get; set; } = "";

        /// <summary>
        /// 현재 ON/OFF 상태
        /// </summary>
        [JsonProperty("is_on")]
        public bool IsOn { get; set; }

        /// <summary>
        /// UI 표시용 상태 텍스트
        /// </summary>
        public string StatusText => IsOn ? "ON" : "OFF";

        /// <summary>
        /// UI 표시용 상태 색상
        /// </summary>
        public string StatusColor => IsOn ? "#4CAF50" : "#F44336"; // 초록/빨강
    }
} 