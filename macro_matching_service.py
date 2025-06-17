"""
VoiceMacro Pro - 매크로 명령어 매칭 서비스
음성 인식 결과를 등록된 매크로 명령어와 매칭하는 고급 기능 제공
"""

import time
from typing import Dict, List, Optional, Tuple, Any
from dataclasses import dataclass
from enum import Enum
from difflib import SequenceMatcher

from common_utils import get_logger
from macro_service import macro_service


class MatchConfidenceLevel(Enum):
    """매칭 신뢰도 레벨 열거형"""
    VERY_HIGH = "very_high"  # 0.9 이상
    HIGH = "high"           # 0.8-0.9
    MEDIUM = "medium"       # 0.7-0.8
    LOW = "low"            # 0.6-0.7
    VERY_LOW = "very_low"   # 0.6 미만


@dataclass
class MacroMatch:
    """매크로 매칭 결과 데이터 클래스"""
    macro_id: int
    macro_name: str
    voice_command: str
    similarity: float
    confidence_level: MatchConfidenceLevel
    match_type: str  # 'exact', 'partial', 'synonym', 'fuzzy'
    action_type: str
    key_sequence: str


class MacroMatchingService:
    """
    매크로 명령어 매칭 서비스 클래스
    - 유사도 기반 매크로 검색
    - 부분 일치 및 동의어 처리
    - 확신도 표시
    - 매칭 히스토리 관리
    """
    
    def __init__(self):
        """매크로 매칭 서비스 초기화"""
        self.logger = get_logger(__name__)
        
        # 매칭 설정
        self.similarity_threshold = 0.6  # 기본 유사도 임계값
        self.max_results = 5  # 최대 결과 수
        
        # 매칭 히스토리 (최근 100개)
        self.match_history: List[Dict] = []
        self.max_history = 100
        
        # 매칭 통계
        self.stats = {
            'total_matches': 0,
            'successful_matches': 0,
            'exact_matches': 0,
            'partial_matches': 0,
            'synonym_matches': 0,
            'fuzzy_matches': 0,
            'failed_matches': 0
        }
        
        # 동의어 사전 (확장된 버전)
        self.synonyms = {
            # 기본 게임 액션
            '공격': ['어택', '때리기', '치기', '타격', '공격하기', 'attack'],
            '방어': ['가드', '막기', '디펜스', '방어하기', 'defend', 'guard'],
            '스킬': ['기술', '능력', '마법', '스킬사용', 'skill', 'magic'],
            '아이템': ['아템', '물건', '도구', '아이템사용', 'item', 'use'],
            '점프': ['뛰기', '뛰어오르기', '껑충', '점프하기', 'jump'],
            '달리기': ['뛰기', '러닝', '빠르게', '달리기시작', 'run', 'sprint'],
            '이동': ['움직이기', '가기', '무브', 'move', 'go'],
            '멈춤': ['정지', '스톱', '그만', 'stop', 'halt'],
            
            # 게임 UI 액션
            '인벤토리': ['가방', '아이템창', '인벤', '창고', 'inventory', 'bag'],
            '설정': ['옵션', '세팅', '환경설정', 'settings', 'options'],
            '저장': ['세이브', '보존', '게임저장', 'save'],
            '로드': ['불러오기', '로딩', '게임로딩', 'load'],
            '종료': ['나가기', '끝내기', '게임종료', 'quit', 'exit'],
            '일시정지': ['멈춤', '정지', '잠깐', 'pause'],
            '계속': ['재개', '다시', '계속하기', 'continue', 'resume'],
            
            # 숫자 및 순서
            '하나': ['1', '첫번째', 'one', 'first'],
            '둘': ['2', '두번째', 'two', 'second'],
            '셋': ['3', '세번째', 'three', 'third'],
            '넷': ['4', '네번째', 'four', 'fourth'],
            '다섯': ['5', '다섯번째', 'five', 'fifth']
        }
        
        self.logger.info("매크로 매칭 서비스가 초기화되었습니다.")
    
    def set_similarity_threshold(self, threshold: float) -> bool:
        """
        유사도 임계값 설정
        
        Args:
            threshold (float): 유사도 임계값 (0.0-1.0)
            
        Returns:
            bool: 설정 성공 여부
        """
        if 0.0 <= threshold <= 1.0:
            self.similarity_threshold = threshold
            self.logger.info(f"유사도 임계값이 {threshold}로 설정되었습니다.")
            return True
        else:
            self.logger.error(f"유효하지 않은 임계값입니다: {threshold}")
            return False
    
    def _calculate_similarity(self, text1: str, text2: str) -> float:
        """
        두 텍스트 간의 유사도 계산
        
        Args:
            text1 (str): 첫 번째 텍스트
            text2 (str): 두 번째 텍스트
            
        Returns:
            float: 유사도 점수 (0.0-1.0)
        """
        if not text1 or not text2:
            return 0.0
        
        # 기본 문자열 유사도
        basic_similarity = SequenceMatcher(None, text1.lower(), text2.lower()).ratio()
        
        # 단어 기반 유사도 (공백으로 분리)
        words1 = set(text1.lower().split())
        words2 = set(text2.lower().split())
        
        if words1 and words2:
            word_similarity = len(words1.intersection(words2)) / len(words1.union(words2))
        else:
            word_similarity = 0.0
        
        # 가중 평균 (문자열 70%, 단어 30%)
        return basic_similarity * 0.7 + word_similarity * 0.3
    
    def _check_synonyms(self, input_text: str, target_text: str) -> Tuple[bool, float]:
        """
        동의어 매칭 확인
        
        Args:
            input_text (str): 입력 텍스트
            target_text (str): 대상 텍스트
            
        Returns:
            Tuple[bool, float]: (매칭 여부, 유사도)
        """
        input_lower = input_text.lower().strip()
        target_lower = target_text.lower().strip()
        
        # 동의어 사전에서 확인
        for main_word, synonym_list in self.synonyms.items():
            # 1. input이 main_word이고 target이 동의어인 경우
            if input_lower == main_word and any(syn in target_lower for syn in synonym_list):
                return True, 0.95
            
            # 2. input이 동의어이고 target이 main_word인 경우
            if any(syn == input_lower for syn in synonym_list) and main_word in target_lower:
                return True, 0.95
            
            # 3. 둘 다 같은 main_word의 동의어인 경우
            if (any(syn == input_lower for syn in synonym_list) and 
                any(syn in target_lower for syn in synonym_list)):
                return True, 0.90
        
        return False, 0.0
    
    def _get_confidence_level(self, similarity: float) -> MatchConfidenceLevel:
        """
        유사도를 신뢰도 레벨로 변환
        
        Args:
            similarity (float): 유사도 점수
            
        Returns:
            MatchConfidenceLevel: 신뢰도 레벨
        """
        if similarity >= 0.9:
            return MatchConfidenceLevel.VERY_HIGH
        elif similarity >= 0.8:
            return MatchConfidenceLevel.HIGH
        elif similarity >= 0.7:
            return MatchConfidenceLevel.MEDIUM
        elif similarity >= 0.6:
            return MatchConfidenceLevel.LOW
        else:
            return MatchConfidenceLevel.VERY_LOW
    
    def _determine_match_type(self, input_text: str, target_text: str, similarity: float) -> str:
        """
        매칭 타입 결정
        
        Args:
            input_text (str): 입력 텍스트
            target_text (str): 대상 텍스트
            similarity (float): 유사도
            
        Returns:
            str: 매칭 타입
        """
        input_lower = input_text.lower().strip()
        target_lower = target_text.lower().strip()
        
        # 정확한 매칭
        if input_lower == target_lower:
            return 'exact'
        
        # 동의어 매칭
        is_synonym, _ = self._check_synonyms(input_text, target_text)
        if is_synonym:
            return 'synonym'
        
        # 부분 매칭
        if input_lower in target_lower or target_lower in input_lower:
            return 'partial'
        
        # 퍼지 매칭
        return 'fuzzy'
    
    def find_matching_macros(self, input_text: str, include_disabled: bool = False) -> List[MacroMatch]:
        """
        입력 텍스트와 매칭되는 매크로들 찾기
        
        Args:
            input_text (str): 입력 텍스트
            include_disabled (bool): 비활성화된 매크로 포함 여부
            
        Returns:
            List[MacroMatch]: 매칭된 매크로 목록
        """
        start_time = time.time()
        
        try:
            if not input_text or not input_text.strip():
                return []
            
            # 데이터베이스에서 모든 매크로 가져오기
            all_macros = macro_service.get_all_macros()
            
            if not all_macros:
                self.logger.warning("데이터베이스에 매크로가 없습니다.")
                return []
            
            matches = []
            input_clean = input_text.strip()
            
            for macro in all_macros:
                voice_command = macro.get('voice_command', '')
                
                if not voice_command:
                    continue
                
                # 기본 유사도 계산
                similarity = self._calculate_similarity(input_clean, voice_command)
                
                # 동의어 매칭 확인
                is_synonym, synonym_similarity = self._check_synonyms(input_clean, voice_command)
                if is_synonym:
                    similarity = max(similarity, synonym_similarity)
                
                # 임계값 확인
                if similarity < self.similarity_threshold:
                    continue
                
                # 매칭 타입 결정
                match_type = self._determine_match_type(input_clean, voice_command, similarity)
                
                # MacroMatch 객체 생성
                macro_match = MacroMatch(
                    macro_id=macro['id'],
                    macro_name=macro.get('name', ''),
                    voice_command=voice_command,
                    similarity=similarity,
                    confidence_level=self._get_confidence_level(similarity),
                    match_type=match_type,
                    action_type=macro.get('action_type', ''),
                    key_sequence=macro.get('key_sequence', '')
                )
                
                matches.append(macro_match)
            
            # 유사도 순으로 정렬
            matches.sort(key=lambda x: x.similarity, reverse=True)
            
            # 최대 결과 수로 제한
            matches = matches[:self.max_results]
            
            # 통계 업데이트
            self._update_stats(input_text, matches)
            
            # 히스토리 저장
            self._save_to_history(input_text, matches, time.time() - start_time)
            
            self.logger.info(f"매칭 완료: 입력='{input_text}', 결과={len(matches)}개, 소요시간={time.time()-start_time:.3f}초")
            
            return matches
            
        except Exception as e:
            self.logger.error(f"매크로 매칭 중 오류: {e}")
            return []
    
    def get_best_match(self, input_text: str) -> Optional[MacroMatch]:
        """
        가장 좋은 매칭 결과 하나만 반환
        
        Args:
            input_text (str): 입력 텍스트
            
        Returns:
            Optional[MacroMatch]: 최고 매칭 결과 또는 None
        """
        matches = self.find_matching_macros(input_text)
        return matches[0] if matches else None
    
    def _update_stats(self, input_text: str, matches: List[MacroMatch]):
        """통계 정보 업데이트"""
        self.stats['total_matches'] += 1
        
        if matches:
            self.stats['successful_matches'] += 1
            
            # 매칭 타입별 통계
            for match in matches:
                if match.match_type == 'exact':
                    self.stats['exact_matches'] += 1
                elif match.match_type == 'partial':
                    self.stats['partial_matches'] += 1
                elif match.match_type == 'synonym':
                    self.stats['synonym_matches'] += 1
                elif match.match_type == 'fuzzy':
                    self.stats['fuzzy_matches'] += 1
                break  # 첫 번째 매칭만 카운트
        else:
            self.stats['failed_matches'] += 1
    
    def _save_to_history(self, input_text: str, matches: List[MacroMatch], processing_time: float):
        """히스토리에 저장"""
        history_entry = {
            'timestamp': time.time(),
            'input_text': input_text,
            'matches_count': len(matches),
            'best_match': {
                'macro_name': matches[0].macro_name if matches else None,
                'similarity': matches[0].similarity if matches else 0.0,
                'match_type': matches[0].match_type if matches else None
            },
            'processing_time': processing_time
        }
        
        self.match_history.append(history_entry)
        
        # 히스토리 크기 제한
        if len(self.match_history) > self.max_history:
            self.match_history = self.match_history[-self.max_history:]
    
    def get_matching_stats(self) -> Dict[str, Any]:
        """
        매칭 통계 정보 반환
        
        Returns:
            Dict: 통계 정보
        """
        total = self.stats['total_matches']
        
        return {
            'total_matches': total,
            'successful_matches': self.stats['successful_matches'],
            'failed_matches': self.stats['failed_matches'],
            'success_rate': (self.stats['successful_matches'] / total * 100) if total > 0 else 0.0,
            'match_types': {
                'exact': self.stats['exact_matches'],
                'partial': self.stats['partial_matches'],
                'synonym': self.stats['synonym_matches'],
                'fuzzy': self.stats['fuzzy_matches']
            },
            'settings': {
                'similarity_threshold': self.similarity_threshold,
                'max_results': self.max_results,
                'synonyms_count': len(self.synonyms)
            },
            'history_size': len(self.match_history)
        }
    
    def get_recent_history(self, limit: int = 10) -> List[Dict]:
        """
        최근 매칭 히스토리 반환
        
        Args:
            limit (int): 반환할 히스토리 수
            
        Returns:
            List[Dict]: 히스토리 목록
        """
        return self.match_history[-limit:] if self.match_history else []
    
    def clear_history(self):
        """히스토리 초기화"""
        self.match_history.clear()
        self.logger.info("매칭 히스토리가 초기화되었습니다.")
    
    def add_synonym(self, main_word: str, synonyms: List[str]) -> bool:
        """
        동의어 추가
        
        Args:
            main_word (str): 메인 단어
            synonyms (List[str]): 동의어 목록
            
        Returns:
            bool: 추가 성공 여부
        """
        try:
            if main_word in self.synonyms:
                # 기존 동의어에 추가
                self.synonyms[main_word].extend(synonyms)
                # 중복 제거
                self.synonyms[main_word] = list(set(self.synonyms[main_word]))
            else:
                # 새로운 항목 생성
                self.synonyms[main_word] = synonyms
            
            self.logger.info(f"동의어 추가 완료: {main_word} <- {synonyms}")
            return True
            
        except Exception as e:
            self.logger.error(f"동의어 추가 실패: {e}")
            return False


# 전역 매크로 매칭 서비스 인스턴스
_matching_service_instance = None

def get_macro_matching_service() -> MacroMatchingService:
    """
    매크로 매칭 서비스 싱글톤 인스턴스 반환
    
    Returns:
        MacroMatchingService: 매크로 매칭 서비스 인스턴스
    """
    global _matching_service_instance
    if _matching_service_instance is None:
        _matching_service_instance = MacroMatchingService()
    return _matching_service_instance 