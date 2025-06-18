"""
PRD 고급 예시 MSL 스크립트 테스트
현재 파서가 어떤 구문에서 실패하는지 확인합니다.
"""

from backend.parsers.msl_lexer import MSLLexer
from backend.parsers.msl_parser import MSLParser, ParseError

def test_prd_examples():
    """PRD에 있는 고급 예시들을 테스트"""
    lexer = MSLLexer()
    parser = MSLParser()
    
    # PRD의 고급 예시들
    test_cases = [
        # 기본 예시
        ("W,A,S,D", "순차적으로 키 누르기"),
        ("W+A+S+D", "동시에 키 누르기"),
        ("W>A>S>D", "순차적으로 누르면서 홀드"),
        ("Q(100)W(150)E(200)R", "지연 시간을 포함한 콤보"),
        ("Shift[2000]+(W,A,S,D)", "키를 홀드하면서 다른 키 실행"),
        ("Space*10{50}", "반복 실행"),
        ("Attack&50", "연속 입력 (빠른 공격)"),
        ("~CapsLock,(W,A,S,D),~CapsLock", "토글 기능"),
        
        # 고급 예시
        ("Down+Forward,Punch(50)Kick(100)*3", "복합 콤보 (격투 게임)"),
        ("Skill1(1000)Skill2(800)Skill3(1200)Buff", "MMO 스킬 로테이션"),
        ("MouseDown[100]@(0,-5)@(0,-3)@(0,-2)MouseUp", "FPS 리코일 제어"),
        ("B(50)H(50)P*5{200}(100)B(50)S", "RTS 빌드 오더"),
        ("Space[200]+(Left*3)(300)Space[200]+(Right*3)", "연속 점프 (플랫포머)"),
    ]
    
    print("=== PRD 고급 예시 MSL 파서 테스트 ===")
    
    for script, description in test_cases:
        print(f"\n📝 테스트: {description}")
        print(f"🔧 스크립트: {script}")
        
        try:
            # 토큰화 테스트
            tokens = lexer.tokenize(script)
            print(f"✅ 토큰화 성공: {len(tokens)} 토큰")
            
            # 토큰 검증
            errors = lexer.validate_tokens(tokens)
            if errors:
                print(f"❌ 토큰 오류: {', '.join(errors)}")
                continue
            
            # 파싱 테스트
            ast = parser.parse(script)
            print(f"✅ 파싱 성공")
            print(f"   AST 타입: {type(ast).__name__}")
            
        except ParseError as e:
            print(f"❌ 파싱 오류: {e}")
        except Exception as e:
            print(f"❌ 예상치 못한 오류: {e}")

if __name__ == "__main__":
    test_prd_examples() 