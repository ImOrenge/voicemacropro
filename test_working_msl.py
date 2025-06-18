"""
VoiceMacro Pro MSL 스크립트 테스트
현재 지원되는 기본 구문들을 테스트합니다.
"""

from msl_lexer import MSLLexer
from msl_parser import MSLParser, ParseError
import json
import sys

def test_basic_scripts():
    """기본 지원 구문들을 테스트"""
    lexer = MSLLexer()
    parser = MSLParser()
    
    # 현재 지원되는 기본 구문들
    test_cases = [
        # 1. 순차 실행 (쉼표)
        ("W,A,S,D", "순차적으로 W → A → S → D 실행"),
        
        # 2. 동시 실행 (플러스)
        ("W+A", "W와 A를 동시에 실행"),
        ("Ctrl+C", "Ctrl과 C를 동시에 실행 (복사)"),
        ("Shift+Tab", "Shift와 Tab을 동시에 실행"),
        
        # 3. 반복 (별표)
        ("Space*5", "Space키를 5번 반복"),
        ("W*3", "W키를 3번 반복"),
        
        # 4. 연속 입력 (앰퍼샌드)
        ("Attack&100", "Attack을 100ms 간격으로 연속 입력"),
        ("Space&50", "Space를 50ms 간격으로 연속 입력"),
        
        # 5. 토글 (물결표)
        ("~CapsLock", "CapsLock 토글"),
        ("~NumLock", "NumLock 토글"),
        
        # 6. 홀드 (대괄호)
        ("Shift[2000]", "Shift키를 2초간 홀드"),
        ("W[1000]", "W키를 1초간 홀드"),
        
        # 7. 기본 키보드 키들
        ("Enter", "엔터키"),
        ("Escape", "ESC키"),
        ("Tab", "탭키"),
        ("Delete", "삭제키"),
        ("Backspace", "백스페이스키"),
        
        # 8. 기본 조합 구문
        ("W,A,W,D", "W → A → W → D 순차 실행"),
        ("W+A,S+D", "W+A 동시 실행 후 S+D 동시 실행"),
        ("Space*3,Enter", "Space 3번 후 Enter"),
        
        # 9. 숫자 키
        ("1,2,3,4", "숫자 1,2,3,4 순차 입력"),
        ("Ctrl+1", "Ctrl+1 단축키"),
        
        # 10. 함수 키
        ("F1,F2,F3", "F1, F2, F3 순차 실행"),
        ("Alt+F4", "Alt+F4 (프로그램 종료)"),
    ]
    
    print("=" * 60)
    print("🧪 VoiceMacro Pro MSL 기본 구문 테스트")
    print("=" * 60)
    
    success_count = 0
    total_count = len(test_cases)
    
    for i, (script_code, description) in enumerate(test_cases, 1):
        print(f"\n📝 테스트 {i:2d}: {description}")
        print(f"   스크립트: {script_code}")
        
        try:
            # 토큰화 테스트
            tokens = lexer.tokenize(script_code)
            print(f"   ✅ 토큰화 성공: {len([t for t in tokens if t.type.value != 'EOF'])}개 토큰")
            
            # 파싱 테스트
            ast = parser.parse(script_code)
            print(f"   ✅ 파싱 성공: AST 생성됨")
            
            success_count += 1
            print(f"   🎉 결과: 성공")
            
        except ParseError as e:
            print(f"   ❌ 파싱 오류: {e}")
        except Exception as e:
            print(f"   💥 예외 발생: {e}")
    
    print("\n" + "=" * 60)
    print(f"📊 테스트 결과: {success_count}/{total_count} 성공 ({success_count/total_count*100:.1f}%)")
    print("=" * 60)
    
    return success_count, total_count

def test_advanced_scripts():
    """고급 구문들을 테스트 (실패 예상)"""
    lexer = MSLLexer()
    parser = MSLParser()
    
    # 아직 지원되지 않는 고급 구문들
    advanced_cases = [
        ("Q(100)W(150)E", "연속 지연시간"),
        ("@(100,200)", "마우스 좌표"),
        ("$combo1,W,A", "변수 사용"),
        ("(W+A)*3", "그룹화 반복"),
        ("wheel+3", "휠 제어"),
    ]
    
    print("\n🚀 고급 구문 테스트 (개발 중인 기능들)")
    print("-" * 60)
    
    for i, (script_code, description) in enumerate(advanced_cases, 1):
        print(f"\n📝 고급 테스트 {i}: {description}")
        print(f"   스크립트: {script_code}")
        
        try:
            tokens = lexer.tokenize(script_code)
            ast = parser.parse(script_code)
            print(f"   😲 예상외 성공! (개발이 완료된 것 같습니다)")
        except ParseError as e:
            print(f"   ⚠️ 예상된 실패: {e}")
        except Exception as e:
            print(f"   ⚠️ 예상된 오류: {e}")
    
    print("-" * 60)

def create_test_validation_request():
    """API 검증 요청을 위한 JSON 생성"""
    
    # 성공할 것으로 예상되는 스크립트들
    working_scripts = [
        "W,A,S,D",           # 순차 실행
        "W+A+S+D",           # 동시 실행  
        "Space*5",           # 반복
        "Attack&100",        # 연속 입력
        "~CapsLock",         # 토글
        "Shift[2000]",       # 홀드
        "Ctrl+C",            # 조합키
        "W+A,S+D",           # 복합 구문
    ]
    
    print("\n🔧 API 검증 테스트용 스크립트들:")
    print("-" * 40)
    
    for i, script in enumerate(working_scripts, 1):
        print(f"{i}. {script}")
    
    print(f"\n💡 추천: WPF 앱에서 위 스크립트들을 하나씩 테스트해보세요!")
    print("   이 스크립트들은 현재 파서에서 성공적으로 처리됩니다.")

if __name__ == "__main__":
    try:
        # 기본 구문 테스트
        success, total = test_basic_scripts()
        
        # 고급 구문 테스트
        test_advanced_scripts()
        
        # API 테스트용 스크립트 추천
        create_test_validation_request()
        
        print(f"\n🎯 전체 요약:")
        print(f"   • 기본 구문 지원: {success}/{total} ({success/total*100:.1f}%)")
        print(f"   • 고급 구문: 개발 진행 중")
        print(f"   • 권장 사항: 기본 구문부터 테스트하세요!")
        
    except Exception as e:
        print(f"💥 테스트 실행 중 오류: {e}")
        sys.exit(1) 