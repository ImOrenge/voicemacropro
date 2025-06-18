"""
VoiceMacro Pro - 설정 관리 모듈
OpenAI API 키, Whisper 모델 설정, 음성 인식 파라미터 관리
"""

import os
from dotenv import load_dotenv

# .env 파일 로드 (있는 경우)
load_dotenv()


class Config:
    """애플리케이션 설정 클래스"""
    
    # OpenAI API 설정
    OPENAI_API_KEY = os.getenv('OPENAI_API_KEY', '')
    WHISPER_MODEL = os.getenv('WHISPER_MODEL', 'whisper-1')
    
    # 음성 인식 설정
    VOICE_RECOGNITION_LANGUAGE = os.getenv('VOICE_RECOGNITION_LANGUAGE', 'ko')
    VOICE_RECOGNITION_TIMEOUT = int(os.getenv('VOICE_RECOGNITION_TIMEOUT', '30'))
    VOICE_RECOGNITION_MAX_FILE_SIZE = int(os.getenv('VOICE_RECOGNITION_MAX_FILE_SIZE', '25'))
    
    # 오디오 설정
    SAMPLE_RATE = 16000  # Whisper 권장 샘플레이트
    AUDIO_CHANNELS = 1   # 모노 채널
    AUDIO_FORMAT = 'wav' # 오디오 포맷
    
    # 파일 저장 설정
    TEMP_AUDIO_DIR = 'temp_audio'
    LOG_DIR = 'logs'
    
    # 로그 설정
    LOG_LEVEL = os.getenv('LOG_LEVEL', 'INFO')
    LOG_FILE = os.getenv('LOG_FILE', 'logs/voice_recognition.log')
    
    # 매크로 매칭 설정
    MATCHING_THRESHOLD = 0.7  # 매크로 매칭 최소 유사도
    MAX_MATCH_RESULTS = 5     # 최대 매칭 결과 개수
    
    @classmethod
    def validate_config(cls) -> bool:
        """
        설정 값 유효성 검증
        
        Returns:
            bool: 설정이 유효한 경우 True
        """
        if not cls.OPENAI_API_KEY:
            print("⚠️  경고: OPENAI_API_KEY가 설정되지 않았습니다.")
            print("📝 환경 변수 또는 .env 파일에 API 키를 설정해주세요.")
            print("🔧 테스트를 위해 서버는 시작하지만 실제 음성 인식은 작동하지 않습니다.")
        
        # 필요한 디렉토리 생성
        os.makedirs(cls.TEMP_AUDIO_DIR, exist_ok=True)
        os.makedirs(cls.LOG_DIR, exist_ok=True)
        
        return True
    
    @classmethod
    def get_openai_client_config(cls) -> dict:
        """
        OpenAI 클라이언트 설정 반환
        
        Returns:
            dict: OpenAI 클라이언트 설정
        """
        return {
            'api_key': cls.OPENAI_API_KEY,
            'timeout': cls.VOICE_RECOGNITION_TIMEOUT
        }


# 설정 인스턴스
config = Config() 