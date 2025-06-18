"""
VoiceMacro Pro - 음성 분석 서비스
음성을 텍스트로 변환하고 매크로 명령어와 매칭하는 기능 제공
"""

import re
import time
import random
from typing import Dict, List, Optional, Tuple
from difflib import SequenceMatcher
from backend.utils.common_utils import get_logger, sanitize_string


class VoiceAnalysisService:
    """
    음성 분석 서비스 클래스
    - 음성을 텍스트로 변환 (시뮬레이션/Whisper)
    - 노이즈 필터링
    - 다국어 지원 (한국어, 영어)
    - 매크로 명령어 매칭
    """
    
    def __init__(self):
        """음성 분석 서비스 초기화"""
        self.logger = get_logger(__name__)
        
        # 지원 언어 설정
        self.supported_languages = ['ko', 'en']
        self.current_language = 'ko'  # 기본값: 한국어
        
        # 시뮬레이션용 샘플 음성 명령어들
        self.sample_commands = {
            'ko': [
                '공격', '방어', '스킬', '아이템', '이동', '점프', '달리기', '멈춰',
                '인벤토리', '설정', '저장', '로드', '종료', '일시정지', '계속',
                '스킬 하나', '스킬 둘', '스킬 셋', '포션 마시기', '무기 바꾸기'
            ],
            'en': [
                'attack', 'defend', 'skill', 'item', 'move', 'jump', 'run', 'stop',
                'inventory', 'settings', 'save', 'load', 'quit', 'pause', 'continue',
                'skill one', 'skill two', 'skill three', 'drink potion', 'change weapon'
            ]
        }
        
        # 노이즈 제거를 위한 패턴들
        self.noise_patterns = [
            r'\b(음|어|아|그|저|그거|이거)\b',  # 한국어 불용어
            r'\b(um|uh|ah|er|like|you know)\b',  # 영어 불용어
            r'[^\w\s]',  # 특수문자 제거
        ]
        
                 # 동의어 사전
        self.synonyms = {
            'ko': {
                '공격': ['때리기', '치기', '타격', '어택', '공격하기'],
                '방어': ['막기', '가드', '디펜스', '방어하기'],
                '스킬': ['기술', '능력', '마법', '스킬 사용'],
                '스킬 사용': ['스킬', '기술', '능력', '마법'],
                '아이템': ['아템', '물건', '도구', '아이템 사용'],
                '아이템 사용': ['아이템', '아템', '물건', '도구'],
                '이동': ['움직이기', '가기', '무브'],
                '점프': ['뛰기', '뛰어오르기', '껑충', '점프하기'],
                '점프하기': ['점프', '뛰기', '뛰어오르기', '껑충'],
                '달리기': ['뛰기', '러닝', '빠르게', '달리기 시작'],
                '달리기 시작': ['달리기', '뛰기', '러닝', '빠르게'],
                '멈춰': ['정지', '스톱', '그만'],
                '인벤토리': ['가방', '아이템창', '인벤', '인벤토리 열기'],
                '인벤토리 열기': ['인벤토리', '가방', '아이템창', '인벤'],
                '저장': ['세이브', '보존', '게임 저장'],
                '게임 저장': ['저장', '세이브', '보존'],
                '로드': ['불러오기', '로딩'],
                '종료': ['나가기', '끝내기', '종료하기', '게임 종료'],
                '게임 종료': ['종료', '나가기', '끝내기', '종료하기'],
                '일시정지': ['멈춤', '정지', '잠깐'],
                '계속': ['재개', '다시', '계속하기']
            },
            'en': {
                'attack': ['hit', 'strike', 'fight', 'assault'],
                'defend': ['guard', 'block', 'protect'],
                'skill': ['ability', 'magic', 'technique'],
                'item': ['thing', 'object', 'tool'],
                'move': ['go', 'walk', 'travel'],
                'jump': ['leap', 'hop', 'bounce'],
                'run': ['sprint', 'dash', 'rush'],
                'stop': ['halt', 'pause', 'cease'],
                'inventory': ['bag', 'items', 'storage'],
                'save': ['store', 'preserve'],
                'load': ['open', 'restore'],
                'quit': ['exit', 'close', 'end'],
                'pause': ['stop', 'halt', 'break'],
                'continue': ['resume', 'proceed', 'go on']
            }
        }
        
        self.logger.info("음성 분석 서비스가 초기화되었습니다.")
    
    def set_language(self, language: str) -> bool:
        """
        음성 인식 언어 설정
        
        Args:
            language (str): 언어 코드 ('ko', 'en')
            
        Returns:
            bool: 설정 성공 여부
        """
        if language not in self.supported_languages:
            self.logger.error(f"지원하지 않는 언어입니다: {language}")
            return False
        
        self.current_language = language
        self.logger.info(f"음성 인식 언어가 {language}로 설정되었습니다.")
        return True
    
    def _clean_text(self, text: str) -> str:
        """
        텍스트에서 노이즈 제거
        
        Args:
            text (str): 정리할 텍스트
            
        Returns:
            str: 정리된 텍스트
        """
        if not text:
            return ""
        
        # 기본 정리
        cleaned = text.lower().strip()
        
        # 노이즈 패턴 제거
        for pattern in self.noise_patterns:
            cleaned = re.sub(pattern, '', cleaned, flags=re.IGNORECASE)
        
        # 연속된 공백 제거
        cleaned = re.sub(r'\s+', ' ', cleaned).strip()
        
        return cleaned
    
    def analyze_audio_simulation(self, audio_data: List[float], duration: float) -> Dict:
        """
        오디오 데이터를 분석하여 텍스트로 변환 (시뮬레이션)
        
        Args:
            audio_data (List[float]): 오디오 데이터
            duration (float): 오디오 길이 (초)
            
        Returns:
            Dict: 분석 결과
        """
        try:
            # 음성 활동 시뮬레이션
            if not audio_data or len(audio_data) == 0:
                return {
                    'success': False,
                    'text': '',
                    'confidence': 0.0,
                    'language': self.current_language,
                    'processing_time': 0.0,
                    'message': '오디오 데이터가 없습니다.'
                }
            
            # 처리 시간 시뮬레이션
            start_time = time.time()
            time.sleep(0.1)  # 실제 처리 시간 시뮬레이션
            
            # 음성 활동 감지 시뮬레이션
            audio_level = sum(abs(x) for x in audio_data) / len(audio_data)
            
            if audio_level < 0.01:  # 너무 조용함
                return {
                    'success': False,
                    'text': '',
                    'confidence': 0.0,
                    'language': self.current_language,
                    'processing_time': time.time() - start_time,
                    'message': '음성이 감지되지 않았습니다.'
                }
            
            # 랜덤 명령어 선택 (시뮬레이션)
            commands = self.sample_commands[self.current_language]
            if random.random() < 0.8:  # 80% 성공률
                recognized_text = random.choice(commands)
                confidence = 0.7 + random.random() * 0.3  # 0.7-1.0 신뢰도
            else:
                # 인식 실패 시뮬레이션
                recognized_text = "알아들을 수 없음"
                confidence = 0.1 + random.random() * 0.3  # 0.1-0.4 신뢰도
            
            # 노이즈 추가 (시뮬레이션)
            if random.random() < 0.3:  # 30% 확률로 노이즈 포함
                noise_words = ['음', '어', '그'] if self.current_language == 'ko' else ['um', 'uh', 'er']
                noise = random.choice(noise_words)
                recognized_text = f"{noise} {recognized_text}"
            
            # 텍스트 정리
            cleaned_text = self._clean_text(recognized_text)
            
            processing_time = time.time() - start_time
            
            return {
                'success': True,
                'text': cleaned_text,
                'original_text': recognized_text,
                'confidence': confidence,
                'language': self.current_language,
                'processing_time': processing_time,
                'audio_level': audio_level,
                'message': '음성 인식 성공'
            }
            
        except Exception as e:
            self.logger.error(f"음성 분석 중 오류: {e}")
            return {
                'success': False,
                'text': '',
                'confidence': 0.0,
                'language': self.current_language,
                'processing_time': 0.0,
                'message': f'음성 분석 실패: {e}'
            }
    
    def find_similar_commands(self, text: str, commands: List[str], threshold: float = 0.6) -> List[Tuple[str, float]]:
        """
        텍스트와 유사한 명령어들을 찾기
        
        Args:
            text (str): 입력 텍스트
            commands (List[str]): 비교할 명령어 목록
            threshold (float): 유사도 임계값 (0.0-1.0)
            
        Returns:
            List[Tuple[str, float]]: (명령어, 유사도) 목록
        """
        if not text or not commands:
            return []
        
        similarities = []
        cleaned_text = self._clean_text(text)
        
        for command in commands:
            cleaned_command = self._clean_text(command)
            
            # 기본 유사도 계산
            similarity = SequenceMatcher(None, cleaned_text, cleaned_command).ratio()
            
            # 동의어 체크 (양방향)
            synonyms = self.synonyms.get(self.current_language, {})
            
            # 1. cleaned_command가 키인 경우
            if cleaned_command in synonyms:
                for synonym in synonyms[cleaned_command]:
                    synonym_similarity = SequenceMatcher(None, cleaned_text, synonym).ratio()
                    similarity = max(similarity, synonym_similarity)
            
            # 2. cleaned_text가 어떤 키의 동의어인 경우
            for main_word, synonym_list in synonyms.items():
                if cleaned_text in synonym_list:
                    main_similarity = SequenceMatcher(None, main_word, cleaned_command).ratio()
                    similarity = max(similarity, main_similarity)
            
            # 부분 일치 체크
            if cleaned_command in cleaned_text or cleaned_text in cleaned_command:
                similarity = max(similarity, 0.8)
            
            if similarity >= threshold:
                similarities.append((command, similarity))
        
        # 유사도 순으로 정렬
        similarities.sort(key=lambda x: x[1], reverse=True)
        return similarities
    
    def match_macro_commands(self, recognized_text: str, macro_commands: List[str]) -> Dict:
        """
        인식된 텍스트를 매크로 명령어와 매칭
        
        Args:
            recognized_text (str): 인식된 텍스트
            macro_commands (List[str]): 매크로 명령어 목록
            
        Returns:
            Dict: 매칭 결과
        """
        try:
            if not recognized_text or not macro_commands:
                return {
                    'success': False,
                    'matches': [],
                    'best_match': None,
                    'confidence': 0.0,
                    'message': '입력 데이터가 없습니다.'
                }
            
            # 유사한 명령어들 찾기
            similar_commands = self.find_similar_commands(
                recognized_text, 
                macro_commands, 
                threshold=0.5
            )
            
            if not similar_commands:
                return {
                    'success': False,
                    'matches': [],
                    'best_match': None,
                    'confidence': 0.0,
                    'message': '매칭되는 명령어가 없습니다.'
                }
            
            # 최고 매칭 결과
            best_match = similar_commands[0]
            
            # 매칭 결과 정리
            matches = [
                {
                    'command': cmd,
                    'similarity': sim,
                    'confidence_level': self._get_confidence_level(sim)
                }
                for cmd, sim in similar_commands[:5]  # 상위 5개만
            ]
            
            return {
                'success': True,
                'matches': matches,
                'best_match': {
                    'command': best_match[0],
                    'similarity': best_match[1],
                    'confidence_level': self._get_confidence_level(best_match[1])
                },
                'confidence': best_match[1],
                'recognized_text': recognized_text,
                'message': f'총 {len(matches)}개의 유사한 명령어를 찾았습니다.'
            }
            
        except Exception as e:
            self.logger.error(f"매크로 명령어 매칭 중 오류: {e}")
            return {
                'success': False,
                'matches': [],
                'best_match': None,
                'confidence': 0.0,
                'message': f'매칭 처리 실패: {e}'
            }
    
    def _get_confidence_level(self, similarity: float) -> str:
        """
        유사도 점수를 신뢰도 레벨로 변환
        
        Args:
            similarity (float): 유사도 점수 (0.0-1.0)
            
        Returns:
            str: 신뢰도 레벨
        """
        if similarity >= 0.9:
            return 'very_high'
        elif similarity >= 0.8:
            return 'high'
        elif similarity >= 0.7:
            return 'medium'
        elif similarity >= 0.6:
            return 'low'
        else:
            return 'very_low'
    
    def get_analysis_stats(self) -> Dict:
        """
        음성 분석 통계 정보 반환
        
        Returns:
            Dict: 통계 정보
        """
        return {
            'current_language': self.current_language,
            'supported_languages': self.supported_languages,
            'sample_commands_count': {
                lang: len(commands) 
                for lang, commands in self.sample_commands.items()
            },
            'synonyms_count': {
                lang: len(synonyms) 
                for lang, synonyms in self.synonyms.items()
            },
            'noise_patterns_count': len(self.noise_patterns)
        }


# 전역 음성 분석 서비스 인스턴스
_analysis_service_instance = None

def get_voice_analysis_service() -> VoiceAnalysisService:
    """
    음성 분석 서비스 싱글톤 인스턴스 반환
    
    Returns:
        VoiceAnalysisService: 음성 분석 서비스 인스턴스
    """
    global _analysis_service_instance
    if _analysis_service_instance is None:
        _analysis_service_instance = VoiceAnalysisService()
    return _analysis_service_instance 