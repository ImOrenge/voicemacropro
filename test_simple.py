from msl_lexer import MSLLexer
from msl_parser import MSLParser, ParseError

lexer = MSLLexer()
parser = MSLParser()

test_script = 'Q(100)W(150)E(200)R'
print(f'테스트: {test_script}')

try:
    tokens = lexer.tokenize(test_script)
    print('토큰화 성공')
    
    # 토큰 확인
    for token in tokens[:10]:  # 처음 10개만
        print(f'  {token.type.value}: {token.value}')
    
    ast = parser.parse(test_script)
    print('파싱 성공')
except ParseError as e:
    print(f'파싱 오류: {e}')
except Exception as e:
    print(f'예외: {e}') 