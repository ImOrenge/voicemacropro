#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
커스텀 스크립트 테스트 데이터 추가 스크립트
MSL(Macro Scripting Language) 기반 테스트 스크립트들을 데이터베이스에 추가합니다.
"""

import sys
import os
import sqlite3
import json
from datetime import datetime

# 백엔드 모듈 경로 추가
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from backend.database.database_manager import db_manager

def add_test_custom_scripts():
    """테스트용 커스텀 스크립트들을 데이터베이스에 추가하는 함수"""
    
    # 데이터베이스 연결
    conn = db_manager.get_connection()
    cursor = conn.cursor()
    
    # 기존 테스트 데이터 삭제 (중복 방지)
    cursor.execute("DELETE FROM custom_scripts WHERE script_code LIKE '%테스트%'")
    
    # 테스트 스크립트 데이터
    test_scripts = [
        {
            'macro_id': 1,  # 기존 매크로에 연결
            'script_code': 'W,A,S,D',
            'description': '기본 이동 키 테스트 - W, A, S, D 순차 입력',
            'variables': {'test_mode': True},
            'dependencies': ['basic_keys']
        },
        {
            'macro_id': 2,
            'script_code': 'Space*3',
            'description': '점프 반복 테스트 - Space 키 3번 반복',
            'variables': {'repeat_count': 3},
            'dependencies': ['repeat_action']
        },
        {
            'macro_id': 3,
            'script_code': 'Ctrl+C,(100),Ctrl+V',
            'description': '복사-붙여넣기 테스트 - 복사 후 100ms 대기, 붙여넣기',
            'variables': {'delay_ms': 100},
            'dependencies': ['clipboard_actions']
        },
        {
            'macro_id': 4,
            'script_code': 'Shift[1000]',
            'description': '홀드 키 테스트 - Shift 키 1초간 홀드',
            'variables': {'hold_duration': 1000},
            'dependencies': ['hold_action']
        },
        {
            'macro_id': 5,
            'script_code': 'Q+W+E',
            'description': '동시 키 입력 테스트 - Q, W, E 동시 누르기',
            'variables': {'combo_keys': ['Q', 'W', 'E']},
            'dependencies': ['combo_action']
        }
    ]
    
    # 스크립트 추가
    for i, script in enumerate(test_scripts, 1):
        try:
            # AST 트리는 간단한 문자열로 저장 (실제로는 파싱 결과)
            ast_tree = json.dumps({
                'type': 'sequence',
                'commands': script['script_code'].split(','),
                'description': script['description']
            })
            
            # 보안 해시 생성 (간단한 해시)
            import hashlib
            security_hash = hashlib.sha256(script['script_code'].encode('utf-8')).hexdigest()
            
            # 데이터베이스에 삽입
            cursor.execute('''
                INSERT INTO custom_scripts (
                    macro_id, script_code, ast_tree, dependencies, variables,
                    security_hash, is_validated, validation_date, created_at, updated_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                script['macro_id'],
                script['script_code'],
                ast_tree,
                json.dumps(script['dependencies']),
                json.dumps(script['variables']),
                security_hash,
                True,  # 검증 완료
                datetime.now().isoformat(),
                datetime.now().isoformat(),
                datetime.now().isoformat()
            ))
            
            script_id = cursor.lastrowid
            print(f"✅ 테스트 스크립트 {i} 추가 완료: ID {script_id}, 코드 '{script['script_code']}'")
            
        except Exception as e:
            print(f"❌ 테스트 스크립트 {i} 추가 실패: {e}")
    
    # 변경사항 커밋
    conn.commit()
    conn.close()
    
    print("\n🎯 테스트 스크립트 추가 완료!")
    print("이제 VoiceMacro Pro에서 커스텀 스크립트 실행을 테스트할 수 있습니다.")

def check_custom_scripts_table():
    """custom_scripts 테이블이 존재하는지 확인하는 함수"""
    try:
        conn = db_manager.get_connection()
        cursor = conn.cursor()
        
        # 테이블 존재 확인
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='custom_scripts'
        """)
        
        result = cursor.fetchone()
        
        if result:
            print("✅ custom_scripts 테이블이 존재합니다.")
            
            # 기존 레코드 수 확인
            cursor.execute("SELECT COUNT(*) FROM custom_scripts")
            count = cursor.fetchone()[0]
            print(f"📊 현재 {count}개의 커스텀 스크립트가 저장되어 있습니다.")
            
        else:
            print("❌ custom_scripts 테이블이 존재하지 않습니다.")
            print("데이터베이스 스키마를 먼저 생성해야 합니다.")
            
            # 테이블 생성 시도
            create_custom_scripts_table(cursor)
        
        conn.close()
        return True
        
    except Exception as e:
        print(f"❌ 데이터베이스 확인 실패: {e}")
        return False

def create_custom_scripts_table(cursor):
    """custom_scripts 테이블을 생성하는 함수"""
    try:
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS custom_scripts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                macro_id INTEGER NOT NULL,
                script_code TEXT NOT NULL,
                ast_tree TEXT,
                dependencies TEXT,
                variables TEXT,
                security_hash TEXT,
                is_validated BOOLEAN DEFAULT FALSE,
                validation_date DATETIME,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (macro_id) REFERENCES macros (id) ON DELETE CASCADE
            )
        ''')
        
        print("✅ custom_scripts 테이블을 생성했습니다.")
        
    except Exception as e:
        print(f"❌ 테이블 생성 실패: {e}")

if __name__ == "__main__":
    print("🚀 커스텀 스크립트 테스트 데이터 추가 시작")
    print("=" * 50)
    
    # 테이블 존재 확인
    if check_custom_scripts_table():
        # 테스트 스크립트 추가
        add_test_custom_scripts()
    else:
        print("❌ 데이터베이스 설정에 문제가 있습니다.") 