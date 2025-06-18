#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
특정 MSL 스크립트 검증 테스트
"""

import sys
import os

# 프로젝트 루트 디렉토리를 Python 경로에 추가
project_root = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, project_root)

from custom_script_service import CustomScriptService

def test_specific_script():
    """사용자가 테스트한 스크립트를 직접 검증"""
    
    service = CustomScriptService()
    
    # 테스트할 스크립트
    test_script = "W+A,S+D"
    
    print(f"🧪 MSL 스크립트 검증 테스트")
    print(f"스크립트: {test_script}")
    print("-" * 50)
    
    # 검증 실행
    result = service.validate_script(test_script)
    
    print(f"✅ 검증 결과:")
    print(f"  - 유효성: {'✅ 성공' if result['valid'] else '❌ 실패'}")
    print(f"  - 오류: {result.get('error', 'None')}")
    print(f"  - AST 노드 수: {result.get('ast_nodes', 0)}")
    print(f"  - 예상 실행시간: {result.get('estimated_execution_time', 0)}ms")
    
    if result.get('suggestions'):
        print(f"  - 제안사항:")
        for suggestion in result['suggestions']:
            print(f"    • {suggestion}")
    
    print("-" * 50)
    
    # 상세 디버깅 정보 출력
    print("🔍 상세 디버깅 정보:")
    
    # 직접 lexer와 parser 테스트
    try:
        from msl_lexer import MSLLexer
        from msl_parser import MSLParser
        
        # 1. 토큰화 테스트
        print("\n1. 토큰화 테스트:")
        lexer = MSLLexer()
        tokens = lexer.tokenize(test_script)
        
        for i, token in enumerate(tokens):
            print(f"  [{i}] {token.type.name}: '{token.value}' (line:{token.line}, col:{token.column})")
        
        # 2. 토큰 검증 테스트
        print("\n2. 토큰 검증 테스트:")
        token_errors = lexer.validate_tokens(tokens)
        if token_errors:
            print(f"  ❌ 토큰 오류: {token_errors}")
        else:
            print(f"  ✅ 토큰 검증 성공")
        
        # 3. 파싱 테스트
        print("\n3. 파싱 테스트:")
        parser = MSLParser()
        ast = parser.parse(test_script)
        print(f"  ✅ 파싱 성공: {type(ast).__name__}")
        print(f"  AST 구조: {ast}")
        
    except Exception as e:
        print(f"  ❌ 파싱 실패: {e}")
        import traceback
        print(f"  스택 트레이스:")
        traceback.print_exc()

if __name__ == "__main__":
    test_specific_script() 