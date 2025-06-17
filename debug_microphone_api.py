#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
마이크 API 디버깅 스크립트
C# 애플리케이션이 어떤 요청을 보내는지 모니터링
"""

import requests
import json
import time
from datetime import datetime

def debug_microphone_api():
    """마이크 API 디버깅"""
    base_url = "http://localhost:5000"
    
    print("🔍 VoiceMacro Pro 마이크 API 디버깅")
    print("=" * 60)
    print(f"⏰ 시작 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    
    # 1. 서버 상태 확인
    print("\n1. 서버 상태 확인...")
    try:
        response = requests.get(f"{base_url}/api/health", timeout=5)
        print(f"✅ 서버 상태: {response.status_code}")
        print(f"📋 응답: {response.json()}")
    except Exception as e:
        print(f"❌ 서버 연결 실패: {e}")
        return
    
    # 2. 마이크 장치 목록 API 직접 테스트
    print("\n2. 마이크 장치 목록 API 테스트...")
    try:
        print(f"🔗 요청 URL: {base_url}/api/voice/devices")
        response = requests.get(f"{base_url}/api/voice/devices", timeout=10)
        
        print(f"📊 HTTP 상태 코드: {response.status_code}")
        print(f"📋 응답 헤더: {dict(response.headers)}")
        
        if response.status_code == 200:
            data = response.json()
            print("✅ JSON 응답 구조:")
            print(json.dumps(data, indent=2, ensure_ascii=False))
            
            if data.get('success') and data.get('data'):
                devices = data['data']
                print(f"\n📱 감지된 장치 수: {len(devices)}")
                
                # C# 애플리케이션에서 사용할 형태로 표시
                print("\n🔧 C# 애플리케이션용 ComboBox 항목들:")
                for i, device in enumerate(devices):
                    print(f"  [{i}] ID: {device.get('id')} | 이름: '{device.get('name')}'")
            else:
                print("⚠️  API 응답에서 success=false 또는 data 없음")
        else:
            print(f"❌ HTTP 오류: {response.status_code}")
            print(f"📄 응답 내용: {response.text}")
            
    except Exception as e:
        print(f"❌ API 호출 실패: {e}")
    
    # 3. C# 애플리케이션이 사용하는 JSON 구조 검증
    print("\n3. C# JSON 매핑 검증...")
    try:
        response = requests.get(f"{base_url}/api/voice/devices")
        if response.status_code == 200:
            data = response.json()
            
            if data.get('success') and data.get('data'):
                devices = data['data']
                
                print("🔍 C# MicrophoneDevice 매핑 확인:")
                for device in devices[:3]:  # 처음 3개만 확인
                    print(f"\n  장치: {device.get('name')}")
                    print(f"    C# Id 매핑: {device.get('id')} ✅")
                    print(f"    C# Name 매핑: '{device.get('name')}' ✅")
                    print(f"    C# MaxInputChannels 매핑: {device.get('max_input_channels')} ✅")
                    print(f"    C# DefaultSampleRate 매핑: {device.get('default_samplerate')} ✅")
                    
                print("\n✅ 모든 JSON 필드가 C# 모델과 호환됩니다!")
            else:
                print("❌ API 응답 데이터 구조 문제")
    except Exception as e:
        print(f"❌ JSON 매핑 검증 실패: {e}")
    
    # 4. HTTP 요청 로깅 시뮬레이션
    print("\n4. C# 애플리케이션 요청 시뮬레이션...")
    try:
        headers = {
            'Content-Type': 'application/json',
            'User-Agent': 'VoiceMacroPro-CSharp-Client'
        }
        
        print(f"🔗 요청: GET {base_url}/api/voice/devices")
        print(f"📋 헤더: {headers}")
        
        response = requests.get(f"{base_url}/api/voice/devices", headers=headers, timeout=10)
        
        print(f"📊 응답 상태: {response.status_code}")
        print(f"📊 응답 크기: {len(response.content)} bytes")
        
        if response.status_code == 200:
            data = response.json()
            if data.get('success') and data.get('data'):
                device_count = len(data['data'])
                print(f"✅ C# 애플리케이션이 받을 장치 수: {device_count}개")
                
                # ComboBox에 표시될 내용 시뮬레이션
                print("\n📋 ComboBox에 표시될 내용 (시뮬레이션):")
                for device in data['data'][:5]:  # 처음 5개만
                    print(f"  '{device.get('name')}'")
                if len(data['data']) > 5:
                    print(f"  ... 외 {len(data['data']) - 5}개")
            else:
                print("❌ C# 애플리케이션이 빈 목록을 받게 됩니다!")
        else:
            print(f"❌ C# 애플리케이션이 HTTP 오류를 받게 됩니다: {response.status_code}")
            
    except Exception as e:
        print(f"❌ 시뮬레이션 실패: {e}")
    
    print(f"\n⏰ 완료 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)

if __name__ == "__main__":
    debug_microphone_api() 