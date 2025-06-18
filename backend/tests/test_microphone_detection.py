#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
마이크 감지 및 테스트 진단 스크립트 (안전 버전)
"""

import sounddevice as sd
import numpy as np
import time
import sys

def test_microphone_detection():
    """마이크 감지 및 기본 테스트"""
    print("🎤 마이크 감지 및 테스트 시작...")
    print("=" * 60)
    
    try:
        # 1. sounddevice 버전 확인
        print(f"📦 sounddevice 버전: {sd.__version__}")
        
        # 2. 기본 설정 확인
        print(f"🔧 기본 샘플레이트: {sd.default.samplerate}")
        
        try:
            default_device = sd.default.device
            if default_device:
                print(f"🔧 기본 입력 장치: {default_device[0] if len(default_device) > 0 else 'None'}")
                print(f"🔧 기본 출력 장치: {default_device[1] if len(default_device) > 1 else 'None'}")
            else:
                print("🔧 기본 장치: None")
        except:
            print("🔧 기본 장치 정보를 가져올 수 없습니다.")
        
        print()
        
        # 3. 모든 오디오 장치 목록 조회 (안전한 방법)
        print("🎵 감지된 모든 오디오 장치:")
        print("-" * 60)
        
        try:
            devices = sd.query_devices()
            input_devices = []
            
            for i, device in enumerate(devices):
                try:
                    # 안전한 방법으로 장치 정보 접근
                    device_name = device.get('name', 'Unknown Device')
                    max_inputs = device.get('max_inputs', 0)
                    max_outputs = device.get('max_outputs', 0)
                    default_samplerate = device.get('default_samplerate', 0)
                    hostapi = device.get('hostapi', 0)
                    
                    device_type = []
                    if max_inputs > 0:
                        device_type.append("🎤입력")
                        input_devices.append((i, device))
                    if max_outputs > 0:
                        device_type.append("🔊출력")
                        
                    print(f"[{i:2d}] {device_name}")
                    print(f"     타입: {', '.join(device_type) if device_type else '알 수 없음'}")
                    print(f"     채널: 입력 {max_inputs}, 출력 {max_outputs}")
                    print(f"     샘플레이트: {default_samplerate} Hz")
                    print(f"     호스트 API: {hostapi}")
                    print()
                    
                except Exception as e:
                    print(f"[{i:2d}] 장치 정보 읽기 오류: {e}")
                    print()
                    
        except Exception as e:
            print(f"❌ 장치 목록 조회 실패: {e}")
            print("\n대안으로 직접 녹음 테스트를 시도합니다...")
            return test_direct_recording()
        
        if not input_devices:
            print("❌ 입력 장치(마이크)가 감지되지 않았습니다!")
            print("\n🔧 해결 방법:")
            print("1. 마이크가 제대로 연결되어 있는지 확인")
            print("2. Windows 설정 > 개인정보 > 마이크 > 앱에서 마이크 액세스 허용")
            print("3. Windows 설정 > 시스템 > 사운드 > 입력 장치 확인")
            print("4. 마이크 드라이버 재설치")
            print("\n대안으로 직접 녹음 테스트를 시도합니다...")
            return test_direct_recording()
        
        print(f"✅ {len(input_devices)}개의 입력 장치가 감지되었습니다!")
        
        # 4. 기본 입력 장치로 간단한 녹음 테스트
        return test_recording_with_device(input_devices[0] if input_devices else None)
        
    except Exception as e:
        print(f"❌ 마이크 감지 오류: {e}")
        print("대안으로 직접 녹음 테스트를 시도합니다...")
        return test_direct_recording()

def test_direct_recording():
    """직접 녹음 테스트 (장치 목록 없이)"""
    print("\n🧪 직접 녹음 테스트 중...")
    
    try:
        # 2초간 짧은 녹음 테스트
        duration = 2  # 초
        sample_rate = 16000  # Hz
        
        print(f"⏺️  {duration}초간 녹음 시작... (소리를 내주세요)")
        
        # 녹음 실행 (기본 장치 사용)
        audio_data = sd.rec(int(duration * sample_rate), 
                          samplerate=sample_rate, 
                          channels=1, 
                          dtype='float64')
        sd.wait()  # 녹음 완료까지 대기
        
        # 볼륨 레벨 분석
        max_amplitude = np.max(np.abs(audio_data))
        rms_amplitude = np.sqrt(np.mean(audio_data**2))
        
        print(f"✅ 녹음 완료!")
        print(f"📊 최대 진폭: {max_amplitude:.4f}")
        print(f"📊 RMS 진폭: {rms_amplitude:.4f}")
        
        if max_amplitude > 0.001:
            print("🎉 마이크가 정상적으로 작동합니다!")
            
            # 볼륨 레벨에 따른 상태 표시
            if max_amplitude > 0.1:
                print("🔊 볼륨 레벨: 높음")
            elif max_amplitude > 0.01:
                print("🔉 볼륨 레벨: 보통")
            else:
                print("🔈 볼륨 레벨: 낮음 (마이크 볼륨을 높여보세요)")
                
            return True
                
        else:
            print("⚠️  마이크에서 소리가 감지되지 않았습니다.")
            print("🔧 해결 방법:")
            print("- 마이크 볼륨 설정 확인")
            print("- 마이크 음소거 해제")
            print("- Windows 사운드 설정에서 기본 입력 장치 변경")
            return False
            
    except Exception as e:
        print(f"❌ 직접 녹음 테스트 실패: {e}")
        print("\n🔧 가능한 원인:")
        print("- 마이크 사용 권한 거부")
        print("- 다른 앱에서 마이크 사용 중")
        print("- 오디오 드라이버 문제")
        print("- sounddevice 라이브러리 문제")
        return False

def test_recording_with_device(device_info):
    """특정 장치로 녹음 테스트"""
    if not device_info:
        return test_direct_recording()
        
    device_id, device = device_info
    print(f"\n🧪 장치 [{device_id}] 테스트 중: {device.get('name', 'Unknown')}")
    
    try:
        # 2초간 짧은 녹음 테스트
        duration = 2  # 초
        sample_rate = 16000  # Hz
        
        print(f"⏺️  {duration}초간 녹음 시작... (소리를 내주세요)")
        
        # 특정 장치로 녹음 실행
        audio_data = sd.rec(int(duration * sample_rate), 
                          samplerate=sample_rate, 
                          channels=1, 
                          dtype='float64',
                          device=device_id)
        sd.wait()  # 녹음 완료까지 대기
        
        # 볼륨 레벨 분석
        max_amplitude = np.max(np.abs(audio_data))
        rms_amplitude = np.sqrt(np.mean(audio_data**2))
        
        print(f"✅ 녹음 완료!")
        print(f"📊 최대 진폭: {max_amplitude:.4f}")
        print(f"📊 RMS 진폭: {rms_amplitude:.4f}")
        
        if max_amplitude > 0.001:
            print("🎉 마이크가 정상적으로 작동합니다!")
            
            # 볼륨 레벨에 따른 상태 표시
            if max_amplitude > 0.1:
                print("🔊 볼륨 레벨: 높음")
            elif max_amplitude > 0.01:
                print("🔉 볼륨 레벨: 보통")
            else:
                print("🔈 볼륨 레벨: 낮음 (마이크 볼륨을 높여보세요)")
                
            # 샘플레이트 테스트
            print(f"\n🔬 장치 상세 테스트:")
            test_rates = [8000, 16000, 22050, 44100, 48000]
            supported_rates = []
            
            for rate in test_rates:
                try:
                    sd.check_input_settings(device=device_id, 
                                          channels=1, 
                                          samplerate=rate)
                    supported_rates.append(rate)
                    print(f"✅ {rate} Hz 지원")
                except Exception:
                    print(f"❌ {rate} Hz 미지원")
            
            if supported_rates:
                print(f"\n🎯 권장 샘플레이트: {supported_rates[0]} Hz")
            
            return True
                
        else:
            print("⚠️  마이크에서 소리가 감지되지 않았습니다.")
            print("🔧 해결 방법:")
            print("- 마이크 볼륨 설정 확인")
            print("- 마이크 음소거 해제")
            print("- 다른 입력 장치 선택")
            return False
            
    except Exception as e:
        print(f"❌ 장치 녹음 테스트 실패: {e}")
        print("기본 장치로 다시 시도합니다...")
        return test_direct_recording()

def test_permissions():
    """Windows 마이크 권한 테스트"""
    print("🔐 Windows 마이크 권한 확인...")
    
    try:
        # 간단한 녹음으로 권한 테스트
        test_data = sd.rec(100, samplerate=16000, channels=1)  # 아주 짧은 녹음
        sd.wait()
        print("✅ 마이크 권한이 허용되어 있습니다.")
        return True
    except Exception as e:
        print(f"❌ 마이크 권한 문제: {e}")
        print("\n🔧 해결 방법:")
        print("1. Windows 설정 열기 (Win + I)")
        print("2. 개인 정보 보호 > 마이크")
        print("3. '앱에서 마이크에 액세스하도록 허용' 켜기")
        print("4. '데스크톱 앱에서 마이크에 액세스하도록 허용' 켜기")
        print("5. Python 또는 PowerShell에 대한 권한 확인")
        return False

if __name__ == "__main__":
    print("🎤 VoiceMacro Pro 마이크 진단 도구 (안전 버전)")
    print("=" * 60)
    
    # 권한 확인
    permission_ok = test_permissions()
    
    if not permission_ok:
        print("\n⚠️  먼저 마이크 권한을 허용해주세요.")
        input("권한을 허용한 후 Enter를 눌러 계속하세요...")
    
    # 마이크 감지 테스트
    success = test_microphone_detection()
    
    if success:
        print("\n🎉 마이크 진단이 완료되었습니다!")
        print("이제 VoiceMacro Pro에서 음성 인식을 사용할 수 있습니다.")
    else:
        print("\n❌ 마이크 문제가 감지되었습니다.")
        print("Windows 사운드 설정을 확인하고 다시 시도해주세요.")
    
    print("\n" + "=" * 60)
    input("진단을 종료하려면 Enter를 누르세요...") 