#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
API 서버의 마이크 장치 목록 테스트 스크립트
"""

import requests
import json
import time

def test_microphone_api():
    """API 서버의 마이크 장치 관련 엔드포인트 테스트"""
    base_url = "http://localhost:5000"
    
    print("🎤 VoiceMacro Pro 마이크 API 테스트")
    print("=" * 60)
    
    # 1. 서버 연결 확인
    print("1. 서버 연결 확인...")
    try:
        response = requests.get(f"{base_url}/api/health", timeout=5)
        if response.status_code == 200:
            print("✅ 서버 연결 성공!")
        else:
            print(f"❌ 서버 상태 오류: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ 서버 연결 실패: {e}")
        return False
    
    # 2. 마이크 장치 목록 조회
    print("\n2. 마이크 장치 목록 조회...")
    try:
        response = requests.get(f"{base_url}/api/voice/devices", timeout=10)
        
        if response.status_code == 200:
            data = response.json()
            
            if data.get('success'):
                devices = data.get('data', [])
                print(f"✅ 장치 목록 조회 성공: {len(devices)}개 장치")
                print(f"📋 메시지: {data.get('message')}")
                
                if devices:
                    print("\n📱 감지된 마이크 장치들:")
                    print("-" * 40)
                    for i, device in enumerate(devices, 1):
                        print(f"{i:2d}. ID: {device.get('id')}")
                        print(f"    이름: {device.get('name')}")
                        print(f"    채널: {device.get('max_input_channels')}개")
                        print(f"    샘플레이트: {device.get('default_samplerate')} Hz")
                        print()
                else:
                    print("⚠️  장치가 감지되지 않았습니다.")
                    
                return True
            else:
                print(f"❌ API 오류: {data.get('message')}")
                return False
        else:
            print(f"❌ HTTP 오류: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"❌ 장치 목록 조회 실패: {e}")
        return False
    
    # 3. 마이크 테스트
    print("\n3. 마이크 테스트...")
    try:
        response = requests.post(f"{base_url}/api/voice/test", timeout=15)
        
        if response.status_code == 200:
            data = response.json()
            
            if data.get('success'):
                test_result = data.get('data', {})
                print("✅ 마이크 테스트 성공!")
                print(f"📋 메시지: {data.get('message')}")
                print(f"🎤 장치 사용 가능: {test_result.get('device_available')}")
                print(f"🎵 녹음 테스트: {test_result.get('recording_test')}")
                print(f"📊 오디오 레벨 감지: {test_result.get('audio_level_detected')}")
                return True
            else:
                print(f"❌ 마이크 테스트 실패: {data.get('message')}")
                return False
        else:
            print(f"❌ HTTP 오류: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"❌ 마이크 테스트 실패: {e}")
        return False

if __name__ == "__main__":
    # 서버가 시작될 때까지 잠시 대기
    print("⏳ API 서버 시작 대기 중... (3초)")
    time.sleep(3)
    
    success = test_microphone_api()
    
    if success:
        print("\n🎉 모든 테스트가 성공했습니다!")
        print("이제 C# 애플리케이션에서 마이크 목록이 표시될 것입니다.")
    else:
        print("\n❌ 테스트에 실패했습니다.")
        print("API 서버나 마이크 설정을 확인해주세요.")
    
    print("\n" + "=" * 60) 