#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
C# 애플리케이션 API 호출 시뮬레이션 테스트
"""

import requests
import json
import time
from datetime import datetime

def test_csharp_api_simulation():
    """C# VoiceRecognitionWrapperService 동작 시뮬레이션"""
    
    base_url = "http://localhost:5000"
    
    print("🧪 C# VoiceRecognitionWrapperService API 호출 시뮬레이션")
    print("=" * 70)
    
    # C# HttpClient 동작 시뮬레이션
    session = requests.Session()
    session.headers.update({
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'User-Agent': 'VoiceMacroPro-CSharp-Client/1.0'
    })
    
    # 1. GetAvailableDevicesAsync() 시뮬레이션
    print("\n1. 🎤 GetAvailableDevicesAsync() 시뮬레이션")
    print("-" * 50)
    
    try:
        print(f"📞 호출: GET {base_url}/api/voice/devices")
        
        start_time = time.time()
        response = session.get(f"{base_url}/api/voice/devices", timeout=10)
        end_time = time.time()
        
        print(f"⏱️  응답 시간: {(end_time - start_time) * 1000:.0f}ms")
        print(f"📊 HTTP 상태: {response.status_code}")
        print(f"📏 응답 크기: {len(response.content)} bytes")
        print(f"🔤 Content-Type: {response.headers.get('Content-Type', 'N/A')}")
        
        if response.status_code == 200:
            try:
                json_data = response.json()
                print("✅ JSON 파싱 성공")
                
                # C# ApiResponse<List<MicrophoneDevice>> 구조 검증
                success = json_data.get('success', False)
                data = json_data.get('data', [])
                message = json_data.get('message', '')
                
                print(f"🔍 success: {success}")
                print(f"🔍 message: '{message}'")
                print(f"🔍 data 타입: {type(data)}")
                print(f"🔍 data 길이: {len(data) if isinstance(data, list) else 'N/A'}")
                
                if success and isinstance(data, list) and len(data) > 0:
                    print(f"\n✅ C# 역직렬화 성공 예상: {len(data)}개 장치")
                    
                    # MicrophoneDevice 객체 매핑 시뮬레이션
                    print("\n🔧 C# MicrophoneDevice 객체 생성 시뮬레이션:")
                    for i, device_data in enumerate(data[:3]):  # 처음 3개만
                        try:
                            device_id = device_data.get('id')
                            device_name = device_data.get('name', '')
                            max_input_channels = device_data.get('max_input_channels', 0)
                            default_samplerate = device_data.get('default_samplerate', 0.0)
                            
                            print(f"  [{i+1}] MicrophoneDevice {{")
                            print(f"         Id: {device_id}")
                            print(f"         Name: \"{device_name}\"")
                            print(f"         MaxInputChannels: {max_input_channels}")
                            print(f"         DefaultSampleRate: {default_samplerate}")
                            print(f"       }}")
                            
                        except Exception as mapping_ex:
                            print(f"  ❌ 매핑 오류: {mapping_ex}")
                    
                    if len(data) > 3:
                        print(f"       ... 외 {len(data) - 3}개")
                        
                    # ComboBoxItem 생성 시뮬레이션
                    print(f"\n📋 ComboBoxItem 생성 시뮬레이션:")
                    for i, device_data in enumerate(data[:5]):  # 처음 5개만
                        device_name = device_data.get('name', '알 수 없는 장치')
                        device_id = device_data.get('id', -1)
                        print(f"  ComboBoxItem {{ Content: \"{device_name}\", Tag: {device_id} }}")
                    
                    if len(data) > 5:
                        print(f"  ... 외 {len(data) - 5}개")
                        
                else:
                    print("❌ C# 역직렬화 실패 예상: success=false 또는 빈 데이터")
                    
            except json.JSONDecodeError as json_ex:
                print(f"❌ JSON 파싱 실패: {json_ex}")
                print(f"📄 원본 응답: {response.text[:200]}...")
                
        else:
            print(f"❌ HTTP 오류: {response.status_code}")
            print(f"📄 오류 내용: {response.text}")
            
    except requests.exceptions.RequestException as req_ex:
        print(f"❌ 네트워크 오류: {req_ex}")
    except Exception as ex:
        print(f"❌ 예상치 못한 오류: {ex}")
    
    # 2. 연결 테스트 시뮬레이션
    print(f"\n2. 🔗 연결 상태 테스트")
    print("-" * 50)
    
    endpoints_to_test = [
        '/api/health',
        '/api/voice/devices',
        '/api/voice/status',
        '/api/macros'
    ]
    
    for endpoint in endpoints_to_test:
        try:
            url = f"{base_url}{endpoint}"
            start_time = time.time()
            response = session.get(url, timeout=5)
            end_time = time.time()
            
            status_icon = "✅" if response.status_code == 200 else "❌"
            print(f"{status_icon} {endpoint}: {response.status_code} ({(end_time - start_time) * 1000:.0f}ms)")
            
        except Exception as ex:
            print(f"❌ {endpoint}: 연결 실패 - {type(ex).__name__}")
    
    print(f"\n⏰ 테스트 완료: {datetime.now().strftime('%H:%M:%S')}")
    print("=" * 70)

if __name__ == "__main__":
    test_csharp_api_simulation() 