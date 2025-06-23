#!/usr/bin/env python3
"""
VoiceMacro Pro - Whisper 음성 인식 정확도 테스트 도구
🎯 개선된 Whisper 서비스의 정확도를 종합적으로 테스트

사용법:
    python test_whisper_accuracy.py

기능:
    1. 실시간 음성 녹음 및 인식 테스트
    2. 게임 명령어 매칭 정확도 테스트
    3. 한국어 동의어 인식 테스트
    4. 오디오 품질 검증
"""

import sys
import os
import asyncio
import time
import numpy as np
import sounddevice as sd
from datetime import datetime

# 프로젝트 경로 설정
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from backend.services.whisper_service import whisper_service
from backend.services.macro_service import macro_service
from backend.utils.config import config
from backend.utils.common_utils import get_logger

logger = get_logger(__name__)

class WhisperAccuracyTester:
    """Whisper 음성 인식 정확도 테스트 클래스"""
    
    def __init__(self):
        self.sample_rate = config.SAMPLE_RATE
        self.channels = config.AUDIO_CHANNELS
        self.recording = False
        self.audio_data = []
        
        # 테스트용 게임 명령어 목록
        self.test_commands = [
            "공격", "스킬", "스킬1", "스킬2", "스킬3",
            "궁극기", "점프", "달리기", "방어", "힐링",
            "포션", "아이템", "무기", "콤보", "연사",
            "시작", "중지", "일시정지", "재시작"
        ]
        
        # 동의어 테스트 쌍
        self.synonym_tests = [
            ("공격", "어택"),
            ("스킬", "기술"),
            ("궁극기", "울트"),
            ("달리기", "뛰기"),
            ("점프", "뛰어오르기"),
            ("포션", "물약"),
            ("힐링", "회복"),
            ("시작", "스타트"),
            ("중지", "스톱")
        ]
    
    def print_header(self, title: str):
        """섹션 헤더 출력"""
        print(f"\n{'='*50}")
        print(f"🎯 {title}")
        print(f"{'='*50}")
    
    def print_status(self, message: str, status: str = "INFO"):
        """상태 메시지 출력"""
        timestamp = datetime.now().strftime("%H:%M:%S")
        
        if status == "SUCCESS":
            print(f"[{timestamp}] ✅ {message}")
        elif status == "WARNING":
            print(f"[{timestamp}] ⚠️  {message}")
        elif status == "ERROR":
            print(f"[{timestamp}] ❌ {message}")
        else:
            print(f"[{timestamp}] 📝 {message}")
    
    def check_system_status(self):
        """시스템 상태 확인"""
        self.print_header("시스템 상태 확인")
        
        # Whisper 서비스 상태
        service_status = whisper_service.get_service_status()
        
        print(f"📊 Whisper 서비스 상태:")
        print(f"   - OpenAI 클라이언트: {'✅ 초기화됨' if service_status['client_initialized'] else '❌ 미초기화'}")
        print(f"   - API 키 설정: {'✅ 설정됨' if service_status['api_key_configured'] else '❌ 미설정'}")
        print(f"   - 모델: {service_status['model']}")
        print(f"   - 언어: {service_status['language']}")
        print(f"   - 샘플레이트: {service_status['sample_rate']}Hz")
        print(f"   - 임시 디렉토리: {'✅ 존재함' if service_status['temp_dir_exists'] else '❌ 없음'}")
        
        # 매크로 상태
        try:
            macros = macro_service.get_all_macros()
            print(f"   - 등록된 매크로: {len(macros)}개")
            
            voice_enabled_macros = [m for m in macros if m.get('voice_command')]
            print(f"   - 음성 명령 설정된 매크로: {len(voice_enabled_macros)}개")
            
        except Exception as e:
            print(f"   - 매크로 조회 실패: {e}")
        
        # 오디오 디바이스 확인
        try:
            devices = sd.query_devices()
            default_input = sd.query_devices(kind='input')
            print(f"   - 기본 입력 디바이스: {default_input['name']}")
            print(f"   - 최대 입력 채널: {default_input['max_input_channels']}")
            
        except Exception as e:
            print(f"   - 오디오 디바이스 확인 실패: {e}")
    
    def record_audio(self, duration: float = 3.0) -> np.ndarray:
        """
        음성 녹음
        
        Args:
            duration: 녹음 시간 (초)
            
        Returns:
            np.ndarray: 녹음된 오디오 데이터
        """
        self.print_status(f"🎙️  {duration}초간 음성을 녹음합니다...")
        
        try:
            # 카운트다운
            for i in range(3, 0, -1):
                print(f"   {i}초 후 녹음 시작...")
                time.sleep(1)
            
            print(f"   🔴 녹음 시작! ('{duration}초간 말씀해주세요')")
            
            # 음성 녹음
            audio_data = sd.rec(
                int(duration * self.sample_rate),
                samplerate=self.sample_rate,
                channels=self.channels,
                dtype=np.float32
            )
            sd.wait()  # 녹음 완료 대기
            
            print(f"   ⏹️  녹음 완료!")
            
            # 오디오 데이터 검증
            audio_flat = audio_data.flatten()
            max_amplitude = np.max(np.abs(audio_flat))
            signal_energy = np.sum(audio_flat ** 2)
            
            print(f"   📊 오디오 분석:")
            print(f"      - 최대 진폭: {max_amplitude:.6f}")
            print(f"      - 신호 에너지: {signal_energy:.6f}")
            print(f"      - 샘플 수: {len(audio_flat)}")
            
            if max_amplitude < 0.001:
                self.print_status("⚠️  오디오 신호가 매우 약합니다. 마이크 볼륨을 확인해주세요.", "WARNING")
            
            return audio_flat
            
        except Exception as e:
            self.print_status(f"음성 녹음 실패: {e}", "ERROR")
            return np.array([])
    
    def test_single_recognition(self, audio_data: np.ndarray) -> dict:
        """
        단일 음성 인식 테스트
        
        Args:
            audio_data: 오디오 데이터
            
        Returns:
            dict: 인식 결과
        """
        if len(audio_data) == 0:
            return {'success': False, 'error': '오디오 데이터 없음'}
        
        try:
            # Whisper 음성 인식 실행
            start_time = time.time()
            result = whisper_service.process_voice_command(audio_data)
            end_time = time.time()
            
            processing_time = end_time - start_time
            
            print(f"\n🔍 음성 인식 결과:")
            print(f"   - 처리 시간: {processing_time:.2f}초")
            
            if result['success']:
                transcription = result['transcription_result']
                recognized_text = result['recognized_text']
                matched_macros = result['matched_macros']
                
                print(f"   - 인식된 텍스트: '{recognized_text}'")
                print(f"   - 신뢰도: {transcription.get('confidence', 0):.2f}")
                print(f"   - 언어: {transcription.get('language', 'unknown')}")
                print(f"   - 오디오 길이: {transcription.get('duration', 0):.1f}초")
                print(f"   - 매칭된 매크로: {len(matched_macros)}개")
                
                if matched_macros:
                    print(f"   🏆 최고 매칭 결과:")
                    for i, match in enumerate(matched_macros[:3], 1):
                        print(f"      {i}. '{match['voice_command']}' ({match['macro_name']}) - {match['confidence']:.1f}%")
                
                return {
                    'success': True,
                    'recognized_text': recognized_text,
                    'confidence': transcription.get('confidence', 0),
                    'matched_macros': matched_macros,
                    'processing_time': processing_time
                }
            else:
                error_msg = result.get('error', '알 수 없는 오류')
                print(f"   - 실패 원인: {error_msg}")
                return {'success': False, 'error': error_msg}
                
        except Exception as e:
            self.print_status(f"음성 인식 테스트 실패: {e}", "ERROR")
            return {'success': False, 'error': str(e)}
    
    def test_command_accuracy(self):
        """게임 명령어 인식 정확도 테스트"""
        self.print_header("게임 명령어 인식 테스트")
        
        print(f"📝 다음 명령어들을 하나씩 말해주세요:")
        for i, command in enumerate(self.test_commands, 1):
            print(f"   {i}. {command}")
        
        results = []
        
        for i, expected_command in enumerate(self.test_commands, 1):
            print(f"\n🎯 테스트 {i}/{len(self.test_commands)}: '{expected_command}'")
            
            audio_data = self.record_audio(3.0)
            if len(audio_data) == 0:
                continue
            
            result = self.test_single_recognition(audio_data)
            
            if result['success']:
                recognized = result['recognized_text']
                accuracy = whisper_service._calculate_similarity(recognized, expected_command)
                
                results.append({
                    'expected': expected_command,
                    'recognized': recognized,
                    'accuracy': accuracy,
                    'matched': accuracy >= config.MATCHING_THRESHOLD
                })
                
                if accuracy >= 0.9:
                    self.print_status(f"🌟 우수: '{expected_command}' → '{recognized}' (정확도: {accuracy:.3f})", "SUCCESS")
                elif accuracy >= 0.7:
                    self.print_status(f"👍 양호: '{expected_command}' → '{recognized}' (정확도: {accuracy:.3f})", "SUCCESS")
                else:
                    self.print_status(f"⚠️  개선 필요: '{expected_command}' → '{recognized}' (정확도: {accuracy:.3f})", "WARNING")
            else:
                self.print_status(f"❌ 인식 실패: '{expected_command}'", "ERROR")
                results.append({
                    'expected': expected_command,
                    'recognized': '',
                    'accuracy': 0.0,
                    'matched': False
                })
            
            # 다음 테스트까지 잠시 대기
            time.sleep(1)
        
        # 결과 요약
        if results:
            total_tests = len(results)
            successful_recognitions = len([r for r in results if r['recognized']])
            matched_commands = len([r for r in results if r['matched']])
            avg_accuracy = np.mean([r['accuracy'] for r in results])
            
            print(f"\n📊 테스트 결과 요약:")
            print(f"   - 총 테스트: {total_tests}개")
            print(f"   - 성공적 인식: {successful_recognitions}개 ({successful_recognitions/total_tests*100:.1f}%)")
            print(f"   - 매크로 매칭: {matched_commands}개 ({matched_commands/total_tests*100:.1f}%)")
            print(f"   - 평균 정확도: {avg_accuracy:.3f}")
    
    def test_synonym_recognition(self):
        """동의어 인식 테스트"""
        self.print_header("동의어 인식 테스트")
        
        print(f"📝 다음 동의어 쌍들을 테스트합니다:")
        for i, (standard, synonym) in enumerate(self.synonym_tests, 1):
            print(f"   {i}. '{standard}' ↔ '{synonym}'")
        
        results = []
        
        for i, (standard, synonym) in enumerate(self.synonym_tests, 1):
            print(f"\n🎯 동의어 테스트 {i}/{len(self.synonym_tests)}")
            
            # 표준어 테스트
            print(f"   1) 표준어 '{standard}' 말해주세요:")
            audio1 = self.record_audio(2.0)
            result1 = self.test_single_recognition(audio1) if len(audio1) > 0 else {'success': False}
            
            # 동의어 테스트
            print(f"   2) 동의어 '{synonym}' 말해주세요:")
            audio2 = self.record_audio(2.0)
            result2 = self.test_single_recognition(audio2) if len(audio2) > 0 else {'success': False}
            
            # 동의어 인식 분석
            if result1['success'] and result2['success']:
                text1 = result1['recognized_text']
                text2 = result2['recognized_text']
                
                # 두 인식 결과가 같은 매크로를 가리키는지 확인
                similarity = whisper_service._calculate_similarity(text1, text2)
                
                results.append({
                    'standard': standard,
                    'synonym': synonym,
                    'recognized_standard': text1,
                    'recognized_synonym': text2,
                    'similarity': similarity,
                    'synonym_detected': similarity >= 0.8
                })
                
                if similarity >= 0.8:
                    self.print_status(f"✅ 동의어 인식 성공: '{text1}' ≈ '{text2}' (유사도: {similarity:.3f})", "SUCCESS")
                else:
                    self.print_status(f"⚠️  동의어 인식 부족: '{text1}' ≠ '{text2}' (유사도: {similarity:.3f})", "WARNING")
            
            time.sleep(1)
        
        # 동의어 인식 결과 요약
        if results:
            total_tests = len(results)
            successful_synonyms = len([r for r in results if r['synonym_detected']])
            avg_similarity = np.mean([r['similarity'] for r in results])
            
            print(f"\n📊 동의어 인식 결과:")
            print(f"   - 총 테스트: {total_tests}개")
            print(f"   - 동의어 인식 성공: {successful_synonyms}개 ({successful_synonyms/total_tests*100:.1f}%)")
            print(f"   - 평균 유사도: {avg_similarity:.3f}")
    
    def interactive_test(self):
        """대화형 음성 인식 테스트"""
        self.print_header("대화형 음성 인식 테스트")
        
        print(f"💬 자유롭게 음성을 테스트해보세요.")
        print(f"   - 'q' 입력 시 종료")
        print(f"   - Enter 키로 녹음 시작")
        
        while True:
            user_input = input(f"\n🎙️  녹음하려면 Enter, 종료하려면 'q': ").strip()
            
            if user_input.lower() == 'q':
                break
            
            audio_data = self.record_audio(3.0)
            if len(audio_data) > 0:
                self.test_single_recognition(audio_data)
    
    def run_full_test(self):
        """전체 테스트 실행"""
        print(f"🎯 VoiceMacro Pro - Whisper 음성 인식 정확도 테스트")
        print(f"   시작 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        
        try:
            # 1. 시스템 상태 확인
            self.check_system_status()
            
            # 2. 기본 설정 확인
            if not config.OPENAI_API_KEY:
                self.print_status("OpenAI API 키가 설정되지 않았습니다. 테스트를 중단합니다.", "ERROR")
                return
            
            # 3. 사용자 선택
            print(f"\n📋 테스트 메뉴:")
            print(f"   1. 게임 명령어 인식 테스트")
            print(f"   2. 동의어 인식 테스트")  
            print(f"   3. 대화형 테스트")
            print(f"   4. 전체 테스트")
            
            choice = input(f"\n선택하세요 (1-4): ").strip()
            
            if choice == '1':
                self.test_command_accuracy()
            elif choice == '2':
                self.test_synonym_recognition()
            elif choice == '3':
                self.interactive_test()
            elif choice == '4':
                self.test_command_accuracy()
                self.test_synonym_recognition()
                self.interactive_test()
            else:
                print(f"잘못된 선택입니다.")
                return
            
            self.print_status("테스트 완료!", "SUCCESS")
            
        except KeyboardInterrupt:
            self.print_status("사용자에 의해 테스트가 중단되었습니다.", "WARNING")
        except Exception as e:
            self.print_status(f"테스트 실행 중 오류 발생: {e}", "ERROR")

def main():
    """메인 함수"""
    tester = WhisperAccuracyTester()
    tester.run_full_test()

if __name__ == "__main__":
    main() 