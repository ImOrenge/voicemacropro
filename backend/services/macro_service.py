import json
from datetime import datetime
from typing import List, Dict, Optional
from backend.database.database_manager import db_manager

class MacroService:
    """
    매크로 관리를 담당하는 서비스 클래스
    매크로의 CRUD(생성, 읽기, 수정, 삭제) 기능을 제공합니다.
    """
    
    def __init__(self):
        """
        매크로 서비스 초기화
        """
        self.db = db_manager
    
    def create_macro(self, name: str, voice_command: str, action_type: str, 
                    key_sequence: str, settings: Dict = None) -> int:
        """
        새로운 매크로를 생성하는 함수
        Args:
            name (str): 매크로 이름
            voice_command (str): 음성 명령어
            action_type (str): 동작 타입 (combo, rapid, hold, toggle, repeat)
            key_sequence (str): 키 시퀀스
            settings (Dict): 추가 설정 정보
        Returns:
            int: 생성된 매크로의 ID
        """
        settings_json = json.dumps(settings) if settings else None
        
        query = '''
        INSERT INTO macros (name, voice_command, action_type, key_sequence, settings)
        VALUES (?, ?, ?, ?, ?)
        '''
        
        macro_id = self.db.execute_query(query, (name, voice_command, action_type, key_sequence, settings_json))
        
        # 로그 남기기
        self._log_action("INFO", f"새 매크로 생성: {name}", macro_id)
        
        return macro_id
    
    def get_all_macros(self, search_term: str = None, sort_by: str = "name") -> List[Dict]:
        """
        모든 매크로를 조회하는 함수
        Args:
            search_term (str): 검색어 (선택사항)
            sort_by (str): 정렬 기준 (name, created_at, usage_count)
        Returns:
            List[Dict]: 매크로 목록
        """
        base_query = '''
        SELECT id, name, voice_command, action_type, key_sequence, 
               settings, created_at, updated_at, usage_count, is_script, script_language
        FROM macros
        '''
        
        # 검색 조건 추가
        if search_term:
            base_query += " WHERE name LIKE ? OR voice_command LIKE ?"
            search_pattern = f"%{search_term}%"
            params = (search_pattern, search_pattern)
        else:
            params = None
        
        # 정렬 조건 추가
        sort_columns = {
            "name": "name ASC",
            "created_at": "created_at DESC",
            "usage_count": "usage_count DESC"
        }
        base_query += f" ORDER BY {sort_columns.get(sort_by, 'name ASC')}"
        
        rows = self.db.execute_query(base_query, params)
        
        # 결과를 딕셔너리 형태로 변환
        macros = []
        for row in rows:
            macro = {
                'id': row[0],
                'name': row[1],
                'voice_command': row[2],
                'action_type': row[3],
                'key_sequence': row[4],
                'settings': json.loads(row[5]) if row[5] else {},
                'created_at': row[6],
                'updated_at': row[7],
                'usage_count': row[8],
                'is_script': bool(row[9]) if len(row) > 9 and row[9] is not None else False,
                'script_language': row[10] if len(row) > 10 else None
            }
            macros.append(macro)
        
        return macros
    
    def get_macro_by_id(self, macro_id: int) -> Optional[Dict]:
        """
        ID로 특정 매크로를 조회하는 함수
        Args:
            macro_id (int): 매크로 ID
        Returns:
            Optional[Dict]: 매크로 정보 또는 None
        """
        query = '''
        SELECT id, name, voice_command, action_type, key_sequence, 
               settings, created_at, updated_at, usage_count, is_script, script_language
        FROM macros WHERE id = ?
        '''
        
        rows = self.db.execute_query(query, (macro_id,))
        
        if rows:
            row = rows[0]
            return {
                'id': row[0],
                'name': row[1],
                'voice_command': row[2],
                'action_type': row[3],
                'key_sequence': row[4],
                'settings': json.loads(row[5]) if row[5] else {},
                'created_at': row[6],
                'updated_at': row[7],
                'usage_count': row[8],
                'is_script': bool(row[9]) if len(row) > 9 and row[9] is not None else False,
                'script_language': row[10] if len(row) > 10 else None
            }
        return None
    
    def update_macro(self, macro_id: int, name: str = None, voice_command: str = None,
                    action_type: str = None, key_sequence: str = None, 
                    settings: Dict = None) -> bool:
        """
        기존 매크로를 수정하는 함수
        Args:
            macro_id (int): 수정할 매크로 ID
            name (str): 새로운 매크로 이름
            voice_command (str): 새로운 음성 명령어
            action_type (str): 새로운 동작 타입
            key_sequence (str): 새로운 키 시퀀스
            settings (Dict): 새로운 설정 정보
        Returns:
            bool: 수정 성공 여부
        """
        # 기존 매크로 정보 조회
        existing_macro = self.get_macro_by_id(macro_id)
        if not existing_macro:
            return False
        
        # 수정할 필드만 업데이트
        update_fields = []
        params = []
        
        if name is not None:
            update_fields.append("name = ?")
            params.append(name)
        
        if voice_command is not None:
            update_fields.append("voice_command = ?")
            params.append(voice_command)
        
        if action_type is not None:
            update_fields.append("action_type = ?")
            params.append(action_type)
        
        if key_sequence is not None:
            update_fields.append("key_sequence = ?")
            params.append(key_sequence)
        
        if settings is not None:
            update_fields.append("settings = ?")
            params.append(json.dumps(settings))
        
        update_fields.append("updated_at = ?")
        params.append(datetime.now().isoformat())
        
        params.append(macro_id)
        
        query = f"UPDATE macros SET {', '.join(update_fields)} WHERE id = ?"
        
        self.db.execute_query(query, tuple(params))
        
        # 로그 남기기
        self._log_action("INFO", f"매크로 수정: {existing_macro['name']}", macro_id)
        
        return True
    
    def copy_macro(self, macro_id: int, new_name: str = None) -> Optional[int]:
        """
        기존 매크로를 복사하여 새로운 매크로를 생성하는 함수
        Args:
            macro_id (int): 복사할 매크로 ID
            new_name (str): 새로운 매크로 이름 (기본값: "원본이름_복사")
        Returns:
            Optional[int]: 새로 생성된 매크로 ID 또는 None
        """
        original_macro = self.get_macro_by_id(macro_id)
        if not original_macro:
            return None
        
        # 새로운 이름 설정
        if new_name is None:
            new_name = f"{original_macro['name']}_복사"
        
        # 복사본 생성
        new_macro_id = self.create_macro(
            name=new_name,
            voice_command=original_macro['voice_command'],
            action_type=original_macro['action_type'],
            key_sequence=original_macro['key_sequence'],
            settings=original_macro['settings']
        )
        
        # 로그 남기기
        self._log_action("INFO", f"매크로 복사: {original_macro['name']} -> {new_name}", new_macro_id)
        
        return new_macro_id
    
    def delete_macro(self, macro_id: int) -> bool:
        """
        매크로를 삭제하는 함수
        커스텀 스크립트와 연결된 매크로인 경우 관련 스크립트도 함께 삭제합니다.
        Args:
            macro_id (int): 삭제할 매크로 ID
        Returns:
            bool: 삭제 성공 여부
        """
        # 매크로 정보 조회 (로그용)
        macro = self.get_macro_by_id(macro_id)
        if not macro:
            return False
        
        # 커스텀 스크립트 연결 여부 확인
        script_query = "SELECT id FROM custom_scripts WHERE macro_id = ?"
        script_rows = self.db.execute_query(script_query, (macro_id,))
        
        # 관련 커스텀 스크립트 삭제
        if script_rows:
            for script_row in script_rows:
                script_id = script_row[0]
                delete_script_query = "DELETE FROM custom_scripts WHERE id = ?"
                self.db.execute_query(delete_script_query, (script_id,))
                self._log_action("INFO", f"연결된 커스텀 스크립트 삭제: Script ID {script_id}", macro_id)
        
        # 매크로 삭제
        query = "DELETE FROM macros WHERE id = ?"
        self.db.execute_query(query, (macro_id,))
        
        # 로그 남기기
        if script_rows:
            self._log_action("INFO", f"매크로 삭제 (스크립트 포함): {macro['name']}", macro_id)
        else:
            self._log_action("INFO", f"매크로 삭제: {macro['name']}", macro_id)
        
        return True
    
    def increment_usage_count(self, macro_id: int):
        """
        매크로 사용 횟수를 증가시키는 함수
        Args:
            macro_id (int): 사용된 매크로 ID
        """
        query = "UPDATE macros SET usage_count = usage_count + 1 WHERE id = ?"
        self.db.execute_query(query, (macro_id,))
    
    def _log_action(self, level: str, message: str, macro_id: int = None):
        """
        매크로 작업 로그를 기록하는 내부 함수
        Args:
            level (str): 로그 레벨 (DEBUG, INFO, WARNING, ERROR)
            message (str): 로그 메시지
            macro_id (int): 관련 매크로 ID (선택사항)
        """
        query = "INSERT INTO logs (level, message, macro_id) VALUES (?, ?, ?)"
        self.db.execute_query(query, (level, message, macro_id))

# 매크로 서비스 인스턴스 생성
macro_service = MacroService() 