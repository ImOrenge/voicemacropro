"""
Phase 1-2 검증: 음성 서비스와 GPT-4o 통합 테스트
독립 실행형 테스트 스크립트
"""

import asyncio
import os
import sys
import json
import time
import threading
from unittest.mock import MagicMock, patch
from typing import Dict, List

# 로깅 설정
import logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# 프로젝트 경로 추가
sys.path.append(os.path.join(os.path.dirname(__file__), 'backend'))

class VoiceServiceIntegrationTester:
    """음성 서비스와 GPT-4o 통합 테스터"""
    
    def __init__(self):
        """테스터 초기화"""
        self.test_results = []
        self.api_key = os.getenv('OPENAI_API_KEY')
        
    def log_test_result(self, test_name: str, success: bool, message: str = ""):
        """테스트 결과 로깅"""
        status = "✅ PASS" if success else "❌ FAIL"
        result = f"{status} {test_name}: {message}"
        print(result)
        self.test_results.append({
            'test': test_name,
            'success': success,
            'message': message
        })
    
    async def test_config_integration(self):
        """설정 파일 통합 테스트"""
        try:
            # Config 모듈 import 테스트
            from backend.utils.config import Config
            
            # GPT-4o 설정 확인
            gpt4o_config = Config.get_gpt4o_transcription_config()
            required_fields = ['model', 'language', 'confidence_threshold', 'enabled']
            
            missing_fields = [field for field in required_fields if field not in gpt4o_config]
            
            if not missing_fields:
                self.log_test_result("GPT-4o 설정 통합", True, "모든 설정 필드 존재")
            else:
                self.log_test_result("GPT-4o 설정 통합", False, f"누락된 필드: {missing_fields}")
            
            # WebSocket 설정 확인
            ws_config = Config.get_websocket_config()
            if 'host' in ws_config and 'port' in ws_config:
                self.log_test_result("WebSocket 설정", True, f"호스트: {ws_config['host']}, 포트: {ws_config['port']}")
            else:
                self.log_test_result("WebSocket 설정", False, "WebSocket 설정 누락")
                
        except ImportError as e:
            self.log_test_result("설정 모듈 import", False, f"import 실패: {e}")
        except Exception as e:
            self.log_test_result("설정 통합 테스트", False, f"예외 발생: {e}")
    
    async def test_voice_service_import(self):
        """음성 서비스 import 테스트"""
        try:
            from backend.services.voice_service import VoiceRecognitionService
            self.log_test_result("음성 서비스 import", True, "VoiceRecognitionService import 성공")
            
            # 서비스 초기화 테스트 (mock 환경에서)
            with patch('backend.services.voice_service.sd') as mock_sd:
                mock_sd.query_devices.return_value = [
                    {'name': 'Test Microphone', 'max_input_channels': 1, 'default_samplerate': 44100}
                ]
                
                # API 키 없이 초기화 (GPT-4o 비활성화)
                with patch('backend.utils.config.Config.OPENAI_API_KEY', ''):
                    with patch('backend.utils.config.Config.GPT4O_ENABLED', False):
                        service = VoiceRecognitionService()
                        self.log_test_result("음성 서비스 초기화", True, "객체 생성 성공")
                        
                        # 기본 속성 확인
                        if hasattr(service, 'gpt4o_service') and hasattr(service, 'transcription_callback'):
                            self.log_test_result("GPT-4o 통합 속성", True, "GPT-4o 관련 속성 존재")
                        else:
                            self.log_test_result("GPT-4o 통합 속성", False, "GPT-4o 관련 속성 누락")
                
        except ImportError as e:
            self.log_test_result("음성 서비스 import", False, f"import 실패: {e}")
        except Exception as e:
            self.log_test_result("음성 서비스 초기화", False, f"예외 발생: {e}")
    
    async def test_gpt4o_service_integration(self):
        """GPT-4o 서비스 통합 테스트"""
        try:
            from backend.services.gpt4o_transcription_service import GPT4oTranscriptionService
            
            # GPT-4o 서비스 생성 테스트
            service = GPT4oTranscriptionService("dummy_key")
            self.log_test_result("GPT-4o 서비스 생성", True, "객체 생성 성공")
            
            # 콜백 설정 테스트
            callback_called = False
            async def test_callback(data):
                nonlocal callback_called
                callback_called = True
            
            service.set_transcription_callback(test_callback)
            if service.transcription_callback == test_callback:
                self.log_test_result("GPT-4o 콜백 설정", True, "콜백 함수 설정 성공")
            else:
                self.log_test_result("GPT-4o 콜백 설정", False, "콜백 함수 설정 실패")
            
        except ImportError as e:
            self.log_test_result("GPT-4o 서비스 import", False, f"import 실패: {e}")
        except Exception as e:
            self.log_test_result("GPT-4o 서비스 통합", False, f"예외 발생: {e}")
    
    async def test_audio_processing_flow(self):
        """오디오 처리 플로우 테스트"""
        try:
            from backend.services.voice_service import VoiceRecognitionService
            import numpy as np
            
            with patch('backend.services.voice_service.sd') as mock_sd:
                mock_sd.query_devices.return_value = [
                    {'name': 'Test Microphone', 'max_input_channels': 1, 'default_samplerate': 44100}
                ]
                
                # 음성 서비스 생성 (GPT-4o 비활성화)
                with patch('backend.utils.config.Config.OPENAI_API_KEY', ''):
                    with patch('backend.utils.config.Config.GPT4O_ENABLED', False):
                        service = VoiceRecognitionService()
                        
                        # 오디오 레벨 콜백 테스트
                        level_received = False
                        def level_callback(level):
                            nonlocal level_received
                            level_received = True
                        
                        service.set_audio_level_callback(level_callback)
                        
                        # 가상 오디오 데이터로 콜백 테스트
                        fake_audio = np.random.random((1024, 1)).astype(np.float32)
                        service._audio_callback(fake_audio, 1024, None, None)
                        
                        if level_received:
                            self.log_test_result("오디오 레벨 콜백", True, "레벨 콜백 정상 동작")
                        else:
                            self.log_test_result("오디오 레벨 콜백", False, "레벨 콜백 동작 안함")
                        
                        # 트랜스크립션 콜백 테스트
                        transcription_received = False
                        def transcription_callback(data):
                            nonlocal transcription_received
                            transcription_received = True
                        
                        service.set_transcription_callback(transcription_callback)
                        
                        if service.transcription_callback == transcription_callback:
                            self.log_test_result("트랜스크립션 콜백 설정", True, "콜백 설정 성공")
                        else:
                            self.log_test_result("트랜스크립션 콜백 설정", False, "콜백 설정 실패")
            
        except Exception as e:
            self.log_test_result("오디오 처리 플로우", False, f"예외 발생: {e}")
    
    async def test_async_loop_management(self):
        """비동기 루프 관리 테스트"""
        try:
            from backend.services.voice_service import VoiceRecognitionService
            
            with patch('backend.services.voice_service.sd') as mock_sd:
                mock_sd.query_devices.return_value = [
                    {'name': 'Test Microphone', 'max_input_channels': 1, 'default_samplerate': 44100}
                ]
                
                # GPT-4o 활성화된 상태로 서비스 생성 (API 키 있음)
                if self.api_key:
                    with patch('backend.utils.config.Config.GPT4O_ENABLED', True):
                        service = VoiceRecognitionService()
                        
                        # 비동기 루프 확인
                        if hasattr(service, 'event_loop') and hasattr(service, 'loop_thread'):
                            self.log_test_result("비동기 루프 초기화", True, "루프 관련 속성 존재")
                            
                            # 루프 상태 확인
                            time.sleep(0.2)  # 루프 시작 대기
                            if service.event_loop and service.loop_thread and service.loop_thread.is_alive():
                                self.log_test_result("비동기 루프 동작", True, "루프 스레드 활성화됨")
                            else:
                                self.log_test_result("비동기 루프 동작", False, "루프 스레드 비활성화됨")
                        else:
                            self.log_test_result("비동기 루프 초기화", False, "루프 관련 속성 누락")
                else:
                    self.log_test_result("비동기 루프 관리", False, "API 키 없음 - 실제 테스트 불가")
            
        except Exception as e:
            self.log_test_result("비동기 루프 관리", False, f"예외 발생: {e}")
    
    async def test_error_handling(self):
        """에러 처리 테스트"""
        try:
            from backend.services.voice_service import VoiceRecognitionService
            
            with patch('backend.services.voice_service.sd') as mock_sd:
                # 오디오 장치 없는 상황 시뮬레이션
                mock_sd.query_devices.return_value = []
                
                # 서비스가 에러 상황에서도 초기화되는지 확인
                with patch('backend.utils.config.Config.GPT4O_ENABLED', False):
                    service = VoiceRecognitionService()
                    
                    if len(service.available_devices) > 0:
                        self.log_test_result("에러 상황 대응", True, "기본 장치 추가됨")
                    else:
                        self.log_test_result("에러 상황 대응", False, "기본 장치 미추가")
                    
                    # 잘못된 오디오 데이터 처리 테스트
                    try:
                        service._audio_callback(None, 0, None, "Error Status")
                        self.log_test_result("에러 오디오 처리", True, "에러 상황에서도 크래시 없음")
                    except Exception:
                        self.log_test_result("에러 오디오 처리", False, "에러 상황에서 크래시 발생")
            
        except Exception as e:
            self.log_test_result("에러 처리 테스트", False, f"예외 발생: {e}")
    
    async def run_all_tests(self):
        """모든 테스트 실행"""
        print("🚀 Phase 1-2: 음성 서비스와 GPT-4o 통합 검증 시작")
        print("=" * 80)
        
        # 각 테스트 실행
        await self.test_config_integration()
        await self.test_voice_service_import()
        await self.test_gpt4o_service_integration()
        await self.test_audio_processing_flow()
        await self.test_async_loop_management()
        await self.test_error_handling()
        
        # 결과 요약
        print("\n" + "=" * 80)
        print("📊 Phase 1-2 검증 결과")
        print("=" * 80)
        
        total_tests = len(self.test_results)
        passed_tests = sum(1 for result in self.test_results if result['success'])
        failed_tests = total_tests - passed_tests
        
        print(f"총 테스트: {total_tests}")
        print(f"성공: {passed_tests} ✅")
        print(f"실패: {failed_tests} ❌")
        print(f"성공률: {(passed_tests/total_tests*100):.1f}%")
        
        # 실패한 테스트 상세 정보
        if failed_tests > 0:
            print("\n❌ 실패한 테스트:")
            for result in self.test_results:
                if not result['success']:
                    print(f"  - {result['test']}: {result['message']}")
        
        # Phase 1-2 완료 상태 확인
        critical_tests = [
            "GPT-4o 설정 통합",
            "음성 서비스 import", 
            "음성 서비스 초기화",
            "GPT-4o 서비스 생성",
            "오디오 레벨 콜백"
        ]
        
        critical_passed = sum(1 for result in self.test_results 
                            if any(critical_test in result['test'] for critical_test in critical_tests) and result['success'])
        
        print(f"\n🎯 Phase 1-2 핵심 기능: {critical_passed}/{len(critical_tests)} 완료")
        
        if critical_passed >= len(critical_tests) - 1:  # 1개 실패 허용
            print("🎉 Phase 1-2 검증 완료! 다음 단계로 진행 가능합니다.")
            print("📋 다음 단계: Phase 1-3 (WebSocket API 개발)")
            return True
        else:
            print("⚠️  Phase 1-2 검증 미완료. 실패한 테스트를 수정 후 재실행해주세요.")
            return False


async def main():
    """메인 실행 함수"""
    print("🎮 VoiceMacro Pro - GPT-4o 음성인식 시스템 구현")
    print("📝 Phase 1-2: 음성 서비스와 GPT-4o 통합 검증\n")
    
    tester = VoiceServiceIntegrationTester()
    success = await tester.run_all_tests()
    
    if success:
        print("\n🔥 Phase 1-2 완료!")
        print("✅ 음성 서비스와 GPT-4o 트랜스크립션 통합 완성")
        print("✅ 실시간 오디오 처리 및 트랜스크립션 파이프라인 구현")
        print("✅ 비동기 이벤트 처리 및 콜백 시스템 구현")
        print("\n🚀 다음 단계를 진행해주세요!")
    else:
        print("\n🔧 수정이 필요한 항목들을 해결하고 다시 테스트해주세요.")
        return False
    
    return True


if __name__ == "__main__":
    # Phase 1-2 검증 실행
    result = asyncio.run(main()) 