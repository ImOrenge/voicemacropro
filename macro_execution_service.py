"""
매크로 실행 서비스
게임 매크로의 다양한 동작 타입(콤보, 연사, 홀드, 토글, 반복)을 실제로 실행하는 서비스
PyAutoGUI를 사용하여 키보드/마우스 입력을 자동화합니다.
"""

import asyncio
import json
import logging
import pyautogui
import time
from datetime import datetime
from typing import Dict, List, Any, Optional
from threading import Thread, Event

# PyAutoGUI 설정
pyautogui.FAILSAFE = True  # 마우스를 화면 왼쪽 상단 모서리로 이동하면 프로그램 중지
pyautogui.PAUSE = 0.1  # 각 PyAutoGUI 함수 호출 사이의 일시 정지 시간

class MacroExecutionService:
    """
    매크로 실행을 담당하는 서비스 클래스
    """
    
    def __init__(self):
        """
        매크로 실행 서비스 초기화
        """
        self.logger = logging.getLogger(__name__)
        self.running_macros: Dict[str, Event] = {}  # 실행 중인 매크로들의 정지 이벤트
        self.toggle_states: Dict[int, bool] = {}  # 토글 매크로의 상태 저장
        
        # 키 매핑 테이블 (PyAutoGUI 키 이름과 일반적인 키 이름 매핑)
        self.key_mapping = {
            # 수정키
            'ctrl': 'ctrl',
            'control': 'ctrl',
            'alt': 'alt',
            'shift': 'shift',
            'win': 'winleft',
            'windows': 'winleft',
            'cmd': 'cmd',
            'command': 'cmd',
            
            # 기능키
            'enter': 'enter',
            'return': 'enter',
            'space': 'space',
            'spacebar': 'space',
            'tab': 'tab',
            'escape': 'esc',
            'esc': 'esc',
            'backspace': 'backspace',
            'delete': 'delete',
            'del': 'delete',
            
            # 방향키
            'up': 'up',
            'down': 'down',
            'left': 'left',
            'right': 'right',
            
            # 기타
            'home': 'home',
            'end': 'end',
            'pageup': 'pageup',
            'pagedown': 'pagedown',
            'insert': 'insert',
            'printscreen': 'printscreen',
            'pause': 'pause',
            
            # F키
            **{f'f{i}': f'f{i}' for i in range(1, 13)},
            
            # 숫자 키패드
            'numpad0': 'num0', 'numpad1': 'num1', 'numpad2': 'num2',
            'numpad3': 'num3', 'numpad4': 'num4', 'numpad5': 'num5',
            'numpad6': 'num6', 'numpad7': 'num7', 'numpad8': 'num8',
            'numpad9': 'num9',
        }
        
    def parse_key_sequence(self, key_sequence: str) -> List[str]:
        """
        키 시퀀스 문자열을 파싱하여 PyAutoGUI에서 사용할 수 있는 키 리스트로 변환
        
        Args:
            key_sequence (str): 키 시퀀스 문자열 (예: "Ctrl+C", "Alt+Tab", "Q,W,E")
            
        Returns:
            List[str]: 파싱된 키 리스트
        """
        try:
            # 콤마나 +로 구분된 키들을 분리
            if '+' in key_sequence:
                # Ctrl+C, Alt+Tab 형태
                keys = [key.strip().lower() for key in key_sequence.split('+')]
            elif ',' in key_sequence:
                # Q,W,E 형태
                keys = [key.strip().lower() for key in key_sequence.split(',')]
            else:
                # 단일 키
                keys = [key_sequence.strip().lower()]
            
            # 키 매핑 적용
            mapped_keys = []
            for key in keys:
                mapped_key = self.key_mapping.get(key, key)
                mapped_keys.append(mapped_key)
            
            return mapped_keys
            
        except Exception as e:
            self.logger.error(f"키 시퀀스 파싱 오류: {key_sequence} - {e}")
            return [key_sequence.lower()]
    
    def execute_key_combination(self, keys: List[str]) -> bool:
        """
        키 조합을 실행합니다 (Ctrl+C, Alt+Tab 등)
        
        Args:
            keys (List[str]): 실행할 키 리스트
            
        Returns:
            bool: 실행 성공 여부
        """
        try:
            if len(keys) == 1:
                # 단일 키 입력
                pyautogui.press(keys[0])
                self.logger.info(f"단일 키 실행: {keys[0]}")
            else:
                # 키 조합 입력
                pyautogui.hotkey(*keys)
                self.logger.info(f"키 조합 실행: {'+'.join(keys)}")
            
            return True
            
        except Exception as e:
            self.logger.error(f"키 실행 오류: {keys} - {e}")
            return False
    
    async def execute_combo_macro(self, macro_id: int, settings: Dict[str, Any]) -> bool:
        """
        콤보 매크로를 실행합니다
        
        Args:
            macro_id (int): 매크로 ID
            settings (Dict[str, Any]): 콤보 설정 (steps, default_delay_ms)
            
        Returns:
            bool: 실행 성공 여부
        """
        try:
            steps = settings.get('steps', [])
            default_delay_ms = settings.get('default_delay_ms', 100)
            
            self.logger.info(f"콤보 매크로 실행 시작: ID={macro_id}, 단계수={len(steps)}")
            
            for i, step in enumerate(steps):
                # 매크로가 중지되었는지 확인
                if str(macro_id) in self.running_macros and self.running_macros[str(macro_id)].is_set():
                    self.logger.info(f"콤보 매크로 중지됨: ID={macro_id}")
                    return False
                
                key_sequence = step.get('key_sequence', '')
                delay_after_ms = step.get('delay_after_ms', default_delay_ms)
                description = step.get('description', '')
                
                self.logger.info(f"콤보 단계 {i+1} 실행: {key_sequence} ({description})")
                
                # 키 시퀀스 파싱 및 실행
                keys = self.parse_key_sequence(key_sequence)
                if not self.execute_key_combination(keys):
                    self.logger.error(f"콤보 단계 {i+1} 실행 실패: {key_sequence}")
                    return False
                
                # 딜레이 대기
                if delay_after_ms > 0:
                    await asyncio.sleep(delay_after_ms / 1000.0)
            
            self.logger.info(f"콤보 매크로 완료: ID={macro_id}")
            return True
            
        except Exception as e:
            self.logger.error(f"콤보 매크로 실행 오류: ID={macro_id} - {e}")
            return False
    
    async def execute_rapid_macro(self, macro_id: int, settings: Dict[str, Any]) -> bool:
        """
        연사 매크로를 실행합니다
        
        Args:
            macro_id (int): 매크로 ID
            settings (Dict[str, Any]): 연사 설정 (key_sequence, clicks_per_second, duration_seconds, use_fixed_duration)
            
        Returns:
            bool: 실행 성공 여부
        """
        try:
            key_sequence = settings.get('key_sequence', '')
            clicks_per_second = settings.get('clicks_per_second', 10.0)
            duration_seconds = settings.get('duration_seconds', 1.0)
            use_fixed_duration = settings.get('use_fixed_duration', True)
            
            self.logger.info(f"연사 매크로 실행 시작: ID={macro_id}, 키={key_sequence}, 속도={clicks_per_second}CPS")
            
            # 키 시퀀스 파싱
            keys = self.parse_key_sequence(key_sequence)
            
            # 연사 간격 계산
            interval = 1.0 / clicks_per_second
            
            # 연사 실행
            start_time = time.time()
            click_count = 0
            
            while True:
                # 매크로가 중지되었는지 확인
                if str(macro_id) in self.running_macros and self.running_macros[str(macro_id)].is_set():
                    self.logger.info(f"연사 매크로 중지됨: ID={macro_id}, 실행횟수={click_count}")
                    return True
                
                # 고정 지속시간 모드에서 시간 체크
                if use_fixed_duration and (time.time() - start_time) >= duration_seconds:
                    self.logger.info(f"연사 매크로 완료: ID={macro_id}, 실행횟수={click_count}")
                    return True
                
                # 키 실행
                if not self.execute_key_combination(keys):
                    self.logger.error(f"연사 매크로 키 실행 실패: ID={macro_id}, 키={key_sequence}")
                    return False
                
                click_count += 1
                
                # 간격 대기
                await asyncio.sleep(interval)
            
        except Exception as e:
            self.logger.error(f"연사 매크로 실행 오류: ID={macro_id} - {e}")
            return False
    
    async def execute_hold_macro(self, macro_id: int, settings: Dict[str, Any]) -> bool:
        """
        홀드 매크로를 실행합니다
        
        Args:
            macro_id (int): 매크로 ID
            settings (Dict[str, Any]): 홀드 설정 (key_sequence, hold_duration_seconds, use_fixed_duration, release_command)
            
        Returns:
            bool: 실행 성공 여부
        """
        try:
            key_sequence = settings.get('key_sequence', '')
            hold_duration_seconds = settings.get('hold_duration_seconds', 1.0)
            use_fixed_duration = settings.get('use_fixed_duration', True)
            release_command = settings.get('release_command', '')
            
            self.logger.info(f"홀드 매크로 실행 시작: ID={macro_id}, 키={key_sequence}")
            
            # 키 시퀀스 파싱
            keys = self.parse_key_sequence(key_sequence)
            
            # 키 눌러서 유지 시작
            if len(keys) == 1:
                pyautogui.keyDown(keys[0])
                self.logger.info(f"홀드 시작: {keys[0]}")
            else:
                # 복합키의 경우 첫 번째 키만 홀드 (예: Ctrl+C에서 Ctrl만 홀드)
                pyautogui.keyDown(keys[0])
                self.logger.info(f"홀드 시작: {keys[0]} (복합키 중 첫 번째)")
            
            # 홀드 유지
            start_time = time.time()
            
            while True:
                # 매크로가 중지되었는지 확인
                if str(macro_id) in self.running_macros and self.running_macros[str(macro_id)].is_set():
                    break
                
                # 고정 지속시간 모드에서 시간 체크
                if use_fixed_duration and (time.time() - start_time) >= hold_duration_seconds:
                    break
                
                # 수동 해제 모드에서는 해제 명령어를 기다림 (TODO: 음성 인식과 연동)
                
                await asyncio.sleep(0.1)
            
            # 키 해제
            if len(keys) == 1:
                pyautogui.keyUp(keys[0])
                self.logger.info(f"홀드 해제: {keys[0]}")
            else:
                pyautogui.keyUp(keys[0])
                self.logger.info(f"홀드 해제: {keys[0]}")
            
            self.logger.info(f"홀드 매크로 완료: ID={macro_id}")
            return True
            
        except Exception as e:
            self.logger.error(f"홀드 매크로 실행 오류: ID={macro_id} - {e}")
            return False
    
    async def execute_toggle_macro(self, macro_id: int, settings: Dict[str, Any]) -> bool:
        """
        토글 매크로를 실행합니다
        
        Args:
            macro_id (int): 매크로 ID
            settings (Dict[str, Any]): 토글 설정 (key_sequence, toggle_off_command, show_status_indicator)
            
        Returns:
            bool: 실행 성공 여부
        """
        try:
            key_sequence = settings.get('key_sequence', '')
            toggle_off_command = settings.get('toggle_off_command', '')
            show_status_indicator = settings.get('show_status_indicator', True)
            
            # 현재 토글 상태 확인
            current_state = self.toggle_states.get(macro_id, False)
            new_state = not current_state
            
            self.logger.info(f"토글 매크로 실행: ID={macro_id}, 이전상태={current_state}, 새상태={new_state}")
            
            # 키 시퀀스 파싱 및 실행
            keys = self.parse_key_sequence(key_sequence)
            if not self.execute_key_combination(keys):
                self.logger.error(f"토글 매크로 키 실행 실패: ID={macro_id}, 키={key_sequence}")
                return False
            
            # 상태 업데이트
            self.toggle_states[macro_id] = new_state
            
            self.logger.info(f"토글 매크로 완료: ID={macro_id}, 현재상태={'ON' if new_state else 'OFF'}")
            return True
            
        except Exception as e:
            self.logger.error(f"토글 매크로 실행 오류: ID={macro_id} - {e}")
            return False
    
    async def execute_repeat_macro(self, macro_id: int, settings: Dict[str, Any]) -> bool:
        """
        반복 매크로를 실행합니다
        
        Args:
            macro_id (int): 매크로 ID
            settings (Dict[str, Any]): 반복 설정 (key_sequence, repeat_count, interval_seconds, stop_on_next_command)
            
        Returns:
            bool: 실행 성공 여부
        """
        try:
            key_sequence = settings.get('key_sequence', '')
            repeat_count = settings.get('repeat_count', 3)
            interval_seconds = settings.get('interval_seconds', 0.5)
            stop_on_next_command = settings.get('stop_on_next_command', False)
            
            self.logger.info(f"반복 매크로 실행 시작: ID={macro_id}, 키={key_sequence}, 횟수={repeat_count}")
            
            # 키 시퀀스 파싱
            keys = self.parse_key_sequence(key_sequence)
            
            # 반복 실행
            for i in range(repeat_count):
                # 매크로가 중지되었는지 확인
                if str(macro_id) in self.running_macros and self.running_macros[str(macro_id)].is_set():
                    self.logger.info(f"반복 매크로 중지됨: ID={macro_id}, 진행={i+1}/{repeat_count}")
                    return True
                
                self.logger.info(f"반복 실행 {i+1}/{repeat_count}: {key_sequence}")
                
                # 키 실행
                if not self.execute_key_combination(keys):
                    self.logger.error(f"반복 매크로 키 실행 실패: ID={macro_id}, 키={key_sequence}")
                    return False
                
                # 마지막 반복이 아닌 경우에만 간격 대기
                if i < repeat_count - 1:
                    await asyncio.sleep(interval_seconds)
            
            self.logger.info(f"반복 매크로 완료: ID={macro_id}")
            return True
            
        except Exception as e:
            self.logger.error(f"반복 매크로 실행 오류: ID={macro_id} - {e}")
            return False
    
    async def execute_macro(self, macro_data: Dict[str, Any]) -> bool:
        """
        매크로를 실행합니다
        
        Args:
            macro_data (Dict[str, Any]): 매크로 데이터 (id, name, action_type, settings 등)
            
        Returns:
            bool: 실행 성공 여부
        """
        try:
            macro_id = macro_data.get('id')
            macro_name = macro_data.get('name', '')
            action_type = macro_data.get('action_type', '').lower()
            settings_json = macro_data.get('settings', '{}')
            
            self.logger.info(f"매크로 실행 요청: ID={macro_id}, 이름={macro_name}, 타입={action_type}")
            
            # 설정 JSON 파싱
            try:
                settings = json.loads(settings_json) if settings_json else {}
            except json.JSONDecodeError as e:
                self.logger.error(f"매크로 설정 JSON 파싱 오류: {settings_json} - {e}")
                settings = {}
            
            # 매크로 실행 중지 이벤트 생성
            stop_event = Event()
            self.running_macros[str(macro_id)] = stop_event
            
            try:
                # 액션 타입에 따른 실행
                if action_type == 'combo':
                    success = await self.execute_combo_macro(macro_id, settings)
                elif action_type == 'rapid':
                    success = await self.execute_rapid_macro(macro_id, settings)
                elif action_type == 'hold':
                    success = await self.execute_hold_macro(macro_id, settings)
                elif action_type == 'toggle':
                    success = await self.execute_toggle_macro(macro_id, settings)
                elif action_type == 'repeat':
                    success = await self.execute_repeat_macro(macro_id, settings)
                else:
                    self.logger.error(f"지원되지 않는 매크로 타입: {action_type}")
                    success = False
                
                if success:
                    self.logger.info(f"매크로 실행 성공: ID={macro_id}, 이름={macro_name}")
                else:
                    self.logger.error(f"매크로 실행 실패: ID={macro_id}, 이름={macro_name}")
                
                return success
                
            finally:
                # 실행 완료 후 정리
                if str(macro_id) in self.running_macros:
                    del self.running_macros[str(macro_id)]
            
        except Exception as e:
            self.logger.error(f"매크로 실행 중 오류 발생: {e}")
            return False
    
    def stop_macro(self, macro_id: int) -> bool:
        """
        실행 중인 매크로를 중지합니다
        
        Args:
            macro_id (int): 중지할 매크로 ID
            
        Returns:
            bool: 중지 성공 여부
        """
        try:
            if str(macro_id) in self.running_macros:
                self.running_macros[str(macro_id)].set()
                self.logger.info(f"매크로 중지 신호 전송: ID={macro_id}")
                return True
            else:
                self.logger.warning(f"실행 중이지 않은 매크로 중지 시도: ID={macro_id}")
                return False
                
        except Exception as e:
            self.logger.error(f"매크로 중지 중 오류 발생: ID={macro_id} - {e}")
            return False
    
    def get_toggle_state(self, macro_id: int) -> bool:
        """
        토글 매크로의 현재 상태를 반환합니다
        
        Args:
            macro_id (int): 매크로 ID
            
        Returns:
            bool: 토글 상태 (True=ON, False=OFF)
        """
        return self.toggle_states.get(macro_id, False)
    
    def set_toggle_state(self, macro_id: int, state: bool) -> None:
        """
        토글 매크로의 상태를 설정합니다
        
        Args:
            macro_id (int): 매크로 ID
            state (bool): 설정할 상태 (True=ON, False=OFF)
        """
        self.toggle_states[macro_id] = state
        self.logger.info(f"토글 상태 설정: ID={macro_id}, 상태={'ON' if state else 'OFF'}")
    
    def get_running_macros(self) -> List[int]:
        """
        현재 실행 중인 매크로 ID 목록을 반환합니다
        
        Returns:
            List[int]: 실행 중인 매크로 ID 목록
        """
        return [int(macro_id) for macro_id in self.running_macros.keys()]

# 싱글톤 인스턴스
macro_execution_service = MacroExecutionService() 