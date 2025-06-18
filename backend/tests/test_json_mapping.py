#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
C# JSON 매핑 테스트 스크립트
ApiResponse<T> 클래스의 JsonPropertyName 매핑이 올바른지 확인
"""

import requests
import json
import sys
from datetime import datetime

def test_json_mapping():
    """C# JSON 매핑 테스트"""
    
    base_url = "http://localhost:5000"
    
    print("🧪 C# JSON 매핑 테스트")
    print("=" * 60)
    print(f"⏰ 시작 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    # 마이크 장치 API 테스트
    print("\n1. 🎤 마이크 장치 API 응답 구조 검증")
    print("-" * 40)
    
    try:
        response = requests.get(f"{base_url}/api/voice/devices", timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            
            print("✅ HTTP 200 OK")
            print(f"📋 응답 구조:")
            
            # C# ApiResponse<T> 매핑 필드 확인
            required_fields = ['success', 'data', 'message']
            optional_fields = ['error', 'timestamp']
            
            print("\n🔍 필수 필드 검증:")
            for field in required_fields:
                if field in data:
                    print(f"  ✅ {field}: {type(data[field]).__name__} = {data[field] if field != 'data' else f'List[{len(data[field])}]'}")
                else:
                    print(f"  ❌ {field}: 누락!")
                    
            print("\n🔍 선택적 필드 검증:")
            for field in optional_fields:
                if field in data:
                    print(f"  ✅ {field}: {type(data[field]).__name__} = {data[field]}")
                else:
                    print(f"  ⚪ {field}: 없음 (정상)")
            
            # C# 역직렬화 시뮬레이션
            if data.get('success') and isinstance(data.get('data'), list):
                devices = data['data']
                print(f"\n✅ C# 역직렬화 성공 예상:")
                print(f"  - ApiResponse.Success: {data['success']}")
                print(f"  - ApiResponse.Data: List<MicrophoneDevice>[{len(devices)}]")
                print(f"  - ApiResponse.Message: \"{data.get('message', '')}\"")
                
                if len(devices) > 0:
                    print(f"\n📱 첫 번째 마이크 장치 매핑:")
                    device = devices[0]
                    print(f"  - MicrophoneDevice.Id: {device.get('id')}")
                    print(f"  - MicrophoneDevice.Name: \"{device.get('name')}\"")
                    print(f"  - MicrophoneDevice.MaxInputChannels: {device.get('max_input_channels')}")
                    print(f"  - MicrophoneDevice.DefaultSampleRate: {device.get('default_samplerate')}")
                    
                    print("\n🎯 ComboBox 항목 생성 시뮬레이션:")
                    for i, dev in enumerate(devices[:3]):
                        print(f"  [{i}] \"{dev.get('name')}\" (ID: {dev.get('id')})")
                    
                    if len(devices) > 3:
                        print(f"  ... 외 {len(devices) - 3}개")
                        
                    print(f"\n🎉 예상 결과: ComboBox에 {len(devices)}개 마이크 장치 표시!")
                else:
                    print("\n❌ 마이크 장치 데이터가 비어있음")
            else:
                print(f"\n❌ C# 역직렬화 실패 예상:")
                print(f"  - success: {data.get('success')}")
                print(f"  - data 타입: {type(data.get('data'))}")
                
        else:
            print(f"❌ HTTP 오류: {response.status_code}")
            print(f"📄 응답: {response.text}")
            
    except Exception as ex:
        print(f"❌ 테스트 실패: {ex}")
        return False
    
    # 2. 다른 API 엔드포인트들도 테스트
    print(f"\n2. 🔗 다른 API 엔드포인트 JSON 구조 확인")
    print("-" * 40)
    
    test_endpoints = [
        ('/api/health', '서버 상태'),
        ('/api/voice/status', '음성 상태'),
        ('/api/macros', '매크로 목록')
    ]
    
    for endpoint, description in test_endpoints:
        try:
            response = requests.get(f"{base_url}{endpoint}", timeout=5)
            if response.status_code == 200:
                data = response.json()
                has_success = 'success' in data
                has_data = 'data' in data
                has_message = 'message' in data
                
                status = "✅" if (has_success and has_data and has_message) else "⚠️"
                print(f"{status} {endpoint} ({description}): JSON 구조 {'완전' if has_success and has_data and has_message else '부분적'}")
            else:
                print(f"❌ {endpoint}: HTTP {response.status_code}")
        except:
            print(f"❌ {endpoint}: 연결 실패")
    
    print(f"\n⏰ 완료 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    return True

if __name__ == "__main__":
    success = test_json_mapping()
    sys.exit(0 if success else 1) 