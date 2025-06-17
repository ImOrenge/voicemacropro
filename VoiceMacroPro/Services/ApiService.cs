using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VoiceMacroPro.Models;

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
        public async Task<List<Macro>> GetMacrosAsync(string searchTerm = null, string sortBy = "name")
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
        public bool Success { get; set; }

        /// <summary>
        /// 응답 데이터
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 응답 메시지
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 오류 정보 (오류 발생 시)
        /// </summary>
        public string? Error { get; set; }
    }
} 