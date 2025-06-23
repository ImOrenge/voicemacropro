"""
GPT-4o 트랜스크립션 서비스 테스트
Phase 1-1 검증용 테스트 스크립트
"""

import asyncio
import os
import logging
from unittest.mock import MagicMock, patch
import sys
import json

# 프로젝트 루트 디렉토리를 Python 경로에 추가 (수정)
project_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, project_root)
sys.path.insert(0, os.path.join(project_root, 'backend'))

# import 오류 해결을 위한 직접 import
try:
    from backend.services.gpt4o_transcription_service import GPT4oTranscriptionService
    from backend.utils.config import Config
except ImportError:
    try:
        # 대안 경로로 시도
        sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
        from services.gpt4o_transcription_service import GPT4oTranscriptionService
        from utils.config import Config
    except ImportError as e:
        print(f"❌ 모듈 import 실패: {e}")
        print("📁 현재 작업 디렉토리:", os.getcwd())
        print("🐍 Python 경로:", sys.path[:3])
        sys.exit(1)

# 로깅 설정
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)


class GPT4oServiceTester:
    """GPT-4o 서비스 테스트 클래스"""
    
    def __init__(self):
        """테스터 초기화"""
        self.test_results = []
        self.api_key = os.getenv('OPENAI_API_KEY')
        
        # API 키 확인
        if not self.api_key:
            print("⚠️ OPENAI_API_KEY 환경변수가 설정되지 않았습니다.")
            print("🔧 설정 방법:")
            print("   PowerShell: $env:OPENAI_API_KEY = 'your_api_key_here'")
            print("   또는 .env 파일에 OPENAI_API_KEY=your_api_key_here 추가")
        
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
    
    async def test_service_initialization(self):
        """서비스 초기화 테스트"""
        try:
            # API 키 없이 초기화 테스트
            service = GPT4oTranscriptionService("")
            self.log_test_result("서비스 초기화 (API키 없음)", True, "객체 생성 성공")
            
            # API 키와 함께 초기화 테스트
            if self.api_key:
                service_with_key = GPT4oTranscriptionService(self.api_key)
                self.log_test_result("서비스 초기화 (API키 있음)", True, "객체 생성 성공")
            else:
                self.log_test_result("서비스 초기화 (API키 있음)", False, "OPENAI_API_KEY 환경변수 없음")
                
        except Exception as e:
            self.log_test_result("서비스 초기화", False, f"예외 발생: {e}")
    
    async def test_gaming_prompt_generation(self):
        """게임 최적화 프롬프트 생성 테스트"""
        try:
            service = GPT4oTranscriptionService("dummy_key")
            prompt = service._get_gaming_optimized_prompt()
            
            # 프롬프트에 필수 키워드가 포함되어 있는지 확인
            required_keywords = ["공격", "스킬", "이동", "아이템", "방어"]
            missing_keywords = [kw for kw in required_keywords if kw not in prompt]
            
            if not missing_keywords:
                self.log_test_result("게임 프롬프트 생성", True, "모든 필수 키워드 포함")
            else:
                self.log_test_result("게임 프롬프트 생성", False, f"누락된 키워드: {missing_keywords}")
                
        except Exception as e:
            self.log_test_result("게임 프롬프트 생성", False, f"예외 발생: {e}")
    
    async def test_session_config_generation(self):
        """세션 설정 생성 테스트"""
        try:
            service = GPT4oTranscriptionService("dummy_key")
            config = service.session_config
            
            # 필수 설정 항목 확인
            required_fields = [
                "type", "input_audio_format", "input_audio_transcription",
                "turn_detection", "input_audio_noise_reduction", "include"
            ]
            
            missing_fields = [field for field in required_fields if field not in config]
            
            if not missing_fields:
                self.log_test_result("세션 설정 생성", True, "모든 필수 필드 포함")
            else:
                self.log_test_result("세션 설정 생성", False, f"누락된 필드: {missing_fields}")
                
            # 모델 설정 확인
            transcription_config = config.get("input_audio_transcription", {})
            if transcription_config.get("model") == "gpt-4o-transcribe":
                self.log_test_result("모델 설정", True, "올바른 모델 설정")
            else:
                self.log_test_result("모델 설정", False, f"잘못된 모델: {transcription_config.get('model')}")
                
        except Exception as e:
            self.log_test_result("세션 설정 생성", False, f"예외 발생: {e}")
    
    async def test_confidence_calculation(self):
        """신뢰도 계산 테스트"""
        try:
            service = GPT4oTranscriptionService("dummy_key")
            
            # 다양한 logprobs 테스트 케이스
            test_cases = [
                ([], 0.0, "빈 배열"),
                ([-1, -2, -3], 0.25, "음수 로그 확률"),
                ([0, -0.5, -1], 0.625, "혼합 로그 확률"),
                ([None, -1, None], 0.5, "None 값 포함")
            ]
            
            for logprobs, expected, description in test_cases:
                result = service._calculate_confidence(logprobs)
                # 부동소수점 비교를 위한 허용 오차
                if abs(result - expected) < 0.1:
                    self.log_test_result(f"신뢰도 계산 ({description})", True, f"결과: {result:.2f}")
                else:
                    self.log_test_result(f"신뢰도 계산 ({description})", False, f"예상: {expected}, 실제: {result}")
                    
        except Exception as e:
            self.log_test_result("신뢰도 계산", False, f"예외 발생: {e}")
    
    async def test_callback_setting(self):
        """콜백 함수 설정 테스트"""
        try:
            service = GPT4oTranscriptionService("dummy_key")
            
            # 콜백 함수 정의
            async def test_callback(data):
                pass
            
            # 콜백 설정
            service.set_transcription_callback(test_callback)
            
            if service.transcription_callback == test_callback:
                self.log_test_result("콜백 함수 설정", True, "콜백 함수 정상 설정")
            else:
                self.log_test_result("콜백 함수 설정", False, "콜백 함수 설정 실패")
                
        except Exception as e:
            self.log_test_result("콜백 함수 설정", False, f"예외 발생: {e}")
    
    async def test_websocket_connection_simulation(self):
        """WebSocket 연결 시뮬레이션 테스트"""
        try:
            if not self.api_key:
                self.log_test_result("WebSocket 연결 시뮬레이션", False, "API 키 없음 - 실제 연결 불가")
                return
            
            service = GPT4oTranscriptionService(self.api_key)
            
            # 실제 연결은 API 키가 유효한 경우에만 테스트
            # 여기서는 연결 메서드의 구조만 확인
            if hasattr(service, 'connect') and callable(service.connect):
                self.log_test_result("WebSocket 연결 메서드", True, "연결 메서드 존재")
            else:
                self.log_test_result("WebSocket 연결 메서드", False, "연결 메서드 없음")
                
        except Exception as e:
            self.log_test_result("WebSocket 연결 시뮬레이션", False, f"예외 발생: {e}")
    
    async def test_actual_websocket_connection(self):
        """실제 WebSocket 연결 테스트 (API 키가 있는 경우)"""
        if not self.api_key:
            self.log_test_result("실제 WebSocket 연결", False, "API 키 없음")
            return
            
        try:
            service = GPT4oTranscriptionService(self.api_key)
            
            print("🔄 GPT-4o API 연결 시도 중...")
            connected = await service.connect()
            
            if connected:
                self.log_test_result("실제 WebSocket 연결", True, "연결 성공")
                await service.disconnect()
            else:
                self.log_test_result("실제 WebSocket 연결", False, "연결 실패")
                
        except Exception as e:
            self.log_test_result("실제 WebSocket 연결", False, f"연결 예외: {e}")
    
    async def run_all_tests(self):
        """모든 테스트 실행"""
        print("🚀 GPT-4o 트랜스크립션 서비스 테스트 시작")
        print("=" * 60)
        
        # 각 테스트 실행
        await self.test_service_initialization()
        await self.test_gaming_prompt_generation()
        await self.test_session_config_generation()
        await self.test_confidence_calculation()
        await self.test_callback_setting()
        await self.test_websocket_connection_simulation()
        
        # API 키가 있는 경우 실제 연결 테스트
        if self.api_key:
            await self.test_actual_websocket_connection()
        
        # 결과 요약
        print("\n" + "=" * 60)
        print("📊 테스트 결과 요약")
        print("=" * 60)
        
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
        
        # Phase 1-1 완료 상태 확인
        critical_tests = [
            "서비스 초기화 (API키 없음)",
            "게임 프롬프트 생성", 
            "세션 설정 생성",
            "신뢰도 계산",
            "콜백 함수 설정"
        ]
        
        critical_passed = sum(1 for result in self.test_results 
                            if result['test'] in critical_tests and result['success'])
        
        print(f"\n🎯 Phase 1-1 핵심 기능: {critical_passed}/{len(critical_tests)} 완료")
        
        if critical_passed == len(critical_tests):
            print("🎉 Phase 1-1 검증 완료! 다음 단계로 진행 가능합니다.")
            return True
        else:
            print("⚠️  Phase 1-1 검증 미완료. 실패한 테스트를 수정 후 재실행해주세요.")
            return False


async def main():
    """메인 테스트 실행 함수"""
    print("🔧 환경 설정 확인")
    print("-" * 40)
    
    # 환경변수 확인
    api_key = os.getenv('OPENAI_API_KEY')
    if api_key:
        print(f"✅ OPENAI_API_KEY 설정됨 (길이: {len(api_key)}자)")
    else:
        print("❌ OPENAI_API_KEY 환경변수가 설정되지 않음")
        print("🔧 설정 방법:")
        print("   PowerShell: $env:OPENAI_API_KEY = 'your_api_key_here'")
    
    print(f"📁 현재 작업 디렉토리: {os.getcwd()}")
    print()
    
    tester = GPT4oServiceTester()
    success = await tester.run_all_tests()
    
    if success:
        print("\n🔥 Phase 1-1 완료!")
        print("📋 다음 단계: Phase 1-2 (음성 서비스 통합)")
    else:
        print("\n🔧 수정 필요한 항목들을 해결하고 다시 테스트해주세요.")


if __name__ == "__main__":
    try:
        # Config 검증 (가능한 경우)
        if 'Config' in globals():
            Config.validate_config()
    except Exception as e:
        print(f"⚠️ Config 검증 오류: {e}")
    
    # 테스트 실행
    asyncio.run(main()) 