using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// Dictionary<string, object> Settings 필드를 위한 커스텀 JSON 컨버터
    /// 백엔드에서 JSON 문자열로 전송되는 settings를 딕셔너리로 변환합니다.
    /// </summary>
    public class SettingsJsonConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> ReadJson(JsonReader reader, Type objectType, Dictionary<string, object>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return new Dictionary<string, object>();
                }

                if (reader.TokenType == JsonToken.String)
                {
                    // JSON 문자열인 경우 파싱
                    var jsonString = reader.Value?.ToString();
                    if (string.IsNullOrWhiteSpace(jsonString))
                    {
                        return new Dictionary<string, object>();
                    }

                    var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                    return result ?? new Dictionary<string, object>();
                }

                if (reader.TokenType == JsonToken.StartObject)
                {
                    // 이미 객체인 경우 직접 역직렬화
                    var result = serializer.Deserialize<Dictionary<string, object>>(reader);
                    return result ?? new Dictionary<string, object>();
                }

                return new Dictionary<string, object>();
            }
            catch (Exception)
            {
                // 파싱 실패 시 빈 딕셔너리 반환
                return new Dictionary<string, object>();
            }
        }

        public override void WriteJson(JsonWriter writer, Dictionary<string, object>? value, JsonSerializer serializer)
        {
            // 쓰기 시에는 기본 직렬화 사용
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// 매크로 정보를 저장하는 데이터 모델 클래스
    /// 데이터베이스의 매크로 테이블과 대응됩니다.
    /// </summary>
    public class Macro
    {
        /// <summary>
        /// 매크로의 고유 ID (데이터베이스에서 자동 생성)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 매크로 이름 (사용자가 지정)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 음성 명령어 (사용자가 말하는 단어나 문장)
        /// </summary>
        public string VoiceCommand { get; set; } = string.Empty;

        /// <summary>
        /// 동작 타입 (combo, rapid, hold, toggle, repeat)
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// 키 시퀀스 (실제로 실행될 키보드/마우스 동작)
        /// </summary>
        public string KeySequence { get; set; } = string.Empty;

        /// <summary>
        /// 추가 설정 정보 (딕셔너리 형태)
        /// 백엔드에서 JSON 문자열로 전송될 수 있어 커스텀 컨버터 사용
        /// </summary>
        [JsonConverter(typeof(SettingsJsonConverter))]
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 생성 날짜 및 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 마지막 수정 날짜 및 시간
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 사용 횟수 (매크로가 실행된 총 횟수)
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 기본 생성자
        /// 새로운 매크로 객체를 생성할 때 사용합니다.
        /// </summary>
        public Macro()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 매개변수가 있는 생성자
        /// 매크로의 기본 정보를 설정하여 객체를 생성합니다.
        /// </summary>
        /// <param name="name">매크로 이름</param>
        /// <param name="voiceCommand">음성 명령어</param>
        /// <param name="actionType">동작 타입</param>
        /// <param name="keySequence">키 시퀀스</param>
        public Macro(string name, string voiceCommand, string actionType, string keySequence)
        {
            Name = name;
            VoiceCommand = voiceCommand;
            ActionType = actionType;
            KeySequence = keySequence;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 매크로 정보를 문자열로 반환하는 함수
        /// 디버깅이나 로그 출력 시 유용합니다.
        /// </summary>
        /// <returns>매크로 정보가 담긴 문자열</returns>
        public override string ToString()
        {
            return $"매크로: {Name} | 명령어: {VoiceCommand} | 타입: {ActionType} | 사용횟수: {UsageCount}";
        }

        /// <summary>
        /// 동작 타입에 따른 한글 표시명을 반환하는 함수
        /// UI에서 사용자에게 보여줄 때 사용합니다.
        /// </summary>
        /// <returns>동작 타입의 한글 표시명</returns>
        public string GetActionTypeDisplayName()
        {
            return ActionType switch
            {
                "combo" => "콤보",
                "rapid" => "연사",
                "hold" => "홀드",
                "toggle" => "토글",
                "repeat" => "반복",
                _ => "알 수 없음"
            };
        }

        /// <summary>
        /// 매크로가 유효한지 검증하는 함수
        /// 필수 정보가 모두 입력되었는지 확인합니다.
        /// </summary>
        /// <returns>유효하면 true, 아니면 false</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(VoiceCommand) &&
                   !string.IsNullOrWhiteSpace(ActionType) &&
                   !string.IsNullOrWhiteSpace(KeySequence);
        }
    }
} 