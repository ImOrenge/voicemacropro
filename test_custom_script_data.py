#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
커스텀 스크립트 테스트 데이터 추가 (간단 버전)
"""

import sqlite3
import json
from datetime import datetime

def add_test_scripts():
    """테스트 스크립트 추가"""
    try:
        # 데이터베이스 연결
        conn = sqlite3.connect('voice_macro.db')
        cursor = conn.cursor()
        
        # custom_scripts 테이블 생성 (없는 경우)
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
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        
        # 기존 테스트 데이터 삭제
        cursor.execute("DELETE FROM custom_scripts WHERE script_code IN ('W,A,S,D', 'Space*3', 'Q+W+E')")
        
        # 테스트 스크립트 추가
        test_scripts = [
            (1, 'W,A,S,D', 'Basic movement test'),
            (2, 'Space*3', 'Jump repeat test'),
            (3, 'Q+W+E', 'Key combo test')
        ]
        
        for macro_id, script_code, description in test_scripts:
            cursor.execute('''
                INSERT INTO custom_scripts (
                    macro_id, script_code, ast_tree, dependencies, variables,
                    security_hash, is_validated, validation_date
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                macro_id,
                script_code,
                json.dumps({'type': 'test', 'description': description}),
                json.dumps(['test']),
                json.dumps({'test_mode': True}),
                'test_hash_' + str(macro_id),
                True,
                datetime.now().isoformat()
            ))
            
            print(f"✅ 스크립트 추가: {script_code}")
        
        # 저장
        conn.commit()
        
        # 결과 확인
        cursor.execute("SELECT COUNT(*) FROM custom_scripts")
        count = cursor.fetchone()[0]
        print(f"📊 총 {count}개의 커스텀 스크립트가 있습니다.")
        
        conn.close()
        print("🎯 테스트 데이터 추가 완료!")
        
    except Exception as e:
        print(f"❌ 오류 발생: {e}")

if __name__ == "__main__":
    add_test_scripts() 