"""
MSL (Macro Scripting Language) 파서 (Parser)
Lexer가 생성한 토큰을 받아서 AST(Abstract Syntax Tree)로 변환합니다.

파싱 우선순위 (높은 순):
1. 그룹화: ( )
2. 토글: ~
3. 반복: *
4. 연속입력: &
5. 홀드연결: >
6. 병렬실행: |
7. 동시실행: +
8. 순차실행: ,

타이밍 제어:
- (숫자): 지연 시간
- [숫자]: 홀드 시간
- {숫자}: 반복 간격
- <숫자>: 페이드 시간
"""

import re
from typing import List, Optional, Union, Dict
from backend.parsers.msl_lexer import MSLLexer, Token, TokenType
from backend.parsers.msl_ast import *


class ParseError(Exception):
    """MSL 파싱 오류"""
    
    def __init__(self, message: str, token: Optional[Token] = None):
        """
        파싱 오류 초기화
        
        Args:
            message (str): 오류 메시지
            token (Token, optional): 오류가 발생한 토큰
        """
        self.message = message
        self.token = token
        
        if token:
            super().__init__(f"Line {token.line}, Column {token.column}: {message}")
        else:
            super().__init__(message)


class MSLParser:
    """MSL 파서"""
    
    def __init__(self):
        """MSL Parser 초기화"""
        self.tokens: List[Token] = []
        self.current_position = 0
        self.current_token: Optional[Token] = None
        
        # 변수 저장소 (파싱 시점에는 체크만)
        self.variables: Dict[str, MSLNode] = {}
    
    def parse(self, text: str) -> MSLNode:
        """
        MSL 스크립트를 파싱하여 AST를 생성합니다.
        
        Args:
            text (str): MSL 스크립트 텍스트
            
        Returns:
            MSLNode: 루트 AST 노드
            
        Raises:
            ParseError: 파싱 오류 발생 시
        """
        # 1. 토큰화
        lexer = MSLLexer()
        self.tokens = lexer.tokenize(text)
        
        # 2. 주석 및 공백 제거
        self.tokens = [token for token in self.tokens 
                      if token.type not in [TokenType.COMMENT, TokenType.WHITESPACE]]
        
        # 3. 토큰 유효성 검사
        errors = lexer.validate_tokens(self.tokens)
        if errors:
            raise ParseError(f"토큰 오류: {', '.join(errors)}")
        
        # 4. 파싱 초기화
        self.current_position = 0
        self.current_token = self.tokens[0] if self.tokens else None
        
        # 5. 파싱 시작
        if not self.tokens or self.tokens[0].type == TokenType.EOF:
            raise ParseError("빈 스크립트입니다")
        
        try:
            ast = self.parse_expression()
            
            # 6. 모든 토큰이 소비되었는지 확인
            if self.current_token and self.current_token.type != TokenType.EOF:
                raise ParseError(f"예상치 못한 토큰: {self.current_token.value}", self.current_token)
            
            return ast
            
        except IndexError:
            raise ParseError("예상치 못한 스크립트 끝")
    
    def advance(self) -> Optional[Token]:
        """다음 토큰으로 이동"""
        if self.current_position < len(self.tokens) - 1:
            self.current_position += 1
            self.current_token = self.tokens[self.current_position]
        else:
            self.current_token = None
        return self.current_token
    
    def peek(self, offset: int = 1) -> Optional[Token]:
        """앞으로 offset만큼 떨어진 토큰 확인 (이동하지 않음)"""
        pos = self.current_position + offset
        if 0 <= pos < len(self.tokens):
            return self.tokens[pos]
        return None
    
    def expect(self, token_type: TokenType) -> Token:
        """현재 토큰이 예상 타입인지 확인하고 다음으로 이동"""
        if not self.current_token or self.current_token.type != token_type:
            expected = token_type.value
            actual = self.current_token.value if self.current_token else "EOF"
            raise ParseError(f"예상: {expected}, 실제: {actual}", self.current_token)
        
        token = self.current_token
        self.advance()
        return token
    
    def match(self, *token_types: TokenType) -> bool:
        """현재 토큰이 주어진 타입들 중 하나인지 확인"""
        if not self.current_token:
            return False
        return self.current_token.type in token_types
    
    def parse_expression(self) -> MSLNode:
        """표현식 파싱 (최상위 레벨)"""
        return self.parse_sequential()
    
    def parse_sequential(self) -> MSLNode:
        """순차 실행 파싱 (가장 낮은 우선순위: ,)"""
        left = self.parse_simultaneous()
        
        if self.match(TokenType.SEQUENTIAL):
            sequential_node = SequentialNode(self._get_position())
            sequential_node.add_child(left)
            
            while self.match(TokenType.SEQUENTIAL):
                self.advance()  # , 소비
                right = self.parse_simultaneous()
                sequential_node.add_child(right)
            
            return sequential_node
        
        return left
    
    def parse_simultaneous(self) -> MSLNode:
        """동시 실행 파싱 (+)"""
        left = self.parse_parallel()
        
        if self.match(TokenType.SIMULTANEOUS):
            simultaneous_node = SimultaneousNode(self._get_position())
            simultaneous_node.add_child(left)
            
            while self.match(TokenType.SIMULTANEOUS):
                self.advance()  # + 소비
                right = self.parse_parallel()
                simultaneous_node.add_child(right)
            
            return simultaneous_node
        
        return left
    
    def parse_parallel(self) -> MSLNode:
        """병렬 실행 파싱 (|)"""
        left = self.parse_hold_chain()
        
        if self.match(TokenType.PARALLEL):
            parallel_node = ParallelNode(self._get_position())
            parallel_node.add_child(left)
            
            while self.match(TokenType.PARALLEL):
                self.advance()  # | 소비
                right = self.parse_hold_chain()
                parallel_node.add_child(right)
            
            return parallel_node
        
        return left
    
    def parse_hold_chain(self) -> MSLNode:
        """홀드 연결 파싱 (>)"""
        left = self.parse_continuous()
        
        if self.match(TokenType.HOLD_CHAIN):
            hold_chain_node = HoldChainNode(self._get_position())
            hold_chain_node.add_child(left)
            
            while self.match(TokenType.HOLD_CHAIN):
                self.advance()  # > 소비
                right = self.parse_continuous()
                hold_chain_node.add_child(right)
            
            return hold_chain_node
        
        return left
    
    def parse_continuous(self) -> MSLNode:
        """연속 입력 파싱 (&)"""
        left = self.parse_repeat()
        
        if self.match(TokenType.CONTINUOUS):
            self.advance()  # & 소비
            
            # 간격 시간 파싱
            if not self.match(TokenType.NUMBER):
                raise ParseError("연속 입력 (&) 다음에는 간격 시간(숫자)이 와야 합니다", self.current_token)
            
            interval_token = self.expect(TokenType.NUMBER)
            interval = int(float(interval_token.value))
            
            continuous_node = ContinuousNode(interval, self._get_position())
            continuous_node.add_child(left)
            
            return continuous_node
        
        return left
    
    def parse_repeat(self) -> MSLNode:
        """반복 파싱 (*)"""
        left = self.parse_toggle()
        
        if self.match(TokenType.REPEAT):
            self.advance()  # * 소비
            
            # 반복 횟수 파싱
            if not self.match(TokenType.NUMBER):
                raise ParseError("반복 (*) 다음에는 반복 횟수(숫자)가 와야 합니다", self.current_token)
            
            count_token = self.expect(TokenType.NUMBER)
            count = int(float(count_token.value))
            
            repeat_node = RepeatNode(count, self._get_position())
            repeat_node.add_child(left)
            
            # 선택적 간격 시간 파싱 {숫자}
            if self.match(TokenType.INTERVAL_START):
                self.advance()  # { 소비
                
                if not self.match(TokenType.NUMBER):
                    raise ParseError("간격 시간 지정 시 숫자가 필요합니다", self.current_token)
                
                interval_token = self.expect(TokenType.NUMBER)
                interval = int(float(interval_token.value))
                
                self.expect(TokenType.INTERVAL_END)  # } 소비
                
                # 간격 정보를 RepeatNode에 추가
                interval_node = IntervalNode(interval, self._get_position())
                repeat_node.add_child(interval_node)
            
            return repeat_node
        
        return left
    
    def parse_toggle(self) -> MSLNode:
        """토글 파싱 (~)"""
        if self.match(TokenType.TOGGLE):
            self.advance()  # ~ 소비
            
            # 토글 대상 파싱
            target = self.parse_primary()
            
            toggle_node = ToggleNode(self._get_position())
            toggle_node.add_child(target)
            
            return toggle_node
        
        return self.parse_primary()
    
    def parse_primary(self) -> MSLNode:
        """기본 요소 파싱 (키, 숫자, 변수, 마우스, 휠, 그룹)"""
        # 그룹화 처리 (괄호)
        if self.match(TokenType.DELAY_START):
            return self.parse_group()
        
        # 키 입력
        if self.match(TokenType.KEY):
            key_token = self.expect(TokenType.KEY)
            key_node = KeyNode(key_token.value, self._get_position(key_token))
            
            # 키 다음에 올 수 있는 타이밍 제어들 파싱
            return self.parse_timing_modifiers(key_node)
        
        # 변수
        if self.match(TokenType.VARIABLE):
            var_token = self.expect(TokenType.VARIABLE)
            var_name = var_token.value[1:]  # $ 제거
            return VariableNode(var_name, self._get_position(var_token))
        
        # 마우스 좌표
        if self.match(TokenType.MOUSE_COORD):
            mouse_token = self.expect(TokenType.MOUSE_COORD)
            # @(x,y) 형태에서 x, y 추출 (음수 지원)
            match = re.match(r'@\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)', mouse_token.value)
            if match:
                x, y = int(match.group(1)), int(match.group(2))
                return MouseCoordNode(x, y, self._get_position(mouse_token))
            else:
                raise ParseError(f"잘못된 마우스 좌표 형식: {mouse_token.value}", mouse_token)
        
        # 휠 제어
        if self.match(TokenType.WHEEL):
            wheel_token = self.expect(TokenType.WHEEL)
            # wheel+숫자 또는 wheel-숫자 파싱
            match = re.match(r'wheel([+-])(\d*)', wheel_token.value)
            if match:
                direction = match.group(1)
                amount = int(match.group(2)) if match.group(2) else 1
                return WheelNode(direction, amount, self._get_position(wheel_token))
            else:
                raise ParseError(f"잘못된 휠 제어 형식: {wheel_token.value}", wheel_token)
        
        # 예상치 못한 토큰
        if self.current_token:
            raise ParseError(f"예상치 못한 토큰: {self.current_token.value}", self.current_token)
        else:
            raise ParseError("예상치 못한 스크립트 끝")
    
    def parse_group(self) -> MSLNode:
        """그룹화 파싱 (괄호)"""
        self.expect(TokenType.DELAY_START)  # ( 소비
        
        # 괄호 내부 표현식 파싱
        inner_expr = self.parse_expression()
        
        self.expect(TokenType.DELAY_END)  # ) 소비
        
        # 그룹 노드로 감싸기
        group_node = GroupNode(self._get_position())
        group_node.add_child(inner_expr)
        
        return group_node
    
    def parse_timing_modifiers(self, base_node: MSLNode) -> MSLNode:
        """키 뒤에 오는 타이밍 제어 파싱"""
        current_node = base_node
        
        while True:
            # 지연 시간 (숫자)
            if self.match(TokenType.DELAY_START):
                self.advance()  # ( 소비
                
                if not self.match(TokenType.NUMBER):
                    raise ParseError("지연 시간 지정 시 숫자가 필요합니다", self.current_token)
                
                delay_token = self.expect(TokenType.NUMBER)
                delay = int(float(delay_token.value))
                
                self.expect(TokenType.DELAY_END)  # ) 소비
                
                # 지연 노드 생성
                delay_node = DelayNode(delay, self._get_position())
                delay_node.add_child(current_node)
                current_node = delay_node
            
            # 홀드 시간 [숫자]
            elif self.match(TokenType.HOLD_START):
                self.advance()  # [ 소비
                
                if not self.match(TokenType.NUMBER):
                    raise ParseError("홀드 시간 지정 시 숫자가 필요합니다", self.current_token)
                
                hold_token = self.expect(TokenType.NUMBER)
                hold_time = int(float(hold_token.value))
                
                self.expect(TokenType.HOLD_END)  # ] 소비
                
                # 홀드 노드로 감싸기
                hold_node = HoldNode(hold_time, self._get_position())
                hold_node.add_child(current_node)
                current_node = hold_node
            
            # 페이드 시간 <숫자>
            elif self.match(TokenType.FADE_START):
                self.advance()  # < 소비
                
                if not self.match(TokenType.NUMBER):
                    raise ParseError("페이드 시간 지정 시 숫자가 필요합니다", self.current_token)
                
                fade_token = self.expect(TokenType.NUMBER)
                fade_time = int(float(fade_token.value))
                
                # > 토큰은 HOLD_CHAIN과 겹치므로 별도 처리 필요
                # 일단 단순하게 다음 토큰이 숫자가 아니면 페이드 종료로 간주
                next_token = self.peek()
                if next_token and next_token.type in [TokenType.KEY, TokenType.VARIABLE]:
                    # W<100>A 형태: 페이드 노드 생성
                    fade_node = FadeNode(fade_time, self._get_position())
                    fade_node.add_child(current_node)
                    current_node = fade_node
                else:
                    raise ParseError("페이드 구문이 완료되지 않았습니다", self.current_token)
            
            else:
                break
        
        return current_node
    
    def _get_position(self, token: Optional[Token] = None) -> Position:
        """현재 토큰 또는 지정된 토큰의 위치 정보 반환"""
        if token is None:
            token = self.current_token
        
        if token:
            return Position(token.line, token.column, token.position)
        else:
            return Position(1, 1, 0)


def test_parser():
    """MSL Parser 테스트 함수"""
    parser = MSLParser()
    
    test_scripts = [
        # 기본 테스트
        "W,A,S,D",
        "W+A+S+D",
        "W>A>S>D",
        "W*5",
        "Space&100",
        "~CapsLock",
        
        # 타이밍 제어 테스트
        "W(500)A",
        "W[1000]",
        "W*5{200}",
        
        # 복합 테스트
        "Q(100)W(150)E(200)R",
        "(W+A),S,D",
        "Shift[2000]+(W,A,S,D)",
        
        # 마우스 및 변수 테스트  
        "@(100,200)",
        "wheel+3",
        "$combo1,W,A",
    ]
    
    print("=== MSL Parser 테스트 ===")
    
    for script in test_scripts:
        print(f"\n스크립트: {script}")
        try:
            ast = parser.parse(script)
            
            # AST 출력
            printer = ASTPrinter()
            ast.accept(printer)
            
        except ParseError as e:
            print(f"  파싱 오류: {e}")
        except Exception as e:
            print(f"  예상치 못한 오류: {e}")


if __name__ == "__main__":
    test_parser() 