#!/usr/bin/env python3
"""
VoiceMacro Pro - 기본 음성 녹음 기능 테스트 스크립트
1단계: 실시간 음성 녹음 기능 검증 (Windows 호환)
"""

import time
import sys
from voice_recognition_service_basic import get_voice_recognition_service_basic

def test_basic_voice_service():
    """기본 음성 인식 서비스 테스트"""
    print("🎤 === 기본 음성 녹음 서비스 테스트 ===")
    
    try:
        # 1. 서비스 초기화
        print("1. 음성 인식 서비스 초기화 중...")
        voice_service = get_voice_recognition_service_basic()
        print("✅ 서비스 초기화 완료")
        
        # 2. 사용 가능한 마이크 장치 확인
        print("\n2. 사용 가능한 마이크 장치 확인...")
        devices = voice_service.get_available_devices()
        print(f"✅ 총 {len(devices)}개의 마이크 장치 발견:")
        for device in devices:
            print(f"   - ID: {device['id']}, 이름: {device['name']}")
        
        # 3. 마이크 테스트
        print("\n3. 마이크 동작 테스트 중...")
        test_result = voice_service.test_microphone()
        
        if test_result['success']:
            print("✅ 마이크 테스트 성공!")
            print(f"   - 장치 사용 가능: {test_result['device_available']}")
            print(f"   - 녹음 기능: {test_result['recording_test']}")
            print(f"   - 음성 레벨 감지: {test_result['audio_level_detected']}")
            print(f"   - 모드: {test_result['mode']}")
        else:
            print(f"❌ 마이크 테스트 실패: {test_result.get('error_message', '알 수 없는 오류')}")
            return False
        
        # 4. 실시간 녹음 테스트
        print("\n4. 실시간 녹음 기능 테스트...")
        
        # 음성 레벨 콜백 설정
        level_display_count = 0
        def level_callback(level):
            nonlocal level_display_count
            level_display_count += 1
            # 너무 자주 출력되지 않도록 제한 (20회마다 한번)
            if level_display_count % 20 == 0:
                bar_length = int(level * 20)
                bar = "█" * bar_length + "░" * (20 - bar_length)
                print(f"\r   음성 레벨: [{bar}] {level:.2f}", end="", flush=True)
        
        voice_service.set_audio_level_callback(level_callback)
        
        # 녹음 시작
        print("\n   녹음 시작... (5초간 시뮬레이션)")
        if voice_service.start_recording():
            print("✅ 녹음 시작 성공")
            
            # 5초간 녹음 상태 모니터링
            for i in range(5):
                time.sleep(1)
                status = voice_service.get_recording_status()
                print(f"\n   {i+1}초 경과 - 모드: {status['mode']}, 장치: {status['current_device']}")
            
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
        
        # 5. 장치 변경 테스트
        print("\n5. 마이크 장치 변경 테스트...")
        if voice_service.set_device(1):  # 두 번째 장치로 변경
            print("✅ 장치 변경 성공")
            status = voice_service.get_recording_status()
            print(f"   현재 장치 ID: {status['current_device']}")
        else:
            print("⚠️ 장치 변경 실패")
        
        print("\n🎉 모든 기본 테스트 완료!")
        return True
        
    except Exception as e:
        print(f"❌ 테스트 중 오류 발생: {e}")
        import traceback
        traceback.print_exc()
        return False

def main():
    """메인 테스트 함수"""
    print("🎮 VoiceMacro Pro - 기본 음성 녹음 기능 테스트")
    print("=" * 50)
    
    # 기본 서비스 테스트
    print("\n📍 1단계: 실시간 음성 녹음 기능 테스트 (시뮬레이션)")
    service_test_success = test_basic_voice_service()
    
    # 결과 요약
    print("\n" + "=" * 50)
    print("📊 테스트 결과 요약:")
    print(f"   - 기본 음성 서비스 테스트: {'✅ 성공' if service_test_success else '❌ 실패'}")
    
    if service_test_success:
        print("\n🎉 1단계 기본 음성 녹음 기능 구현 완료!")
        print("   ✅ 마이크 장치 관리")
        print("   ✅ 음성 입력 레벨 모니터링")  
        print("   ✅ 백그라운드 녹음 기능")
        print("   ✅ 녹음 상태 관리")
        print("\n다음 단계: 2단계 - OpenAI Whisper 음성 분석 기능")
        return True
    else:
        print("\n❌ 기본 테스트 실패. 문제를 해결한 후 다시 시도하세요.")
        return False

if __name__ == "__main__":
    """
    테스트 스크립트 실행
    
    실행 방법:
    python test_basic_voice.py
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