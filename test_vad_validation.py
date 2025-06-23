#!/usr/bin/env python3
"""
VoiceMacro Pro - Voice Activity Detection (VAD) 테스트 스크립트
마이크 권한 확인 및 VAD 로직 검증을 위한 테스트 도구
"""

import os
import sys
import time
import threading
import requests
import json
import base64
from datetime import datetime

# Backend 서비스 임포트 (경로 추가)
sys.path.append(os.path.join(os.path.dirname(__file__), '..'))

try:
    from backend.services.voice_service import get_voice_recognition_service
    from backend.api.server import validate_audio_chunk_backend
    from backend.utils.config import Config
except ImportError as e:
    print(f"❌ 백엔드 모듈 임포트 실패: {e}")
    print("   프로젝트 루트 디렉토리에서 실행하세요.")
    sys.exit(1)

class VADTester:
    """
    Voice Activity Detection 테스트 클래스
    """
    
    def __init__(self):
        self.voice_service = None
        self.server_url = "http://localhost:5000"
        self.test_results = {
            'microphone_permission': False,
            'device_detection': False,
            'audio_capture': False,
            'vad_client_logic': False,
            'vad_backend_logic': False,
            'end_to_end': False
        }
        
    def run_all_tests(self):
        """모든 VAD 관련 테스트 실행"""
        print("🔍 === Voice Activity Detection 검증 테스트 ===\n")
        
        # 1. 마이크 권한 및 장치 감지 테스트
        print("1️⃣ 마이크 권한 및 장치 감지 테스트")
        self.test_microphone_permission()
        
        # 2. 오디오 캡처 기능 테스트
        print("\n2️⃣ 오디오 캡처 기능 테스트")
        self.test_audio_capture()
        
        # 3. 백엔드 VAD 로직 테스트
        print("\n3️⃣ 백엔드 VAD 로직 테스트")
        self.test_backend_vad_logic()
        
        # 4. 서버 연결 테스트
        print("\n4️⃣ API 서버 연결 테스트")
        self.test_server_connection()
        
        # 5. 종합 결과 출력
        print("\n📊 === 테스트 결과 요약 ===")
        self.print_test_summary()
        
        # 6. 해결 방안 제시
        if not all(self.test_results.values()):
            print("\n🔧 === 문제 해결 방안 ===")
            self.suggest_solutions()
    
    def test_microphone_permission(self):
        """마이크 권한 및 장치 감지 테스트"""
        try:
            print("   마이크 장치 초기화 중...")
            self.voice_service = get_voice_recognition_service()
            
            # 사용 가능한 장치 확인
            devices = self.voice_service.get_available_devices()
            
            if devices:
                print(f"   ✅ {len(devices)}개의 마이크 장치 발견:")
                for i, device in enumerate(devices[:3]):  # 최대 3개만 표시
                    print(f"      - [{device['id']}] {device['name']}")
                if len(devices) > 3:
                    print(f"      ... 외 {len(devices) - 3}개 장치")
                
                self.test_results['device_detection'] = True
                
                # 마이크 테스트 실행
                print("   마이크 동작 테스트 중... (잠시 말씀해 주세요)")
                test_result = self.voice_service.test_microphone()
                
                if test_result['success']:
                    print("   ✅ 마이크 권한 및 동작 확인됨")
                    self.test_results['microphone_permission'] = True
                else:
                    print(f"   ❌ 마이크 테스트 실패: {test_result.get('error_message', '알 수 없는 오류')}")
                    self.print_microphone_troubleshooting()
            else:
                print("   ❌ 사용 가능한 마이크 장치를 찾을 수 없습니다")
                self.print_microphone_troubleshooting()
                
        except Exception as e:
            print(f"   ❌ 마이크 테스트 중 오류: {e}")
            self.print_microphone_troubleshooting()
    
    def test_audio_capture(self):
        """오디오 캡처 기능 테스트"""
        try:
            if not self.voice_service:
                print("   ❌ 음성 서비스가 초기화되지 않았습니다")
                return
            
            print("   5초간 녹음 테스트 시작... (말씀해 주세요)")
            
            # 녹음 시작
            if self.voice_service.start_recording():
                print("   🎤 녹음 중... (5초)")
                time.sleep(5)
                
                # 오디오 데이터 수집
                audio_data = self.voice_service.get_audio_data(2.0)
                
                # 녹음 중지
                self.voice_service.stop_recording()
                
                if audio_data is not None and len(audio_data) > 0:
                    # RMS 레벨 계산
                    import numpy as np
                    rms = np.sqrt(np.mean(audio_data ** 2))
                    
                    if rms > 0.001:  # 최소 임계값
                        print(f"   ✅ 오디오 캡처 성공 (RMS: {rms:.4f})")
                        self.test_results['audio_capture'] = True
                        
                        # VAD 시뮬레이션 테스트
                        self.test_client_vad_simulation(audio_data)
                    else:
                        print(f"   ❌ 오디오 레벨이 너무 낮음 (RMS: {rms:.4f})")
                        print("      마이크 볼륨을 확인하거나 더 크게 말해보세요")
                else:
                    print("   ❌ 오디오 데이터 수집 실패")
            else:
                print("   ❌ 녹음 시작 실패")
                
        except Exception as e:
            print(f"   ❌ 오디오 캡처 테스트 중 오류: {e}")
    
    def test_client_vad_simulation(self, audio_data):
        """클라이언트 VAD 로직 시뮬레이션"""
        try:
            import numpy as np
            
            # 16-bit PCM으로 변환 (C#과 동일한 방식)
            audio_16bit = (audio_data * 32767).astype(np.int16)
            audio_bytes = audio_16bit.tobytes()
            
            # 오디오 레벨 계산 (C#과 동일한 방식)
            samples = np.frombuffer(audio_bytes, dtype=np.int16)
            avg_amplitude = np.mean(np.abs(samples))
            audio_level = min(1.0, avg_amplitude / 32768.0)
            
            # VAD 임계값 테스트
            MIN_VOLUME_THRESHOLD = 0.02
            MAX_VOLUME_THRESHOLD = 0.95
            
            print(f"   📊 오디오 분석 결과:")
            print(f"      - 오디오 레벨: {audio_level:.3f}")
            print(f"      - 샘플 수: {len(samples)}")
            print(f"      - 데이터 크기: {len(audio_bytes)} bytes")
            
            # VAD 조건 확인
            vad_conditions = {
                'volume_above_min': audio_level >= MIN_VOLUME_THRESHOLD,
                'volume_below_max': audio_level <= MAX_VOLUME_THRESHOLD,
                'sufficient_data': len(audio_bytes) >= 960
            }
            
            print(f"   🔍 VAD 조건 검사:")
            for condition, result in vad_conditions.items():
                status = "✅" if result else "❌"
                print(f"      - {condition}: {status}")
            
            if all(vad_conditions.values()):
                print("   ✅ 클라이언트 VAD 로직 통과")
                self.test_results['vad_client_logic'] = True
            else:
                print("   ❌ 클라이언트 VAD 로직 실패")
                
        except Exception as e:
            print(f"   ❌ 클라이언트 VAD 시뮬레이션 오류: {e}")
    
    def test_backend_vad_logic(self):
        """백엔드 VAD 로직 테스트"""
        try:
            # 테스트 데이터 생성
            test_cases = [
                {
                    'name': '정상 음성 데이터',
                    'audio_level': 0.15,
                    'has_voice': True,
                    'data_size': 2400,  # 100ms at 24kHz
                    'expected': True
                },
                {
                    'name': '너무 낮은 볼륨',
                    'audio_level': 0.005,
                    'has_voice': True,
                    'data_size': 2400,
                    'expected': False
                },
                {
                    'name': '클라이언트 VAD 실패',
                    'audio_level': 0.15,
                    'has_voice': False,
                    'data_size': 2400,
                    'expected': False
                },
                {
                    'name': '데이터 크기 부족',
                    'audio_level': 0.15,
                    'has_voice': True,
                    'data_size': 100,
                    'expected': False
                }
            ]
            
            print("   백엔드 VAD 로직 검증:")
            all_passed = True
            
            for test_case in test_cases:
                # 테스트 오디오 데이터 생성
                import numpy as np
                
                # 16-bit PCM 데이터 생성
                samples = np.random.randint(-16384, 16384, size=test_case['data_size']//2, dtype=np.int16)
                
                # 원하는 레벨로 조정
                target_amplitude = test_case['audio_level'] * 32768
                current_amplitude = np.mean(np.abs(samples))
                if current_amplitude > 0:
                    samples = (samples * target_amplitude / current_amplitude).astype(np.int16)
                
                audio_bytes = samples.tobytes()
                
                # 백엔드 VAD 테스트
                result = validate_audio_chunk_backend(
                    audio_bytes, 
                    test_case['audio_level'], 
                    test_case['has_voice']
                )
                
                passed = result['is_valid'] == test_case['expected']
                status = "✅" if passed else "❌"
                
                print(f"      {status} {test_case['name']}: {result['reason']}")
                
                if not passed:
                    all_passed = False
                    print(f"         예상: {test_case['expected']}, 실제: {result['is_valid']}")
            
            if all_passed:
                print("   ✅ 백엔드 VAD 로직 모든 테스트 통과")
                self.test_results['vad_backend_logic'] = True
            else:
                print("   ❌ 백엔드 VAD 로직 일부 테스트 실패")
                
        except Exception as e:
            print(f"   ❌ 백엔드 VAD 테스트 중 오류: {e}")
    
    def test_server_connection(self):
        """서버 연결 및 API 테스트"""
        try:
            print("   Flask 서버 연결 테스트...")
            
            # 헬스 체크
            response = requests.get(f"{self.server_url}/api/health", timeout=5)
            
            if response.status_code == 200:
                print("   ✅ Flask 서버 연결 성공")
                
                # 마이크 장치 API 테스트
                devices_response = requests.get(f"{self.server_url}/api/voice/devices", timeout=5)
                if devices_response.status_code == 200:
                    print("   ✅ 음성 장치 API 동작 확인")
                    self.test_results['end_to_end'] = True
                else:
                    print(f"   ❌ 음성 장치 API 오류: {devices_response.status_code}")
            else:
                print(f"   ❌ Flask 서버 연결 실패: {response.status_code}")
                
        except requests.RequestException as e:
            print(f"   ❌ 서버 연결 오류: {e}")
            print("   서버가 실행 중인지 확인하세요: py run_server.py")
    
    def print_test_summary(self):
        """테스트 결과 요약 출력"""
        total_tests = len(self.test_results)
        passed_tests = sum(self.test_results.values())
        
        print(f"전체 테스트: {total_tests}개")
        print(f"통과한 테스트: {passed_tests}개")
        print(f"실패한 테스트: {total_tests - passed_tests}개")
        print()
        
        for test_name, result in self.test_results.items():
            status = "✅ 통과" if result else "❌ 실패"
            test_display = test_name.replace('_', ' ').title()
            print(f"- {test_display}: {status}")
        
        if passed_tests == total_tests:
            print("\n🎉 모든 테스트가 통과되었습니다! VAD 시스템이 정상 작동합니다.")
        else:
            print(f"\n⚠️ {total_tests - passed_tests}개의 테스트가 실패했습니다. 아래 해결 방안을 확인하세요.")
    
    def suggest_solutions(self):
        """문제 해결 방안 제시"""
        if not self.test_results['microphone_permission']:
            print("\n🎤 마이크 권한 문제:")
            print("   1. Windows 설정 > 개인정보 > 마이크에서 앱 액세스 허용")
            print("   2. 마이크가 올바르게 연결되어 있는지 확인")
            print("   3. 다른 프로그램에서 마이크를 사용하고 있지 않은지 확인")
            print("   4. 마이크 드라이버 업데이트")
        
        if not self.test_results['device_detection']:
            print("\n📱 장치 감지 문제:")
            print("   1. 마이크가 기본 녹음 장치로 설정되어 있는지 확인")
            print("   2. 제어판 > 사운드 > 녹음 탭에서 마이크 활성화")
            print("   3. USB 마이크인 경우 다른 포트에 연결 시도")
        
        if not self.test_results['audio_capture']:
            print("\n🔇 오디오 캡처 문제:")
            print("   1. 마이크 볼륨을 50% 이상으로 설정")
            print("   2. 마이크에 더 가까이 말하기")
            print("   3. 마이크 음소거 해제 확인")
            print("   4. 백그라운드 소음 최소화")
        
        if not self.test_results['vad_backend_logic']:
            print("\n🧠 VAD 로직 문제:")
            print("   1. 오디오 품질 확인 (잡음, 에코 제거)")
            print("   2. VAD 임계값 조정 고려")
            print("   3. 로그 파일에서 상세 오류 확인")
        
        if not self.test_results['end_to_end']:
            print("\n🌐 서버 연결 문제:")
            print("   1. Flask 서버 실행: py run_server.py")
            print("   2. 방화벽 설정 확인 (포트 5000)")
            print("   3. 백엔드 서비스 의존성 설치 확인")
    
    def print_microphone_troubleshooting(self):
        """마이크 문제 해결 가이드"""
        print("\n   🔧 마이크 문제 해결 방법:")
        print("      1. Windows 마이크 권한 확인:")
        print("         - 설정 > 개인정보 > 마이크")
        print("         - '앱이 마이크에 액세스하도록 허용' 활성화")
        print("      2. 마이크 장치 확인:")
        print("         - 제어판 > 사운드 > 녹음 탭")
        print("         - 마이크가 기본 장치로 설정되어 있는지 확인")
        print("      3. 다른 앱에서 마이크 사용 여부 확인")
        print("      4. 마이크 드라이버 재설치")


def main():
    """메인 실행 함수"""
    tester = VADTester()
    
    try:
        tester.run_all_tests()
    except KeyboardInterrupt:
        print("\n\n⏹️ 테스트가 사용자에 의해 중단되었습니다.")
    except Exception as e:
        print(f"\n❌ 테스트 실행 중 예상치 못한 오류: {e}")
    finally:
        print("\n테스트 완료.")


if __name__ == "__main__":
    main() 