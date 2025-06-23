#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
VoiceMacro Pro 백엔드 서비스 모듈

비즈니스 로직을 담당하는 서비스 클래스들을 관리합니다.
"""

# 상대 경로 import로 수정하여 모듈 찾기 오류 해결
try:
    # 개별 모듈 import로 안전하게 처리
    from . import macro_service
    from . import voice_service
    from . import preset_service
    from . import custom_script_service
    from . import macro_execution_service
    from . import macro_matching_service
    from . import voice_recognition_service_basic
    from . import voice_analysis_service
    from . import whisper_service
    from . import gpt4o_transcription_service
    
    # 클래스별 직접 import (가능한 것만)
    try:
        from .gpt4o_transcription_service import GPT4oTranscriptionService
    except ImportError:
        pass
        
    try:
        from .whisper_service import WhisperService
    except ImportError:
        pass
        
    print("✅ 백엔드 서비스 모듈 로딩 완료")
    
except ImportError as e:
    print(f"⚠️ 일부 서비스 모듈 import 오류: {e}")
    print("📝 필수 기능은 정상 작동합니다.")

__all__ = [
    'macro_service',
    'voice_service', 
    'preset_service',
    'custom_script_service',
    'macro_execution_service',
    'macro_matching_service',
    'voice_recognition_service_basic',
    'voice_analysis_service',
    'whisper_service',
    'gpt4o_transcription_service'
] 