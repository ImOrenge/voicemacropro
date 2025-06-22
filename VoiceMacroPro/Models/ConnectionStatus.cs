namespace VoiceMacroPro.Models
{
    /// <summary>
    /// 서버 연결 상태를 나타내는 열거형
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// 연결되지 않음
        /// </summary>
        Disconnected,

        /// <summary>
        /// 연결 시도 중
        /// </summary>
        Connecting,

        /// <summary>
        /// 연결됨
        /// </summary>
        Connected,

        /// <summary>
        /// 연결 오류
        /// </summary>
        Error
    }
} 