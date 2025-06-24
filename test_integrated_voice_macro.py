"""
VoiceMacro Pro 통합 테스트 스크립트
GPT-4o 실시간 음성 인식 → 매크로 매칭 → 실행까지의 전체 파이프라인 테스트
"""

import asyncio
import time
import sys
import os

# 프로젝트 루트를 Python 경로에 추가
sys.path.insert(0, os.path.dirname(__file__))

from backend.services.gpt4o_transcription_service import GPT4oTranscriptionService
from backend.services.macro_matching_service import get_macro_matching_service
from backend.services.macro_service import macro_service
from backend.services.macro_execution_service import macro_execution_service
from backend.utils.config import Config


class IntegratedVoiceMacroTest:
    """통합 음성 매크로 테스트 클래스"""
    
    def __init__(self):
        """테스트 초기화"""
        self.gpt4o_service = None
        self.matching_service = get_macro_matching_service()
        
        # 테스트 결과 저장
        self.test_results = []
        
        print("🔧 VoiceMacro Pro 통합 테스트 초기화 중...")
    
    async def setup_services(self):
        """서비스 설정 및 초기화"""
        print("\n📋 1단계: 서비스 초기화")
        
        # GPT-4o 서비스 초기화 (API 키가 있는 경우에만)
        if Config.OPENAI_API_KEY and Config.GPT4O_ENABLED:
            try:
                self.gpt4o_service = GPT4oTranscriptionService(Config.OPENAI_API_KEY)
                self.gpt4o_service.set_transcription_callback(self.handle_transcription_result)
                
                # 연결 시도
                connected = await self.gpt4o_service.connect()
                if connected:
                    print("✅ GPT-4o 트랜스크립션 서비스 연결 성공")
                else:
                    print("⚠️ GPT-4o 서비스 연결 실패, 시뮬레이션 모드로 진행")
                    self.gpt4o_service = None
            except Exception as e:
                print(f"⚠️ GPT-4o 서비스 초기화 실패: {e}, 시뮬레이션 모드로 진행")
                self.gpt4o_service = None
        else:
            print("⚠️ GPT-4o API 키가 없어 시뮬레이션 모드로 진행")
            self.gpt4o_service = None
        
        # 매크로 데이터베이스 확인
        macros = macro_service.get_all_macros()
        print(f"📊 등록된 매크로: {len(macros)}개")
        
        if len(macros) < 5:
            print("⚠️ 테스트용 매크로가 부족합니다. 기본 매크로를 생성합니다.")
            await self.create_test_macros()
    
    async def create_test_macros(self):
        """테스트용 기본 매크로 생성"""
        test_macros = [
            {
                "name": "공격 매크로",
                "voice_command": "공격해",
                "action_type": "combo",
                "key_sequence": "Q,W,E",
                "settings": '{"delay": 100}'
            },
            {
                "name": "포션 사용",
                "voice_command": "포션 먹어",
                "action_type": "key",
                "key_sequence": "H",
                "settings": '{"hold_time": 0}'
            },
            {
                "name": "스킬 콤보",
                "voice_command": "스킬 사용",
                "action_type": "combo", 
                "key_sequence": "R,T,Y",
                "settings": '{"delay": 150}'
            },
            {
                "name": "점프 공격",
                "voice_command": "점프해서 공격",
                "action_type": "combo",
                "key_sequence": "Space,Q",
                "settings": '{"delay": 200}'
            },
            {
                "name": "회복 물약",
                "voice_command": "회복해",
                "action_type": "key",
                "key_sequence": "G",
                "settings": '{"hold_time": 0}'
            }
        ]
        
        for macro_data in test_macros:
            try:
                result = macro_service.create_macro(
                    name=macro_data["name"],
                    voice_command=macro_data["voice_command"],
                    action_type=macro_data["action_type"],
                    key_sequence=macro_data["key_sequence"],
                    settings=macro_data["settings"]
                )
                if result.get("success"):
                    print(f"✅ 테스트 매크로 생성: {macro_data['name']}")
                else:
                    print(f"⚠️ 매크로 생성 실패: {macro_data['name']} - {result.get('message')}")
            except Exception as e:
                print(f"❌ 매크로 생성 오류: {macro_data['name']} - {e}")
    
    async def handle_transcription_result(self, transcription_data):
        """GPT-4o 트랜스크립션 결과 처리"""
        if transcription_data.get("type") == "final":
            text = transcription_data.get("text", "").strip()
            confidence = transcription_data.get("confidence", 0.0)
            
            print(f"🎤 음성 인식 결과: '{text}' (신뢰도: {confidence:.2f})")
            
            # 매크로 매칭 및 실행
            await self.process_voice_command(text, confidence)
    
    async def process_voice_command(self, text: str, confidence: float):
        """음성 명령어 처리 (매칭 + 실행)"""
        try:
            # 매크로 매칭
            matches = self.matching_service.find_matching_macros(text)
            
            if matches:
                best_match = matches[0]
                print(f"🎯 매크로 매칭: '{best_match.macro_name}' (유사도: {best_match.similarity:.2f})")
                
                # 매크로 실행
                execution_result = await self.execute_macro(best_match.macro_id, text, confidence)
                
                # 결과 저장
                self.test_results.append({
                    "input_text": text,
                    "confidence": confidence,
                    "matched_macro": best_match.macro_name,
                    "similarity": best_match.similarity,
                    "execution_success": execution_result,
                    "timestamp": time.time()
                })
                
            else:
                print(f"❌ 매칭되는 매크로가 없습니다: '{text}'")
                self.test_results.append({
                    "input_text": text,
                    "confidence": confidence,
                    "matched_macro": None,
                    "similarity": 0.0,
                    "execution_success": False,
                    "timestamp": time.time()
                })
                
        except Exception as e:
            print(f"❌ 음성 명령어 처리 오류: {e}")
    
    async def execute_macro(self, macro_id: int, original_text: str, confidence: float) -> bool:
        """매크로 실행"""
        try:
            print(f"⚡ 매크로 실행 시작: ID={macro_id}")
            
            # 실제 매크로 실행 (시뮬레이션 모드)
            result = macro_execution_service.execute_macro(macro_id)
            
            if result.get("success"):
                print(f"✅ 매크로 실행 완료: {result.get('message', '성공')}")
                return True
            else:
                print(f"❌ 매크로 실행 실패: {result.get('message', '알 수 없는 오류')}")
                return False
                
        except Exception as e:
            print(f"❌ 매크로 실행 오류: {e}")
            return False
    
    async def test_voice_simulation(self):
        """음성 명령어 시뮬레이션 테스트"""
        print("\n📋 2단계: 음성 명령어 시뮬레이션 테스트")
        
        # 테스트할 음성 명령어들
        test_commands = [
            {"text": "공격해", "confidence": 0.95},
            {"text": "포션 먹어", "confidence": 0.90},
            {"text": "스킬 사용", "confidence": 0.88},
            {"text": "점프해서 공격", "confidence": 0.85},
            {"text": "회복해", "confidence": 0.92},
            {"text": "어택", "confidence": 0.87},  # 동의어 테스트
            {"text": "치료", "confidence": 0.82},  # 부분 매칭 테스트
            {"text": "알 수 없는 명령어", "confidence": 0.70},  # 매칭 실패 테스트
        ]
        
        print(f"🎯 {len(test_commands)}개의 테스트 명령어 실행 중...")
        
        for i, command in enumerate(test_commands, 1):
            print(f"\n--- 테스트 {i}/{len(test_commands)} ---")
            await self.process_voice_command(command["text"], command["confidence"])
            await asyncio.sleep(0.5)  # 잠시 대기
    
    async def test_real_time_audio(self):
        """실시간 오디오 테스트 (GPT-4o가 연결된 경우)"""
        if not self.gpt4o_service:
            print("\n⚠️ GPT-4o 서비스가 연결되지 않아 실시간 오디오 테스트를 건너뜁니다.")
            return
        
        print("\n📋 3단계: 실시간 오디오 테스트")
        print("🎤 실제 마이크로 음성 명령어를 말해보세요. (10초간 대기)")
        print("💡 예시: '공격해', '포션 먹어', '스킬 사용' 등")
        
        try:
            # 여기서 실제 마이크 입력을 받을 수 있지만, 
            # 현재는 시뮬레이션으로 대체
            print("📝 실제 마이크 입력은 향후 구현 예정입니다.")
            
            # 가상의 오디오 데이터 전송 시뮬레이션
            dummy_audio = b'\x00' * 2400  # 100ms worth of silence
            await self.gpt4o_service.send_audio_chunk(dummy_audio)
            
            # 잠시 대기하여 응답 확인
            await asyncio.sleep(2)
            
        except Exception as e:
            print(f"❌ 실시간 오디오 테스트 오류: {e}")
    
    def print_test_results(self):
        """테스트 결과 출력"""
        print("\n📊 테스트 결과 요약")
        print("=" * 60)
        
        total_tests = len(self.test_results)
        successful_matches = sum(1 for r in self.test_results if r["matched_macro"] is not None)
        successful_executions = sum(1 for r in self.test_results if r["execution_success"])
        
        print(f"📈 전체 테스트: {total_tests}개")
        print(f"✅ 매칭 성공: {successful_matches}개 ({successful_matches/total_tests*100:.1f}%)")
        print(f"⚡ 실행 성공: {successful_executions}개 ({successful_executions/total_tests*100:.1f}%)")
        
        print("\n📋 상세 결과:")
        for i, result in enumerate(self.test_results, 1):
            status = "✅" if result["execution_success"] else "❌"
            matched = result["matched_macro"] or "매칭 실패"
            print(f"{status} {i}. '{result['input_text']}' → {matched} (유사도: {result['similarity']:.2f})")
        
        # 매칭 서비스 통계
        stats = self.matching_service.get_matching_stats()
        print(f"\n📊 매칭 서비스 통계:")
        print(f"   전체 매칭 시도: {stats['total_matches']}회")
        print(f"   성공률: {stats['success_rate']:.1f}%")
        print(f"   정확한 매칭: {stats['match_types']['exact']}회")
        print(f"   부분 매칭: {stats['match_types']['partial']}회")
        print(f"   동의어 매칭: {stats['match_types']['synonym']}회")
        print(f"   퍼지 매칭: {stats['match_types']['fuzzy']}회")
    
    async def cleanup(self):
        """테스트 후 정리"""
        print("\n🧹 테스트 정리 중...")
        
        if self.gpt4o_service:
            try:
                await self.gpt4o_service.disconnect()
                print("✅ GPT-4o 서비스 연결 해제 완료")
            except Exception as e:
                print(f"⚠️ GPT-4o 연결 해제 오류: {e}")
    
    async def run_full_test(self):
        """전체 테스트 실행"""
        print("🚀 VoiceMacro Pro 통합 테스트 시작")
        print("=" * 60)
        
        try:
            # 1. 서비스 초기화
            await self.setup_services()
            
            # 2. 음성 명령어 시뮬레이션 테스트
            await self.test_voice_simulation()
            
            # 3. 실시간 오디오 테스트 (선택적)
            await self.test_real_time_audio()
            
            # 4. 결과 출력
            self.print_test_results()
            
        except Exception as e:
            print(f"❌ 테스트 실행 중 오류: {e}")
        finally:
            await self.cleanup()
        
        print("\n🎉 통합 테스트 완료!")


async def main():
    """메인 함수"""
    test = IntegratedVoiceMacroTest()
    await test.run_full_test()


if __name__ == "__main__":
    print("🎯 VoiceMacro Pro 통합 테스트")
    print("GPT-4o 실시간 음성 인식 → 매크로 매칭 → 실행 파이프라인 테스트")
    print()
    
    # API 키 확인
    if Config.OPENAI_API_KEY:
        print(f"🔑 OpenAI API 키: {Config.OPENAI_API_KEY[:10]}...")
    else:
        print("⚠️ OpenAI API 키가 설정되지 않았습니다.")
        print("   환경변수 OPENAI_API_KEY를 설정하거나 .env 파일을 만들어주세요.")
    
    print()
    
    # 비동기 실행
    asyncio.run(main()) 