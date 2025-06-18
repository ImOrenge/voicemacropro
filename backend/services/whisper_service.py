"""
VoiceMacro Pro - OpenAI Whisper 음성 인식 서비스
실제 OpenAI Whisper API를 사용한 음성-텍스트 변환 및 매크로 매칭
"""

import os
import io
import wave
import tempfile
import numpy as np
from typing import Optional, List, Dict, Tuple
from openai import OpenAI
from difflib import SequenceMatcher
import logging
from datetime import datetime

from backend.utils.config import config
from backend.utils.common_utils import get_logger
from backend.services.macro_service import macro_service


class WhisperService:
    """
    OpenAI Whisper API를 사용한 음성 인식 서비스 클래스
    - 오디오 데이터를 OpenAI Whisper API로 전송
    - 음성-텍스트 변환 수행
    - 변환된 텍스트와 매크로 명령어 매칭
    """
    
    def __init__(self):
        """Whisper 서비스 초기화"""
        self.logger = get_logger(__name__)
        
        # OpenAI 클라이언트 초기화
        try:
            if not config.OPENAI_API_KEY:
                raise ValueError("OpenAI API 키가 설정되지 않았습니다.")
            
            self.client = OpenAI(api_key=config.OPENAI_API_KEY)
            self.logger.info("OpenAI Whisper 클라이언트가 초기화되었습니다.")
        except Exception as e:
            self.logger.error(f"OpenAI 클라이언트 초기화 실패: {e}")
            self.client = None
        
        # 설정 값들
        self.model = config.WHISPER_MODEL
        self.language = config.VOICE_RECOGNITION_LANGUAGE
        self.sample_rate = config.SAMPLE_RATE
        self.channels = config.AUDIO_CHANNELS
        
        # 매크로 캐시 (성능 최적화용)
        self._macro_cache = []
        self._cache_last_updated = None
        
        self.logger.info("Whisper 서비스가 초기화되었습니다.")
    
    def _save_audio_to_file(self, audio_data: np.ndarray) -> str:
        """
        numpy 오디오 데이터를 임시 WAV 파일로 저장
        
        Args:
            audio_data (np.ndarray): 오디오 데이터 배열
            
        Returns:
            str: 저장된 임시 파일 경로
        """
        try:
            # 임시 파일 생성
            temp_file = tempfile.NamedTemporaryFile(
                suffix='.wav', 
                dir=config.TEMP_AUDIO_DIR, 
                delete=False
            )
            
            # numpy 배열을 int16으로 변환 (WAV 포맷용)
            # sounddevice는 float32 (-1.0 ~ 1.0) 범위로 제공
            audio_int16 = (audio_data * 32767).astype(np.int16)
            
            # WAV 파일로 저장
            with wave.open(temp_file.name, 'wb') as wav_file:
                wav_file.setnchannels(self.channels)  # 모노
                wav_file.setsampwidth(2)  # 16-bit
                wav_file.setframerate(self.sample_rate)  # 16kHz
                wav_file.writeframes(audio_int16.tobytes())
            
            self.logger.debug(f"오디오 파일 저장 완료: {temp_file.name}")
            return temp_file.name
            
        except Exception as e:
            self.logger.error(f"오디오 파일 저장 실패: {e}")
            raise
    
    def _cleanup_temp_file(self, file_path: str):
        """
        임시 파일 정리
        
        Args:
            file_path (str): 삭제할 파일 경로
        """
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
                self.logger.debug(f"임시 파일 삭제 완료: {file_path}")
        except Exception as e:
            self.logger.warning(f"임시 파일 삭제 실패: {e}")
    
    def transcribe_audio(self, audio_data: np.ndarray) -> Optional[str]:
        """
        오디오 데이터를 OpenAI Whisper API로 텍스트로 변환
        
        Args:
            audio_data (np.ndarray): 오디오 데이터 배열
            
        Returns:
            Optional[str]: 변환된 텍스트, 실패 시 None
        """
        if self.client is None:
            self.logger.error("OpenAI 클라이언트가 초기화되지 않았습니다.")
            return None
        
        temp_file_path = None
        try:
            # 오디오 데이터를 파일로 저장
            temp_file_path = self._save_audio_to_file(audio_data)
            
            # 파일 크기 확인 (OpenAI는 25MB 제한)
            file_size_mb = os.path.getsize(temp_file_path) / (1024 * 1024)
            if file_size_mb > config.VOICE_RECOGNITION_MAX_FILE_SIZE:
                self.logger.error(f"오디오 파일이 너무 큽니다: {file_size_mb:.1f}MB")
                return None
            
            # OpenAI Whisper API 호출
            with open(temp_file_path, 'rb') as audio_file:
                response = self.client.audio.transcriptions.create(
                    model=self.model,
                    file=audio_file,
                    language=self.language,
                    response_format="text"
                )
            
            # 응답에서 텍스트 추출
            transcribed_text = response.strip()
            
            if transcribed_text:
                self.logger.info(f"음성 인식 성공: '{transcribed_text}'")
                return transcribed_text
            else:
                self.logger.warning("음성 인식 결과가 비어있습니다.")
                return None
                
        except Exception as e:
            self.logger.error(f"Whisper API 호출 실패: {e}")
            return None
        
        finally:
            # 임시 파일 정리
            if temp_file_path:
                self._cleanup_temp_file(temp_file_path)
    
    def _update_macro_cache(self):
        """
        매크로 캐시 업데이트 (성능 최적화)
        """
        try:
            # 5분마다 캐시 갱신
            now = datetime.now()
            if (self._cache_last_updated is None or 
                (now - self._cache_last_updated).seconds > 300):
                
                self._macro_cache = macro_service.get_all_macros()
                self._cache_last_updated = now
                self.logger.debug(f"매크로 캐시 갱신 완료: {len(self._macro_cache)}개")
                
        except Exception as e:
            self.logger.error(f"매크로 캐시 갱신 실패: {e}")
    
    def _calculate_similarity(self, text1: str, text2: str) -> float:
        """
        두 텍스트 간의 유사도 계산
        
        Args:
            text1 (str): 첫 번째 텍스트
            text2 (str): 두 번째 텍스트
            
        Returns:
            float: 유사도 (0.0 ~ 1.0)
        """
        # 공백 제거 및 소문자 변환
        clean_text1 = text1.strip().lower()
        clean_text2 = text2.strip().lower()
        
        # SequenceMatcher를 사용한 유사도 계산
        similarity = SequenceMatcher(None, clean_text1, clean_text2).ratio()
        
        # 완전 일치나 부분 일치 보너스
        if clean_text1 == clean_text2:
            similarity = 1.0
        elif clean_text1 in clean_text2 or clean_text2 in clean_text1:
            similarity = max(similarity, 0.8)
        
        return similarity
    
    def find_matching_macros(self, recognized_text: str) -> List[Dict]:
        """
        인식된 텍스트와 매크로 명령어 매칭
        
        Args:
            recognized_text (str): 음성 인식된 텍스트
            
        Returns:
            List[Dict]: 매칭된 매크로 목록 (유사도 순 정렬)
        """
        if not recognized_text or not recognized_text.strip():
            return []
        
        try:
            # 매크로 캐시 업데이트
            self._update_macro_cache()
            
            matches = []
            recognized_text = recognized_text.strip()
            
            # 각 매크로와 유사도 계산
            for macro in self._macro_cache:
                if not macro.get('voice_command'):
                    continue
                
                voice_command = macro['voice_command']
                similarity = self._calculate_similarity(recognized_text, voice_command)
                
                # 임계값 이상인 경우만 추가
                if similarity >= config.MATCHING_THRESHOLD:
                    match_result = {
                        'macro_id': macro['id'],
                        'macro_name': macro['name'],
                        'voice_command': voice_command,
                        'action_type': macro['action_type'],
                        'key_sequence': macro['key_sequence'],
                        'similarity': similarity,
                        'confidence': similarity * 100,  # 퍼센트로 변환
                        'settings': macro.get('settings', {})
                    }
                    matches.append(match_result)
            
            # 유사도 순으로 정렬 (높은 순)
            matches.sort(key=lambda x: x['similarity'], reverse=True)
            
            # 최대 결과 개수 제한
            matches = matches[:config.MAX_MATCH_RESULTS]
            
            self.logger.info(f"매크로 매칭 완료: '{recognized_text}' -> {len(matches)}개 매칭")
            
            return matches
            
        except Exception as e:
            self.logger.error(f"매크로 매칭 실패: {e}")
            return []
    
    def process_voice_command(self, audio_data: np.ndarray) -> Dict:
        """
        음성 명령 전체 처리 파이프라인
        오디오 -> 텍스트 변환 -> 매크로 매칭
        
        Args:
            audio_data (np.ndarray): 오디오 데이터
            
        Returns:
            Dict: 처리 결과
        """
        start_time = datetime.now()
        
        result = {
            'success': False,
            'recognized_text': '',
            'matched_macros': [],
            'processing_time': 0,
            'error': None
        }
        
        try:
            # 1단계: 음성-텍스트 변환
            self.logger.info("음성 인식 시작...")
            recognized_text = self.transcribe_audio(audio_data)
            
            if not recognized_text:
                result['error'] = '음성 인식에 실패했습니다.'
                return result
            
            result['recognized_text'] = recognized_text
            
            # 2단계: 매크로 매칭
            self.logger.info("매크로 매칭 시작...")
            matched_macros = self.find_matching_macros(recognized_text)
            
            result['matched_macros'] = matched_macros
            result['success'] = True
            
            # 처리 시간 계산
            processing_time = (datetime.now() - start_time).total_seconds()
            result['processing_time'] = processing_time
            
            self.logger.info(f"음성 명령 처리 완료 ({processing_time:.2f}초)")
            
        except Exception as e:
            result['error'] = f'음성 명령 처리 중 오류 발생: {str(e)}'
            self.logger.error(result['error'])
        
        return result
    
    def get_service_status(self) -> Dict:
        """
        Whisper 서비스 상태 정보 반환
        
        Returns:
            Dict: 서비스 상태 정보
        """
        return {
            'client_initialized': self.client is not None,
            'api_key_configured': bool(config.OPENAI_API_KEY),
            'model': self.model,
            'language': self.language,
            'sample_rate': self.sample_rate,
            'temp_dir': config.TEMP_AUDIO_DIR,
            'temp_dir_exists': os.path.exists(config.TEMP_AUDIO_DIR),
            'macro_cache_size': len(self._macro_cache),
            'cache_last_updated': self._cache_last_updated.isoformat() if self._cache_last_updated else None
        }


# 서비스 인스턴스 생성
whisper_service = WhisperService() 