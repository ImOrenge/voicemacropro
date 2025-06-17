"""
VoiceMacro Pro - 매크로 실행 서비스
PyAutoGUI를 사용하여 실제 키보드/마우스 매크로를 실행합니다.
"""

import pyautogui
import time
import threading
from typing import Dict, Any, Optional, List
import json
import logging
from datetime import datetime

from common_utils import get_logger
from macro_service import macro_service


class MacroExecutionService:
    """
    매크로 실행 서비스 클래스
    - 다양한 타입의 매크로 실행 (combo, rapid, hold, toggle, repeat)
    - PyAutoGUI를 사용한 키보드/마우스 제어
    - 안전 기능 및 오류 처리
    """
    
    def __init__(self):
        """매크로 실행 서비스 초기화"""
        self.logger = get_logger(__name__)
        
        # PyAutoGUI 설정
        pyautogui.FAILSAFE = True  # 마우스를 화면 왼쪽 위 모서리로 이동시 중단
        pyautogui.PAUSE = 0.1  # 각 명령 사이 기본 대기 시간
        
        # 실행 중인 매크로 추적
        self._running_macros: Dict[int, threading.Thread] = {}
        self._toggle_states: Dict[int, bool] = {}
        
        self.logger.info("매크로 실행 서비스가 초기화되었습니다.")
    
    def _parse_key_sequence(self, key_sequence: str) -> List[str]:
        """
        키 시퀀스 문자열을 개별 키 리스트로 파싱
        
        Args:
            key_sequence (str): 키 시퀀스 (예: "q,w,e" 또는 "ctrl+s")
            
        Returns:
            List[str]: 파싱된 키 리스트
        """
        if not key_sequence:
            return []
        
        # 콤마로 분리된 키들을 처리
        keys = [key.strip() for key in key_sequence.split(',')]
        return keys
    
    def _execute_key(self, key: str):
        """
        개별 키 또는 키 조합을 실행
        
        Args:
            key (str): 실행할 키 (예: "q", "ctrl+s", "space", "left_click")
        """
        try:
            key = key.strip().lower()
            
            # 마우스 클릭 처리
            if key == "left_click":
                pyautogui.click()
                return
            elif key == "right_click":
                pyautogui.rightClick()
                return
            elif key == "middle_click":
                pyautogui.middleClick()
                return
            
            # 키 조합 처리 (ctrl+s, alt+tab 등)
            if '+' in key:
                modifier_keys = key.split('+')
                pyautogui.hotkey(*modifier_keys)
                return
            
            # 특수 키 매핑
            special_keys = {
                'space': 'space',
                'enter': 'enter',
                'tab': 'tab',
                'shift': 'shift',
                'ctrl': 'ctrl',
                'alt': 'alt',
                'esc': 'escape',
                'backspace': 'backspace',
                'delete': 'delete',
                'up': 'up',
                'down': 'down',
                'left': 'left', 
                'right': 'right',
                'home': 'home',
                'end': 'end',
                'pageup': 'pageup',
                'pagedown': 'pagedown'
            }
            
            # 특수 키 또는 일반 키 실행
            actual_key = special_keys.get(key, key)
            pyautogui.press(actual_key)
            
        except Exception as e:
            self.logger.error(f"키 실행 중 오류: {key} - {e}")
            raise
    
    def execute_combo_macro(self, keys: List[str], settings: Dict[str, Any]):
        """
        콤보 매크로 실행 (키를 순차적으로 입력)
        
        Args:
            keys (List[str]): 실행할 키 리스트
            settings (Dict): 매크로 설정 (delay 등)
        """
        delay = settings.get('delay', 0.1)
        
        for key in keys:
            self._execute_key(key)
            if delay > 0:
                time.sleep(delay)
    
    def execute_rapid_macro(self, keys: List[str], settings: Dict[str, Any]):
        """
        연사 매크로 실행 (키를 빠르게 반복 입력)
        
        Args:
            keys (List[str]): 실행할 키 리스트
            settings (Dict): 매크로 설정 (rate, duration 등)
        """
        rate = settings.get('rate', 10)  # 초당 횟수
        duration = settings.get('duration', 2.0)  # 지속 시간
        
        interval = 1.0 / rate
        end_time = time.time() + duration
        
        while time.time() < end_time:
            for key in keys:
                self._execute_key(key)
                time.sleep(interval)
                
                if time.time() >= end_time:
                    break
    
    def execute_hold_macro(self, keys: List[str], settings: Dict[str, Any]):
        """
        홀드 매크로 실행 (키를 눌러서 유지)
        
        Args:
            keys (List[str]): 실행할 키 리스트
            settings (Dict): 매크로 설정 (duration 등)
        """
        duration = settings.get('duration', 3.0)
        
        try:
            # 키 누르기
            for key in keys:
                if key.strip().lower() in ['shift', 'ctrl', 'alt']:
                    pyautogui.keyDown(key.strip().lower())
            
            # 지정된 시간만큼 대기
            time.sleep(duration)
            
        finally:
            # 키 떼기
            for key in keys:
                if key.strip().lower() in ['shift', 'ctrl', 'alt']:
                    pyautogui.keyUp(key.strip().lower())
    
    def execute_toggle_macro(self, macro_id: int, keys: List[str], settings: Dict[str, Any]):
        """
        토글 매크로 실행 (ON/OFF 전환)
        
        Args:
            macro_id (int): 매크로 ID
            keys (List[str]): 실행할 키 리스트
            settings (Dict): 매크로 설정
        """
        current_state = self._toggle_states.get(macro_id, False)
        
        if not current_state:
            # 토글 ON
            for key in keys:
                if key.strip().lower() in ['shift', 'ctrl', 'alt']:
                    pyautogui.keyDown(key.strip().lower())
            self._toggle_states[macro_id] = True
            self.logger.info(f"토글 매크로 ON: {macro_id}")
        else:
            # 토글 OFF
            for key in keys:
                if key.strip().lower() in ['shift', 'ctrl', 'alt']:
                    pyautogui.keyUp(key.strip().lower())
            self._toggle_states[macro_id] = False
            self.logger.info(f"토글 매크로 OFF: {macro_id}")
    
    def execute_repeat_macro(self, keys: List[str], settings: Dict[str, Any]):
        """
        반복 매크로 실행 (지정된 횟수만큼 반복)
        
        Args:
            keys (List[str]): 실행할 키 리스트
            settings (Dict): 매크로 설정 (count, interval 등)
        """
        count = settings.get('count', 3)
        interval = settings.get('interval', 1.0)
        
        for i in range(count):
            for key in keys:
                self._execute_key(key)
            
            if i < count - 1:  # 마지막 반복이 아닌 경우만 대기
                time.sleep(interval)
    
    def execute_macro(self, macro_id: int) -> Dict[str, Any]:
        """
        매크로 ID로 매크로를 실행
        
        Args:
            macro_id (int): 실행할 매크로 ID
            
        Returns:
            Dict: 실행 결과
        """
        result = {
            'success': False,
            'message': '',
            'execution_time': 0,
            'error': None
        }
        
        start_time = time.time()
        
        try:
            # 매크로 정보 가져오기
            macro = macro_service.get_macro_by_id(macro_id)
            if not macro:
                result['error'] = f'매크로를 찾을 수 없습니다: ID {macro_id}'
                return result
            
            # 키 시퀀스 파싱
            keys = self._parse_key_sequence(macro['key_sequence'])
            if not keys:
                result['error'] = '키 시퀀스가 비어있습니다'
                return result
            
            # 설정 파싱
            settings = macro.get('settings', {})
            if isinstance(settings, str):
                try:
                    settings = json.loads(settings)
                except:
                    settings = {}
            
            action_type = macro['action_type']
            
            self.logger.info(f"매크로 실행 시작: {macro['name']} (ID: {macro_id}, 타입: {action_type})")
            
            # 타입별 매크로 실행
            if action_type == 'combo':
                self.execute_combo_macro(keys, settings)
            elif action_type == 'rapid':
                self.execute_rapid_macro(keys, settings)
            elif action_type == 'hold':
                self.execute_hold_macro(keys, settings)
            elif action_type == 'toggle':
                self.execute_toggle_macro(macro_id, keys, settings)
            elif action_type == 'repeat':
                self.execute_repeat_macro(keys, settings)
            else:
                result['error'] = f'지원하지 않는 매크로 타입: {action_type}'
                return result
            
            # 사용 횟수 증가
            try:
                macro_service.increment_usage_count(macro_id)
            except Exception as e:
                self.logger.warning(f"사용 횟수 업데이트 실패: {e}")
            
            execution_time = time.time() - start_time
            
            result['success'] = True
            result['message'] = f'매크로 실행 완료: {macro["name"]}'
            result['execution_time'] = execution_time
            
            self.logger.info(f"매크로 실행 완료: {macro['name']} ({execution_time:.3f}초)")
            
        except Exception as e:
            execution_time = time.time() - start_time
            result['error'] = f'매크로 실행 중 오류: {str(e)}'
            result['execution_time'] = execution_time
            
            self.logger.error(f"매크로 실행 실패: ID {macro_id} - {e}")
        
        return result
    
    def stop_all_toggle_macros(self):
        """
        모든 토글 매크로를 중지 (비상 정지)
        """
        try:
            for macro_id in list(self._toggle_states.keys()):
                if self._toggle_states[macro_id]:  # 현재 ON 상태인 경우
                    macro = macro_service.get_macro_by_id(macro_id)
                    if macro:
                        keys = self._parse_key_sequence(macro['key_sequence'])
                        for key in keys:
                            if key.strip().lower() in ['shift', 'ctrl', 'alt']:
                                pyautogui.keyUp(key.strip().lower())
                        self._toggle_states[macro_id] = False
            
            self.logger.info("모든 토글 매크로가 중지되었습니다.")
            
        except Exception as e:
            self.logger.error(f"토글 매크로 중지 중 오류: {e}")
    
    def get_execution_status(self) -> Dict[str, Any]:
        """
        매크로 실행 상태 정보 반환
        
        Returns:
            Dict: 실행 상태 정보
        """
        return {
            'service_active': True,
            'failsafe_enabled': pyautogui.FAILSAFE,
            'pause_duration': pyautogui.PAUSE,
            'running_macros': len(self._running_macros),
            'toggle_states': dict(self._toggle_states),
            'timestamp': datetime.now().isoformat()
        }


# 서비스 인스턴스 생성
macro_execution_service = MacroExecutionService() 