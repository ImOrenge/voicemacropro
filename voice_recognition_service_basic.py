"""
VoiceMacro Pro - 기본 음성 인식 서비스 (Windows 테스트용)
실시간 음성 녹음, OpenAI Whisper 분석, 매크로 매칭 기능 제공
"""

import threading
import time
import queue
import logging
from typing import Optional, Callable, Dict, List
from common_utils import get_logger


class VoiceRecognitionServiceBasic:
    """
    기본 음성 인식 서비스 클래스 (테스트용)
    - 시뮬레이션된 음성 입력
    - 기본 매크로 매칭 테스트
    - Windows 환경 호환성 우선
    """
    
    def __init__(self):
        """음성 인식 서비스 초기화"""
        self.logger = get_logger(__name__)
        
        # 녹음 설정 (시뮬레이션)
        self.sample_rate = 16000  # Whisper 권장 샘플레이트
        self.channels = 1  # 모노 채널
        
        # 녹음 상태 관리
        self.is_recording = False
        self.simulation_thread = None
        
        # 마이크 시뮬레이션
        self.available_devices = [
            {'id': 0, 'name': '기본 마이크 (시뮬레이션)', 'max_input_channels': 1, 'default_samplerate': 16000},
            {'id': 1, 'name': '가상 마이크 1', 'max_input_channels': 1, 'default_samplerate': 16000},
            {'id': 2, 'name': '가상 마이크 2', 'max_input_channels': 1, 'default_samplerate': 16000}
        ]
        self.current_device_id = 0
        
        # 콜백 함수들
        self.audio_level_callback: Optional[Callable[[float], None]] = None
        self.recording_status_callback: Optional[Callable[[bool], None]] = None
        
        self.logger.info("기본 음성 인식 서비스가 초기화되었습니다.")
    
    def get_available_devices(self) -> List[Dict]:
        """
        사용 가능한 마이크 장치 목록 반환
        
        Returns:
            List[Dict]: 장치 정보 목록
        """
        return self.available_devices.copy()
    
    def set_device(self, device_id: int) -> bool:
        """
        마이크 장치 설정
        
        Args:
            device_id (int): 장치 ID
            
        Returns:
            bool: 설정 성공 여부
        """
        try:
            # 유효한 장치 ID인지 확인
            device_found = False
            for device in self.available_devices:
                if device['id'] == device_id:
                    self.current_device_id = device_id
                    device_found = True
                    self.logger.info(f"마이크 장치 변경: {device['name']}")
                    break
            
            if not device_found:
                self.logger.error(f"유효하지 않은 장치 ID: {device_id}")
                return False
                
            return True
            
        except Exception as e:
            self.logger.error(f"마이크 장치 설정 실패: {e}")
            return False
    
    def set_audio_level_callback(self, callback: Callable[[float], None]):
        """
        음성 입력 레벨 콜백 함수 설정
        
        Args:
            callback: 레벨 값(0.0-1.0)을 받는 콜백 함수
        """
        self.audio_level_callback = callback
        self.logger.debug("음성 레벨 콜백 함수가 설정되었습니다.")
    
    def set_recording_status_callback(self, callback: Callable[[bool], None]):
        """
        녹음 상태 콜백 함수 설정
        
        Args:
            callback: 녹음 상태(True/False)를 받는 콜백 함수
        """
        self.recording_status_callback = callback
        self.logger.debug("녹음 상태 콜백 함수가 설정되었습니다.")
    
    def _simulation_worker(self):
        """
        음성 입력 시뮬레이션 워커 스레드
        """
        import random
        import math
        
        start_time = time.time()
        
        while self.is_recording:
            # 가상 음성 레벨 생성 (sine wave 기반)
            elapsed = time.time() - start_time
            base_level = 0.1 + 0.3 * math.sin(elapsed * 2)  # 기본 노이즈
            
            # 랜덤 음성 이벤트 추가
            if random.random() < 0.1:  # 10% 확률로 음성 감지
                voice_level = 0.5 + 0.4 * random.random()
                level = min(1.0, base_level + voice_level)
            else:
                level = max(0.0, base_level + random.uniform(-0.1, 0.1))
            
            # 콜백 호출
            if self.audio_level_callback:
                self.audio_level_callback(level)
            
            time.sleep(0.05)  # 20Hz 업데이트
    
    def start_recording(self) -> bool:
        """
        실시간 녹음 시작 (시뮬레이션)
        
        Returns:
            bool: 녹음 시작 성공 여부
        """
        if self.is_recording:
            self.logger.warning("이미 녹음이 진행 중입니다.")
            return False
        
        try:
            self.is_recording = True
            
            # 시뮬레이션 스레드 시작
            self.simulation_thread = threading.Thread(target=self._simulation_worker, daemon=True)
            self.simulation_thread.start()
            
            # 상태 콜백 호출
            if self.recording_status_callback:
                self.recording_status_callback(True)
            
            self.logger.info("실시간 녹음 시뮬레이션이 시작되었습니다.")
            return True
            
        except Exception as e:
            self.is_recording = False
            self.logger.error(f"녹음 시작 실패: {e}")
            return False
    
    def stop_recording(self) -> bool:
        """
        실시간 녹음 중지
        
        Returns:
            bool: 녹음 중지 성공 여부
        """
        if not self.is_recording:
            self.logger.warning("녹음이 진행 중이지 않습니다.")
            return False
        
        try:
            self.is_recording = False
            
            # 스레드 종료 대기
            if self.simulation_thread and self.simulation_thread.is_alive():
                self.simulation_thread.join(timeout=1.0)
            
            # 상태 콜백 호출
            if self.recording_status_callback:
                self.recording_status_callback(False)
            
            self.logger.info("실시간 녹음이 중지되었습니다.")
            return True
            
        except Exception as e:
            self.logger.error(f"녹음 중지 실패: {e}")
            return False
    
    def get_recording_status(self) -> Dict:
        """
        현재 녹음 상태 정보 반환
        
        Returns:
            Dict: 녹음 상태 정보
        """
        return {
            'is_recording': self.is_recording,
            'current_device': self.current_device_id,
            'sample_rate': self.sample_rate,
            'channels': self.channels,
            'available_devices_count': len(self.available_devices),
            'queue_size': 0,  # 시뮬레이션에서는 0
            'mode': 'simulation'
        }
    
    def get_audio_data(self, duration_seconds: float = 1.0) -> Optional[List[float]]:
        """
        지정된 시간만큼의 오디오 데이터 수집 (시뮬레이션)
        
        Args:
            duration_seconds (float): 수집할 오디오 길이 (초)
            
        Returns:
            Optional[List[float]]: 시뮬레이션 오디오 데이터
        """
        if not self.is_recording:
            self.logger.warning("녹음이 진행 중이지 않습니다.")
            return None
        
        # 시뮬레이션 데이터 생성
        import random
        sample_count = int(duration_seconds * self.sample_rate)
        audio_data = [random.uniform(-0.1, 0.1) for _ in range(sample_count)]
        
        self.logger.debug(f"{sample_count} 샘플의 시뮬레이션 오디오 데이터 생성")
        return audio_data
    
    def test_microphone(self) -> Dict:
        """
        마이크 테스트 수행 (시뮬레이션)
        
        Returns:
            Dict: 테스트 결과
        """
        test_result = {
            'success': False,
            'device_available': False,
            'recording_test': False,
            'audio_level_detected': False,
            'error_message': None,
            'mode': 'simulation'
        }
        
        try:
            # 1. 장치 사용 가능 여부 확인
            if not self.available_devices:
                test_result['error_message'] = "사용 가능한 마이크 장치가 없습니다."
                return test_result
            
            test_result['device_available'] = True
            
            # 2. 짧은 녹음 테스트
            if self.start_recording():
                test_result['recording_test'] = True
                
                # 3. 오디오 레벨 감지 테스트 (2초간)
                self.logger.info("마이크 테스트 중... (2초간 시뮬레이션)")
                time.sleep(2.0)
                audio_data = self.get_audio_data(1.0)
                
                if audio_data is not None and len(audio_data) > 0:
                    # 시뮬레이션에서는 항상 성공
                    test_result['audio_level_detected'] = True
                
                self.stop_recording()
            
            # 모든 테스트 통과
            if (test_result['device_available'] and 
                test_result['recording_test'] and 
                test_result['audio_level_detected']):
                test_result['success'] = True
            
        except Exception as e:
            test_result['error_message'] = f"마이크 테스트 실패: {e}"
            self.logger.error(f"마이크 테스트 중 오류: {e}")
        
        return test_result


# 전역 음성 인식 서비스 인스턴스
_voice_service_instance = None

def get_voice_recognition_service_basic() -> VoiceRecognitionServiceBasic:
    """
    기본 음성 인식 서비스 싱글톤 인스턴스 반환
    
    Returns:
        VoiceRecognitionServiceBasic: 기본 음성 인식 서비스 인스턴스
    """
    global _voice_service_instance
    if _voice_service_instance is None:
        _voice_service_instance = VoiceRecognitionServiceBasic()
    return _voice_service_instance 