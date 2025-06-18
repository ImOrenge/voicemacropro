"""
MSL (Macro Scripting Language) 어휘 분석기 (Lexer)
게이머를 위한 직관적인 매크로 스크립팅 언어의 토큰화를 담당합니다.

지원 연산자:
- `,` : 순차 실행 (Sequential)
- `+` : 동시 실행 (Simultaneous)  
- `>` : 홀드 연결 (Hold Chain)
- `|` : 병렬 실행 (Parallel)
- `~` : 토글 (Toggle)
- `*` : 반복 (Repeat)
- `&` : 연속 입력 (Continuous)

타이밍 제어:
- `(숫자)` : 지연 시간 (Delay)
- `[숫자]` : 홀드 시간 (Hold Duration) 
- `{숫자}` : 반복 간격 (Repeat Interval)
- `<숫자>` : 페이드 시간 (Fade Time)
"""

import re
from enum import Enum
from typing import List, NamedTuple, Optional
from dataclasses import dataclass

class TokenType(Enum):
    """MSL 토큰 타입 정의"""
    # 기본 토큰
    KEY = "KEY"                    # W, A, Space, Ctrl 등 키 이름
    NUMBER = "NUMBER"              # 숫자 (시간, 횟수 등)
    
    # 연산자 토큰
    SEQUENTIAL = "SEQUENTIAL"      # , (순차 실행)
    SIMULTANEOUS = "SIMULTANEOUS" # + (동시 실행)
    HOLD_CHAIN = "HOLD_CHAIN"     # > (홀드 연결)
    PARALLEL = "PARALLEL"         # | (병렬 실행)
    TOGGLE = "TOGGLE"             # ~ (토글)
    REPEAT = "REPEAT"             # * (반복)
    CONTINUOUS = "CONTINUOUS"     # & (연속 입력)
    
    # 타이밍 제어 토큰
    DELAY_START = "DELAY_START"           # ( 
    DELAY_END = "DELAY_END"               # )
    HOLD_START = "HOLD_START"             # [
    HOLD_END = "HOLD_END"                 # ]
    INTERVAL_START = "INTERVAL_START"     # {
    INTERVAL_END = "INTERVAL_END"         # }
    FADE_START = "FADE_START"             # <
    FADE_END = "FADE_END"                 # >
    
    # 그룹화 토큰
    GROUP_START = "GROUP_START"           # (
    GROUP_END = "GROUP_END"               # )
    
    # 특수 토큰
    VARIABLE = "VARIABLE"         # $변수명
    MOUSE_COORD = "MOUSE_COORD"   # @(x,y)
    WHEEL = "WHEEL"               # wheel+/wheel-
    
    # 기타
    EOF = "EOF"                   # 파일 끝
    WHITESPACE = "WHITESPACE"     # 공백
    COMMENT = "COMMENT"           # # 주석
    INVALID = "INVALID"           # 잘못된 토큰


@dataclass
class Token:
    """MSL 토큰 클래스"""
    type: TokenType
    value: str
    position: int
    line: int = 1
    column: int = 1


class MSLLexer:
    """MSL 어휘 분석기"""
    
    def __init__(self):
        """MSL Lexer 초기화"""
        # 키 이름 패턴 (알파벳, 숫자, 언더스코어 조합)
        self.key_pattern = re.compile(r'[A-Za-z][A-Za-z0-9_]*')
        
        # 숫자 패턴 (정수 및 소수)
        self.number_pattern = re.compile(r'\d+\.?\d*')
        
        # 변수 패턴 ($로 시작)
        self.variable_pattern = re.compile(r'\$[A-Za-z][A-Za-z0-9_]*')
        
        # 마우스 좌표 패턴 @(x,y) - 음수 지원
        self.mouse_coord_pattern = re.compile(r'@\(\s*-?\d+\s*,\s*-?\d+\s*\)')
        
        # 휠 패턴 wheel+숫자 또는 wheel-숫자
        self.wheel_pattern = re.compile(r'wheel[+-]\d*')
        
        # 주석 패턴 (# 으로 시작)
        self.comment_pattern = re.compile(r'#.*')
        
        # 공백 패턴
        self.whitespace_pattern = re.compile(r'\s+')
        
        # 음수 패턴 (-숫자)
        self.negative_number_pattern = re.compile(r'-\d+\.?\d*')
        
        # 연산자 매핑
        self.operators = {
            ',': TokenType.SEQUENTIAL,
            '+': TokenType.SIMULTANEOUS,
            '>': TokenType.HOLD_CHAIN,
            '|': TokenType.PARALLEL,
            '~': TokenType.TOGGLE,
            '*': TokenType.REPEAT,
            '&': TokenType.CONTINUOUS,
        }
        
        # 구분자 매핑
        self.delimiters = {
            '(': TokenType.DELAY_START,
            ')': TokenType.DELAY_END,
            '[': TokenType.HOLD_START,
            ']': TokenType.HOLD_END,
            '{': TokenType.INTERVAL_START,
            '}': TokenType.INTERVAL_END,
            '<': TokenType.FADE_START,
            # '>' 는 이미 HOLD_CHAIN으로 사용됨
        }
    
    def tokenize(self, text: str) -> List[Token]:
        """
        MSL 스크립트를 토큰 리스트로 변환합니다.
        
        Args:
            text (str): MSL 스크립트 텍스트
            
        Returns:
            List[Token]: 토큰 리스트
        """
        tokens = []
        position = 0
        line = 1
        column = 1
        
        while position < len(text):
            # 현재 문자
            char = text[position]
            
            # 공백 처리
            if self.whitespace_pattern.match(text[position:]):
                match = self.whitespace_pattern.match(text[position:])
                whitespace = match.group()
                
                # 줄바꿈 카운트
                line += whitespace.count('\n')
                if '\n' in whitespace:
                    column = len(whitespace) - whitespace.rfind('\n')
                else:
                    column += len(whitespace)
                
                position += len(whitespace)
                continue
            
            # 주석 처리
            if self.comment_pattern.match(text[position:]):
                match = self.comment_pattern.match(text[position:])
                comment = match.group()
                
                tokens.append(Token(
                    type=TokenType.COMMENT,
                    value=comment,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += len(comment)
                column += len(comment)
                continue
            
            # 마우스 좌표 처리 @(x,y)
            if self.mouse_coord_pattern.match(text[position:]):
                match = self.mouse_coord_pattern.match(text[position:])
                coord = match.group()
                
                tokens.append(Token(
                    type=TokenType.MOUSE_COORD,
                    value=coord,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += len(coord)
                column += len(coord)
                continue
            
            # 휠 제어 처리 wheel+/wheel-
            if self.wheel_pattern.match(text[position:]):
                match = self.wheel_pattern.match(text[position:])
                wheel = match.group()
                
                tokens.append(Token(
                    type=TokenType.WHEEL,
                    value=wheel,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += len(wheel)
                column += len(wheel)
                continue
            
            # 변수 처리 $변수명
            if self.variable_pattern.match(text[position:]):
                match = self.variable_pattern.match(text[position:])
                variable = match.group()
                
                tokens.append(Token(
                    type=TokenType.VARIABLE,
                    value=variable,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += len(variable)
                column += len(variable)
                continue
            
            # 음수 처리 (-숫자) - 먼저 체크해야 함
            if char == '-' and self.negative_number_pattern.match(text[position:]):
                match = self.negative_number_pattern.match(text[position:])
                number = match.group()
                
                tokens.append(Token(
                    type=TokenType.NUMBER,
                    value=number,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += len(number)
                column += len(number)
                continue
            
            # 숫자 처리
            if self.number_pattern.match(text[position:]):
                match = self.number_pattern.match(text[position:])
                number = match.group()
                
                tokens.append(Token(
                    type=TokenType.NUMBER,
                    value=number,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += len(number)
                column += len(number)
                continue
            
            # 키 이름 처리 (알파벳으로 시작)
            if self.key_pattern.match(text[position:]):
                match = self.key_pattern.match(text[position:])
                key = match.group()
                
                tokens.append(Token(
                    type=TokenType.KEY,
                    value=key,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += len(key)
                column += len(key)
                continue
            
            # 연산자 처리
            if char in self.operators:
                tokens.append(Token(
                    type=self.operators[char],
                    value=char,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += 1
                column += 1
                continue
            
            # 구분자 처리
            if char in self.delimiters:
                tokens.append(Token(
                    type=self.delimiters[char],
                    value=char,
                    position=position,
                    line=line,
                    column=column
                ))
                
                position += 1
                column += 1
                continue
            
            # 잘못된 문자 처리
            tokens.append(Token(
                type=TokenType.INVALID,
                value=char,
                position=position,
                line=line,
                column=column
            ))
            
            position += 1
            column += 1
        
        # EOF 토큰 추가
        tokens.append(Token(
            type=TokenType.EOF,
            value="",
            position=position,
            line=line,
            column=column
        ))
        
        return tokens
    
    def validate_tokens(self, tokens: List[Token]) -> List[str]:
        """
        토큰 리스트의 유효성을 검사합니다.
        
        Args:
            tokens (List[Token]): 검사할 토큰 리스트
            
        Returns:
            List[str]: 오류 메시지 리스트 (빈 리스트면 오류 없음)
        """
        errors = []
        
        for i, token in enumerate(tokens):
            # 잘못된 토큰 검사
            if token.type == TokenType.INVALID:
                errors.append(f"Line {token.line}, Column {token.column}: "
                             f"Invalid character '{token.value}'")
            
            # 괄호 매칭 검사는 파서에서 수행
            
        return errors


def test_lexer():
    """MSL Lexer 테스트 함수"""
    lexer = MSLLexer()
    
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
        "Shift[2000]+(W,A,S,D)",
        
        # 마우스 및 변수 테스트
        "@(100,200)",
        "wheel+3",
        "$combo1,W,A",
        
        # 주석 테스트
        "W,A # 이동 매크로",
    ]
    
    print("=== MSL Lexer 테스트 ===")
    for script in test_scripts:
        print(f"\n스크립트: {script}")
        tokens = lexer.tokenize(script)
        
        for token in tokens:
            if token.type != TokenType.EOF:
                print(f"  {token.type.value:12} | {token.value}")
        
        # 오류 검사
        errors = lexer.validate_tokens(tokens)
        if errors:
            print("  오류:")
            for error in errors:
                print(f"    {error}")


if __name__ == "__main__":
    test_lexer() 