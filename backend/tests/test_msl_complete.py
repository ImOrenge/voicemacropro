"""
MSL (Macro Scripting Language) 통합 테스트
Lexer → Parser → Interpreter 전체 파이프라인을 테스트합니다.

테스트 항목:
1. 기본 문법 테스트
2. 연산자 우선순위 테스트  
3. 오류 처리 테스트
4. 성능 측정 테스트
"""

import time
import logging
from typing import List, Dict, Any

from backend.parsers.msl_lexer import MSLLexer, TokenType
from backend.parsers.msl_parser import MSLParser, ParseError
from backend.parsers.msl_interpreter import MSLInterpreter, ExecutionResult
from backend.parsers.msl_ast import ASTPrinter


class MSLTestSuite:
    """MSL 테스트 도구"""
    
    def __init__(self, enable_real_execution: bool = False):
        """
        MSL 테스트 도구 초기화
        
        Args:
            enable_real_execution (bool): 실제 키보드 입력 실행 여부
        """
        self.lexer = MSLLexer()
        self.parser = MSLParser()
        self.interpreter = MSLInterpreter()
        self.enable_real_execution = enable_real_execution
        
        # 로깅 설정
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )
        self.logger = logging.getLogger('MSLTestSuite')
        
        # 테스트 결과 저장
        self.test_results = []
    
    def run_lexer_tests(self):
        """Lexer 테스트 실행"""
        print("\n" + "="*50)
        print("🔤 MSL LEXER 테스트")
        print("="*50)
        
        test_cases = [
            # 기본 연산자 테스트
            ("W,A,S,D", "순차 실행"),
            ("W+A+S+D", "동시 실행"),
            ("W>A>S>D", "홀드 연결"),
            ("W|A|S|D", "병렬 실행"),
            ("~CapsLock", "토글"),
            ("W*5", "반복"),
            ("Space&100", "연속 입력"),
            
            # 타이밍 제어 테스트
            ("W(500)A", "지연 시간"),
            ("W[1000]", "홀드 시간"),
            ("W*5{200}", "반복 간격"),
            
            # 복합 테스트
            ("Q(100)W(150)E(200)R", "복합 콤보"),
            ("(W+A),S,D", "그룹화"),
            ("Shift[2000]+(W,A,S,D)", "복잡한 조합"),
            
            # 특수 기능 테스트
            ("@(100,200)", "마우스 좌표"),
            ("wheel+3", "휠 제어"),
            ("$combo1,W,A", "변수 사용"),
            ("W,A # 이동 매크로", "주석"),
        ]
        
        for script, description in test_cases:
            print(f"\n테스트: {description}")
            print(f"스크립트: {script}")
            
            try:
                tokens = self.lexer.tokenize(script)
                
                # 의미있는 토큰만 출력 (EOF, 공백, 주석 제외)
                meaningful_tokens = [
                    token for token in tokens 
                    if token.type not in [TokenType.EOF, TokenType.WHITESPACE, TokenType.COMMENT]
                ]
                
                print(f"토큰 수: {len(meaningful_tokens)}")
                for token in meaningful_tokens:
                    print(f"  {token.type.value:12} | {token.value}")
                
                # 오류 검사
                errors = self.lexer.validate_tokens(tokens)
                if errors:
                    print(f"  ❌ 오류: {', '.join(errors)}")
                else:
                    print("  ✅ 성공")
                    
            except Exception as e:
                print(f"  ❌ 예외: {e}")
    
    def run_parser_tests(self):
        """Parser 테스트 실행"""
        print("\n" + "="*50)
        print("🌳 MSL PARSER 테스트")
        print("="*50)
        
        test_cases = [
            # 기본 문법 테스트
            ("W", "단일 키"),
            ("W,A,S,D", "순차 실행"),
            ("W+A+S+D", "동시 실행"),
            ("W>A>S>D", "홀드 연결"),
            ("W*5", "반복"),
            ("Space&100", "연속 입력"),
            ("~CapsLock", "토글"),
            
            # 그룹화 테스트
            ("(W+A),S,D", "그룹화"),
            ("(W,A)+(S,D)", "복합 그룹화"),
            
            # 타이밍 제어 테스트
            ("W[1000]", "홀드 시간"),
            ("W*5{200}", "반복 간격"),
            
            # 특수 기능 테스트
            ("@(100,200)", "마우스 좌표"),
            ("wheel+3", "휠 제어"),
            
            # 변수 테스트
            ("$combo1", "변수 참조"),
        ]
        
        for script, description in test_cases:
            print(f"\n테스트: {description}")
            print(f"스크립트: {script}")
            
            try:
                ast = self.parser.parse(script)
                
                # AST 구조 출력
                print("AST 구조:")
                printer = ASTPrinter()
                ast.accept(printer)
                
                print("  ✅ 파싱 성공")
                
            except ParseError as e:
                print(f"  ❌ 파싱 오류: {e}")
            except Exception as e:
                print(f"  ❌ 예외: {e}")
    
    def run_interpreter_tests(self):
        """Interpreter 테스트 실행"""
        print("\n" + "="*50)
        print("⚡ MSL INTERPRETER 테스트")
        print("="*50)
        
        if not self.enable_real_execution:
            print("🔒 실제 실행 비활성화됨 (파싱까지만 테스트)")
        
        test_cases = [
            ("W", "단일 키 입력"),
            ("W,A", "순차 입력"), 
            ("W+Shift", "동시 입력"),
            ("W*3", "반복 입력"),
            ("~CapsLock", "토글"),
        ]
        
        # 테스트용 변수
        test_variables = {
            "combo1": "Q,W,E,R"
        }
        
        for script, description in test_cases:
            print(f"\n테스트: {description}")
            print(f"스크립트: {script}")
            
            try:
                # 파싱
                ast = self.parser.parse(script)
                
                if self.enable_real_execution:
                    print("⚠️  실제 키보드 입력이 실행됩니다!")
                    time.sleep(1)
                    
                    # 실행
                    result = self.interpreter.execute(ast, test_variables)
                    
                    if result.success:
                        print(f"  ✅ 실행 성공:")
                        print(f"    실행 시간: {result.execution_time:.3f}초")
                        print(f"    실행 액션: {result.executed_actions}개")
                    else:
                        print(f"  ❌ 실행 실패: {result.error_message}")
                else:
                    print("  ✅ 파싱 성공 (실행 생략)")
                    
            except ParseError as e:
                print(f"  ❌ 파싱 오류: {e}")
            except Exception as e:
                print(f"  ❌ 예외: {e}")
    
    def run_error_tests(self):
        """오류 처리 테스트"""
        print("\n" + "="*50)
        print("❌ 오류 처리 테스트")
        print("="*50)
        
        error_test_cases = [
            ("", "빈 스크립트"),
            ("W,", "불완전한 순차 실행"),
            ("W+", "불완전한 동시 실행"),
            ("W*", "불완전한 반복"),
            ("W&", "불완전한 연속 입력"),
            ("~", "불완전한 토글"),
            ("(W+A", "괄호 미매칭"),
            ("W[", "괄호 미매칭 2"),
            ("@(100)", "잘못된 마우스 좌표"),
            ("wheel", "잘못된 휠 형식"),
            ("$", "잘못된 변수"),
            ("W**5", "잘못된 연산자 조합"),
        ]
        
        for script, description in error_test_cases:
            print(f"\n테스트: {description}")
            print(f"스크립트: '{script}'")
            
            try:
                if script:
                    tokens = self.lexer.tokenize(script)
                    errors = self.lexer.validate_tokens(tokens)
                    
                    if errors:
                        print(f"  ✅ Lexer 오류 감지: {errors[0]}")
                        continue
                    
                    ast = self.parser.parse(script)
                    print(f"  ❌ 예상과 달리 파싱 성공")
                else:
                    ast = self.parser.parse(script)
                    print(f"  ❌ 예상과 달리 파싱 성공")
                    
            except ParseError as e:
                print(f"  ✅ 파싱 오류 감지: {e}")
            except Exception as e:
                print(f"  ✅ 예외 감지: {e}")
    
    def run_performance_tests(self):
        """성능 테스트"""
        print("\n" + "="*50)
        print("⚡ 성능 테스트")
        print("="*50)
        
        performance_test_cases = [
            ("W", 1000, "단순 키 (1000회)"),
            ("W,A,S,D", 500, "순차 실행 (500회)"),
            ("W+A+S+D", 500, "동시 실행 (500회)"),
            ("Q(100)W(150)E(200)R", 100, "복합 콤보 (100회)"),
            ("(W+A)*5,S,D", 100, "복잡한 조합 (100회)"),
        ]
        
        for script, iterations, description in performance_test_cases:
            print(f"\n테스트: {description}")
            print(f"스크립트: {script}")
            
            # Lexer 성능
            start_time = time.time()
            for _ in range(iterations):
                tokens = self.lexer.tokenize(script)
            lexer_time = time.time() - start_time
            
            # Parser 성능
            start_time = time.time()
            for _ in range(iterations):
                ast = self.parser.parse(script)
            parser_time = time.time() - start_time
            
            print(f"  Lexer:  {lexer_time:.3f}초 ({lexer_time/iterations*1000:.2f}ms/회)")
            print(f"  Parser: {parser_time:.3f}초 ({parser_time/iterations*1000:.2f}ms/회)")
            print(f"  총합:   {(lexer_time+parser_time):.3f}초")
    
    def run_all_tests(self):
        """모든 테스트 실행"""
        print("🧪 MSL 통합 테스트 시작")
        print("="*70)
        
        start_time = time.time()
        
        # 순서대로 테스트 실행
        self.run_lexer_tests()
        self.run_parser_tests()
        self.run_interpreter_tests()
        self.run_error_tests()
        self.run_performance_tests()
        
        # 총 실행 시간
        total_time = time.time() - start_time
        
        print("\n" + "="*70)
        print(f"🎉 MSL 통합 테스트 완료! (총 {total_time:.2f}초)")
        print("="*70)
        
        # 인터프리터 통계 출력
        if hasattr(self.interpreter, 'execution_stats'):
            stats = self.interpreter.get_statistics()
            print(f"\n📊 실행 통계:")
            print(f"  총 실행: {stats['total_executions']}회")
            print(f"  성공: {stats['successful_executions']}회")
            print(f"  실패: {stats['failed_executions']}회")
            print(f"  평균 실행 시간: {stats['average_execution_time']:.3f}초")


def main():
    """메인 함수"""
    print("MSL (Macro Scripting Language) 통합 테스트")
    print("=" * 50)
    
    # 실제 키보드 입력 실행 여부 확인
    while True:
        choice = input("\n실제 키보드 입력을 실행하시겠습니까? (y/N): ").strip().lower()
        if choice in ['y', 'yes']:
            enable_execution = True
            print("⚠️  실제 키보드 입력이 실행됩니다!")
            print("   마우스를 화면 왼쪽 위 모서리로 이동하면 중단됩니다.")
            input("   계속하려면 Enter를 누르세요...")
            break
        elif choice in ['n', 'no', '']:
            enable_execution = False
            print("🔒 실제 실행은 비활성화됩니다 (파싱까지만 테스트)")
            break
        else:
            print("y 또는 n을 입력하세요.")
    
    # 테스트 실행
    test_suite = MSLTestSuite(enable_real_execution=enable_execution)
    test_suite.run_all_tests()


if __name__ == "__main__":
    main() 