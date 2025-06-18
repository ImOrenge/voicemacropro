#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
API 서버의 스크립트 검증 엔드포인트 테스트
"""

import requests
import json
import time

def test_api_validation():
    """API 서버의 스크립트 검증을 직접 테스트"""
    
    # API 서버 URL
    base_url = "http://localhost:5000"
    validate_url = f"{base_url}/api/scripts/validate"
    
    # 테스트할 스크립트
    test_script = "W+A,S+D"
    
    print(f"🧪 API 스크립트 검증 테스트")
    print(f"URL: {validate_url}")
    print(f"스크립트: {test_script}")
    print("-" * 50)
    
    # 요청 데이터
    request_data = {
        "script_code": test_script
    }
    
    print(f"📤 요청 데이터: {json.dumps(request_data, ensure_ascii=False)}")
    
    try:
        # API 서버가 준비될 시간을 줌
        print("⏳ API 서버 연결 대기 중...")
        time.sleep(2)
        
        # API 요청 전송
        response = requests.post(
            validate_url, 
            json=request_data,
            headers={'Content-Type': 'application/json'},
            timeout=10
        )
        
        print(f"📥 응답 상태 코드: {response.status_code}")
        print(f"📥 응답 헤더: {dict(response.headers)}")
        
        # 응답 데이터 파싱
        try:
            response_data = response.json()
            print(f"📥 응답 데이터: {json.dumps(response_data, ensure_ascii=False, indent=2)}")
            
            # 검증 결과 분석
            if response_data.get('success'):
                validation_data = response_data.get('data', {})
                print(f"\n✅ API 검증 성공!")
                print(f"  - 유효성: {'✅ 성공' if validation_data.get('valid') else '❌ 실패'}")
                print(f"  - 오류: {validation_data.get('error', 'None')}")
                print(f"  - AST 노드 수: {validation_data.get('ast_nodes', 0)}")
                print(f"  - 예상 실행시간: {validation_data.get('estimated_execution_time', 0)}ms")
                
                if validation_data.get('suggestions'):
                    print(f"  - 제안사항:")
                    for suggestion in validation_data['suggestions']:
                        print(f"    • {suggestion}")
            else:
                print(f"\n❌ API 검증 실패!")
                print(f"  - 오류: {response_data.get('error', 'Unknown')}")
                print(f"  - 메시지: {response_data.get('message', 'No message')}")
                
        except json.JSONDecodeError as e:
            print(f"❌ JSON 파싱 실패: {e}")
            print(f"원본 응답: {response.text}")
            
    except requests.exceptions.ConnectionError:
        print(f"❌ API 서버 연결 실패: {validate_url}")
        print("API 서버가 실행 중인지 확인하세요.")
    except requests.exceptions.Timeout:
        print(f"❌ API 요청 타임아웃")
    except Exception as e:
        print(f"❌ 예상치 못한 오류: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    test_api_validation() 