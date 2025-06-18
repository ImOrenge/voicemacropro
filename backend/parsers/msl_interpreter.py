"""
MSL (Macro Scripting Language) 인터프리터 (Interpreter)
AST를 받아서 실제 키보드/마우스 입력을 실행하는 실행 엔진입니다.

실행 환경:
- PyAutoGUI를 사용한 실제 입력 제어
- 멀티스레딩을 통한 병렬 실행 지원
- 안전성 검사 및 오류 처리
- 실행 로그 및 성능 측정
"""

import time
import threading
import pyautogui
from typing import Dict, List, Optional, Any, Callable
from dataclasses import dataclass
from concurrent.futures import ThreadPoolExecutor, Future
import logging

from backend.parsers.msl_ast import *


@dataclass
class ExecutionContext:
    """실행 컨텍스트"""
    variables: Dict[str, Any]
    execution_id: str
    start_time: float
    logger: logging.Logger
    safety_enabled: bool = True
    max_execution_time: float = 30.0  # 최대 실행 시간 (초)


@dataclass 
class ExecutionResult:
    """실행 결과"""
    success: bool
    execution_time: float
    error_message: Optional[str] = None
    executed_actions: int = 0
    performance_metrics: Dict[str, Any] = None


class ExecutionError(Exception):
    """실행 오류"""
    pass


class MSLInterpreter(MSLVisitor):
    """MSL 인터프리터"""
    
    def __init__(self):
        """MSL Interpreter 초기화"""
        # PyAutoGUI 설정
        pyautogui.FAILSAFE = True  # 마우스를 화면 모서리로 이동하면 중단
        pyautogui.PAUSE = 0.01     # 기본 지연 시간
        
        # 실행 환경
        self.context: Optional[ExecutionContext] = None
        self.is_running = False
        self.thread_pool = ThreadPoolExecutor(max_workers=10)
        
        # 통계
        self.execution_stats = {
            'total_executions': 0,
            'successful_executions': 0,
            'failed_executions': 0,
            'average_execution_time': 0.0
        }
        
        # 키 매핑 (MSL 키 이름 -> PyAutoGUI 키 이름)
        self.key_mapping = {
            # 기본 키
            'Space': 'space',
            'Enter': 'enter',
            'Tab': 'tab',
            'Escape': 'escape',
            'Backspace': 'backspace',
            'Delete': 'delete',
            
            # 방향키
            'Up': 'up',
            'Down': 'down',
            'Left': 'left',
            'Right': 'right',
            
            # 기능키
            'F1': 'f1', 'F2': 'f2', 'F3': 'f3', 'F4': 'f4',
            'F5': 'f5', 'F6': 'f6', 'F7': 'f7', 'F8': 'f8',
            'F9': 'f9', 'F10': 'f10', 'F11': 'f11', 'F12': 'f12',
            
            # 수정자 키
            'Shift': 'shift',
            'Ctrl': 'ctrl',
            'Alt': 'alt',
            'Win': 'win',
            
            # 특수 키
            'CapsLock': 'capslock',
            'NumLock': 'numlock',
            'ScrollLock': 'scrolllock',
            
            # 숫자 키패드
            'Num0': 'num0', 'Num1': 'num1', 'Num2': 'num2',
            'Num3': 'num3', 'Num4': 'num4', 'Num5': 'num5',
            'Num6': 'num6', 'Num7': 'num7', 'Num8': 'num8', 'Num9': 'num9',
        }
        
        # 로거 설정
        self.logger = logging.getLogger('MSLInterpreter')
        self.logger.setLevel(logging.INFO)
    
    def execute(self, ast: MSLNode, variables: Dict[str, Any] = None) -> ExecutionResult:
        """
        AST를 실행합니다.
        
        Args:
            ast (MSLNode): 실행할 AST
            variables (Dict[str, Any], optional): 변수 딕셔너리
            
        Returns:
            ExecutionResult: 실행 결과
        """
        if variables is None:
            variables = {}
        
        # 실행 컨텍스트 생성
        execution_id = f"exec_{int(time.time() * 1000)}"
        self.context = ExecutionContext(
            variables=variables,
            execution_id=execution_id,
            start_time=time.time(),
            logger=self.logger,
            safety_enabled=True
        )
        
        # 실행 상태 설정
        self.is_running = True
        
        try:
            self.logger.info(f"MSL 실행 시작: {execution_id}")
            
            # AST 실행
            result = ast.accept(self)
            
            # 실행 시간 계산
            execution_time = time.time() - self.context.start_time
            
            # 성공 결과 반환
            execution_result = ExecutionResult(
                success=True,
                execution_time=execution_time,
                executed_actions=getattr(self.context, 'action_count', 0),
                performance_metrics={
                    'start_time': self.context.start_time,
                    'end_time': time.time(),
                    'execution_id': execution_id
                }
            )
            
            # 통계 업데이트
            self.execution_stats['total_executions'] += 1
            self.execution_stats['successful_executions'] += 1
            self._update_average_execution_time(execution_time)
            
            self.logger.info(f"MSL 실행 완료: {execution_id}, 시간: {execution_time:.3f}s")
            
            return execution_result
            
        except Exception as e:
            # 실행 오류 처리
            execution_time = time.time() - self.context.start_time
            error_message = str(e)
            
            self.logger.error(f"MSL 실행 오류: {execution_id}, 오류: {error_message}")
            
            # 실패 결과 반환
            execution_result = ExecutionResult(
                success=False,
                execution_time=execution_time,
                error_message=error_message,
                executed_actions=getattr(self.context, 'action_count', 0)
            )
            
            # 통계 업데이트
            self.execution_stats['total_executions'] += 1
            self.execution_stats['failed_executions'] += 1
            self._update_average_execution_time(execution_time)
            
            return execution_result
            
        finally:
            self.is_running = False
            self.context = None
    
    def stop_execution(self):
        """실행 중단"""
        self.is_running = False
        self.logger.info("MSL 실행 중단 요청")
    
    # Visitor 패턴 구현
    
    def visit_key_node(self, node: KeyNode):
        """키 입력 실행"""
        if not self.is_running:
            return
        
        key_name = self._map_key_name(node.key_name)
        
        try:
            self.logger.debug(f"키 입력: {node.key_name} -> {key_name}")
            pyautogui.press(key_name)
            self._increment_action_count()
            
        except Exception as e:
            raise ExecutionError(f"키 입력 실패: {node.key_name}, 오류: {e}")
    
    def visit_number_node(self, node: NumberNode):
        """숫자 노드 (일반적으로 직접 실행되지 않음)"""
        return node.number
    
    def visit_variable_node(self, node: VariableNode):
        """변수 참조 실행"""
        if node.variable_name not in self.context.variables:
            raise ExecutionError(f"정의되지 않은 변수: ${node.variable_name}")
        
        variable_value = self.context.variables[node.variable_name]
        
        # 변수 값이 AST 노드인 경우 실행
        if isinstance(variable_value, MSLNode):
            return variable_value.accept(self)
        else:
            return variable_value
    
    def visit_mouse_coord_node(self, node: MouseCoordNode):
        """마우스 이동 실행"""
        if not self.is_running:
            return
        
        try:
            self.logger.debug(f"마우스 이동: ({node.x}, {node.y})")
            pyautogui.moveTo(node.x, node.y)
            self._increment_action_count()
            
        except Exception as e:
            raise ExecutionError(f"마우스 이동 실패: ({node.x}, {node.y}), 오류: {e}")
    
    def visit_wheel_node(self, node: WheelNode):
        """휠 제어 실행"""
        if not self.is_running:
            return
        
        try:
            scroll_amount = node.amount if node.direction == '+' else -node.amount
            self.logger.debug(f"휠 스크롤: {scroll_amount}")
            pyautogui.scroll(scroll_amount)
            self._increment_action_count()
            
        except Exception as e:
            raise ExecutionError(f"휠 제어 실패: {node.direction}{node.amount}, 오류: {e}")
    
    def visit_sequential_node(self, node: SequentialNode):
        """순차 실행"""
        if not self.is_running:
            return
        
        self.logger.debug(f"순차 실행 시작: {len(node.children)}개 액션")
        
        for child in node.children:
            if not self.is_running:
                break
            child.accept(self)
    
    def visit_simultaneous_node(self, node: SimultaneousNode):
        """동시 실행"""
        if not self.is_running:
            return
        
        self.logger.debug(f"동시 실행 시작: {len(node.children)}개 액션")
        
        # 모든 키를 동시에 누름
        keys_to_press = []
        
        # 키 노드들만 수집
        for child in node.children:
            if isinstance(child, KeyNode):
                key_name = self._map_key_name(child.key_name)
                keys_to_press.append(key_name)
            elif isinstance(child, GroupNode) and len(child.children) == 1:
                # 그룹 내의 키도 포함
                inner_child = child.children[0]
                if isinstance(inner_child, KeyNode):
                    key_name = self._map_key_name(inner_child.key_name)
                    keys_to_press.append(key_name)
        
        try:
            if keys_to_press:
                # 여러 키 동시 입력
                self.logger.debug(f"동시 키 입력: {', '.join(keys_to_press)}")
                pyautogui.hotkey(*keys_to_press)
                self._increment_action_count()
            
            # 키가 아닌 다른 액션들은 순차 실행
            for child in node.children:
                if not isinstance(child, KeyNode) and not self.is_running:
                    break
                if not isinstance(child, KeyNode):
                    child.accept(self)
                    
        except Exception as e:
            raise ExecutionError(f"동시 실행 실패: {e}")
    
    def visit_hold_chain_node(self, node: HoldChainNode):
        """홀드 연결 실행"""
        if not self.is_running:
            return
        
        self.logger.debug(f"홀드 연결 실행: {len(node.children)}개 액션")
        
        # 첫 번째 키를 누르고 유지
        if node.children and isinstance(node.children[0], KeyNode):
            first_key = self._map_key_name(node.children[0].key_name)
            
            try:
                self.logger.debug(f"홀드 시작: {first_key}")
                pyautogui.keyDown(first_key)
                
                # 나머지 키들을 순차적으로 실행
                for child in node.children[1:]:
                    if not self.is_running:
                        break
                    child.accept(self)
                    time.sleep(0.05)  # 짧은 지연
                
                # 첫 번째 키 해제
                self.logger.debug(f"홀드 종료: {first_key}")
                pyautogui.keyUp(first_key)
                self._increment_action_count()
                
            except Exception as e:
                # 오류 발생 시에도 키 해제
                try:
                    pyautogui.keyUp(first_key)
                except:
                    pass
                raise ExecutionError(f"홀드 연결 실행 실패: {e}")
        else:
            # 첫 번째가 키가 아닌 경우 순차 실행으로 대체
            for child in node.children:
                if not self.is_running:
                    break
                child.accept(self)
    
    def visit_parallel_node(self, node: ParallelNode):
        """병렬 실행 (멀티스레딩)"""
        if not self.is_running:
            return
        
        self.logger.debug(f"병렬 실행 시작: {len(node.children)}개 스레드")
        
        # 스레드풀을 사용한 병렬 실행
        futures = []
        
        for child in node.children:
            if not self.is_running:
                break
            
            # 각 자식을 별도 스레드에서 실행
            future = self.thread_pool.submit(self._execute_in_thread, child)
            futures.append(future)
        
        # 모든 스레드 완료 대기
        for future in futures:
            if not self.is_running:
                break
            try:
                future.result(timeout=10.0)  # 10초 타임아웃
            except Exception as e:
                self.logger.error(f"병렬 실행 오류: {e}")
    
    def visit_toggle_node(self, node: ToggleNode):
        """토글 실행"""
        if not self.is_running:
            return
        
        # 토글 대상 실행 (보통 키)
        if node.children:
            child = node.children[0]
            if isinstance(child, KeyNode):
                key_name = self._map_key_name(child.key_name)
                
                try:
                    self.logger.debug(f"토글: {key_name}")
                    pyautogui.press(key_name)
                    self._increment_action_count()
                    
                except Exception as e:
                    raise ExecutionError(f"토글 실행 실패: {key_name}, 오류: {e}")
            else:
                child.accept(self)
    
    def visit_repeat_node(self, node: RepeatNode):
        """반복 실행"""
        if not self.is_running:
            return
        
        self.logger.debug(f"반복 실행: {node.count}회")
        
        # 간격 시간 찾기
        interval_time = 0
        action_node = None
        
        for child in node.children:
            if isinstance(child, IntervalNode):
                interval_time = child.interval_time
            else:
                action_node = child
        
        if action_node is None:
            return
        
        # 반복 실행
        for i in range(node.count):
            if not self.is_running:
                break
            
            self.logger.debug(f"반복 {i+1}/{node.count}")
            action_node.accept(self)
            
            # 마지막 반복이 아니면 간격 대기
            if i < node.count - 1 and interval_time > 0:
                time.sleep(interval_time / 1000.0)  # ms -> seconds
        
        self._increment_action_count()
    
    def visit_continuous_node(self, node: ContinuousNode):
        """연속 입력 실행"""
        if not self.is_running:
            return
        
        if not node.children:
            return
        
        action_node = node.children[0]
        interval = node.interval / 1000.0  # ms -> seconds
        
        self.logger.debug(f"연속 입력: {interval*1000}ms 간격")
        
        # 연속 입력 (중단 조건: 특정 키 입력 또는 시간 제한)
        start_time = time.time()
        max_duration = 10.0  # 최대 10초
        
        while self.is_running and (time.time() - start_time) < max_duration:
            action_node.accept(self)
            time.sleep(interval)
            
            # ESC 키가 눌렸는지 확인 (안전장치)
            if pyautogui.position() == (0, 0):  # Failsafe 조건
                break
        
        self._increment_action_count()
    
    def visit_delay_node(self, node: DelayNode):
        """지연 실행"""
        if not self.is_running:
            return
        
        delay_seconds = node.delay_time / 1000.0  # ms -> seconds
        self.logger.debug(f"지연: {node.delay_time}ms")
        time.sleep(delay_seconds)
    
    def visit_hold_node(self, node: HoldNode):
        """홀드 실행"""
        if not self.is_running:
            return
        
        if not node.children:
            return
        
        action_node = node.children[0]
        hold_time = node.hold_time / 1000.0  # ms -> seconds
        
        if isinstance(action_node, KeyNode):
            key_name = self._map_key_name(action_node.key_name)
            
            try:
                self.logger.debug(f"홀드: {key_name}, {node.hold_time}ms")
                pyautogui.keyDown(key_name)
                time.sleep(hold_time)
                pyautogui.keyUp(key_name)
                self._increment_action_count()
                
            except Exception as e:
                # 오류 발생 시에도 키 해제
                try:
                    pyautogui.keyUp(key_name)
                except:
                    pass
                raise ExecutionError(f"홀드 실행 실패: {key_name}, 오류: {e}")
    
    def visit_interval_node(self, node: IntervalNode):
        """간격 노드 (일반적으로 다른 노드와 함께 사용됨)"""
        pass
    
    def visit_fade_node(self, node: FadeNode):
        """페이드 실행 (점진적 전환)"""
        if not self.is_running:
            return
        
        # 페이드는 복잡한 구현이 필요하므로 일단 일반 실행으로 대체
        self.logger.debug(f"페이드 (단순 실행): {node.fade_time}ms")
        for child in node.children:
            if not self.is_running:
                break
            child.accept(self)
    
    def visit_group_node(self, node: GroupNode):
        """그룹 실행"""
        if not self.is_running:
            return
        
        self.logger.debug("그룹 실행")
        for child in node.children:
            if not self.is_running:
                break
            child.accept(self)
    
    # 헬퍼 메서드들
    
    def _map_key_name(self, msl_key: str) -> str:
        """MSL 키 이름을 PyAutoGUI 키 이름으로 변환"""
        if msl_key in self.key_mapping:
            return self.key_mapping[msl_key]
        
        # 단일 문자는 그대로 사용
        if len(msl_key) == 1:
            return msl_key.lower()
        
        # 기본적으로 소문자로 변환
        return msl_key.lower()
    
    def _increment_action_count(self):
        """액션 카운트 증가"""
        if self.context:
            if not hasattr(self.context, 'action_count'):
                self.context.action_count = 0
            self.context.action_count += 1
    
    def _execute_in_thread(self, node: MSLNode):
        """스레드에서 노드 실행"""
        try:
            return node.accept(self)
        except Exception as e:
            self.logger.error(f"스레드 실행 오류: {e}")
            raise
    
    def _update_average_execution_time(self, execution_time: float):
        """평균 실행 시간 업데이트"""
        total = self.execution_stats['total_executions']
        current_avg = self.execution_stats['average_execution_time']
        
        # 누적 평균 계산
        new_avg = ((current_avg * (total - 1)) + execution_time) / total
        self.execution_stats['average_execution_time'] = new_avg
    
    def get_statistics(self) -> Dict[str, Any]:
        """실행 통계 반환"""
        return self.execution_stats.copy()


def test_interpreter():
    """MSL Interpreter 테스트"""
    print("=== MSL Interpreter 테스트 ===")
    
    # 간단한 AST 생성 및 테스트
    from backend.parsers.msl_parser import MSLParser
    
    parser = MSLParser()
    interpreter = MSLInterpreter()
    
    # 간단한 테스트 스크립트들
    test_scripts = [
        "W",      # 단순 키 입력
        "W,A",    # 순차 입력
        "~CapsLock",  # 토글
    ]
    
    print("주의: 실제 키보드 입력이 실행됩니다!")
    input("계속하려면 Enter를 누르세요... (Ctrl+C로 중단)")
    
    for script in test_scripts:
        print(f"\n스크립트: {script}")
        try:
            ast = parser.parse(script)
            result = interpreter.execute(ast)
            
            if result.success:
                print(f"  실행 성공: {result.execution_time:.3f}s, {result.executed_actions}개 액션")
            else:
                print(f"  실행 실패: {result.error_message}")
                
        except Exception as e:
            print(f"  오류: {e}")
        
        time.sleep(1)  # 테스트 간 간격
    
    # 통계 출력
    stats = interpreter.get_statistics()
    print(f"\n실행 통계: {stats}")


if __name__ == "__main__":
    test_interpreter() 