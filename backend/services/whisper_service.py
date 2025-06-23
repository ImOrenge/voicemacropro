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
        🔧 빈 파일 생성 방지를 위한 강화된 검증 추가
        
        Args:
            audio_data (np.ndarray): 오디오 데이터 배열
            
        Returns:
            str: 저장된 임시 파일 경로
            
        Raises:
            ValueError: 오디오 데이터가 유효하지 않은 경우
            IOError: 파일 저장에 실패한 경우
        """
        try:
            # 🔍 입력 데이터 검증
            if audio_data is None:
                raise ValueError("오디오 데이터가 None입니다.")
            
            if not isinstance(audio_data, np.ndarray):
                raise ValueError(f"오디오 데이터가 numpy 배열이 아닙니다: {type(audio_data)}")
            
            if audio_data.size == 0:
                raise ValueError("오디오 데이터가 비어있습니다.")
            
            # 1차원 배열로 변환 (다차원인 경우 flatten)
            if audio_data.ndim > 1:
                audio_data = audio_data.flatten()
                self.logger.debug(f"다차원 배열을 1차원으로 변환: {audio_data.shape}")
            
            # 데이터 타입 및 범위 확인
            self.logger.debug(f"📊 오디오 데이터 정보:")
            self.logger.debug(f"   - 형태: {audio_data.shape}")
            self.logger.debug(f"   - 타입: {audio_data.dtype}")
            self.logger.debug(f"   - 최소값: {audio_data.min():.6f}")
            self.logger.debug(f"   - 최대값: {audio_data.max():.6f}")
            self.logger.debug(f"   - 평균값: {audio_data.mean():.6f}")
            self.logger.debug(f"   - 표준편차: {audio_data.std():.6f}")
            
            # 🎵 오디오 신호 품질 검증
            signal_energy = np.sum(audio_data ** 2)  # 신호 에너지
            signal_max_amplitude = np.max(np.abs(audio_data))  # 최대 진폭
            
            self.logger.debug(f"🎶 신호 품질 분석:")
            self.logger.debug(f"   - 신호 에너지: {signal_energy:.6f}")
            self.logger.debug(f"   - 최대 진폭: {signal_max_amplitude:.6f}")
            
            # 최소 신호 강도 확인
            if signal_energy < 1e-8:  # 매우 작은 값
                self.logger.warning(f"⚠️  신호 에너지가 매우 낮습니다: {signal_energy}")
            
            if signal_max_amplitude < 1e-6:  # 매우 작은 진폭
                self.logger.warning(f"⚠️  신호 진폭이 매우 낮습니다: {signal_max_amplitude}")
            
            # 임시 파일 생성
            temp_file = tempfile.NamedTemporaryFile(
                suffix='.wav', 
                dir=config.TEMP_AUDIO_DIR, 
                delete=False
            )
            temp_path = temp_file.name
            temp_file.close()  # 파일 핸들 닫기 (Windows 호환성)
            
            # 🔄 데이터 형식 변환 및 정규화
            if audio_data.dtype == np.float32 or audio_data.dtype == np.float64:
                # float 타입 (-1.0 ~ 1.0 범위로 가정)
                # 범위 확인 및 정규화
                if np.max(np.abs(audio_data)) > 1.0:
                    self.logger.warning(f"⚠️  오디오 데이터가 [-1,1] 범위를 벗어남, 정규화 적용")
                    max_val = np.max(np.abs(audio_data))
                    audio_data = audio_data / max_val
                
                # int16으로 변환 (-32768 ~ 32767)
                audio_int16 = (audio_data * 32767).astype(np.int16)
                
            elif audio_data.dtype == np.int16:
                # 이미 int16인 경우 그대로 사용
                audio_int16 = audio_data
                
            elif audio_data.dtype == np.int32:
                # int32에서 int16으로 변환
                audio_int16 = (audio_data / 65536).astype(np.int16)
                
            else:
                # 기타 타입은 float로 변환 후 처리
                audio_float = audio_data.astype(np.float32)
                max_val = np.max(np.abs(audio_float))
                if max_val > 0:
                    audio_float = audio_float / max_val
                audio_int16 = (audio_float * 32767).astype(np.int16)
                self.logger.debug(f"🔄 타입 변환: {audio_data.dtype} -> int16")
            
            # 변환된 데이터 검증
            if audio_int16.size == 0:
                raise ValueError("변환된 오디오 데이터가 비어있습니다.")
            
            # 예상 파일 크기 계산 (바이트 단위)
            expected_size = audio_int16.size * 2 + 44  # PCM 데이터 + WAV 헤더
            expected_duration = audio_int16.size / self.sample_rate  # 초 단위
            
            self.logger.debug(f"💾 저장 예정 정보:")
            self.logger.debug(f"   - 샘플 수: {audio_int16.size}")
            self.logger.debug(f"   - 예상 크기: {expected_size} 바이트")
            self.logger.debug(f"   - 예상 길이: {expected_duration:.2f} 초")
            
            # 🎵 WAV 파일로 저장
            with wave.open(temp_path, 'wb') as wav_file:
                wav_file.setnchannels(self.channels)     # 1 (모노)
                wav_file.setsampwidth(2)                 # 16-bit = 2 bytes
                wav_file.setframerate(self.sample_rate)  # 16000 Hz
                wav_file.writeframes(audio_int16.tobytes())
            
            # 💾 저장된 파일 검증
            if not os.path.exists(temp_path):
                raise IOError(f"WAV 파일 생성 실패: {temp_path}")
            
            actual_size = os.path.getsize(temp_path)
            if actual_size == 0:
                os.remove(temp_path)  # 빈 파일 삭제
                raise IOError("빈 WAV 파일이 생성되었습니다.")
            
            if actual_size < 100:  # 최소 크기 (WAV 헤더 + 일부 데이터)
                os.remove(temp_path)  # 너무 작은 파일 삭제
                raise IOError(f"WAV 파일이 너무 작습니다: {actual_size} 바이트")
            
            # 📊 최종 검증 정보 로깅
            size_difference = abs(actual_size - expected_size)
            self.logger.info(f"✅ WAV 파일 저장 성공!")
            self.logger.info(f"📁 파일 경로: {temp_path}")
            self.logger.info(f"📦 파일 크기: {actual_size} 바이트 (예상: {expected_size})")
            self.logger.info(f"⏱️  오디오 길이: {expected_duration:.2f} 초")
            
            if size_difference > 100:  # 100바이트 이상 차이나면 경고
                self.logger.warning(f"⚠️  예상 크기와 실제 크기 차이: {size_difference} 바이트")
            
            return temp_path
            
        except Exception as e:
            self.logger.error(f"❌ 오디오 파일 저장 실패: {e}")
            # 실패한 경우 임시 파일 정리
            if 'temp_path' in locals() and os.path.exists(temp_path):
                try:
                    os.remove(temp_path)
                except:
                    pass
            raise IOError(f"오디오 파일 저장 실패: {str(e)}")
    
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
    
    def transcribe_audio(self, audio_input) -> Dict:
        """
        오디오 데이터 또는 파일을 OpenAI Whisper API로 텍스트로 변환
        🎯 정확도 개선을 위한 매개변수 최적화 적용
        
        Args:
            audio_input: 오디오 데이터 (np.ndarray) 또는 파일 경로 (str)
            
        Returns:
            Dict: 변환 결과 {'success': bool, 'text': str, 'confidence': float}
        """
        if self.client is None:
            self.logger.error("OpenAI 클라이언트가 초기화되지 않았습니다.")
            return {'success': False, 'text': '', 'confidence': 0.0, 'error': 'OpenAI 클라이언트 미초기화'}
        
        temp_file_path = None
        file_created_by_us = False
        
        try:
            # 입력 타입에 따라 처리
            if isinstance(audio_input, str):
                # 파일 경로가 전달된 경우
                if not os.path.exists(audio_input):
                    self.logger.error(f"오디오 파일이 존재하지 않습니다: {audio_input}")
                    return {'success': False, 'text': '', 'confidence': 0.0, 'error': '파일이 존재하지 않음'}
                
                temp_file_path = audio_input
                self.logger.debug(f"파일 경로로 전달됨: {temp_file_path}")
                
            elif isinstance(audio_input, np.ndarray):
                # numpy 배열이 전달된 경우
                temp_file_path = self._save_audio_to_file(audio_input)
                file_created_by_us = True
                self.logger.debug(f"numpy 배열을 파일로 저장함: {temp_file_path}")
                
            else:
                self.logger.error(f"지원하지 않는 오디오 입력 타입: {type(audio_input)}")
                return {'success': False, 'text': '', 'confidence': 0.0, 'error': '지원하지 않는 입력 타입'}
            
            # 🔍 오디오 파일 품질 검증 (빈 파일 방지)
            file_size_bytes = os.path.getsize(temp_file_path)
            file_size_mb = file_size_bytes / (1024 * 1024)
            
            # 최소 파일 크기 검증 (100바이트 이상)
            if file_size_bytes < 100:
                self.logger.error(f"❌ 오디오 파일이 너무 작습니다: {file_size_bytes}바이트 (빈 파일로 추정)")
                return {'success': False, 'text': '', 'confidence': 0.0, 'error': f'오디오 파일 너무 작음: {file_size_bytes}바이트'}
            
            # 최대 파일 크기 검증 (OpenAI는 25MB 제한)
            if file_size_mb > config.VOICE_RECOGNITION_MAX_FILE_SIZE:
                self.logger.error(f"❌ 오디오 파일이 너무 큽니다: {file_size_mb:.1f}MB")
                return {'success': False, 'text': '', 'confidence': 0.0, 'error': f'파일 크기 초과: {file_size_mb:.1f}MB'}
            
            # 오디오 지속시간 추정 (16kHz 모노 기준)
            estimated_duration = file_size_bytes / (self.sample_rate * 2)  # 16-bit = 2 bytes per sample
            
            self.logger.info(f"🎵 오디오 파일 검증 완료 - 크기: {file_size_bytes}바이트 ({file_size_mb:.2f}MB), 예상 길이: {estimated_duration:.1f}초")
            
            # 🎯 게임 매크로 명령어 최적화를 위한 프롬프트
            optimization_prompt = (
                "자연스러운 대화를 하는 것처럼 인식해줘"
            )
            
            self.logger.info(f"🚀 Whisper API 호출 시작 (최적화된 설정 적용)")
            
            # 🔧 OpenAI Whisper API 호출 (정확도 최적화 설정)
            with open(temp_file_path, 'rb') as audio_file:
                response = self.client.audio.transcriptions.create(
                    model=self.model,              # whisper-1
                    file=audio_file,               # 오디오 파일
                    language=self.language,        # ko (한국어)
                    response_format="verbose_json", # 상세 정보 포함 (신뢰도 등)
                    temperature=0.0,               # 창의성 최소화 (정확도 우선)
                    prompt=optimization_prompt     # 게임 매크로 명령어 힌트
                )
            
            # 🔍 상세 응답 정보 추출
            transcribed_text = response.text.strip() if response.text else ""
            
            # verbose_json 응답에서 추가 정보 추출
            language_detected = getattr(response, 'language', 'unknown')
            duration = getattr(response, 'duration', 0.0)
            
            # 세그먼트 정보가 있으면 평균 확신도 계산
            segments = getattr(response, 'segments', [])
            if segments and len(segments) > 0:
                # 각 세그먼트의 평균 확신도 계산
                confidences = []
                for segment in segments:
                    if hasattr(segment, 'avg_logprob'):
                        # log probability를 확률로 변환 (0.0 ~ 1.0)
                        confidence = min(1.0, max(0.0, np.exp(segment.avg_logprob)))
                        confidences.append(confidence)
                
                if confidences:
                    avg_confidence = sum(confidences) / len(confidences)
                else:
                    # 텍스트 길이 기반 추정
                    avg_confidence = min(0.95, 0.6 + (len(transcribed_text) * 0.03))
            else:
                # 세그먼트 정보가 없으면 텍스트 길이 기반 추정
                avg_confidence = min(0.95, 0.6 + (len(transcribed_text) * 0.03))
            
            if transcribed_text:
                self.logger.info(f"✅ Whisper 음성 인식 성공!")
                self.logger.info(f"📝 인식된 텍스트: '{transcribed_text}'")
                self.logger.info(f"🎯 신뢰도: {avg_confidence:.2f} ({avg_confidence*100:.1f}%)")
                self.logger.info(f"🌍 감지된 언어: {language_detected}")
                self.logger.info(f"⏱️  오디오 길이: {duration:.1f}초")
                
                return {
                    'success': True,
                    'text': transcribed_text,
                    'confidence': avg_confidence,
                    'source': 'whisper',
                    'language': language_detected,
                    'duration': duration,
                    'segments_count': len(segments)
                }
            else:
                self.logger.warning("🔇 Whisper 음성 인식 결과가 비어있습니다.")
                self.logger.warning(f"🔍 디버그 정보 - 언어: {language_detected}, 길이: {duration:.1f}초")
                return {
                    'success': False,
                    'text': '',
                    'confidence': 0.0,
                    'error': '인식 결과 없음',
                    'language': language_detected,
                    'duration': duration
                }
                
        except Exception as e:
            self.logger.error(f"❌ Whisper API 호출 실패: {e}")
            # 상세 오류 정보 로깅
            if hasattr(e, 'response'):
                self.logger.error(f"🔍 API 응답 상세: {e.response}")
            return {
                'success': False,
                'text': '',
                'confidence': 0.0,
                'error': f'API 호출 실패: {str(e)}'
            }
        
        finally:
            # 우리가 만든 임시 파일만 정리
            if temp_file_path and file_created_by_us:
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
        두 텍스트 간의 유사도 계산 (게임 매크로 명령어 특화)
        🎯 한국어 게임 명령어 인식 정확도 최적화
        
        Args:
            text1 (str): 첫 번째 텍스트 (인식된 음성)
            text2 (str): 두 번째 텍스트 (등록된 매크로 명령어)
            
        Returns:
            float: 유사도 (0.0 ~ 1.0)
        """
        # 텍스트 정규화
        clean_text1 = text1.strip().lower().replace(" ", "")
        clean_text2 = text2.strip().lower().replace(" ", "")
        
        # 빈 문자열 처리
        if not clean_text1 or not clean_text2:
            return 0.0
        
        # 🎯 완전 일치 검사 (최고 우선순위)
        if clean_text1 == clean_text2:
            return 1.0
        
        # 🎯 한국어 게임 명령어 동의어 매핑
        synonym_groups = {
            # 공격 관련
            "공격": ["공격", "어택", "때리기", "치기", "타격"],
            "스킬": ["스킬", "기술", "능력", "특수공격"],
            "궁극기": ["궁극기", "궁극", "울트", "필살기", "대기술"],
            
            # 이동 관련  
            "달리기": ["달리기", "뛰기", "런", "스프린트"],
            "점프": ["점프", "뛰기", "뛰어오르기", "점프하기"],
            
            # 아이템 관련
            "아이템": ["아이템", "템", "물건", "도구"],
            "포션": ["포션", "물약", "힐팩", "회복약"],
            "무기": ["무기", "웨폰", "검", "총", "칼"],
            
            # 액션 관련
            "방어": ["방어", "막기", "가드", "블록"],
            "힐링": ["힐링", "회복", "치료", "힐"],
            "콤보": ["콤보", "연계", "연속기"],
            "연사": ["연사", "빠른공격", "자동공격", "속사"],
            
            # 제어 관련
            "시작": ["시작", "스타트", "개시", "실행"],
            "중지": ["중지", "정지", "스톱", "멈춤"],
            "일시정지": ["일시정지", "멈춤", "포즈"],
            "재시작": ["재시작", "리스타트", "다시시작"],
            
            # 숫자 관련
            "1": ["1", "하나", "일", "원"],
            "2": ["2", "둘", "이", "투"],
            "3": ["3", "셋", "삼", "쓰리"],
            "4": ["4", "넷", "사", "포"],
            "5": ["5", "다섯", "오", "파이브"]
        }
        
        # 🔄 동의어 확장 검사
        def get_canonical_word(word):
            """단어를 표준형으로 변환"""
            for canonical, synonyms in synonym_groups.items():
                if word in synonyms:
                    return canonical
            return word
        
        canonical_text1 = get_canonical_word(clean_text1)
        canonical_text2 = get_canonical_word(clean_text2)
        
        # 표준형으로 완전 일치 검사
        if canonical_text1 == canonical_text2:
            return 0.95  # 동의어 일치
        
        # 🎯 부분 일치 검사 (포함 관계)
        if clean_text1 in clean_text2 or clean_text2 in clean_text1:
            longer_text = max(clean_text1, clean_text2, key=len)
            shorter_text = min(clean_text1, clean_text2, key=len)
            # 부분 일치 점수 = 짧은 텍스트 길이 / 긴 텍스트 길이
            partial_score = len(shorter_text) / len(longer_text)
            return min(0.9, 0.7 + partial_score * 0.2)  # 최대 0.9점
        
        # 🎯 숫자 + 텍스트 조합 처리 (예: "스킬1", "스킬 1")
        import re
        
        # 숫자 패턴 추출
        def extract_base_and_number(text):
            match = re.match(r'([^\d]+)(\d+)', text)
            if match:
                return match.group(1).strip(), match.group(2)
            return text, None
        
        base1, num1 = extract_base_and_number(clean_text1)
        base2, num2 = extract_base_and_number(clean_text2)
        
        # 기본 단어가 같고 숫자도 같은 경우
        if base1 == base2 and num1 == num2 and num1 is not None:
            return 0.95
        
        # 기본 단어만 같은 경우 (숫자 다름)
        if base1 == base2 and num1 is not None and num2 is not None:
            return 0.75
        
        # 🎯 레벤슈타인 거리 기반 유사도 (SequenceMatcher)
        base_similarity = SequenceMatcher(None, clean_text1, clean_text2).ratio()
        
        # 🎯 자음/모음 분리 유사도 (한국어 특화)
        def get_consonants_vowels(text):
            """한국어 텍스트에서 자음과 모음 추출"""
            consonants = ""
            vowels = ""
            
            for char in text:
                if '가' <= char <= '힣':  # 한글인 경우
                    # 유니코드 분해 (초성, 중성, 종성)
                    code = ord(char) - ord('가')
                    cho = code // (21 * 28)  # 초성
                    jung = (code % (21 * 28)) // 28  # 중성
                    jong = code % 28  # 종성
                    
                    # 초성 (자음)
                    cho_list = ['ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ']
                    consonants += cho_list[cho]
                    
                    # 중성 (모음)
                    jung_list = ['ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ', 'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ']
                    vowels += jung_list[jung]
                    
                    # 종성 (자음, 있는 경우)
                    if jong > 0:
                        jong_list = ['', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ']
                        consonants += jong_list[jong]
                else:
                    # 한글이 아닌 경우 그대로 추가
                    consonants += char
                    vowels += char
            
            return consonants, vowels
        
        try:
            cons1, vowels1 = get_consonants_vowels(clean_text1)
            cons2, vowels2 = get_consonants_vowels(clean_text2)
            
            # 자음 유사도와 모음 유사도 계산
            consonant_similarity = SequenceMatcher(None, cons1, cons2).ratio()
            vowel_similarity = SequenceMatcher(None, vowels1, vowels2).ratio()
            
            # 한국어 특화 유사도 (자음 가중치 높임)
            korean_similarity = consonant_similarity * 0.7 + vowel_similarity * 0.3
            
        except Exception:
            # 한국어 처리 실패 시 기본 유사도만 사용
            korean_similarity = base_similarity
        
        # 🎯 최종 유사도 계산 (여러 방법의 가중 평균)
        final_similarity = (
            base_similarity * 0.4 +      # 기본 문자열 유사도
            korean_similarity * 0.4 +    # 한국어 특화 유사도
            (1.0 if len(clean_text1) == len(clean_text2) else 0.8) * 0.2  # 길이 유사도
        )
        
        # 🎯 게임 명령어 특별 보정
        if any(word in clean_text1 for word in ["스킬", "공격", "점프", "달리기"]) and \
           any(word in clean_text2 for word in ["스킬", "공격", "점프", "달리기"]):
            final_similarity += 0.1  # 게임 명령어 보너스
        
        # 0.0 ~ 1.0 범위로 제한
        return max(0.0, min(1.0, final_similarity))
    
    def find_matching_macros(self, recognized_text: str) -> List[Dict]:
        """
        인식된 텍스트와 매크로 명령어 매칭
        🔍 상세한 디버깅 정보 포함
        
        Args:
            recognized_text (str): 음성 인식된 텍스트
            
        Returns:
            List[Dict]: 매칭된 매크로 목록 (유사도 순 정렬)
        """
        if not recognized_text or not recognized_text.strip():
            self.logger.warning("🔍 매크로 매칭 실패: 인식된 텍스트가 비어있습니다.")
            return []
        
        try:
            # 매크로 캐시 업데이트
            self._update_macro_cache()
            
            if not self._macro_cache:
                self.logger.warning("🔍 매크로 매칭 실패: 등록된 매크로가 없습니다.")
                return []
            
            matches = []
            recognized_text = recognized_text.strip()
            
            self.logger.info(f"🎯 매크로 매칭 시작")
            self.logger.info(f"📝 인식된 텍스트: '{recognized_text}'")
            self.logger.info(f"📊 등록된 매크로 수: {len(self._macro_cache)}")
            self.logger.info(f"🎯 매칭 임계값: {config.MATCHING_THRESHOLD:.2f}")
            
            # 각 매크로와 유사도 계산
            all_similarities = []  # 모든 유사도 저장 (디버깅용)
            
            for i, macro in enumerate(self._macro_cache):
                if not macro.get('voice_command'):
                    continue
                
                voice_command = macro['voice_command']
                similarity = self._calculate_similarity(recognized_text, voice_command)
                
                # 모든 유사도 기록 (디버깅용)
                all_similarities.append({
                    'macro_name': macro['name'],
                    'voice_command': voice_command,
                    'similarity': similarity,
                    'above_threshold': similarity >= config.MATCHING_THRESHOLD
                })
                
                # 임계값 이상인 경우만 결과에 추가
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
            
            # 🔍 상세 디버깅 정보 로깅
            self.logger.info(f"📈 매크로 매칭 분석 결과:")
            
            # 상위 5개 유사도 결과 표시 (임계값 무관)
            top_similarities = sorted(all_similarities, key=lambda x: x['similarity'], reverse=True)[:5]
            for j, sim_info in enumerate(top_similarities, 1):
                status = "✅ 매칭" if sim_info['above_threshold'] else "❌ 임계값 미달"
                self.logger.info(f"   {j}. '{sim_info['voice_command']}' ({sim_info['macro_name']}) - {sim_info['similarity']:.3f} {status}")
            
            # 유사도 순으로 정렬 (높은 순)
            matches.sort(key=lambda x: x['similarity'], reverse=True)
            
            # 최대 결과 개수 제한
            matches = matches[:config.MAX_MATCH_RESULTS]
            
            # 🎯 최종 매칭 결과 로깅
            if matches:
                self.logger.info(f"✅ 매크로 매칭 성공: '{recognized_text}' -> {len(matches)}개 매칭")
                self.logger.info(f"🏆 최고 매칭 결과:")
                for k, match in enumerate(matches[:3], 1):  # 상위 3개만 표시
                    self.logger.info(f"   {k}. '{match['voice_command']}' ({match['macro_name']}) - {match['confidence']:.1f}%")
                
                # 매칭 품질 평가
                best_similarity = matches[0]['similarity']
                if best_similarity >= 0.9:
                    quality = "🌟 우수"
                elif best_similarity >= 0.8:
                    quality = "👍 양호"
                elif best_similarity >= 0.7:
                    quality = "⚠️  보통"
                else:
                    quality = "⚠️  낮음"
                
                self.logger.info(f"📊 매칭 품질: {quality} (최고 유사도: {best_similarity:.3f})")
                
            else:
                self.logger.warning(f"❌ 매크로 매칭 실패: '{recognized_text}'")
                self.logger.warning(f"🔍 가능한 원인:")
                
                if not all_similarities:
                    self.logger.warning(f"   - 음성 명령어가 설정된 매크로가 없음")
                else:
                    max_similarity = max(s['similarity'] for s in all_similarities)
                    closest_match = next(s for s in all_similarities if s['similarity'] == max_similarity)
                    
                    self.logger.warning(f"   - 가장 유사한 명령어: '{closest_match['voice_command']}' (유사도: {max_similarity:.3f})")
                    self.logger.warning(f"   - 임계값 부족: {max_similarity:.3f} < {config.MATCHING_THRESHOLD:.3f}")
                    
                    # 개선 제안
                    if max_similarity > 0.5:
                        self.logger.warning(f"💡 개선 제안: 임계값을 {max_similarity:.2f}로 낮추거나 음성 명령어를 '{recognized_text}'로 수정")
            
            return matches
            
        except Exception as e:
            self.logger.error(f"❌ 매크로 매칭 중 오류 발생: {e}")
            return []
    
    def process_voice_command(self, audio_input) -> Dict:
        """
        음성 명령 전체 처리 파이프라인
        오디오 -> 텍스트 변환 -> 매크로 매칭
        
        Args:
            audio_input: 오디오 데이터 (np.ndarray) 또는 파일 경로 (str)
            
        Returns:
            Dict: 처리 결과
        """
        start_time = datetime.now()
        
        result = {
            'success': False,
            'recognized_text': '',
            'matched_macros': [],
            'processing_time': 0,
            'error': None,
            'transcription_result': {}
        }
        
        try:
            # 1단계: 음성-텍스트 변환
            self.logger.info("음성 인식 시작...")
            transcription_result = self.transcribe_audio(audio_input)
            result['transcription_result'] = transcription_result
            
            if not transcription_result.get('success'):
                result['error'] = transcription_result.get('error', '음성 인식에 실패했습니다.')
                return result
            
            recognized_text = transcription_result.get('text', '')
            if not recognized_text.strip():
                result['error'] = '인식된 텍스트가 비어있습니다.'
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