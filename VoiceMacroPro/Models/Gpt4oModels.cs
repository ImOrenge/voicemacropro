using System;
using System.Text.Json.Serialization;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// GPT-4o 트랜스크립션 결과를 나타내는 모델 클래스
    /// 실시간 음성 인식 결과와 신뢰도 정보를 포함합니다.
    /// </summary>
    public class TranscriptionResult
    {
        /// <summary>
        /// 트랜스크립션 타입 (partial: 부분 결과, final: 최종 결과)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 인식된 텍스트 내용
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 인식 신뢰도 (0.0 ~ 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 트랜스크립션 생성 시간
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 고신뢰도 여부 (0.7 이상)
        /// </summary>
        public bool IsHighConfidence => Confidence >= 0.7;

        /// <summary>
        /// 신뢰도에 따른 색상 (높음: 녹색, 중간: 주황색, 낮음: 빨간색)
        /// </summary>
        public string ConfidenceColor
        {
            get
            {
                if (Confidence >= 0.8) return "#38A169";  // 녹색
                if (Confidence >= 0.6) return "#DD6B20";  // 주황색
                return "#E53E3E";  // 빨간색
            }
        }
    }

    /// <summary>
    /// WebSocket 서버와의 트랜스크립션 데이터 전송 객체
    /// JSON 직렬화를 위한 속성 매핑 포함
    /// </summary>
    public class TranscriptionData
    {
        /// <summary>
        /// 트랜스크립션 타입
        /// </summary>
        [JsonPropertyName("type")]
        public string type { get; set; } = string.Empty;

        /// <summary>
        /// 인식된 텍스트
        /// </summary>
        [JsonPropertyName("text")]
        public string text { get; set; } = string.Empty;

        /// <summary>
        /// 신뢰도 점수
        /// </summary>
        [JsonPropertyName("confidence")]
        public double confidence { get; set; }

        /// <summary>
        /// 타임스탬프 문자열
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string timestamp { get; set; } = string.Empty;
    }

    /// <summary>
    /// WebSocket 연결 상태를 나타내는 모델 클래스
    /// </summary>
    public class ConnectionStatus
    {
        /// <summary>
        /// 연결 상태 (true: 연결됨, false: 연결 끊김)
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 연결 상태 설명 메시지
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 마지막 연결 시도 시간
        /// </summary>
        public DateTime LastConnectionAttempt { get; set; }

        /// <summary>
        /// 재연결 시도 횟수
        /// </summary>
        public int ReconnectAttempts { get; set; }
    }

    /// <summary>
    /// 에러 정보 전송 객체
    /// </summary>
    public class ErrorData
    {
        /// <summary>
        /// 에러 메시지
        /// </summary>
        [JsonPropertyName("error")]
        public string error { get; set; } = string.Empty;
    }

    /// <summary>
    /// 실시간 오디오 캡처 설정 정보
    /// </summary>
    public class AudioCaptureSettings
    {
        /// <summary>
        /// 샘플링 레이트 (GPT-4o 최적화: 24kHz)
        /// </summary>
        public int SampleRate { get; set; } = 24000;

        /// <summary>
        /// 비트 깊이 (16비트)
        /// </summary>
        public int BitsPerSample { get; set; } = 16;

        /// <summary>
        /// 채널 수 (모노: 1)
        /// </summary>
        public int Channels { get; set; } = 1;

        /// <summary>
        /// 버퍼 크기 (밀리초, 실시간 처리용: 100ms)
        /// </summary>
        public int BufferMilliseconds { get; set; } = 100;
    }

    /// <summary>
    /// 음성 인식 세션 정보
    /// </summary>
    public class VoiceSession
    {
        /// <summary>
        /// 세션 ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 녹음 시작 시간
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 총 트랜스크립션 수
        /// </summary>
        public int TranscriptionCount { get; set; }

        /// <summary>
        /// 성공한 매크로 실행 수
        /// </summary>
        public int SuccessfulMacroExecutions { get; set; }

        /// <summary>
        /// 평균 신뢰도
        /// </summary>
        public double AverageConfidence { get; set; }

        /// <summary>
        /// 세션 통계 문자열
        /// </summary>
        public string SessionStats =>
            $"트랜스크립션: {TranscriptionCount}개 | 매크로 실행: {SuccessfulMacroExecutions}개 | 평균 신뢰도: {AverageConfidence:F2}";
    }

    /// <summary>
    /// GPT-4o 실시간 음성인식 통계 정보
    /// </summary>
    public class VoiceRecognitionStats
    {
        /// <summary>
        /// 총 처리된 오디오 시간 (초)
        /// </summary>
        public double TotalAudioDuration { get; set; }

        /// <summary>
        /// 평균 응답 시간 (밀리초)
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// 성공률 (%)
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
} 