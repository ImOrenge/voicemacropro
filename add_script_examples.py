#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
MSL 스크립트 예제를 데이터베이스에 추가하는 스크립트
"""

import sqlite3
import json
from datetime import datetime
import sys
import os

# 프로젝트 루트 디렉토리를 Python 경로에 추가
project_root = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, project_root)

from custom_script_service import CustomScriptService
from macro_service import MacroService

def add_script_examples():
    """다양한 MSL 스크립트 예제들을 데이터베이스에 추가"""
    
    # 서비스 초기화
    macro_service = MacroService()
    script_service = CustomScriptService()
    
    # MSL 스크립트 예제 데이터
    script_examples = [
        {
            'name': '기본 이동 콤보',
            'voice_command': '이동 콤보',
            'script_code': 'W,A,S,D',
            'category': '이동',
            'game_title': '공통',
            'description': '기본적인 WASD 순차 이동'
        },
        {
            'name': '동시 이동',
            'voice_command': '동시 이동',
            'script_code': 'W+A,W+D,S+A,S+D',
            'category': '이동',
            'game_title': '공통',
            'description': '대각선 이동을 위한 동시 키 입력'
        },
        {
            'name': '점프 연속',
            'voice_command': '점프 연속',
            'script_code': 'Space*5{200}',
            'category': '이동',
            'game_title': '플랫포머',
            'description': '200ms 간격으로 5번 점프'
        },
        {
            'name': '빠른 공격',
            'voice_command': '빠른 공격',
            'script_code': 'Attack&50',
            'category': '전투',
            'game_title': 'FPS',
            'description': '50ms 간격 연속 공격'
        },
        {
            'name': '달리기 토글',
            'voice_command': '달리기 토글',
            'script_code': '~Shift',
            'category': '이동',
            'game_title': '공통',
            'description': 'Shift 키 토글로 달리기 ON/OFF'
        },
        {
            'name': '스킬 홀드',
            'voice_command': '스킬 홀드',
            'script_code': 'Q[3000]',
            'category': '전투',
            'game_title': 'MOBA',
            'description': 'Q 스킬을 3초간 홀드'
        },
        {
            'name': 'LOL 콤보 기본',
            'voice_command': 'LOL 기본 콤보',
            'script_code': 'Q(100)W(150)E(200)R',
            'category': '전투',
            'game_title': 'LOL',
            'description': 'Q-W-E-R 스킬 연계 (지연시간 포함)'
        },
        {
            'name': 'FPS 리로드 콤보',
            'voice_command': 'FPS 리로드',
            'script_code': 'R(1000)Mouse1*3',
            'category': '전투',
            'game_title': 'FPS',
            'description': '리로드 후 3번 사격'
        },
        {
            'name': '인벤토리 정리',
            'voice_command': '인벤토리 정리',
            'script_code': 'I(500)Tab*3{100}Enter',
            'category': 'UI 조작',
            'game_title': 'RPG',
            'description': '인벤토리 열고 탭으로 정리 후 확인'
        },
        {
            'name': '채팅 매크로',
            'voice_command': '채팅 매크로',
            'script_code': 'Enter(100)T(50)h(50)a(50)n(50)k(50)s(100)Enter',
            'category': 'UI 조작',
            'game_title': '공통',
            'description': '채팅창에 "Thanks" 입력'
        },
        {
            'name': '건설 매크로',
            'voice_command': '건설 매크로',
            'script_code': 'B(100)H*5{150}Enter',
            'category': '상호작용',
            'game_title': 'RTS',
            'description': '건물 메뉴 열고 5개 집 건설'
        },
        {
            'name': '힐링 시퀀스',
            'voice_command': '힐링 시퀀스',
            'script_code': 'H[500]+(Shift*2{100})',
            'category': '전투',
            'game_title': 'RPG',
            'description': '힐링 스킬을 홀드하면서 Shift 2번'
        },
        {
            'name': '카메라 리셋',
            'voice_command': '카메라 리셋',
            'script_code': 'F5(200)Space(200)Y',
            'category': 'UI 조작',
            'game_title': '공통',
            'description': '화면 캡처 후 카메라 센터링'
        },
        {
            'name': '응급 탈출',
            'voice_command': '응급 탈출',
            'script_code': 'Escape*2{50}Alt+F4',
            'category': '기타',
            'game_title': '공통',
            'description': 'ESC 2번 후 Alt+F4로 강제 종료'
        },
        {
            'name': '복잡한 콤보',
            'voice_command': '복잡한 콤보',
            'script_code': 'Ctrl+Shift[1000]+(A,S,D)*3{200}~CapsLock',
            'category': '전투',
            'game_title': '격투',
            'description': 'Ctrl+Shift 홀드하며 복합 동작 후 토글'
        }
    ]
    
    print("🎮 MSL 스크립트 예제 추가를 시작합니다...")
    print(f"총 {len(script_examples)}개의 예제를 추가할 예정입니다.\n")
    
    success_count = 0
    failed_count = 0
    
    for i, example in enumerate(script_examples, 1):
        try:
            print(f"[{i:2d}/{len(script_examples)}] {example['name']} 추가 중...")
            
            # 1. 먼저 매크로 생성
            macro_id = macro_service.create_macro(
                name=example['name'],
                voice_command=example['voice_command'],
                action_type='custom_script',  # 커스텀 스크립트 타입
                key_sequence=example['script_code'],  # MSL 코드를 key_sequence에 저장
                settings={
                    'category': example['category'],
                    'game_title': example['game_title'],
                    'description': example['description']
                }
            )
            
            if macro_id:
                
                # 2. 커스텀 스크립트 생성 (검증 포함)
                script_result = script_service.create_custom_script(
                    macro_id=macro_id,
                    script_code=example['script_code'],
                    variables={
                        'category': example['category'],
                        'game_title': example['game_title'],
                        'description': example['description']
                    }
                )
                
                if script_result['success']:
                    validation = script_result['validation_result']
                    status = "✅ 성공" if validation['valid'] else "⚠️ 검증 실패"
                    print(f"    매크로 ID: {macro_id}, 스크립트 ID: {script_result['script_id']} {status}")
                    success_count += 1
                else:
                    print(f"    ❌ 스크립트 생성 실패: {script_result['error']}")
                    failed_count += 1
            else:
                print(f"    ❌ 매크로 생성 실패: ID가 반환되지 않음")
                failed_count += 1
                
        except Exception as e:
            print(f"    ❌ 예외 발생: {str(e)}")
            failed_count += 1
        
        print()  # 빈 줄 추가
    
    # 결과 요약
    print("=" * 60)
    print("📊 MSL 스크립트 예제 추가 완료!")
    print(f"✅ 성공: {success_count}개")
    print(f"❌ 실패: {failed_count}개")
    print(f"📝 총합: {success_count + failed_count}개")
    print("=" * 60)
    
    if success_count > 0:
        print("\n🎉 VoiceMacro Pro 애플리케이션의 '커스텀 스크립팅' 탭에서")
        print("   새로 추가된 스크립트 예제들을 확인하실 수 있습니다!")

def main():
    """메인 실행 함수"""
    try:
        print("🔧 VoiceMacro Pro - MSL 스크립트 예제 추가 도구")
        print("=" * 60)
        
        # 데이터베이스 파일 존재 확인
        db_path = os.path.join(project_root, 'voice_macro.db')
        if not os.path.exists(db_path):
            print(f"❌ 데이터베이스 파일을 찾을 수 없습니다: {db_path}")
            print("   먼저 api_server.py를 실행하여 데이터베이스를 초기화해주세요.")
            return
        
        # 사용자 확인
        response = input("\nMSL 스크립트 예제들을 데이터베이스에 추가하시겠습니까? (y/N): ")
        if response.lower() not in ['y', 'yes', 'ㅇ']:
            print("작업이 취소되었습니다.")
            return
        
        # 예제 추가 실행
        add_script_examples()
        
        print("\n🚀 작업이 완료되었습니다!")
        print("   VoiceMacro Pro 애플리케이션을 실행하여 결과를 확인해보세요.")
        
    except KeyboardInterrupt:
        print("\n\n⚠️ 작업이 사용자에 의해 중단되었습니다.")
    except Exception as e:
        print(f"\n❌ 예상치 못한 오류가 발생했습니다: {str(e)}")

if __name__ == "__main__":
    main() 