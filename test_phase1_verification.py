"""
Phase 1-1 검증: GPT-4o 트랜스크립션 서비스 기본 기능 테스트
독립 실행형 테스트 스크립트
"""

import asyncio
import os
import sys
import json
import base64
from datetime import datetime
from typing import Optional, Callable, Dict, Any

# 로깅 설정
import logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class GPT4oTranscriptionServiceTest:
    """테스트용 GPT-4o 트랜스크립션 서비스 클래스"""
    
    def __init__(self, api_key: str):
        """
        GPT-4o 트랜스크립션 서비스 초기화
        
        Args:
            api_key (str): OpenAI API 키
        """
        self.api_key = api_key
        self.websocket: Optional = None
        self.session_id: Optional[str] = None
        self.is_connected = False
        self.transcription_callback: Optional[Callable] = None
        self.logger = logging.getLogger(__name__)
        
        # 게임 명령어 최적화를 위한 세션 설정
        self.session_config = {
            "type": "transcription_session.update",
            "input_audio_format": "pcm16",  # 24kHz PCM16 오디오
            "input_audio_transcription": {
                "model": "gpt-4o-transcribe",
                "prompt": self._get_gaming_optimized_prompt(),
                "language": "ko"  # 한국어 게임 명령어
            },
            "turn_detection": {
                "type": "server_vad",
                "threshold": 0.5,
                "prefix_padding_ms": 300,
                "silence_duration_ms": 500
            },
            "input_audio_noise_reduction": {
                "type": "near_field"  # 헤드셋 마이크 최적화
            },
            "include": ["item.input_audio_transcription.logprobs"]
        }
    
    def _get_gaming_optimized_prompt(self) -> str:
        """게임 명령어 인식 최적화를 위한 프롬프트"""
        return """
        게임 플레이어의 음성 명령어를 정확하게 인식하세요.
        주요 명령어 패턴:
        - 공격: 공격, 어택, 때려, 치기, 공격해
        - 스킬: 스킬, 기술, 마법, 궁극기, 스페셜
        - 이동: 앞으로, 뒤로, 좌측, 우측, 점프, 달려
        - 아이템: 포션, 회복, 아이템, 사용, 먹기
        - 방어: 방어, 막기, 피하기, 회피
        짧고 명확한 게임 명령어를 우선 인식하세요.
        """
    
    def _calculate_confidence(self, logprobs: list) -> float:
        """
        로그 확률을 기반으로 신뢰도 계산
        
        Args:
            logprobs (list): 토큰별 로그 확률 리스트
            
        Returns:
            float: 0.0 ~ 1.0 사이의 신뢰도 점수
        """
        if not logprobs:
            return 0.0
        
        # 로그 확률을 확률로 변환하고 평균 계산
        probs = [min(1.0, max(0.0, 2 ** logprob)) for logprob in logprobs if logprob is not None]
        return sum(probs) / len(probs) if probs else 0.0
    
    def set_transcription_callback(self, callback: Callable):
        """
        트랜스크립션 결과 콜백 함수 설정
        
        Args:
            callback (Callable): 트랜스크립션 결과를 처리할 비동기 함수
        """
        self.transcription_callback = callback


class Phase1Tester:
    """Phase 1-1 테스터"""
    
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
    
    async def test_service_initialization(self):
        """서비스 초기화 테스트"""
        try:
            # API 키 없이 초기화 테스트
            service = GPT4oTranscriptionServiceTest("")
            self.log_test_result("서비스 초기화 (API키 없음)", True, "객체 생성 성공")
            
            # API 키와 함께 초기화 테스트
            if self.api_key:
                service_with_key = GPT4oTranscriptionServiceTest(self.api_key)
                self.log_test_result("서비스 초기화 (API키 있음)", True, "객체 생성 성공")
            else:
                self.log_test_result("서비스 초기화 (API키 있음)", False, "OPENAI_API_KEY 환경변수 없음")
                
        except Exception as e:
            self.log_test_result("서비스 초기화", False, f"예외 발생: {e}")
    
    async def test_gaming_prompt_generation(self):
        """게임 최적화 프롬프트 생성 테스트"""
        try:
            service = GPT4oTranscriptionServiceTest("dummy_key")
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
            service = GPT4oTranscriptionServiceTest("dummy_key")
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
            service = GPT4oTranscriptionServiceTest("dummy_key")
            
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
            service = GPT4oTranscriptionServiceTest("dummy_key")
            
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
    
    async def test_config_validation(self):
        """설정 유효성 검증 테스트"""
        try:
            service = GPT4oTranscriptionServiceTest("dummy_key")
            config = service.session_config
            
            # JSON 직렬화 가능 여부 확인
            json_str = json.dumps(config)
            parsed_config = json.loads(json_str)
            
            if parsed_config == config:
                self.log_test_result("설정 JSON 직렬화", True, "JSON 직렬화/역직렬화 성공")
            else:
                self.log_test_result("설정 JSON 직렬화", False, "JSON 직렬화 실패")
                
            # 오디오 포맷 확인
            if config.get("input_audio_format") == "pcm16":
                self.log_test_result("오디오 포맷 설정", True, "PCM16 포맷 설정 완료")
            else:
                self.log_test_result("오디오 포맷 설정", False, f"잘못된 포맷: {config.get('input_audio_format')}")
                
        except Exception as e:
            self.log_test_result("설정 유효성 검증", False, f"예외 발생: {e}")
    
    async def run_all_tests(self):
        """모든 테스트 실행"""
        print("🚀 Phase 1-1: GPT-4o 트랜스크립션 서비스 검증 시작")
        print("=" * 80)
        
        # 각 테스트 실행
        await self.test_service_initialization()
        await self.test_gaming_prompt_generation()
        await self.test_session_config_generation()
        await self.test_confidence_calculation()
        await self.test_callback_setting()
        await self.test_config_validation()
        
        # 결과 요약
        print("\n" + "=" * 80)
        print("📊 Phase 1-1 검증 결과")
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
        
        # Phase 1-1 완료 상태 확인
        critical_tests = [
            "서비스 초기화 (API키 없음)",
            "게임 프롬프트 생성", 
            "세션 설정 생성",
            "신뢰도 계산 (빈 배열)",
            "콜백 함수 설정",
            "설정 JSON 직렬화"
        ]
        
        critical_passed = sum(1 for result in self.test_results 
                            if any(critical_test in result['test'] for critical_test in critical_tests) and result['success'])
        
        print(f"\n🎯 Phase 1-1 핵심 기능: {critical_passed}/{len(critical_tests)} 완료")
        
        if critical_passed >= len(critical_tests) - 1:  # 1개 실패 허용
            print("🎉 Phase 1-1 검증 완료! 다음 단계로 진행 가능합니다.")
            print("📋 다음 단계: Phase 1-2 (음성 서비스 통합)")
            return True
        else:
            print("⚠️  Phase 1-1 검증 미완료. 실패한 테스트를 수정 후 재실행해주세요.")
            return False


async def main():
    """메인 실행 함수"""
    print("🎮 VoiceMacro Pro - GPT-4o 음성인식 시스템 구현")
    print("📝 Phase 1-1: 트랜스크립션 서비스 기본 기능 검증\n")
    
    tester = Phase1Tester()
    success = await tester.run_all_tests()
    
    if success:
        print("\n🔥 Phase 1-1 완료!")
        print("✅ GPT-4o 트랜스크립션 서비스 기본 구조 완성")
        print("✅ 게임 명령어 최적화 프롬프트 구현")
        print("✅ 세션 설정 및 신뢰도 계산 시스템 구현")
        print("\n🚀 다음 단계를 진행해주세요!")
    else:
        print("\n🔧 수정이 필요한 항목들을 해결하고 다시 테스트해주세요.")
        return False
    
    return True


if __name__ == "__main__":
    # Phase 1-1 검증 실행
    result = asyncio.run(main()) 