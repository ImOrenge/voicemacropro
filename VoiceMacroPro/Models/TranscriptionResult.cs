using System;

namespace VoiceMacroPro.Models
{
    /// <summary>
    /// 음성 인식 결과를 나타내는 클래스
    /// </summary>
    public class TranscriptionResult
    {
        /// <summary>
        /// 결과 타입 (partial 또는 final)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 인식된 텍스트
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 인식 신뢰도 (0.0 ~ 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 인식 시간
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
} 