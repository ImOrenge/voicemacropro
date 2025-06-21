"""
VoiceMacro Pro - 음성 인식 서비스
실시간 음성 녹음, GPT-4o 트랜스크립션, 매크로 매칭 기능 제공
"""

import threading
import time
import queue
import numpy as np
import sounddevice as sd
import asyncio
from typing import Optional, Callable, Dict, List
import logging
from backend.utils.common_utils import get_logger
from backend.services.gpt4o_transcription_service import GPT4oTranscriptionService
from backend.utils.config import Config


class VoiceRecognitionService:
    """
    음성 인식 서비스 클래스
    - 실시간 음성 녹음
    - GPT-4o 트랜스크립션
    - 마이크 권한 관리  
    - 음성 입력 레벨 모니터링
    - 백그라운드 녹음 기능
    """
    
    def __init__(self):
        """음성 인식 서비스 초기화"""
        self.logger = get_logger(__name__)
        
        # 녹음 설정 (GPT-4o 최적화)
        self.sample_rate = 24000  # GPT-4o 권장 샘플레이트
        self.channels = 1  # 모노 채널
        self.chunk_size = int(Config.GPT4O_BUFFER_SIZE_MS * self.sample_rate / 1000)  # 100ms 버퍼
        
        # 녹음 상태 관리
        self.is_recording = False
        self.recording_thread = None
        self.audio_queue = queue.Queue()
        
        # 마이크 관리
        self.current_device_id = None
        self.available_devices = []
        
        # GPT-4o 트랜스크립션 서비스
        self.gpt4o_service: Optional[GPT4oTranscriptionService] = None
        self.gpt4o_enabled = Config.GPT4O_ENABLED
        self.confidence_threshold = Config.GPT4O_CONFIDENCE_THRESHOLD
        
        # 비동기 루프 관리
        self.event_loop = None
        self.loop_thread = None
        
        # 콜백 함수들
        self.audio_level_callback: Optional[Callable[[float], None]] = None
        self.recording_status_callback: Optional[Callable[[bool], None]] = None
        self.transcription_callback: Optional[Callable[[Dict], None]] = None
        
        # 초기화
        self._initialize_audio_devices()
        
        if self.gpt4o_enabled and Config.OPENAI_API_KEY:
            self._initialize_gpt4o_service()
        
        self.logger.info(f"음성 인식 서비스가 초기화되었습니다. (GPT-4o: {'활성' if self.gpt4o_enabled else '비활성'})")
    
    def _initialize_gpt4o_service(self):
        """GPT-4o 트랜스크립션 서비스 초기화"""
        try:
            self.gpt4o_service = GPT4oTranscriptionService(Config.OPENAI_API_KEY)
            self.gpt4o_service.set_transcription_callback(self._handle_transcription_result)
            
            # 비동기 루프 시작
            self._start_async_loop()
            
            self.logger.info("GPT-4o 트랜스크립션 서비스가 초기화되었습니다.")
            
        except Exception as e:
            self.logger.error(f"GPT-4o 서비스 초기화 실패: {e}")
            self.gpt4o_enabled = False
    
    def _start_async_loop(self):
        """비동기 이벤트 루프 시작"""
        def run_loop():
            self.event_loop = asyncio.new_event_loop()
            asyncio.set_event_loop(self.event_loop)
            self.event_loop.run_forever()
        
        self.loop_thread = threading.Thread(target=run_loop, daemon=True)
        self.loop_thread.start()
        
        # 루프가 시작될 때까지 잠시 대기
        time.sleep(0.1)
    
    async def _handle_transcription_result(self, transcription_data: Dict):
        """
        GPT-4o 트랜스크립션 결과 처리
        
        Args:
            transcription_data (Dict): 트랜스크립션 결과 데이터
        """
        try:
            if transcription_data["type"] == "final":
                transcript = transcription_data["text"].strip()
                confidence = transcription_data["confidence"]
                
                self.logger.info(f"음성 인식 결과: '{transcript}' (신뢰도: {confidence:.2f})")
                
                # 신뢰도 임계값 확인
                if confidence >= self.confidence_threshold:
                    # 트랜스크립션 콜백 호출 (메인 스레드에서)
                    if self.transcription_callback:
                        # 스레드 안전하게 콜백 실행
                        def call_callback():
                            try:
                                self.transcription_callback({
                                    "transcript": transcript,
                                    "confidence": confidence,
                                    "timestamp": transcription_data["timestamp"],
                                    "success": True
                                })
                            except Exception as e:
                                self.logger.error(f"트랜스크립션 콜백 실행 오류: {e}")
                        
                        # 메인 스레드에서 콜백 실행
                        threading.Thread(target=call_callback, daemon=True).start()
                else:
                    self.logger.warning(f"낮은 신뢰도로 인한 무시: {confidence:.2f} < {self.confidence_threshold}")
                    
            elif transcription_data["type"] == "partial":
                # 부분 트랜스크립션은 로그만 기록
                self.logger.debug(f"부분 인식: {transcription_data['text']}")
                
        except Exception as e:
            self.logger.error(f"트랜스크립션 결과 처리 오류: {e}")
    
    def set_transcription_callback(self, callback: Callable[[Dict], None]):
        """
        트랜스크립션 결과 콜백 함수 설정
        
        Args:
            callback (Callable[[Dict], None]): 트랜스크립션 결과를 받을 콜백 함수
        """
        self.transcription_callback = callback
        self.logger.debug("트랜스크립션 콜백 함수가 설정되었습니다.")
    
    def _initialize_audio_devices(self):
        """사용 가능한 오디오 장치 초기화 (안전한 방법)"""
        try:
            # 사용 가능한 입력 장치 목록 가져오기
            devices = sd.query_devices()
            self.available_devices = []
            
            # 알려진 마이크 키워드들
            mic_keywords = ['마이크', 'mic', 'microphone', 'input', '입력', 'capture', 'record']
            
            for idx, device in enumerate(devices):
                try:
                    # 안전한 방법으로 장치 정보 접근
                    device_name = device.get('name', 'Unknown Device')
                    max_inputs = device.get('max_inputs', 0)
                    max_input_channels = device.get('max_input_channels', 0)
                    default_samplerate = device.get('default_samplerate', 44100)
                    
                    # 입력 장치 감지 방법 개선
                    is_input_device = False
                    
                    # 방법 1: max_input_channels 또는 max_inputs 확인
                    if max_input_channels > 0 or max_inputs > 0:
                        is_input_device = True
                    
                    # 방법 2: 이름에 마이크 관련 키워드가 포함된 경우
                    elif any(keyword in device_name.lower() for keyword in mic_keywords):
                        is_input_device = True
                    
                    # 방법 3: 실제 녹음 테스트로 확인 (안전한 방법)
                    elif self._test_device_input_capability(idx):
                        is_input_device = True
                    
                    if is_input_device:
                        device_info = {
                            'id': idx,
                            'name': device_name,
                            'max_input_channels': max(max_input_channels, max_inputs, 1),  # 최소 1로 설정
                            'default_samplerate': default_samplerate
                        }
                        self.available_devices.append(device_info)
                        self.logger.debug(f"입력 장치 발견: [{idx}] {device_name}")
                    
                except Exception as e:
                    self.logger.debug(f"장치 [{idx}] 정보 읽기 실패: {e}")
                    continue
            
            # 기본 입력 장치 설정
            if self.available_devices:
                # 첫 번째 사용 가능한 장치를 기본으로 설정
                default_device = self.available_devices[0]
                self.current_device_id = default_device['id']
                self.logger.info(f"기본 마이크 장치 설정: [{default_device['id']}] {default_device['name']}")
                self.logger.info(f"총 {len(self.available_devices)}개의 입력 장치가 발견되었습니다.")
            else:
                self.logger.warning("사용 가능한 마이크 장치가 없습니다. 기본 장치로 시도합니다.")
                # 기본 장치라도 추가 (sounddevice가 자동으로 선택)
                self.available_devices.append({
                    'id': None,  # None은 기본 장치를 의미
                    'name': '시스템 기본 마이크',
                    'max_input_channels': 1,
                    'default_samplerate': 16000
                })
                self.current_device_id = None
                
        except Exception as e:
            self.logger.error(f"오디오 장치 초기화 실패: {e}")
            # 최후의 수단: 기본 장치만 추가
            self.available_devices = [{
                'id': None,
                'name': '시스템 기본 마이크 (자동 감지)',
                'max_input_channels': 1,
                'default_samplerate': 16000
            }]
            self.current_device_id = None
    
    def _test_device_input_capability(self, device_id: int) -> bool:
        """
        특정 장치의 입력 기능을 실제로 테스트하는 함수
        
        Args:
            device_id (int): 테스트할 장치 ID
            
        Returns:
            bool: 입력 기능이 있는 경우 True
        """
        try:
            # 아주 짧은 시간(0.1초) 동안 녹음 테스트
            test_duration = 0.1
            test_samplerate = 16000
            
            test_data = sd.rec(
                int(test_duration * test_samplerate),
                samplerate=test_samplerate,
                channels=1,
                device=device_id,
                dtype='float32'
            )
            sd.wait()  # 녹음 완료 대기
            
            # 녹음이 성공하면 입력 장치임
            return True
            
        except Exception:
            # 녹음이 실패하면 입력 장치가 아님
            return False
    
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
    
    def _audio_callback(self, indata, frames, time, status):
        """
        오디오 입력 콜백 함수 - GPT-4o 트랜스크립션 연동
        sounddevice에서 호출됨
        """
        if status:
            self.logger.warning(f"오디오 입력 상태 경고: {status}")
        
        # 오디오 데이터를 큐에 추가
        audio_data = indata.copy()
        
        # GPT-4o 서비스로 오디오 데이터 전송
        if self.gpt4o_enabled and self.gpt4o_service and self.gpt4o_service.is_connected:
            self._send_audio_to_gpt4o(audio_data)
        
        # 음성 레벨 계산 (RMS)
        if self.audio_level_callback:
            rms = np.sqrt(np.mean(audio_data ** 2))
            # 0.0 ~ 1.0 범위로 정규화
            level = min(1.0, rms * 10)  # 감도 조절
            self.audio_level_callback(level)
        
        # 오디오 데이터 저장
        if not self.audio_queue.full():
            self.audio_queue.put(audio_data)
    
    def _send_audio_to_gpt4o(self, audio_data):
        """
        오디오 데이터를 GPT-4o 서비스로 전송
        
        Args:
            audio_data: 오디오 데이터 (numpy array, float32)
        """
        try:
            # float32 numpy array를 int16 PCM으로 변환
            # GPT-4o는 PCM16 형식을 요구함
            audio_int16 = (audio_data.flatten() * 32767).astype(np.int16)
            audio_bytes = audio_int16.tobytes()
            
            # 비동기적으로 오디오 전송
            if self.event_loop and not self.event_loop.is_closed():
                asyncio.run_coroutine_threadsafe(
                    self.gpt4o_service.send_audio_chunk(audio_bytes),
                    self.event_loop
                )
        except Exception as e:
            self.logger.error(f"GPT-4o 오디오 전송 오류: {e}")
    
    def start_recording(self) -> bool:
        """
        실시간 녹음 시작 - GPT-4o 트랜스크립션 연결 포함
        
        Returns:
            bool: 녹음 시작 성공 여부
        """
        if self.is_recording:
            self.logger.warning("이미 녹음이 진행 중입니다.")
            return False
        
        try:
            # GPT-4o 서비스 연결 시작
            if self.gpt4o_enabled and self.gpt4o_service:
                self.logger.info("GPT-4o 트랜스크립션 서비스 연결 중...")
                
                # 비동기적으로 연결 시도
                if self.event_loop and not self.event_loop.is_closed():
                    future = asyncio.run_coroutine_threadsafe(
                        self.gpt4o_service.connect(),
                        self.event_loop
                    )
                    # 연결 결과 대기 (최대 5초)
                    try:
                        connection_success = future.result(timeout=5.0)
                        if connection_success:
                            self.logger.info("GPT-4o 서비스 연결 성공")
                            # 백그라운드에서 트랜스크립션 수신 시작
                            asyncio.run_coroutine_threadsafe(
                                self.gpt4o_service.listen_for_transcriptions(),
                                self.event_loop
                            )
                        else:
                            self.logger.warning("GPT-4o 서비스 연결 실패 - 기본 모드로 계속")
                    except Exception as e:
                        self.logger.warning(f"GPT-4o 연결 타임아웃: {e} - 기본 모드로 계속")
            
            # 오디오 큐 초기화
            while not self.audio_queue.empty():
                self.audio_queue.get()
            
            # 녹음 시작
            self.is_recording = True
            
            # sounddevice 스트림 시작
            self.stream = sd.InputStream(
                device=self.current_device_id,
                channels=self.channels,
                samplerate=self.sample_rate,
                blocksize=self.chunk_size,
                callback=self._audio_callback,
                dtype=np.float32
            )
            
            self.stream.start()
            
            # 상태 콜백 호출
            if self.recording_status_callback:
                self.recording_status_callback(True)
            
            self.logger.info("실시간 녹음이 시작되었습니다.")
            return True
            
        except Exception as e:
            self.is_recording = False
            self.logger.error(f"녹음 시작 실패: {e}")
            return False
    
    def stop_recording(self) -> bool:
        """
        실시간 녹음 중지 - GPT-4o 트랜스크립션 연결 해제 포함
        
        Returns:
            bool: 녹음 중지 성공 여부
        """
        if not self.is_recording:
            self.logger.warning("녹음이 진행 중이지 않습니다.")
            return False
        
        try:
            self.is_recording = False
            
            # 스트림 중지
            if hasattr(self, 'stream'):
                self.stream.stop()
                self.stream.close()
            
            # GPT-4o 서비스 연결 해제
            if self.gpt4o_enabled and self.gpt4o_service and self.gpt4o_service.is_connected:
                if self.event_loop and not self.event_loop.is_closed():
                    # 남은 오디오 버퍼 커밋
                    asyncio.run_coroutine_threadsafe(
                        self.gpt4o_service.commit_audio_buffer(),
                        self.event_loop
                    )
                    # 연결 해제
                    asyncio.run_coroutine_threadsafe(
                        self.gpt4o_service.disconnect(),
                        self.event_loop
                    )
                self.logger.info("GPT-4o 트랜스크립션 서비스 연결 해제됨")
            
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
            'queue_size': self.audio_queue.qsize()
        }
    
    def get_audio_data(self, duration_seconds: float = 1.0) -> Optional[np.ndarray]:
        """
        지정된 시간만큼의 오디오 데이터 수집
        
        Args:
            duration_seconds (float): 수집할 오디오 길이 (초)
            
        Returns:
            Optional[np.ndarray]: 오디오 데이터 (None이면 충분한 데이터 없음)
        """
        if not self.is_recording:
            self.logger.warning("녹음이 진행 중이지 않습니다.")
            return None
        
        # 필요한 프레임 수 계산
        required_frames = int(duration_seconds * self.sample_rate)
        audio_chunks = []
        total_frames = 0
        
        # 큐에서 오디오 데이터 수집
        start_time = time.time()
        timeout = duration_seconds + 1.0  # 타임아웃 설정
        
        while total_frames < required_frames:
            try:
                # 타임아웃 체크
                if time.time() - start_time > timeout:
                    self.logger.warning("오디오 데이터 수집 타임아웃")
                    break
                
                # 큐에서 데이터 가져오기 (0.1초 대기)
                chunk = self.audio_queue.get(timeout=0.1)
                audio_chunks.append(chunk)
                total_frames += len(chunk)
                
            except queue.Empty:
                # 큐가 비어있으면 잠시 대기
                time.sleep(0.01)
                continue
        
        if not audio_chunks:
            return None
        
        # 오디오 청크들을 하나로 합치기
        audio_data = np.concatenate(audio_chunks, axis=0)
        
        # 요청된 길이만큼 자르기
        if len(audio_data) > required_frames:
            audio_data = audio_data[:required_frames]
        
        self.logger.debug(f"{len(audio_data)} 프레임의 오디오 데이터 수집 완료")
        return audio_data.flatten()  # 1차원 배열로 변환
    
    def test_microphone(self) -> Dict:
        """
        마이크 테스트 수행
        
        Returns:
            Dict: 테스트 결과
        """
        test_result = {
            'success': False,
            'device_available': False,
            'recording_test': False,
            'audio_level_detected': False,
            'error_message': None
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
                time.sleep(2.0)
                audio_data = self.get_audio_data(1.0)
                
                if audio_data is not None and len(audio_data) > 0:
                    # 음성 레벨 확인
                    rms = np.sqrt(np.mean(audio_data ** 2))
                    if rms > 0.001:  # 최소 임계값
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

def get_voice_recognition_service() -> VoiceRecognitionService:
    """
    음성 인식 서비스 싱글톤 인스턴스 반환
    
    Returns:
        VoiceRecognitionService: 음성 인식 서비스 인스턴스
    """
    global _voice_service_instance
    if _voice_service_instance is None:
        _voice_service_instance = VoiceRecognitionService()
    return _voice_service_instance 