#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
데이터베이스에 직접 MSL 스크립트 예제를 추가하는 스크립트
"""

import sqlite3
import json
from datetime import datetime
import os
import hashlib

def add_direct_script_examples():
    """데이터베이스에 직접 MSL 스크립트 예제들을 추가"""
    
    # 데이터베이스 연결
    db_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'voice_macro.db')
    
    if not os.path.exists(db_path):
        print(f"❌ 데이터베이스 파일을 찾을 수 없습니다: {db_path}")
        return
    
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    # 기본 MSL 스크립트 예제들
    script_examples = [
        {
            'name': '기본 순차 이동',
            'voice_command': '순차 이동',
            'script_code': 'W,A,S,D',
            'category': '이동',
            'game_title': '공통',
            'description': 'WASD 키를 순서대로 누르기'
        },
        {
            'name': '동시 키 입력',
            'voice_command': '동시 입력',
            'script_code': 'W+A',
            'category': '이동',
            'game_title': '공통',
            'description': 'W와 A키를 동시에 누르기'
        },
        {
            'name': '복합 동시 입력',
            'voice_command': '복합 동시',
            'script_code': 'Ctrl+C',
            'category': 'UI 조작',
            'game_title': '공통',
            'description': 'Ctrl+C 복사 명령'
        },
        {
            'name': '키 반복',
            'voice_command': '키 반복',
            'script_code': 'Space*5',
            'category': '이동',
            'game_title': '공통',
            'description': 'Space키를 5번 반복'
        },
        {
            'name': '연속 입력',
            'voice_command': '연속 입력',
            'script_code': 'Attack&100',
            'category': '전투',
            'game_title': 'FPS',
            'description': '100ms 간격으로 연속 공격'
        },
        {
            'name': '토글 기능',
            'voice_command': '토글 기능',
            'script_code': '~CapsLock',
            'category': 'UI 조작',
            'game_title': '공통',
            'description': 'CapsLock 토글'
        },
        {
            'name': '홀드 기능',
            'voice_command': '홀드 기능',
            'script_code': 'Shift[2000]',
            'category': '이동',
            'game_title': '공통',
            'description': 'Shift키를 2초간 홀드'
        },
        {
            'name': '기본 콤보',
            'voice_command': '기본 콤보',
            'script_code': 'W+A,S+D',
            'category': '이동',
            'game_title': '공통',
            'description': '대각선 이동 조합'
        },
        {
            'name': '펑션키 시퀀스',
            'voice_command': '펑션키',
            'script_code': 'F1,F2,F3',
            'category': 'UI 조작',
            'game_title': '공통',
            'description': 'F1-F2-F3 순차 입력'
        },
        {
            'name': '특수키 조합',
            'voice_command': '특수키',
            'script_code': 'Tab,Enter,Escape',
            'category': 'UI 조작',
            'game_title': '공통',
            'description': 'Tab-Enter-Escape 순서'
        },
        {
            'name': '방향키 이동',
            'voice_command': '방향키',
            'script_code': 'Up,Down,Left,Right',
            'category': '이동',
            'game_title': '공통',
            'description': '방향키 순환 이동'
        },
        {
            'name': '마우스 클릭',
            'voice_command': '마우스 클릭',
            'script_code': 'MouseLeft,MouseRight',
            'category': '상호작용',
            'game_title': '공통',
            'description': '좌클릭 후 우클릭'
        }
    ]
    
    print("🎮 데이터베이스에 직접 MSL 스크립트 예제 추가를 시작합니다...")
    print(f"총 {len(script_examples)}개의 예제를 추가할 예정입니다.\n")
    
    success_count = 0
    failed_count = 0
    
    for i, example in enumerate(script_examples, 1):
        try:
            print(f"[{i:2d}/{len(script_examples)}] {example['name']} 추가 중...")
            
            # 1. 매크로 테이블에 삽입
            macro_query = '''
            INSERT INTO macros (name, voice_command, action_type, key_sequence, settings, is_script, script_language)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            '''
            
            settings = {
                'category': example['category'],
                'game_title': example['game_title'],
                'description': example['description']
            }
            
            cursor.execute(macro_query, (
                example['name'],
                example['voice_command'],
                'custom_script',  # action_type
                example['script_code'],  # key_sequence에 스크립트 코드 저장
                json.dumps(settings),
                True,  # is_script
                'MSL'  # script_language
            ))
            
            macro_id = cursor.lastrowid
            
            # 2. custom_scripts 테이블에 삽입
            script_query = '''
            INSERT INTO custom_scripts (
                macro_id, script_code, ast_tree, dependencies, variables,
                security_hash, is_validated, validation_date
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            '''
            
            # 간단한 해시 생성
            security_hash = hashlib.md5(example['script_code'].encode()).hexdigest()
            
            # AST와 의존성을 간단한 형태로 저장
            ast_tree = json.dumps([f"Node: {example['script_code']}"])
            dependencies = json.dumps([])  # 빈 의존성
            variables = json.dumps(settings)
            
            cursor.execute(script_query, (
                macro_id,
                example['script_code'],
                ast_tree,
                dependencies,
                variables,
                security_hash,
                True,  # is_validated
                datetime.now().isoformat()
            ))
            
            script_id = cursor.lastrowid
            
            print(f"    ✅ 성공 - 매크로 ID: {macro_id}, 스크립트 ID: {script_id}")
            success_count += 1
            
        except Exception as e:
            print(f"    ❌ 실패: {str(e)}")
            failed_count += 1
        
        print()  # 빈 줄 추가
    
    # 변경사항 커밋
    conn.commit()
    conn.close()
    
    # 결과 요약
    print("=" * 60)
    print("📊 MSL 스크립트 예제 직접 추가 완료!")
    print(f"✅ 성공: {success_count}개")
    print(f"❌ 실패: {failed_count}개")
    print(f"📝 총합: {success_count + failed_count}개")
    print("=" * 60)
    
    if success_count > 0:
        print("\n🎉 VoiceMacro Pro 애플리케이션의 '커스텀 스크립팅' 탭에서")
        print("   새로 추가된 스크립트 예제들을 확인하실 수 있습니다!")
        print("\n💡 추가된 예제 구문:")
        print("   • 순차 실행: W,A,S,D")
        print("   • 동시 실행: W+A, Ctrl+C")
        print("   • 반복: Space*5")
        print("   • 연속 입력: Attack&100")
        print("   • 토글: ~CapsLock")
        print("   • 홀드: Shift[2000]")

def main():
    """메인 실행 함수"""
    try:
        print("🔧 VoiceMacro Pro - 직접 MSL 스크립트 예제 추가 도구")
        print("=" * 60)
        
        # 사용자 확인
        response = input("\nMSL 스크립트 예제들을 데이터베이스에 직접 추가하시겠습니까? (y/N): ")
        if response.lower() not in ['y', 'yes', 'ㅇ']:
            print("작업이 취소되었습니다.")
            return
        
        # 예제 추가 실행
        add_direct_script_examples()
        
        print("\n🚀 작업이 완료되었습니다!")
        print("   VoiceMacro Pro 애플리케이션을 실행하여 결과를 확인해보세요.")
        
    except KeyboardInterrupt:
        print("\n\n⚠️ 작업이 사용자에 의해 중단되었습니다.")
    except Exception as e:
        print(f"\n❌ 예상치 못한 오류가 발생했습니다: {str(e)}")

if __name__ == "__main__":
    main() 