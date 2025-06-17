import sqlite3
import json
from datetime import datetime
from typing import List, Dict, Optional

class DatabaseManager:
    """
    데이터베이스 관리를 담당하는 클래스
    SQLite를 사용하여 매크로 데이터를 저장하고 관리합니다.
    스키마 버전 관리와 데이터 무결성을 보장합니다.
    """
    
    # 스키마 버전 관리 (향후 데이터베이스 구조 변경 시 버전 업그레이드 용도)
    SCHEMA_VERSION = 1
    
    def __init__(self, db_path: str = "voice_macro.db"):
        """
        데이터베이스 초기화 함수
        Args:
            db_path (str): 데이터베이스 파일 경로
        """
        self.db_path = db_path
        self.init_database()
    
    def init_database(self):
        """
        데이터베이스 테이블을 초기화하는 함수
        스키마 버전 관리와 함께 매크로, 프리셋, 로그 테이블을 생성합니다.
        """
        conn = sqlite3.connect(self.db_path)
        cursor = conn.cursor()
        
        # 스키마 버전 확인 및 마이그레이션
        current_version = self._get_schema_version(cursor)
        if current_version < self.SCHEMA_VERSION:
            self._migrate_schema(cursor, current_version)
        
        conn.commit()
        conn.close()
    
    def _get_schema_version(self, cursor):
        """
        현재 데이터베이스의 스키마 버전을 가져오는 함수
        Args:
            cursor: 데이터베이스 커서
        Returns:
            int: 현재 스키마 버전 (기본값: 0)
        """
        try:
            cursor.execute("SELECT version FROM schema_version ORDER BY id DESC LIMIT 1")
            result = cursor.fetchone()
            return result[0] if result else 0
        except sqlite3.OperationalError:
            # schema_version 테이블이 없으면 버전 0
            return 0
    
    def _migrate_schema(self, cursor, from_version):
        """
        스키마 마이그레이션을 실행하는 함수
        Args:
            cursor: 데이터베이스 커서
            from_version (int): 현재 버전
        """
        if from_version == 0:
            # 초기 스키마 생성
            self._create_initial_schema(cursor)
        
        # 향후 버전 업그레이드 로직 추가
        # if from_version == 1:
        #     self._migrate_v1_to_v2(cursor)
    
    def _create_initial_schema(self, cursor):
        """
        초기 데이터베이스 스키마를 생성하는 함수
        Args:
            cursor: 데이터베이스 커서
        """
        # 스키마 버전 관리 테이블 생성
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS schema_version (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            version INTEGER NOT NULL,
            applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
        ''')
        
        # 매크로 테이블 생성 (개선된 스키마)
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS macros (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            voice_command TEXT NOT NULL,
            action_type TEXT NOT NULL,
            key_sequence TEXT NOT NULL,
            settings TEXT,
            is_active BOOLEAN DEFAULT TRUE,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            usage_count INTEGER DEFAULT 0,
            version INTEGER DEFAULT 1
        )
        ''')
        
        # 프리셋 테이블 생성 (개선된 스키마)
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS presets (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            description TEXT,
            macro_ids TEXT NOT NULL,
            is_favorite BOOLEAN DEFAULT FALSE,
            is_active BOOLEAN DEFAULT TRUE,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
        ''')
        
        # 로그 테이블 생성 (개선된 스키마)
        cursor.execute('''
        CREATE TABLE IF NOT EXISTS logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            level TEXT NOT NULL,
            message TEXT NOT NULL,
            macro_id INTEGER,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (macro_id) REFERENCES macros (id) ON DELETE CASCADE
        )
        ''')
        
        # 성능 향상을 위한 인덱스 생성
        self._create_indexes(cursor)
        
        # 현재 스키마 버전 기록
        cursor.execute("INSERT INTO schema_version (version) VALUES (?)", (self.SCHEMA_VERSION,))
    
    def _create_indexes(self, cursor):
        """
        검색 성능 향상을 위한 인덱스를 생성하는 함수
        Args:
            cursor: 데이터베이스 커서
        """
        # 매크로 테이블 인덱스
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_macros_name ON macros(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_macros_voice_command ON macros(voice_command)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_macros_created_at ON macros(created_at)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_macros_usage_count ON macros(usage_count)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_macros_is_active ON macros(is_active)")
        
        # 프리셋 테이블 인덱스
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_presets_name ON presets(name)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_presets_is_favorite ON presets(is_favorite)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_presets_is_active ON presets(is_active)")
        
        # 로그 테이블 인덱스
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_logs_level ON logs(level)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_logs_created_at ON logs(created_at)")
        cursor.execute("CREATE INDEX IF NOT EXISTS idx_logs_macro_id ON logs(macro_id)")
    
    def get_connection(self):
        """
        데이터베이스 연결을 반환하는 함수
        Returns:
            sqlite3.Connection: 데이터베이스 연결 객체
        """
        return sqlite3.connect(self.db_path)
    
    def execute_query(self, query: str, params: tuple = None):
        """
        SQL 쿼리를 실행하는 함수
        Args:
            query (str): 실행할 SQL 쿼리
            params (tuple): 쿼리 매개변수
        Returns:
            list: 쿼리 실행 결과
        """
        conn = self.get_connection()
        cursor = conn.cursor()
        
        if params:
            cursor.execute(query, params)
        else:
            cursor.execute(query)
        
        if query.strip().upper().startswith('SELECT'):
            result = cursor.fetchall()
        else:
            conn.commit()
            result = cursor.lastrowid
        
        conn.close()
        return result

# 데이터베이스 매니저 인스턴스 생성
db_manager = DatabaseManager() 