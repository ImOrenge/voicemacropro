"""
MSL (Macro Scripting Language) AST (Abstract Syntax Tree) 정의
파서가 생성하는 구문 트리의 노드들을 정의합니다.

AST 노드 계층:
- MSLNode (최상위 추상 클래스)
  - ExpressionNode (표현식 노드)
    - KeyNode (키 입력)
    - NumberNode (숫자)
    - VariableNode (변수)
    - MouseCoordNode (마우스 좌표)
    - WheelNode (휠 제어)
  - OperatorNode (연산자 노드)
    - SequentialNode (순차 실행)
    - SimultaneousNode (동시 실행)
    - HoldChainNode (홀드 연결)
    - ParallelNode (병렬 실행)
    - ToggleNode (토글)
    - RepeatNode (반복)
    - ContinuousNode (연속 입력)
  - TimingNode (타이밍 제어 노드)
    - DelayNode (지연)
    - HoldNode (홀드)
    - IntervalNode (간격)
    - FadeNode (페이드)
  - GroupNode (그룹화 노드)
"""

from abc import ABC, abstractmethod
from typing import List, Optional, Any, Dict
from dataclasses import dataclass
from enum import Enum


class NodeType(Enum):
    """AST 노드 타입"""
    # 표현식 노드
    KEY = "KEY"
    NUMBER = "NUMBER"
    VARIABLE = "VARIABLE"
    MOUSE_COORD = "MOUSE_COORD"
    WHEEL = "WHEEL"
    
    # 연산자 노드
    SEQUENTIAL = "SEQUENTIAL"
    SIMULTANEOUS = "SIMULTANEOUS"
    HOLD_CHAIN = "HOLD_CHAIN"
    PARALLEL = "PARALLEL"
    TOGGLE = "TOGGLE"
    REPEAT = "REPEAT"
    CONTINUOUS = "CONTINUOUS"
    
    # 타이밍 노드
    DELAY = "DELAY"
    HOLD = "HOLD"
    INTERVAL = "INTERVAL"
    FADE = "FADE"
    
    # 그룹 노드
    GROUP = "GROUP"


@dataclass
class Position:
    """AST 노드의 소스 코드 위치 정보"""
    line: int
    column: int
    position: int


class MSLNode(ABC):
    """MSL AST 노드의 추상 기본 클래스"""
    
    def __init__(self, node_type: NodeType, position: Optional[Position] = None):
        """
        MSL 노드 초기화
        
        Args:
            node_type (NodeType): 노드 타입
            position (Position, optional): 소스 코드 위치
        """
        self.node_type = node_type
        self.position = position
        self.parent: Optional['MSLNode'] = None
        self.children: List['MSLNode'] = []
    
    def add_child(self, child: 'MSLNode'):
        """자식 노드 추가"""
        child.parent = self
        self.children.append(child)
    
    def remove_child(self, child: 'MSLNode'):
        """자식 노드 제거"""
        if child in self.children:
            child.parent = None
            self.children.remove(child)
    
    @abstractmethod
    def accept(self, visitor):
        """Visitor 패턴을 위한 메서드"""
        pass
    
    def __str__(self) -> str:
        """노드의 문자열 표현"""
        return f"{self.node_type.value}"
    
    def tree_string(self, indent: int = 0) -> str:
        """트리 구조를 문자열로 표현"""
        result = "  " * indent + str(self) + "\n"
        for child in self.children:
            result += child.tree_string(indent + 1)
        return result


class ExpressionNode(MSLNode):
    """표현식 노드 (값을 가지는 노드)"""
    
    def __init__(self, node_type: NodeType, value: Any, position: Optional[Position] = None):
        """
        표현식 노드 초기화
        
        Args:
            node_type (NodeType): 노드 타입
            value (Any): 노드 값
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(node_type, position)
        self.value = value
    
    def __str__(self) -> str:
        return f"{self.node_type.value}({self.value})"


class KeyNode(ExpressionNode):
    """키 입력 노드 (W, A, Space, Ctrl 등)"""
    
    def __init__(self, key_name: str, position: Optional[Position] = None):
        """
        키 노드 초기화
        
        Args:
            key_name (str): 키 이름
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.KEY, key_name, position)
        self.key_name = key_name
    
    def accept(self, visitor):
        return visitor.visit_key_node(self)


class NumberNode(ExpressionNode):
    """숫자 노드 (시간, 횟수 등)"""
    
    def __init__(self, number: float, position: Optional[Position] = None):
        """
        숫자 노드 초기화
        
        Args:
            number (float): 숫자 값
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.NUMBER, number, position)
        self.number = number
    
    def accept(self, visitor):
        return visitor.visit_number_node(self)


class VariableNode(ExpressionNode):
    """변수 노드 ($combo1 등)"""
    
    def __init__(self, variable_name: str, position: Optional[Position] = None):
        """
        변수 노드 초기화
        
        Args:
            variable_name (str): 변수 이름 ($ 제외)
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.VARIABLE, variable_name, position)
        self.variable_name = variable_name
    
    def accept(self, visitor):
        return visitor.visit_variable_node(self)


class MouseCoordNode(ExpressionNode):
    """마우스 좌표 노드 (@(100,200))"""
    
    def __init__(self, x: int, y: int, position: Optional[Position] = None):
        """
        마우스 좌표 노드 초기화
        
        Args:
            x (int): X 좌표
            y (int): Y 좌표
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.MOUSE_COORD, (x, y), position)
        self.x = x
        self.y = y
    
    def accept(self, visitor):
        return visitor.visit_mouse_coord_node(self)
    
    def __str__(self) -> str:
        return f"MOUSE_COORD({self.x},{self.y})"


class WheelNode(ExpressionNode):
    """휠 제어 노드 (wheel+3, wheel-2)"""
    
    def __init__(self, direction: str, amount: int = 1, position: Optional[Position] = None):
        """
        휠 노드 초기화
        
        Args:
            direction (str): 방향 ('+' 위, '-' 아래)
            amount (int): 휠 횟수
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.WHEEL, (direction, amount), position)
        self.direction = direction
        self.amount = amount
    
    def accept(self, visitor):
        return visitor.visit_wheel_node(self)
    
    def __str__(self) -> str:
        return f"WHEEL({self.direction}{self.amount})"


class OperatorNode(MSLNode):
    """연산자 노드 (여러 자식을 가지는 노드)"""
    
    def __init__(self, node_type: NodeType, position: Optional[Position] = None):
        """
        연산자 노드 초기화
        
        Args:
            node_type (NodeType): 연산자 타입
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(node_type, position)


class SequentialNode(OperatorNode):
    """순차 실행 노드 (W,A,S,D)"""
    
    def __init__(self, position: Optional[Position] = None):
        super().__init__(NodeType.SEQUENTIAL, position)
    
    def accept(self, visitor):
        return visitor.visit_sequential_node(self)


class SimultaneousNode(OperatorNode):
    """동시 실행 노드 (W+A+S+D)"""
    
    def __init__(self, position: Optional[Position] = None):
        super().__init__(NodeType.SIMULTANEOUS, position)
    
    def accept(self, visitor):
        return visitor.visit_simultaneous_node(self)


class HoldChainNode(OperatorNode):
    """홀드 연결 노드 (W>A>S>D)"""
    
    def __init__(self, position: Optional[Position] = None):
        super().__init__(NodeType.HOLD_CHAIN, position)
    
    def accept(self, visitor):
        return visitor.visit_hold_chain_node(self)


class ParallelNode(OperatorNode):
    """병렬 실행 노드 (W|A|S|D)"""
    
    def __init__(self, position: Optional[Position] = None):
        super().__init__(NodeType.PARALLEL, position)
    
    def accept(self, visitor):
        return visitor.visit_parallel_node(self)


class ToggleNode(OperatorNode):
    """토글 노드 (~CapsLock)"""
    
    def __init__(self, position: Optional[Position] = None):
        super().__init__(NodeType.TOGGLE, position)
    
    def accept(self, visitor):
        return visitor.visit_toggle_node(self)


class RepeatNode(OperatorNode):
    """반복 노드 (W*5)"""
    
    def __init__(self, count: int, position: Optional[Position] = None):
        """
        반복 노드 초기화
        
        Args:
            count (int): 반복 횟수
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.REPEAT, position)
        self.count = count
    
    def accept(self, visitor):
        return visitor.visit_repeat_node(self)
    
    def __str__(self) -> str:
        return f"REPEAT({self.count})"


class ContinuousNode(OperatorNode):
    """연속 입력 노드 (Space&100)"""
    
    def __init__(self, interval: int, position: Optional[Position] = None):
        """
        연속 입력 노드 초기화
        
        Args:
            interval (int): 입력 간격 (ms)
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.CONTINUOUS, position)
        self.interval = interval
    
    def accept(self, visitor):
        return visitor.visit_continuous_node(self)
    
    def __str__(self) -> str:
        return f"CONTINUOUS({self.interval}ms)"


class TimingNode(MSLNode):
    """타이밍 제어 노드"""
    
    def __init__(self, node_type: NodeType, duration: int, position: Optional[Position] = None):
        """
        타이밍 노드 초기화
        
        Args:
            node_type (NodeType): 타이밍 타입
            duration (int): 지속 시간 (ms)
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(node_type, position)
        self.duration = duration
    
    def __str__(self) -> str:
        return f"{self.node_type.value}({self.duration}ms)"


class DelayNode(TimingNode):
    """지연 노드 (W(500)A)"""
    
    def __init__(self, delay_time: int, position: Optional[Position] = None):
        """
        지연 노드 초기화
        
        Args:
            delay_time (int): 지연 시간 (ms)
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.DELAY, delay_time, position)
        self.delay_time = delay_time
    
    def accept(self, visitor):
        return visitor.visit_delay_node(self)


class HoldNode(TimingNode):
    """홀드 노드 (W[1000])"""
    
    def __init__(self, hold_time: int, position: Optional[Position] = None):
        """
        홀드 노드 초기화
        
        Args:
            hold_time (int): 홀드 시간 (ms)
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.HOLD, hold_time, position)
        self.hold_time = hold_time
    
    def accept(self, visitor):
        return visitor.visit_hold_node(self)


class IntervalNode(TimingNode):
    """간격 노드 (W*5{200})"""
    
    def __init__(self, interval_time: int, position: Optional[Position] = None):
        """
        간격 노드 초기화
        
        Args:
            interval_time (int): 간격 시간 (ms)
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.INTERVAL, interval_time, position)
        self.interval_time = interval_time
    
    def accept(self, visitor):
        return visitor.visit_interval_node(self)


class FadeNode(TimingNode):
    """페이드 노드 (W<100>A)"""
    
    def __init__(self, fade_time: int, position: Optional[Position] = None):
        """
        페이드 노드 초기화
        
        Args:
            fade_time (int): 페이드 시간 (ms)
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.FADE, fade_time, position)
        self.fade_time = fade_time
    
    def accept(self, visitor):
        return visitor.visit_fade_node(self)


class GroupNode(MSLNode):
    """그룹화 노드 ((W+A),S,D)"""
    
    def __init__(self, position: Optional[Position] = None):
        """
        그룹 노드 초기화
        
        Args:
            position (Position, optional): 소스 코드 위치
        """
        super().__init__(NodeType.GROUP, position)
    
    def accept(self, visitor):
        return visitor.visit_group_node(self)


class MSLVisitor(ABC):
    """MSL AST Visitor 인터페이스 (Visitor 패턴)"""
    
    @abstractmethod
    def visit_key_node(self, node: KeyNode):
        pass
    
    @abstractmethod
    def visit_number_node(self, node: NumberNode):
        pass
    
    @abstractmethod
    def visit_variable_node(self, node: VariableNode):
        pass
    
    @abstractmethod
    def visit_mouse_coord_node(self, node: MouseCoordNode):
        pass
    
    @abstractmethod
    def visit_wheel_node(self, node: WheelNode):
        pass
    
    @abstractmethod
    def visit_sequential_node(self, node: SequentialNode):
        pass
    
    @abstractmethod
    def visit_simultaneous_node(self, node: SimultaneousNode):
        pass
    
    @abstractmethod
    def visit_hold_chain_node(self, node: HoldChainNode):
        pass
    
    @abstractmethod
    def visit_parallel_node(self, node: ParallelNode):
        pass
    
    @abstractmethod
    def visit_toggle_node(self, node: ToggleNode):
        pass
    
    @abstractmethod
    def visit_repeat_node(self, node: RepeatNode):
        pass
    
    @abstractmethod
    def visit_continuous_node(self, node: ContinuousNode):
        pass
    
    @abstractmethod
    def visit_delay_node(self, node: DelayNode):
        pass
    
    @abstractmethod
    def visit_hold_node(self, node: HoldNode):
        pass
    
    @abstractmethod
    def visit_interval_node(self, node: IntervalNode):
        pass
    
    @abstractmethod
    def visit_fade_node(self, node: FadeNode):
        pass
    
    @abstractmethod
    def visit_group_node(self, node: GroupNode):
        pass


class ASTPrinter(MSLVisitor):
    """AST를 보기 좋게 출력하는 Visitor"""
    
    def __init__(self):
        self.indent_level = 0
    
    def _print_with_indent(self, text: str):
        print("  " * self.indent_level + text)
    
    def _visit_children(self, node: MSLNode):
        self.indent_level += 1
        for child in node.children:
            child.accept(self)
        self.indent_level -= 1
    
    def visit_key_node(self, node: KeyNode):
        self._print_with_indent(f"Key: {node.key_name}")
    
    def visit_number_node(self, node: NumberNode):
        self._print_with_indent(f"Number: {node.number}")
    
    def visit_variable_node(self, node: VariableNode):
        self._print_with_indent(f"Variable: ${node.variable_name}")
    
    def visit_mouse_coord_node(self, node: MouseCoordNode):
        self._print_with_indent(f"Mouse: ({node.x}, {node.y})")
    
    def visit_wheel_node(self, node: WheelNode):
        self._print_with_indent(f"Wheel: {node.direction}{node.amount}")
    
    def visit_sequential_node(self, node: SequentialNode):
        self._print_with_indent("Sequential:")
        self._visit_children(node)
    
    def visit_simultaneous_node(self, node: SimultaneousNode):
        self._print_with_indent("Simultaneous:")
        self._visit_children(node)
    
    def visit_hold_chain_node(self, node: HoldChainNode):
        self._print_with_indent("Hold Chain:")
        self._visit_children(node)
    
    def visit_parallel_node(self, node: ParallelNode):
        self._print_with_indent("Parallel:")
        self._visit_children(node)
    
    def visit_toggle_node(self, node: ToggleNode):
        self._print_with_indent("Toggle:")
        self._visit_children(node)
    
    def visit_repeat_node(self, node: RepeatNode):
        self._print_with_indent(f"Repeat ({node.count}x):")
        self._visit_children(node)
    
    def visit_continuous_node(self, node: ContinuousNode):
        self._print_with_indent(f"Continuous ({node.interval}ms):")
        self._visit_children(node)
    
    def visit_delay_node(self, node: DelayNode):
        self._print_with_indent(f"Delay: {node.delay_time}ms")
    
    def visit_hold_node(self, node: HoldNode):
        self._print_with_indent(f"Hold: {node.hold_time}ms")
    
    def visit_interval_node(self, node: IntervalNode):
        self._print_with_indent(f"Interval: {node.interval_time}ms")
    
    def visit_fade_node(self, node: FadeNode):
        self._print_with_indent(f"Fade: {node.fade_time}ms")
    
    def visit_group_node(self, node: GroupNode):
        self._print_with_indent("Group:")
        self._visit_children(node)


def test_ast():
    """AST 노드들의 기본 테스트"""
    print("=== MSL AST 테스트 ===")
    
    # 간단한 AST 생성: W,A,S,D
    root = SequentialNode()
    root.add_child(KeyNode("W"))
    root.add_child(KeyNode("A"))
    root.add_child(KeyNode("S"))
    root.add_child(KeyNode("D"))
    
    print("\n1. 순차 실행 (W,A,S,D):")
    printer = ASTPrinter()
    root.accept(printer)
    
    # 복합 AST 생성: Q(100)W*5
    print("\n2. 복합 매크로 (Q(100)W*5):")
    complex_root = SequentialNode()
    
    # Q(100) 부분
    q_with_delay = SequentialNode()
    q_with_delay.add_child(KeyNode("Q"))
    q_with_delay.add_child(DelayNode(100))
    
    # W*5 부분
    w_repeat = RepeatNode(5)
    w_repeat.add_child(KeyNode("W"))
    
    complex_root.add_child(q_with_delay)
    complex_root.add_child(w_repeat)
    
    complex_root.accept(printer)
    
    print("\n3. 트리 구조 출력:")
    print(complex_root.tree_string())


if __name__ == "__main__":
    test_ast() 