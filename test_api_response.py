#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
API 응답 구조 확인 테스트 스크립트
"""

import requests
import json

def test_api_response():
    """API 응답 구조 확인"""
    try:
        # 매크로 목록 API 호출
        response = requests.get('http://localhost:5000/api/macros')
        
        if response.status_code == 200:
            print("✅ API 응답 성공!")
            print(f"📊 응답 크기: {len(response.content)} bytes")
            
            # JSON 파싱
            data = response.json()
            print("\n📋 응답 구조:")
            print(f"- success: {data.get('success')}")
            print(f"- message: {data.get('message')}")
            print(f"- data 타입: {type(data.get('data'))}")
            
            if data.get('data') and len(data['data']) > 0:
                print(f"- 매크로 개수: {len(data['data'])}")
                
                # 첫 번째 매크로 상세 정보
                first_macro = data['data'][0]
                print(f"\n🎮 첫 번째 매크로 구조:")
                for key, value in first_macro.items():
                    print(f"  - {key}: {value} ({type(value).__name__})")
                
                # JSON 문자열로 예쁘게 출력
                print(f"\n📄 첫 번째 매크로 JSON:")
                print(json.dumps(first_macro, indent=2, ensure_ascii=False))
            else:
                print("❌ 매크로 데이터가 없습니다.")
                
        else:
            print(f"❌ API 요청 실패: {response.status_code}")
            print(f"응답 내용: {response.text}")
            
    except Exception as e:
        print(f"❌ 오류 발생: {e}")

if __name__ == "__main__":
    test_api_response() 