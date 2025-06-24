"""
Phase 4: 매크로 매칭 시스템 통합 테스트
VoiceMacro Pro의 TDD 기반 개발을 위한 테스트 코드
"""

import os
import sys
import json
import pytest
import asyncio
import time
import sqlite3
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from typing import Dict, Any, List, Optional, Tuple
from dataclasses import dataclass
from difflib import SequenceMatcher

# 프로젝트 루트를 Python 경로에 추가
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))


@dataclass
class MacroDefinition:
    """매크로 정의 데이터 클래스"""
    id: int
    name: str
    voice_command: str
    action_type: str  # combo, rapid, hold, toggle, repeat
    key_sequence: str
    settings: Dict[str, Any]
    usage_count: int = 0
    success_rate: float = 100.0


@dataclass
class VoiceMatchResult:
    """음성 매칭 결과 데이터 클래스"""
    macro_id: int
    confidence: float
    matched_text: str
    original_text: str
    similarity: float
    execution_time: Optional[float] = None


class MockMacroDatabase:
    """테스트용 모의 매크로 데이터베이스"""
    
    def __init__(self):
        """모의 데이터베이스 초기화"""
        self.macros: Dict[int, MacroDefinition] = {}
        self.next_id = 1
        self._init_test_data()
    
    def _init_test_data(self):
        """테스트용 초기 데이터 설정"""
        test_macros = [
            MacroDefinition(1, "공격 매크로", "공격해", "combo", "Q,W,E", {"delay": 100}, 50, 95.0),
            MacroDefinition(2, "스킬 콤보", "스킬 사용", "combo", "R,T,Y", {"delay": 150}, 30, 92.0),
            MacroDefinition(3, "포션 사용", "포션 먹어", "key", "H", {"hold_time": 0}, 80, 98.0),
            MacroDefinition(4, "점프 공격", "점프해서 공격", "combo", "Space,Q", {"delay": 200}, 25, 88.0),
            MacroDefinition(5, "연사 공격", "빠르게 공격", "rapid", "LButton", {"rate": 10}, 40, 93.0),
            MacroDefinition(6, "방어 자세", "방어해", "hold", "S", {"hold_time": 2000}, 35, 90.0),
            MacroDefinition(7, "달리기", "달려", "toggle", "LShift", {"toggle": True}, 60, 96.0),
            MacroDefinition(8, "아이템 줍기", "아이템 주워", "key", "F", {"hold_time": 0}, 45, 94.0),
            MacroDefinition(9, "궁극기", "궁극기 써", "combo", "Alt+R", {"delay": 300}, 15, 85.0),
            MacroDefinition(10, "회복 물약", "회복해", "key", "G", {"hold_time": 0}, 70, 97.0),
        ]
        
        for macro in test_macros:
            self.macros[macro.id] = macro
            self.next_id = max(self.next_id, macro.id + 1)
    
    def get_all_macros(self) -> List[MacroDefinition]:
        """모든 매크로 조회"""
        return list(self.macros.values())
    
    def get_macro_by_id(self, macro_id: int) -> Optional[MacroDefinition]:
        """ID로 매크로 조회"""
        return self.macros.get(macro_id)
    
    def add_macro(self, name: str, voice_command: str, action_type: str, 
                  key_sequence: str, settings: Dict[str, Any]) -> int:
        """매크로 추가"""
        macro = MacroDefinition(
            id=self.next_id,
            name=name,
            voice_command=voice_command,
            action_type=action_type,
            key_sequence=key_sequence,
            settings=settings
        )
        self.macros[self.next_id] = macro
        self.next_id += 1
        return macro.id
    
    def update_usage_stats(self, macro_id: int, success: bool):
        """매크로 사용 통계 업데이트"""
        if macro_id in self.macros:
            macro = self.macros[macro_id]
            macro.usage_count += 1
            
            # 성공률 업데이트 (간단한 이동 평균)
            if success:
                macro.success_rate = (macro.success_rate * 0.9) + (100 * 0.1)
            else:
                macro.success_rate = (macro.success_rate * 0.9) + (0 * 0.1)


class MockMacroMatchingService:
    """테스트용 모의 매크로 매칭 서비스"""
    
    def __init__(self, database: MockMacroDatabase):
        """매칭 서비스 초기화"""
        self.database = database
        self.min_confidence = 0.7
        self.min_similarity = 0.6
        self.fuzzy_matching_enabled = True
        self.context_learning_enabled = True
        self.recent_commands = []  # 최근 명령어 이력
        
    def calculate_similarity(self, text1: str, text2: str) -> float:
        """두 텍스트 간 유사도 계산"""
        # 단순 문자열 유사도
        matcher = SequenceMatcher(None, text1.lower(), text2.lower())
        return matcher.ratio()
    
    def normalize_text(self, text: str) -> str:
        """텍스트 정규화"""
        # 공백 제거 및 소문자 변환
        return text.replace(" ", "").lower()
    
    def calculate_confidence(self, similarity: float, usage_stats: MacroDefinition) -> float:
        """매칭 신뢰도 계산"""
        base_confidence = similarity
        
        # 사용 빈도에 따른 보너스 (자주 사용되는 매크로에 가산점)
        usage_bonus = min(0.1, usage_stats.usage_count / 1000)
        
        # 성공률에 따른 보너스
        success_bonus = (usage_stats.success_rate / 100) * 0.05
        
        # 최근 사용 이력에 따른 보너스
        recent_bonus = 0.0
        if usage_stats.voice_command in self.recent_commands[-5:]:  # 최근 5개 명령어
            recent_bonus = 0.05
        
        total_confidence = base_confidence + usage_bonus + success_bonus + recent_bonus
        return min(1.0, total_confidence)
    
    def find_best_matches(self, voice_text: str, top_k: int = 3) -> List[VoiceMatchResult]:
        """최적 매크로 매칭 찾기"""
        matches = []
        normalized_input = self.normalize_text(voice_text)
        
        for macro in self.database.get_all_macros():
            normalized_command = self.normalize_text(macro.voice_command)
            
            # 정확한 매칭 확인
            if normalized_input == normalized_command:
                similarity = 1.0
            else:
                # 퍼지 매칭
                similarity = self.calculate_similarity(normalized_input, normalized_command)
                
                # 부분 매칭 확인
                if normalized_command in normalized_input or normalized_input in normalized_command:
                    similarity = max(similarity, 0.8)
            
            if similarity >= self.min_similarity:
                confidence = self.calculate_confidence(similarity, macro)
                
                if confidence >= self.min_confidence:
                    match_result = VoiceMatchResult(
                        macro_id=macro.id,
                        confidence=confidence,
                        matched_text=macro.voice_command,
                        original_text=voice_text,
                        similarity=similarity
                    )
                    matches.append(match_result)
        
        # 신뢰도 순으로 정렬
        matches.sort(key=lambda x: x.confidence, reverse=True)
        return matches[:top_k]
    
    async def process_voice_command(self, voice_text: str) -> Optional[VoiceMatchResult]:
        """음성 명령어 처리"""
        matches = self.find_best_matches(voice_text)
        
        if matches:
            best_match = matches[0]
            
            # 최근 명령어 이력에 추가
            self.recent_commands.append(voice_text)
            if len(self.recent_commands) > 10:
                self.recent_commands.pop(0)
            
            return best_match
        
        return None


class MockMacroExecutionService:
    """테스트용 모의 매크로 실행 서비스"""
    
    def __init__(self, database: MockMacroDatabase):
        """실행 서비스 초기화"""
        self.database = database
        self.executed_macros = []  # 실행된 매크로 이력
        self.execution_times = {}  # 매크로별 실행 시간
        
    async def execute_macro(self, macro_id: int) -> Dict[str, Any]:
        """매크로 실행"""
        macro = self.database.get_macro_by_id(macro_id)
        if not macro:
            raise ValueError(f"매크로 ID {macro_id}를 찾을 수 없습니다")
        
        start_time = time.perf_counter()
        
        # 실행 시뮬레이션
        execution_result = {
            "macro_id": macro_id,
            "name": macro.name,
            "action_type": macro.action_type,
            "key_sequence": macro.key_sequence,
            "success": True,
            "execution_time": 0.0,
            "timestamp": time.time()
        }
        
        # 액션 타입별 시뮬레이션
        if macro.action_type == "combo":
            await self._execute_combo(macro)
            execution_result["executed_keys"] = macro.key_sequence.split(",")
        elif macro.action_type == "rapid":
            await self._execute_rapid(macro)
            execution_result["rapid_count"] = macro.settings.get("rate", 1)
        elif macro.action_type == "hold":
            await self._execute_hold(macro)
            execution_result["hold_duration"] = macro.settings.get("hold_time", 1000)
        elif macro.action_type == "key":
            await self._execute_single_key(macro)
            execution_result["key"] = macro.key_sequence
        
        end_time = time.perf_counter()
        execution_time = (end_time - start_time) * 1000  # ms
        execution_result["execution_time"] = execution_time
        
        # 실행 이력 추가
        self.executed_macros.append(execution_result)
        self.execution_times[macro_id] = execution_time
        
        # 사용 통계 업데이트
        self.database.update_usage_stats(macro_id, True)
        
        return execution_result
    
    async def _execute_combo(self, macro: MacroDefinition):
        """콤보 매크로 실행 시뮬레이션"""
        keys = macro.key_sequence.split(",")
        delay = macro.settings.get("delay", 100) / 1000  # ms to seconds
        
        for key in keys:
            await asyncio.sleep(delay)  # 키 간 딜레이 시뮬레이션
    
    async def _execute_rapid(self, macro: MacroDefinition):
        """연사 매크로 실행 시뮬레이션"""
        rate = macro.settings.get("rate", 10)
        duration = macro.settings.get("duration", 1000) / 1000  # ms to seconds
        interval = 1.0 / rate
        
        end_time = time.time() + duration
        while time.time() < end_time:
            await asyncio.sleep(interval)
    
    async def _execute_hold(self, macro: MacroDefinition):
        """홀드 매크로 실행 시뮬레이션"""
        hold_time = macro.settings.get("hold_time", 1000) / 1000  # ms to seconds
        await asyncio.sleep(hold_time)
    
    async def _execute_single_key(self, macro: MacroDefinition):
        """단일 키 매크로 실행 시뮬레이션"""
        await asyncio.sleep(0.01)  # 최소 실행 시간


class TestMacroDatabase:
    """매크로 데이터베이스 테스트"""
    
    @pytest.fixture
    def mock_db(self):
        """테스트용 모의 데이터베이스"""
        return MockMacroDatabase()
    
    def test_database_initialization(self, mock_db):
        """✅ Test 1: 데이터베이스 초기화"""
        macros = mock_db.get_all_macros()
        assert len(macros) == 10
        assert mock_db.next_id == 11
    
    def test_macro_retrieval(self, mock_db):
        """✅ Test 2: 매크로 조회"""
        macro = mock_db.get_macro_by_id(1)
        assert macro is not None
        assert macro.name == "공격 매크로"
        assert macro.voice_command == "공격해"
        assert macro.action_type == "combo"
        
        # 존재하지 않는 매크로
        nonexistent = mock_db.get_macro_by_id(999)
        assert nonexistent is None
    
    def test_macro_addition(self, mock_db):
        """✅ Test 3: 매크로 추가"""
        initial_count = len(mock_db.get_all_macros())
        
        new_id = mock_db.add_macro(
            name="테스트 매크로",
            voice_command="테스트해",
            action_type="key",
            key_sequence="T",
            settings={"delay": 0}
        )
        
        assert new_id == 11
        assert len(mock_db.get_all_macros()) == initial_count + 1
        
        new_macro = mock_db.get_macro_by_id(new_id)
        assert new_macro.name == "테스트 매크로"
        assert new_macro.voice_command == "테스트해"
    
    def test_usage_stats_update(self, mock_db):
        """✅ Test 4: 사용 통계 업데이트"""
        macro_id = 1
        macro = mock_db.get_macro_by_id(macro_id)
        initial_usage = macro.usage_count
        initial_success_rate = macro.success_rate
        
        # 성공적인 실행
        mock_db.update_usage_stats(macro_id, True)
        
        updated_macro = mock_db.get_macro_by_id(macro_id)
        assert updated_macro.usage_count == initial_usage + 1
        assert updated_macro.success_rate >= initial_success_rate


class TestMacroMatching:
    """매크로 매칭 테스트"""
    
    @pytest.fixture
    def matching_service(self):
        """테스트용 매칭 서비스"""
        db = MockMacroDatabase()
        return MockMacroMatchingService(db)
    
    def test_exact_matching(self, matching_service):
        """✅ Test 5: 정확한 매칭"""
        matches = matching_service.find_best_matches("공격해")
        
        assert len(matches) > 0
        best_match = matches[0]
        assert best_match.macro_id == 1
        assert best_match.similarity == 1.0
        assert best_match.confidence > 0.9
    
    def test_fuzzy_matching(self, matching_service):
        """✅ Test 6: 퍼지 매칭"""
        # 약간 다른 표현
        matches = matching_service.find_best_matches("공격 해")
        assert len(matches) > 0
        
        # 유사한 표현
        matches = matching_service.find_best_matches("공격하기")
        assert len(matches) > 0
    
    def test_partial_matching(self, matching_service):
        """✅ Test 7: 부분 매칭"""
        matches = matching_service.find_best_matches("빠르게 공격해서")
        
        # "빠르게 공격"과 "공격해" 모두 매칭될 수 있음
        assert len(matches) >= 1
        
        # 가장 좋은 매칭 확인
        best_match = matches[0]
        assert best_match.confidence > 0.7
    
    def test_similarity_calculation(self, matching_service):
        """✅ Test 8: 유사도 계산"""
        # 동일한 텍스트
        similarity = matching_service.calculate_similarity("공격해", "공격해")
        assert similarity == 1.0
        
        # 비슷한 텍스트
        similarity = matching_service.calculate_similarity("공격해", "공격하기")
        assert 0.5 <= similarity <= 0.9
        
        # 완전히 다른 텍스트
        similarity = matching_service.calculate_similarity("공격해", "회복해")
        assert similarity < 0.5
    
    def test_confidence_calculation(self, matching_service):
        """✅ Test 9: 신뢰도 계산"""
        # 높은 사용 빈도 매크로
        high_usage_macro = matching_service.database.get_macro_by_id(1)
        high_usage_macro.usage_count = 100
        high_usage_macro.success_rate = 98.0
        
        confidence = matching_service.calculate_confidence(0.8, high_usage_macro)
        assert confidence > 0.8
        
        # 낮은 사용 빈도 매크로
        low_usage_macro = matching_service.database.get_macro_by_id(2)
        low_usage_macro.usage_count = 5
        low_usage_macro.success_rate = 70.0
        
        confidence = matching_service.calculate_confidence(0.8, low_usage_macro)
        assert confidence <= 0.85
    
    @pytest.mark.asyncio
    async def test_voice_command_processing(self, matching_service):
        """✅ Test 10: 음성 명령어 처리"""
        result = await matching_service.process_voice_command("포션 먹어")
        
        assert result is not None
        assert result.macro_id == 3
        assert result.matched_text == "포션 먹어"
        assert result.confidence > 0.9
        
        # 매칭되지 않는 명령어
        no_match = await matching_service.process_voice_command("완전히 다른 명령어")
        assert no_match is None


class TestMacroExecution:
    """매크로 실행 테스트"""
    
    @pytest.fixture
    def execution_service(self):
        """테스트용 실행 서비스"""
        db = MockMacroDatabase()
        return MockMacroExecutionService(db)
    
    @pytest.mark.asyncio
    async def test_single_key_execution(self, execution_service):
        """✅ Test 11: 단일 키 매크로 실행"""
        result = await execution_service.execute_macro(3)  # 포션 사용
        
        assert result["success"] is True
        assert result["macro_id"] == 3
        assert result["action_type"] == "key"
        assert result["key_sequence"] == "H"
        assert "execution_time" in result
    
    @pytest.mark.asyncio
    async def test_combo_execution(self, execution_service):
        """✅ Test 12: 콤보 매크로 실행"""
        result = await execution_service.execute_macro(1)  # 공격 매크로
        
        assert result["success"] is True
        assert result["action_type"] == "combo"
        assert result["executed_keys"] == ["Q", "W", "E"]
        assert result["execution_time"] > 0
    
    @pytest.mark.asyncio
    async def test_rapid_execution(self, execution_service):
        """✅ Test 13: 연사 매크로 실행"""
        result = await execution_service.execute_macro(5)  # 연사 공격
        
        assert result["success"] is True
        assert result["action_type"] == "rapid"
        assert "rapid_count" in result
        assert result["execution_time"] > 0
    
    @pytest.mark.asyncio
    async def test_hold_execution(self, execution_service):
        """✅ Test 14: 홀드 매크로 실행"""
        result = await execution_service.execute_macro(6)  # 방어 자세
        
        assert result["success"] is True
        assert result["action_type"] == "hold"
        assert "hold_duration" in result
        assert result["execution_time"] > 0
    
    @pytest.mark.asyncio
    async def test_execution_performance(self, execution_service):
        """⚡ Test 15: 실행 성능 테스트"""
        start_time = time.perf_counter()
        
        # 여러 매크로 연속 실행
        for macro_id in [1, 2, 3, 4, 5]:
            await execution_service.execute_macro(macro_id)
        
        end_time = time.perf_counter()
        total_time = (end_time - start_time) * 1000  # ms
        
        # 5개 매크로 실행이 1초 내에 완료되어야 함
        assert total_time < 1000, f"5개 매크로 실행 시간 {total_time:.2f}ms > 1000ms"
        
        # 실행 이력 확인
        assert len(execution_service.executed_macros) == 5
    
    @pytest.mark.asyncio
    async def test_invalid_macro_execution(self, execution_service):
        """❌ Test 16: 잘못된 매크로 실행"""
        with pytest.raises(ValueError) as exc_info:
            await execution_service.execute_macro(999)  # 존재하지 않는 매크로
        
        assert "매크로 ID 999를 찾을 수 없습니다" in str(exc_info.value)


class TestIntegratedPipeline:
    """통합 파이프라인 테스트"""
    
    @pytest.fixture
    def integrated_system(self):
        """통합 시스템 픽스처"""
        db = MockMacroDatabase()
        matching_service = MockMacroMatchingService(db)
        execution_service = MockMacroExecutionService(db)
        
        return {
            "database": db,
            "matching": matching_service,
            "execution": execution_service
        }
    
    @pytest.mark.asyncio
    async def test_voice_to_execution_pipeline(self, integrated_system):
        """🔄 Test 17: 음성 → 실행 파이프라인"""
        matching = integrated_system["matching"]
        execution = integrated_system["execution"]
        
        # 음성 명령어 처리
        voice_text = "공격해"
        match_result = await matching.process_voice_command(voice_text)
        
        assert match_result is not None
        assert match_result.confidence > 0.9
        
        # 매크로 실행
        execution_result = await execution.execute_macro(match_result.macro_id)
        
        assert execution_result["success"] is True
        assert execution_result["macro_id"] == match_result.macro_id
        
        # 통합 결과 검증
        assert match_result.original_text == voice_text
        assert execution_result["name"] == "공격 매크로"
    
    @pytest.mark.asyncio
    async def test_multiple_commands_pipeline(self, integrated_system):
        """🔄 Test 18: 다중 명령어 파이프라인"""
        matching = integrated_system["matching"]
        execution = integrated_system["execution"]
        
        commands = ["공격해", "포션 먹어", "스킬 사용", "방어해", "달려"]
        results = []
        
        for command in commands:
            match_result = await matching.process_voice_command(command)
            if match_result:
                execution_result = await execution.execute_macro(match_result.macro_id)
                results.append({
                    "command": command,
                    "match": match_result,
                    "execution": execution_result
                })
        
        # 모든 명령어가 성공적으로 처리되었는지 확인
        assert len(results) == len(commands)
        
        for result in results:
            assert result["match"].confidence > 0.7
            assert result["execution"]["success"] is True
    
    @pytest.mark.asyncio
    async def test_context_learning(self, integrated_system):
        """🧠 Test 19: 컨텍스트 학습"""
        matching = integrated_system["matching"]
        
        # 반복적으로 같은 명령어 사용
        for _ in range(5):
            await matching.process_voice_command("공격해")
        
        # 최근 명령어 이력 확인
        assert "공격해" in matching.recent_commands
        
        # 컨텍스트를 고려한 매칭 (최근 사용 보너스)
        matches = matching.find_best_matches("공격")
        best_match = matches[0] if matches else None
        
        assert best_match is not None
        assert best_match.confidence > 0.8  # 컨텍스트 보너스로 신뢰도 증가
    
    @pytest.mark.asyncio
    async def test_performance_optimization(self, integrated_system):
        """⚡ Test 20: 성능 최적화"""
        matching = integrated_system["matching"]
        execution = integrated_system["execution"]
        
        # 대량 명령어 처리 성능 테스트
        start_time = time.perf_counter()
        
        tasks = []
        for i in range(50):
            command = "공격해" if i % 2 == 0 else "포션 먹어"
            task = self._process_single_command(matching, execution, command)
            tasks.append(task)
        
        results = await asyncio.gather(*tasks)
        
        end_time = time.perf_counter()
        total_time = (end_time - start_time) * 1000  # ms
        
        # 50개 명령어가 5초 내에 처리되어야 함
        assert total_time < 5000, f"50개 명령어 처리 시간 {total_time:.2f}ms > 5000ms"
        
        # 평균 처리 시간
        avg_time = total_time / 50
        assert avg_time < 100, f"평균 명령어 처리 시간 {avg_time:.2f}ms > 100ms"
        
        # 모든 명령어가 성공적으로 처리되었는지 확인
        successful_results = [r for r in results if r is not None]
        assert len(successful_results) == 50
    
    async def _process_single_command(self, matching, execution, command):
        """단일 명령어 처리 헬퍼 함수"""
        match_result = await matching.process_voice_command(command)
        if match_result:
            execution_result = await execution.execute_macro(match_result.macro_id)
            return {
                "match": match_result,
                "execution": execution_result
            }
        return None


class TestGameSpecificOptimization:
    """게임별 최적화 테스트"""
    
    @pytest.fixture
    def game_optimized_system(self):
        """게임 최적화 시스템"""
        db = MockMacroDatabase()
        
        # 게임별 특화 매크로 추가
        db.add_macro("LOL 플래시", "플래시", "key", "F", {"game": "lol"})
        db.add_macro("LOL 점화", "점화", "key", "D", {"game": "lol"})
        db.add_macro("오버워치 궁극기", "궁", "key", "Q", {"game": "overwatch"})
        
        matching = MockMacroMatchingService(db)
        return {"database": db, "matching": matching}
    
    def test_game_context_matching(self, game_optimized_system):
        """🎮 Test 21: 게임 컨텍스트 매칭"""
        matching = game_optimized_system["matching"]
        
        # LOL 관련 명령어
        matches = matching.find_best_matches("플래시")
        assert len(matches) > 0
        
        best_match = matches[0]
        matched_macro = game_optimized_system["database"].get_macro_by_id(best_match.macro_id)
        assert matched_macro.settings.get("game") == "lol"
    
    def test_command_abbreviation(self, game_optimized_system):
        """📝 Test 22: 명령어 줄임말 처리"""
        matching = game_optimized_system["matching"]
        
        # 줄임말로 매칭
        matches = matching.find_best_matches("궁")
        assert len(matches) > 0
        
        # 전체 명령어로 매칭
        matches_full = matching.find_best_matches("궁극기")
        assert len(matches_full) > 0
    
    def test_multilingual_support(self, game_optimized_system):
        """🌍 Test 23: 다국어 지원"""
        db = game_optimized_system["database"]
        matching = game_optimized_system["matching"]
        
        # 영어 명령어 추가
        db.add_macro("English Attack", "attack", "key", "A", {"language": "en"})
        db.add_macro("English Heal", "heal", "key", "H", {"language": "en"})
        
        # 영어 명령어 매칭
        matches = matching.find_best_matches("attack")
        assert len(matches) > 0
        
        # 혼용 매칭 (한국어 + 영어)
        matches_mixed = matching.find_best_matches("공격 attack")
        assert len(matches_mixed) > 0


# 테스트 실행 함수
def run_phase4_tests():
    """Phase 4 테스트 실행"""
    import subprocess
    
    print("🧪 Phase 4: 매크로 매칭 시스템 통합 테스트 실행 중...")
    
    # pytest 실행
    result = subprocess.run([
        'py', '-m', 'pytest', 
        __file__,
        '-v',  # 상세 출력
        '--tb=short',  # 짧은 트레이스백
        '--disable-warnings'  # 경고 메시지 숨김
    ], capture_output=True, text=True)
    
    print("📊 테스트 결과:")
    print(result.stdout)
    
    if result.stderr:
        print("⚠️ 경고/오류:")
        print(result.stderr)
    
    return result.returncode == 0


if __name__ == "__main__":
    # 직접 실행 시 테스트 수행
    success = run_phase4_tests()
    
    if success:
        print("✅ Phase 4 테스트 모두 통과!")
        print("🚀 Phase 5 구현을 시작할 수 있습니다.")
    else:
        print("❌ 일부 테스트 실패. 구현을 수정해주세요.") 