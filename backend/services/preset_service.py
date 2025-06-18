import json
import os
from datetime import datetime
from typing import List, Dict, Optional, Union
from backend.database.database_manager import db_manager

class PresetService:
    """
    프리셋 관리를 담당하는 서비스 클래스
    매크로 집합을 프리셋으로 관리하여 게임별 또는 상황별로 빠르게 전환할 수 있습니다.
    프리셋 CRUD, JSON 파일 내보내기/가져오기, 즐겨찾기 기능을 제공합니다.
    """
    
    def __init__(self):
        """프리셋 서비스 초기화"""
        self.presets_folder = "presets"
        self._ensure_presets_folder()
    
    def _ensure_presets_folder(self):
        """프리셋 저장용 폴더가 없으면 생성하는 함수"""
        if not os.path.exists(self.presets_folder):
            os.makedirs(self.presets_folder)
    
    def create_preset(self, name: str, description: str = "", macro_ids: List[int] = None, 
                     is_favorite: bool = False) -> int:
        """
        새로운 프리셋을 생성하는 함수
        
        Args:
            name (str): 프리셋 이름 - 고유해야 함
            description (str): 프리셋 설명 (선택사항)
            macro_ids (List[int]): 포함할 매크로 ID 목록
            is_favorite (bool): 즐겨찾기 여부
        
        Returns:
            int: 생성된 프리셋의 ID
            
        Raises:
            ValueError: 필수 매개변수가 누락되거나 중복된 이름인 경우
            DatabaseError: 데이터베이스 저장 실패시
        """
        if not name or not name.strip():
            raise ValueError("프리셋 이름은 필수입니다")
        
        if macro_ids is None:
            macro_ids = []
        
        # 중복 이름 확인
        if self._is_duplicate_name(name):
            raise ValueError(f"프리셋 이름 '{name}'은 이미 사용 중입니다")
        
        # 매크로 ID 유효성 검증
        self._validate_macro_ids(macro_ids)
        
        # 프리셋 데이터 준비
        preset_data = {
            'name': name.strip(),
            'description': description.strip() if description else "",
            'macro_ids': json.dumps(macro_ids),
            'is_favorite': is_favorite,
            'is_active': True,
            'created_at': datetime.now().isoformat(),
            'updated_at': datetime.now().isoformat()
        }
        
        # 데이터베이스에 저장
        query = '''
        INSERT INTO presets (name, description, macro_ids, is_favorite, is_active, 
                           created_at, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?)
        '''
        params = (
            preset_data['name'],
            preset_data['description'],
            preset_data['macro_ids'],
            preset_data['is_favorite'],
            preset_data['is_active'],
            preset_data['created_at'],
            preset_data['updated_at']
        )
        
        preset_id = db_manager.execute_query(query, params)
        return preset_id
    
    def get_preset_by_id(self, preset_id: int) -> Optional[Dict]:
        """
        ID로 프리셋을 조회하는 함수
        
        Args:
            preset_id (int): 조회할 프리셋 ID
            
        Returns:
            Dict: 프리셋 정보 또는 None
        """
        query = "SELECT * FROM presets WHERE id = ? AND is_active = 1"
        result = db_manager.execute_query(query, (preset_id,))
        
        if result:
            return self._format_preset_data(result[0])
        return None
    
    def get_all_presets(self, include_inactive: bool = False) -> List[Dict]:
        """
        모든 프리셋 목록을 조회하는 함수
        
        Args:
            include_inactive (bool): 비활성 프리셋 포함 여부
            
        Returns:
            List[Dict]: 프리셋 목록
        """
        if include_inactive:
            query = "SELECT * FROM presets ORDER BY is_favorite DESC, name ASC"
            params = None
        else:
            query = "SELECT * FROM presets WHERE is_active = 1 ORDER BY is_favorite DESC, name ASC"
            params = None
        
        results = db_manager.execute_query(query, params)
        return [self._format_preset_data(row) for row in results]
    
    def get_favorite_presets(self) -> List[Dict]:
        """
        즐겨찾기 프리셋 목록을 조회하는 함수
        
        Returns:
            List[Dict]: 즐겨찾기 프리셋 목록
        """
        query = '''
        SELECT * FROM presets 
        WHERE is_favorite = 1 AND is_active = 1 
        ORDER BY name ASC
        '''
        results = db_manager.execute_query(query)
        return [self._format_preset_data(row) for row in results]
    
    def update_preset(self, preset_id: int, name: str = None, description: str = None,
                     macro_ids: List[int] = None, is_favorite: bool = None) -> bool:
        """
        프리셋 정보를 수정하는 함수
        
        Args:
            preset_id (int): 수정할 프리셋 ID
            name (str): 새로운 프리셋 이름 (선택사항)
            description (str): 새로운 설명 (선택사항)
            macro_ids (List[int]): 새로운 매크로 ID 목록 (선택사항)
            is_favorite (bool): 즐겨찾기 여부 (선택사항)
            
        Returns:
            bool: 수정 성공 여부
        """
        # 프리셋 존재 확인
        existing_preset = self.get_preset_by_id(preset_id)
        if not existing_preset:
            return False
        
        # 수정할 필드들 준비
        update_fields = []
        update_values = []
        
        if name is not None and name.strip():
            # 중복 이름 확인 (자기 자신 제외)
            if name.strip() != existing_preset['name'] and self._is_duplicate_name(name.strip()):
                raise ValueError(f"프리셋 이름 '{name.strip()}'은 이미 사용 중입니다")
            update_fields.append("name = ?")
            update_values.append(name.strip())
        
        if description is not None:
            update_fields.append("description = ?")
            update_values.append(description.strip())
        
        if macro_ids is not None:
            self._validate_macro_ids(macro_ids)
            update_fields.append("macro_ids = ?")
            update_values.append(json.dumps(macro_ids))
        
        if is_favorite is not None:
            update_fields.append("is_favorite = ?")
            update_values.append(is_favorite)
        
        if not update_fields:
            return True  # 수정할 내용이 없음
        
        # updated_at 필드 추가
        update_fields.append("updated_at = ?")
        update_values.append(datetime.now().isoformat())
        
        # 쿼리 실행
        query = f"UPDATE presets SET {', '.join(update_fields)} WHERE id = ?"
        update_values.append(preset_id)
        
        db_manager.execute_query(query, tuple(update_values))
        return True
    
    def delete_preset(self, preset_id: int, hard_delete: bool = False) -> bool:
        """
        프리셋을 삭제하는 함수
        
        Args:
            preset_id (int): 삭제할 프리셋 ID
            hard_delete (bool): 완전 삭제 여부 (기본값: 논리적 삭제)
            
        Returns:
            bool: 삭제 성공 여부
        """
        # 프리셋 존재 확인
        existing_preset = self.get_preset_by_id(preset_id)
        if not existing_preset:
            return False
        
        if hard_delete:
            # 완전 삭제
            query = "DELETE FROM presets WHERE id = ?"
        else:
            # 논리적 삭제
            query = "UPDATE presets SET is_active = 0, updated_at = ? WHERE id = ?"
            db_manager.execute_query(query, (datetime.now().isoformat(), preset_id))
            return True
        
        db_manager.execute_query(query, (preset_id,))
        return True
    
    def copy_preset(self, preset_id: int, new_name: str = None) -> int:
        """
        프리셋을 복사하는 함수
        
        Args:
            preset_id (int): 복사할 프리셋 ID
            new_name (str): 새로운 프리셋 이름 (기본값: "원본이름 - 복사본")
            
        Returns:
            int: 복사된 프리셋의 ID
        """
        # 원본 프리셋 조회
        original_preset = self.get_preset_by_id(preset_id)
        if not original_preset:
            raise ValueError("복사할 프리셋을 찾을 수 없습니다")
        
        # 새 이름 생성
        if not new_name:
            new_name = f"{original_preset['name']} - 복사본"
            counter = 1
            while self._is_duplicate_name(new_name):
                new_name = f"{original_preset['name']} - 복사본 ({counter})"
                counter += 1
        
        # 새 프리셋 생성
        return self.create_preset(
            name=new_name,
            description=original_preset['description'],
            macro_ids=original_preset['macro_ids'],
            is_favorite=False  # 복사본은 즐겨찾기에서 제외
        )
    
    def toggle_favorite(self, preset_id: int) -> bool:
        """
        프리셋 즐겨찾기 상태를 토글하는 함수
        
        Args:
            preset_id (int): 토글할 프리셋 ID
            
        Returns:
            bool: 새로운 즐겨찾기 상태
        """
        preset = self.get_preset_by_id(preset_id)
        if not preset:
            raise ValueError("프리셋을 찾을 수 없습니다")
        
        new_favorite_status = not preset['is_favorite']
        self.update_preset(preset_id, is_favorite=new_favorite_status)
        return new_favorite_status
    
    def apply_preset(self, preset_id: int) -> bool:
        """
        프리셋을 적용하는 함수 (현재는 활성화만 처리, 향후 확장 가능)
        
        Args:
            preset_id (int): 적용할 프리셋 ID
            
        Returns:
            bool: 적용 성공 여부
        """
        preset = self.get_preset_by_id(preset_id)
        if not preset:
            return False
        
        # 향후 프리셋 적용 로직 구현
        # 예: 매크로 활성화/비활성화, 설정 적용 등
        
        return True
    
    def export_preset_to_json(self, preset_id: int, file_path: str = None) -> str:
        """
        프리셋을 JSON 파일로 내보내는 함수
        
        Args:
            preset_id (int): 내보낼 프리셋 ID
            file_path (str): 저장할 파일 경로 (선택사항)
            
        Returns:
            str: 저장된 파일 경로
        """
        preset = self.get_preset_by_id(preset_id)
        if not preset:
            raise ValueError("내보낼 프리셋을 찾을 수 없습니다")
        
        # 매크로 정보도 함께 포함
        macro_details = self._get_macros_for_preset(preset['macro_ids'])
        
        export_data = {
            'preset_info': {
                'name': preset['name'],
                'description': preset['description'],
                'created_at': preset['created_at'],
                'exported_at': datetime.now().isoformat()
            },
            'macros': macro_details,
            'export_metadata': {
                'version': '1.0',
                'exported_by': 'VoiceMacro Pro',
                'format': 'preset_export'
            }
        }
        
        # 파일 경로 생성
        if not file_path:
            safe_name = "".join(c for c in preset['name'] if c.isalnum() or c in (' ', '-', '_')).rstrip()
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            file_path = os.path.join(self.presets_folder, f"{safe_name}_{timestamp}.json")
        
        # JSON 파일로 저장
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(export_data, f, ensure_ascii=False, indent=2)
        
        return file_path
    
    def import_preset_from_json(self, file_path: str, preset_name: str = None) -> int:
        """
        JSON 파일에서 프리셋을 가져오는 함수
        
        Args:
            file_path (str): 가져올 JSON 파일 경로
            preset_name (str): 새로운 프리셋 이름 (선택사항)
            
        Returns:
            int: 가져온 프리셋의 ID
        """
        if not os.path.exists(file_path):
            raise ValueError("파일을 찾을 수 없습니다")
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                import_data = json.load(f)
        except json.JSONDecodeError:
            raise ValueError("올바르지 않은 JSON 파일입니다")
        
        # 데이터 유효성 검사
        if 'preset_info' not in import_data or 'macros' not in import_data:
            raise ValueError("올바르지 않은 프리셋 파일 형식입니다")
        
        preset_info = import_data['preset_info']
        macros_data = import_data['macros']
        
        # 프리셋 이름 결정
        if not preset_name:
            preset_name = preset_info['name']
            if self._is_duplicate_name(preset_name):
                counter = 1
                while self._is_duplicate_name(f"{preset_name} - 가져온항목 ({counter})"):
                    counter += 1
                preset_name = f"{preset_name} - 가져온항목 ({counter})"
        
        # 매크로들을 먼저 가져오기 (또는 기존 매크로와 매칭)
        macro_ids = self._import_macros_from_data(macros_data)
        
        # 프리셋 생성
        return self.create_preset(
            name=preset_name,
            description=preset_info.get('description', ''),
            macro_ids=macro_ids,
            is_favorite=False
        )
    
    def search_presets(self, search_term: str) -> List[Dict]:
        """
        프리셋을 검색하는 함수
        
        Args:
            search_term (str): 검색어
            
        Returns:
            List[Dict]: 검색 결과 프리셋 목록
        """
        if not search_term.strip():
            return self.get_all_presets()
        
        search_pattern = f"%{search_term.strip()}%"
        query = '''
        SELECT * FROM presets 
        WHERE is_active = 1 AND (name LIKE ? OR description LIKE ?)
        ORDER BY is_favorite DESC, name ASC
        '''
        
        results = db_manager.execute_query(query, (search_pattern, search_pattern))
        return [self._format_preset_data(row) for row in results]
    
    def get_preset_statistics(self) -> Dict:
        """
        프리셋 통계 정보를 조회하는 함수
        
        Returns:
            Dict: 통계 정보
        """
        stats = {}
        
        # 전체 프리셋 수
        query = "SELECT COUNT(*) FROM presets WHERE is_active = 1"
        total_count = db_manager.execute_query(query)[0][0]
        stats['total_presets'] = total_count
        
        # 즐겨찾기 프리셋 수
        query = "SELECT COUNT(*) FROM presets WHERE is_active = 1 AND is_favorite = 1"
        favorite_count = db_manager.execute_query(query)[0][0]
        stats['favorite_presets'] = favorite_count
        
        # 최근 생성된 프리셋
        query = '''
        SELECT name, created_at FROM presets 
        WHERE is_active = 1 
        ORDER BY created_at DESC 
        LIMIT 1
        '''
        recent_result = db_manager.execute_query(query)
        if recent_result:
            stats['most_recent_preset'] = {
                'name': recent_result[0][0],
                'created_at': recent_result[0][1]
            }
        
        return stats
    
    def _format_preset_data(self, row) -> Dict:
        """
        데이터베이스 행을 프리셋 딕셔너리로 변환하는 함수
        
        Args:
            row: 데이터베이스 행 데이터
            
        Returns:
            Dict: 프리셋 정보
        """
        try:
            macro_ids = json.loads(row[3]) if row[3] else []
        except json.JSONDecodeError:
            macro_ids = []
        
        return {
            'id': row[0],
            'name': row[1],
            'description': row[2] if row[2] else "",
            'macro_ids': macro_ids,
            'macro_count': len(macro_ids),
            'is_favorite': bool(row[4]),
            'is_active': bool(row[5]),
            'created_at': row[6],
            'updated_at': row[7]
        }
    
    def _is_duplicate_name(self, name: str) -> bool:
        """
        프리셋 이름 중복 확인 함수
        
        Args:
            name (str): 확인할 이름
            
        Returns:
            bool: 중복 여부
        """
        query = "SELECT COUNT(*) FROM presets WHERE name = ? AND is_active = 1"
        result = db_manager.execute_query(query, (name.strip(),))
        return result[0][0] > 0
    
    def _validate_macro_ids(self, macro_ids: List[int]):
        """
        매크로 ID 목록의 유효성을 검증하는 함수
        
        Args:
            macro_ids (List[int]): 검증할 매크로 ID 목록
            
        Raises:
            ValueError: 유효하지 않은 매크로 ID가 포함된 경우
        """
        if not macro_ids:
            return
        
        # 매크로 존재 여부 확인
        placeholders = ','.join(['?'] * len(macro_ids))
        query = f"SELECT COUNT(*) FROM macros WHERE id IN ({placeholders}) AND is_active = 1"
        result = db_manager.execute_query(query, tuple(macro_ids))
        
        if result[0][0] != len(macro_ids):
            raise ValueError("일부 매크로 ID가 유효하지 않습니다")
    
    def _get_macros_for_preset(self, macro_ids: List[int]) -> List[Dict]:
        """
        프리셋에 포함된 매크로들의 상세 정보를 조회하는 함수
        
        Args:
            macro_ids (List[int]): 매크로 ID 목록
            
        Returns:
            List[Dict]: 매크로 상세 정보 목록
        """
        if not macro_ids:
            return []
        
        placeholders = ','.join(['?'] * len(macro_ids))
        query = f'''
        SELECT id, name, voice_command, action_type, key_sequence, settings
        FROM macros 
        WHERE id IN ({placeholders}) AND is_active = 1
        ORDER BY name
        '''
        
        results = db_manager.execute_query(query, tuple(macro_ids))
        macros = []
        
        for row in results:
            try:
                settings = json.loads(row[5]) if row[5] else {}
            except json.JSONDecodeError:
                settings = {}
            
            macros.append({
                'id': row[0],
                'name': row[1],
                'voice_command': row[2],
                'action_type': row[3],
                'key_sequence': row[4],
                'settings': settings
            })
        
        return macros
    
    def _import_macros_from_data(self, macros_data: List[Dict]) -> List[int]:
        """
        가져온 데이터에서 매크로들을 처리하는 함수
        기존 매크로와 매칭하거나 새로 생성합니다.
        
        Args:
            macros_data (List[Dict]): 가져올 매크로 데이터 목록
            
        Returns:
            List[int]: 처리된 매크로 ID 목록
        """
        # 이 함수는 macro_service.py와 연동이 필요하므로 간단하게 구현
        # 실제로는 매크로 서비스와 협력하여 매크로를 생성/매칭해야 함
        
        macro_ids = []
        for macro_data in macros_data:
            # 기존 매크로와 이름/음성명령어가 일치하는지 확인
            query = '''
            SELECT id FROM macros 
            WHERE name = ? AND voice_command = ? AND is_active = 1
            LIMIT 1
            '''
            existing = db_manager.execute_query(
                query, 
                (macro_data['name'], macro_data['voice_command'])
            )
            
            if existing:
                # 기존 매크로 사용
                macro_ids.append(existing[0][0])
            else:
                # 새 매크로 생성 (간단한 버전)
                create_query = '''
                INSERT INTO macros (name, voice_command, action_type, key_sequence, settings)
                VALUES (?, ?, ?, ?, ?)
                '''
                new_macro_id = db_manager.execute_query(
                    create_query,
                    (
                        f"{macro_data['name']} (가져온항목)",
                        macro_data['voice_command'],
                        macro_data['action_type'],
                        macro_data['key_sequence'],
                        json.dumps(macro_data.get('settings', {}))
                    )
                )
                macro_ids.append(new_macro_id)
        
        return macro_ids

# 프리셋 서비스 인스턴스 생성
preset_service = PresetService() 