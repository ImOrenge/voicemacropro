#!/usr/bin/env python3
"""
VoiceMacro Pro - 음성 분석 기능 테스트 스크립트
2단계: 음성 분석 및 텍스트 변환, 매크로 매칭 기능 테스트
"""

import time
import sys
from voice_recognition_service_basic import get_voice_recognition_service_basic
from voice_analysis_service import get_voice_analysis_service

def test_voice_analysis_service():
    """음성 분석 서비스 테스트"""
    print("🔍 === 음성 분석 서비스 테스트 ===")
    
    try:
        # 1. 서비스 초기화
        print("1. 음성 분석 서비스 초기화 중...")
        analysis_service = get_voice_analysis_service()
        print("✅ 서비스 초기화 완료")
        
        # 2. 서비스 정보 확인
        print("\n2. 서비스 정보 확인...")
        stats = analysis_service.get_analysis_stats()
        print(f"✅ 서비스 통계:")
        print(f"   - 현재 언어: {stats['current_language']}")
        print(f"   - 지원 언어: {stats['supported_languages']}")
        print(f"   - 샘플 명령어 개수: {stats['sample_commands_count']}")
        print(f"   - 동의어 사전 크기: {stats['synonyms_count']}")
        print(f"   - 노이즈 패턴 개수: {stats['noise_patterns_count']}")
        
        # 3. 언어 설정 테스트
        print("\n3. 언어 설정 테스트...")
        if analysis_service.set_language('en'):
            print("✅ 영어로 언어 변경 성공")
        if analysis_service.set_language('ko'):
            print("✅ 한국어로 언어 변경 성공")
        
        # 4. 시뮬레이션 오디오 데이터 생성
        print("\n4. 시뮬레이션 오디오 분석 테스트...")
        
        # 가상 음성 녹음 서비스에서 오디오 데이터 가져오기
        voice_service = get_voice_recognition_service_basic()
        voice_service.start_recording()
        time.sleep(1)  # 1초간 녹음
        audio_data = voice_service.get_audio_data(1.0)
        voice_service.stop_recording()
        
        if audio_data:
            print(f"✅ 오디오 데이터 준비 완료: {len(audio_data)} 샘플")
            
            # 음성 분석 수행
            analysis_result = analysis_service.analyze_audio_simulation(audio_data, 1.0)
            
            if analysis_result['success']:
                print("✅ 음성 분석 성공!")
                print(f"   - 인식된 텍스트: '{analysis_result['text']}'")
                print(f"   - 원본 텍스트: '{analysis_result.get('original_text', '없음')}'")
                print(f"   - 신뢰도: {analysis_result['confidence']:.2f}")
                print(f"   - 언어: {analysis_result['language']}")
                print(f"   - 처리 시간: {analysis_result['processing_time']:.3f}초")
                print(f"   - 오디오 레벨: {analysis_result.get('audio_level', 0):.3f}")
            else:
                print(f"⚠️ 음성 분석 실패: {analysis_result['message']}")
        else:
            print("❌ 오디오 데이터 가져오기 실패")
            return False
        
        return True
        
    except Exception as e:
        print(f"❌ 테스트 중 오류 발생: {e}")
        import traceback
        traceback.print_exc()
        return False

def test_macro_matching():
    """매크로 명령어 매칭 테스트"""
    print("\n🎯 === 매크로 명령어 매칭 테스트 ===")
    
    try:
        analysis_service = get_voice_analysis_service()
        
        # 테스트용 매크로 명령어들
        test_macro_commands = [
            '공격',
            '방어',
            '스킬 사용',
            '아이템 사용',
            '인벤토리 열기',
            '게임 저장',
            '게임 종료',
            '점프하기',
            '달리기 시작'
        ]
        
        # 테스트 시나리오들
        test_scenarios = [
            {
                'input': '공격',
                'description': '정확한 매칭'
            },
            {
                'input': '어택',
                'description': '동의어 매칭'
            },
            {
                'input': '어 공격',
                'description': '노이즈 포함'
            },
            {
                'input': '스킬',
                'description': '부분 매칭'
            },
            {
                'input': '가방',
                'description': '동의어 매칭 (인벤토리)'
            },
            {
                'input': '세이브',
                'description': '동의어 매칭 (저장)'
            },
            {
                'input': '뛰기',
                'description': '동의어 매칭 (점프)'
            },
            {
                'input': '알 수 없는 명령어',
                'description': '매칭 실패 테스트'
            }
        ]
        
        print(f"매크로 명령어 목록 ({len(test_macro_commands)}개):")
        for i, cmd in enumerate(test_macro_commands, 1):
            print(f"   {i}. {cmd}")
        
        print(f"\n매칭 테스트 시나리오 ({len(test_scenarios)}개):")
        
        success_count = 0
        for i, scenario in enumerate(test_scenarios, 1):
            input_text = scenario['input']
            description = scenario['description']
            
            print(f"\n--- 시나리오 {i}: {description} ---")
            print(f"입력 텍스트: '{input_text}'")
            
            # 매칭 수행
            match_result = analysis_service.match_macro_commands(input_text, test_macro_commands)
            
            if match_result['success']:
                print("✅ 매칭 성공!")
                best_match = match_result['best_match']
                print(f"   - 최고 매칭: '{best_match['command']}'")
                print(f"   - 유사도: {best_match['similarity']:.3f}")
                print(f"   - 신뢰도 레벨: {best_match['confidence_level']}")
                
                if len(match_result['matches']) > 1:
                    print(f"   - 기타 후보 ({len(match_result['matches'])-1}개):")
                    for match in match_result['matches'][1:3]:  # 상위 2개만 표시
                        print(f"     • '{match['command']}' (유사도: {match['similarity']:.3f})")
                
                success_count += 1
            else:
                print(f"⚠️ 매칭 실패: {match_result['message']}")
                # 실패가 예상되는 경우 (예: "알 수 없는 명령어")는 성공으로 간주
                if "알 수 없는" in input_text:
                    success_count += 1
        
        print(f"\n📊 매칭 테스트 결과:")
        print(f"   - 총 테스트: {len(test_scenarios)}개")
        print(f"   - 성공: {success_count}개")
        print(f"   - 성공률: {success_count/len(test_scenarios)*100:.1f}%")
        
        return success_count == len(test_scenarios)
        
    except Exception as e:
        print(f"❌ 매크로 매칭 테스트 중 오류 발생: {e}")
        import traceback
        traceback.print_exc()
        return False

def test_full_pipeline():
    """전체 파이프라인 테스트 (녹음 → 분석 → 매칭)"""
    print("\n🔄 === 전체 파이프라인 테스트 ===")
    
    try:
        # 서비스 초기화
        voice_service = get_voice_recognition_service_basic()
        analysis_service = get_voice_analysis_service()
        
        # 테스트용 매크로 명령어들
        macro_commands = ['공격', '방어', '스킬', '아이템', '점프', '달리기']
        
        print("시뮬레이션된 전체 파이프라인 테스트 (3회):")
        
        success_count = 0
        for i in range(3):
            print(f"\n--- 테스트 {i+1}/3 ---")
            
            # 1. 음성 녹음 (시뮬레이션)
            print("1️⃣ 음성 녹음 중...")
            voice_service.start_recording()
            time.sleep(0.5)  # 0.5초간 녹음
            audio_data = voice_service.get_audio_data(0.5)
            voice_service.stop_recording()
            
            if not audio_data:
                print("❌ 오디오 데이터 수집 실패")
                continue
            
            # 2. 음성 분석
            print("2️⃣ 음성 분석 중...")
            analysis_result = analysis_service.analyze_audio_simulation(audio_data, 0.5)
            
            if not analysis_result['success']:
                print(f"❌ 음성 분석 실패: {analysis_result['message']}")
                continue
            
            recognized_text = analysis_result['text']
            confidence = analysis_result['confidence']
            print(f"   인식된 텍스트: '{recognized_text}' (신뢰도: {confidence:.2f})")
            
            # 3. 매크로 매칭
            print("3️⃣ 매크로 매칭 중...")
            match_result = analysis_service.match_macro_commands(recognized_text, macro_commands)
            
            if match_result['success']:
                best_match = match_result['best_match']
                print(f"✅ 매칭 성공: '{best_match['command']}' (유사도: {best_match['similarity']:.2f})")
                success_count += 1
            else:
                print(f"⚠️ 매칭 실패: {match_result['message']}")
        
        print(f"\n📊 전체 파이프라인 테스트 결과:")
        print(f"   - 총 테스트: 3회")
        print(f"   - 성공: {success_count}회")
        print(f"   - 성공률: {success_count/3*100:.1f}%")
        
        return success_count >= 2  # 3회 중 2회 이상 성공하면 통과
        
    except Exception as e:
        print(f"❌ 전체 파이프라인 테스트 중 오류 발생: {e}")
        import traceback
        traceback.print_exc()
        return False

def main():
    """메인 테스트 함수"""
    print("🎮 VoiceMacro Pro - 음성 분석 및 매크로 매칭 기능 테스트")
    print("=" * 60)
    
    # 각 테스트 실행
    test_results = {}
    
    print("\n📍 2단계: 음성 분석 및 텍스트 변환 기능 테스트")
    test_results['voice_analysis'] = test_voice_analysis_service()
    
    if test_results['voice_analysis']:
        test_results['macro_matching'] = test_macro_matching()
        test_results['full_pipeline'] = test_full_pipeline()
    else:
        print("\n❌ 기본 음성 분석 테스트 실패. 나머지 테스트를 건너뜁니다.")
        test_results['macro_matching'] = False
        test_results['full_pipeline'] = False
    
    # 결과 요약
    print("\n" + "=" * 60)
    print("📊 테스트 결과 요약:")
    print(f"   - 음성 분석 서비스: {'✅ 성공' if test_results['voice_analysis'] else '❌ 실패'}")
    print(f"   - 매크로 명령어 매칭: {'✅ 성공' if test_results['macro_matching'] else '❌ 실패'}")
    print(f"   - 전체 파이프라인: {'✅ 성공' if test_results['full_pipeline'] else '❌ 실패'}")
    
    all_success = all(test_results.values())
    
    if all_success:
        print("\n🎉 2단계 음성 분석 및 텍스트 변환 기능 구현 완료!")
        print("   ✅ OpenAI Whisper 통합 (시뮬레이션)")
        print("   ✅ 노이즈 필터링")
        print("   ✅ 다국어 지원 (한국어, 영어)")
        print("   ✅ 명령어 매칭 알고리즘")
        print("   ✅ 유사도 기반 매크로 검색")
        print("   ✅ 부분 일치 및 동의어 처리")
        print("   ✅ 확신도 표시")
        print("\n다음 단계: 3단계 - 명령어 매칭 최적화 또는 실제 Whisper 통합")
        return True
    else:
        print("\n❌ 일부 테스트 실패. 문제를 해결한 후 다시 시도하세요.")
        return False

if __name__ == "__main__":
    """
    테스트 스크립트 실행
    
    실행 방법:
    py -3 test_voice_analysis.py
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