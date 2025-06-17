#!/usr/bin/env python3
"""
VoiceMacro Pro - 음성 녹음 기능 테스트 스크립트
1단계: 실시간 음성 녹음 기능 테스트
"""

import time
import sys
import requests
from voice_recognition_service import get_voice_recognition_service

def test_voice_recording_service():
    """음성 인식 서비스 직접 테스트"""
    print("🎤 === 음성 녹음 서비스 직접 테스트 ===")
    
    try:
        # 1. 서비스 초기화
        print("1. 음성 인식 서비스 초기화 중...")
        voice_service = get_voice_recognition_service()
        print("✅ 서비스 초기화 완료")
        
        # 2. 사용 가능한 마이크 장치 확인
        print("\n2. 사용 가능한 마이크 장치 확인...")
        devices = voice_service.get_available_devices()
        print(f"✅ 총 {len(devices)}개의 마이크 장치 발견:")
        for device in devices:
            print(f"   - ID: {device['id']}, 이름: {device['name']}")
        
        if not devices:
            print("❌ 사용 가능한 마이크 장치가 없습니다!")
            return False
        
        # 3. 마이크 테스트
        print("\n3. 마이크 동작 테스트 중...")
        print("   (마이크에 말을 해보세요 - 2초간 테스트)")
        test_result = voice_service.test_microphone()
        
        if test_result['success']:
            print("✅ 마이크 테스트 성공!")
            print(f"   - 장치 사용 가능: {test_result['device_available']}")
            print(f"   - 녹음 기능: {test_result['recording_test']}")
            print(f"   - 음성 레벨 감지: {test_result['audio_level_detected']}")
        else:
            print(f"❌ 마이크 테스트 실패: {test_result.get('error_message', '알 수 없는 오류')}")
            return False
        
        # 4. 실시간 녹음 테스트
        print("\n4. 실시간 녹음 기능 테스트...")
        
        # 음성 레벨 콜백 설정
        def level_callback(level):
            # 간단한 음성 레벨 표시 (너무 자주 출력되지 않도록 제한)
            bar_length = int(level * 20)
            bar = "█" * bar_length + "░" * (20 - bar_length)
            print(f"\r   음성 레벨: [{bar}] {level:.2f}", end="", flush=True)
        
        voice_service.set_audio_level_callback(level_callback)
        
        # 녹음 시작
        print("\n   녹음 시작... (5초간 말을 해보세요)")
        if voice_service.start_recording():
            print("✅ 녹음 시작 성공")
            
            # 5초간 녹음 상태 모니터링
            for i in range(5):
                time.sleep(1)
                status = voice_service.get_recording_status()
                print(f"\n   {i+1}초 경과 - 큐 크기: {status['queue_size']}")
            
            # 오디오 데이터 수집 테스트
            print("\n   오디오 데이터 수집 테스트...")
            audio_data = voice_service.get_audio_data(1.0)  # 1초간 데이터
            if audio_data is not None:
                print(f"✅ 오디오 데이터 수집 성공: {len(audio_data)} 샘플")
            else:
                print("⚠️ 오디오 데이터 수집 실패")
            
            # 녹음 중지
            print("\n   녹음 중지...")
            if voice_service.stop_recording():
                print("✅ 녹음 중지 성공")
            else:
                print("❌ 녹음 중지 실패")
        else:
            print("❌ 녹음 시작 실패")
            return False
        
        print("\n🎉 모든 테스트 완료!")
        return True
        
    except Exception as e:
        print(f"❌ 테스트 중 오류 발생: {e}")
        return False

def test_voice_api_endpoints():
    """API 엔드포인트 테스트"""
    print("\n🌐 === API 엔드포인트 테스트 ===")
    
    base_url = "http://localhost:5000"
    
    try:
        # 1. 헬스 체크
        print("1. 서버 상태 확인...")
        response = requests.get(f"{base_url}/api/health", timeout=5)
        if response.status_code == 200:
            print("✅ 서버 정상 동작")
        else:
            print(f"❌ 서버 응답 오류: {response.status_code}")
            return False
        
        # 2. 마이크 장치 목록 조회
        print("\n2. 마이크 장치 목록 API 테스트...")
        response = requests.get(f"{base_url}/api/voice/devices", timeout=5)
        if response.status_code == 200:
            data = response.json()
            print(f"✅ 장치 목록 조회 성공: {len(data['data'])}개 장치")
            for device in data['data']:
                print(f"   - {device['name']} (ID: {device['id']})")
        else:
            print(f"❌ 장치 목록 조회 실패: {response.status_code}")
            return False
        
        # 3. 음성 녹음 상태 확인
        print("\n3. 음성 녹음 상태 API 테스트...")
        response = requests.get(f"{base_url}/api/voice/status", timeout=5)
        if response.status_code == 200:
            data = response.json()
            status = data['data']
            print(f"✅ 상태 조회 성공:")
            print(f"   - 녹음 중: {status['is_recording']}")
            print(f"   - 현재 장치: {status['current_device']}")
            print(f"   - 샘플레이트: {status['sample_rate']}Hz")
        else:
            print(f"❌ 상태 조회 실패: {response.status_code}")
            return False
        
        # 4. 마이크 테스트 API
        print("\n4. 마이크 테스트 API...")
        print("   (마이크에 말을 해보세요 - 2초간 테스트)")
        response = requests.post(f"{base_url}/api/voice/test", timeout=10)
        if response.status_code == 200:
            data = response.json()
            result = data['data']
            print(f"✅ 마이크 테스트 API 성공:")
            print(f"   - 전체 테스트: {result['success']}")
            print(f"   - 장치 사용 가능: {result['device_available']}")
            print(f"   - 녹음 기능: {result['recording_test']}")
            print(f"   - 음성 감지: {result['audio_level_detected']}")
        else:
            data = response.json()
            print(f"❌ 마이크 테스트 API 실패: {data.get('message', '알 수 없는 오류')}")
            return False
        
        print("\n🎉 API 테스트 완료!")
        return True
        
    except requests.exceptions.ConnectionError:
        print("❌ 서버에 연결할 수 없습니다. API 서버가 실행 중인지 확인하세요.")
        return False
    except Exception as e:
        print(f"❌ API 테스트 중 오류 발생: {e}")
        return False

def main():
    """메인 테스트 함수"""
    print("🎮 VoiceMacro Pro - 음성 녹음 기능 테스트")
    print("=" * 50)
    
    # 직접 서비스 테스트
    print("\n📍 1단계: 실시간 음성 녹음 기능 테스트")
    service_test_success = test_voice_recording_service()
    
    if not service_test_success:
        print("\n❌ 기본 서비스 테스트 실패. API 테스트를 건너뜁니다.")
        return False
    
    # API 엔드포인트 테스트
    print("\n📍 2단계: API 엔드포인트 테스트")
    print("   (API 서버를 먼저 실행해주세요: python api_server.py)")
    input("   준비되면 Enter를 눌러주세요...")
    
    api_test_success = test_voice_api_endpoints()
    
    # 결과 요약
    print("\n" + "=" * 50)
    print("📊 테스트 결과 요약:")
    print(f"   - 음성 서비스 테스트: {'✅ 성공' if service_test_success else '❌ 실패'}")
    print(f"   - API 엔드포인트 테스트: {'✅ 성공' if api_test_success else '❌ 실패'}")
    
    if service_test_success and api_test_success:
        print("\n🎉 1단계 실시간 음성 녹음 기능 구현 완료!")
        print("   다음 단계: 2단계 - OpenAI Whisper 음성 분석 기능")
        return True
    else:
        print("\n❌ 일부 테스트 실패. 문제를 해결한 후 다시 시도하세요.")
        return False

if __name__ == "__main__":
    """
    테스트 스크립트 실행
    
    실행 방법:
    1. 필요한 패키지 설치: pip install -r requirements.txt
    2. 이 스크립트 실행: python test_voice_recording.py
    3. API 테스트를 위해 별도 터미널에서 API 서버 실행: python api_server.py
    """
    try:
        success = main()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⏹️ 사용자에 의해 테스트가 중단되었습니다.")
        sys.exit(0)
    except Exception as e:
        print(f"\n❌ 예상치 못한 오류 발생: {e}")
        sys.exit(1) 