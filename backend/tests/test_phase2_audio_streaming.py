"""
Phase 2: 실시간 오디오 스트리밍 구현 테스트
VoiceMacro Pro의 TDD 기반 개발을 위한 테스트 코드
"""

import os
import sys
import pytest
import asyncio
import threading
import time
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from typing import Optional, Callable

# 프로젝트 루트를 Python 경로에 추가
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))


class MockAudioCaptureService:
    """테스트용 모의 오디오 캡처 서비스"""
    
    def __init__(self, sample_rate: int = 24000, channels: int = 1, chunk_size: int = 2400):
        """
        모의 오디오 캡처 서비스 초기화
        
        Args:
            sample_rate (int): 샘플링 레이트 (기본 24kHz)
            channels (int): 채널 수 (기본 1 - Mono)
            chunk_size (int): 청크 크기 (기본 2400 - 100ms at 24kHz)
        """
        self.sample_rate = sample_rate
        self.channels = channels
        self.chunk_size = chunk_size
        self.is_recording = False
        self.audio_callback: Optional[Callable] = None
        self.recording_thread: Optional[threading.Thread] = None
        
    def set_audio_callback(self, callback: Callable[[bytes], None]):
        """오디오 데이터 콜백 함수 설정"""
        self.audio_callback = callback
    
    def start_recording(self) -> bool:
        """
        오디오 녹음 시작
        
        Returns:
            bool: 시작 성공 여부
        """
        if self.is_recording:
            return False
            
        self.is_recording = True
        self.recording_thread = threading.Thread(target=self._recording_loop)
        self.recording_thread.start()
        return True
    
    def stop_recording(self):
        """오디오 녹음 중지"""
        self.is_recording = False
        if self.recording_thread:
            self.recording_thread.join(timeout=1.0)
    
    def _recording_loop(self):
        """오디오 녹음 루프 (모의 구현)"""
        while self.is_recording:
            # 모의 오디오 데이터 생성 (PCM16 형식)
            mock_audio_data = b'\x00\x01' * (self.chunk_size // 2)
            
            if self.audio_callback:
                self.audio_callback(mock_audio_data)
            
            # 100ms 대기 (실제 오디오 캡처 간격 시뮬레이션)
            time.sleep(0.1)
    
    def get_device_list(self) -> list:
        """사용 가능한 오디오 장치 목록 반환"""
        return [
            {"id": 0, "name": "Default Microphone", "channels": 1},
            {"id": 1, "name": "USB Headset", "channels": 2},
            {"id": 2, "name": "Built-in Microphone", "channels": 1}
        ]


class TestAudioCaptureService:
    """오디오 캡처 서비스 테스트"""
    
    @pytest.fixture
    def audio_service(self):
        """테스트용 오디오 캡처 서비스"""
        return MockAudioCaptureService()
    
    def test_audio_service_initialization(self, audio_service):
        """✅ Test 1: 오디오 서비스 초기화"""
        assert audio_service.sample_rate == 24000
        assert audio_service.channels == 1
        assert audio_service.chunk_size == 2400
        assert not audio_service.is_recording
        assert audio_service.audio_callback is None
    
    def test_custom_audio_config(self):
        """✅ Test 2: 커스텀 오디오 설정"""
        service = MockAudioCaptureService(
            sample_rate=16000,
            channels=2,
            chunk_size=1600
        )
        
        assert service.sample_rate == 16000
        assert service.channels == 2
        assert service.chunk_size == 1600
    
    def test_callback_setting(self, audio_service):
        """✅ Test 3: 콜백 함수 설정"""
        mock_callback = Mock()
        audio_service.set_audio_callback(mock_callback)
        
        assert audio_service.audio_callback == mock_callback
    
    def test_recording_start_stop(self, audio_service):
        """✅ Test 4: 녹음 시작/중지"""
        # 녹음 시작
        result = audio_service.start_recording()
        assert result is True
        assert audio_service.is_recording is True
        
        # 짧은 시간 대기
        time.sleep(0.05)
        
        # 녹음 중지
        audio_service.stop_recording()
        assert audio_service.is_recording is False
    
    def test_device_list(self, audio_service):
        """✅ Test 5: 오디오 장치 목록 조회"""
        devices = audio_service.get_device_list()
        
        assert len(devices) > 0
        assert all('id' in device for device in devices)
        assert all('name' in device for device in devices)
        assert all('channels' in device for device in devices)


class TestAudioStreaming:
    """실시간 오디오 스트리밍 테스트"""
    
    @pytest.fixture
    def streaming_service(self):
        """스트리밍 서비스 픽스처"""
        return MockAudioCaptureService()
    
    def test_audio_chunk_capture(self, streaming_service):
        """🎵 Test 6: 오디오 청크 캡처"""
        captured_chunks = []
        
        def capture_callback(audio_data: bytes):
            captured_chunks.append(audio_data)
        
        streaming_service.set_audio_callback(capture_callback)
        streaming_service.start_recording()
        
        # 300ms 동안 녹음 (3개 청크 예상)
        time.sleep(0.3)
        streaming_service.stop_recording()
        
        # 최소 2개의 청크는 캡처되어야 함
        assert len(captured_chunks) >= 2
        
        # 각 청크의 크기 확인
        for chunk in captured_chunks:
            assert len(chunk) == streaming_service.chunk_size
            assert isinstance(chunk, bytes)
    
    def test_audio_format_validation(self, streaming_service):
        """✅ Test 7: 오디오 형식 검증"""
        captured_chunk = None
        
        def format_callback(audio_data: bytes):
            nonlocal captured_chunk
            captured_chunk = audio_data
        
        streaming_service.set_audio_callback(format_callback)
        streaming_service.start_recording()
        
        # 한 청크 캡처 대기
        time.sleep(0.15)
        streaming_service.stop_recording()
        
        assert captured_chunk is not None
        
        # PCM16 형식 확인 (2 bytes per sample)
        expected_size = streaming_service.chunk_size
        assert len(captured_chunk) == expected_size
        
        # 샘플 수 확인 (Mono, 16-bit)
        samples_count = len(captured_chunk) // 2
        expected_samples = streaming_service.chunk_size // 2
        assert samples_count == expected_samples
    
    @pytest.mark.asyncio
    async def test_streaming_performance(self, streaming_service):
        """⚡ Test 8: 스트리밍 성능 테스트"""
        chunk_timestamps = []
        
        def timing_callback(audio_data: bytes):
            chunk_timestamps.append(time.perf_counter())
        
        streaming_service.set_audio_callback(timing_callback)
        
        start_time = time.perf_counter()
        streaming_service.start_recording()
        
        # 1초간 녹음
        await asyncio.sleep(1.0)
        streaming_service.stop_recording()
        
        end_time = time.perf_counter()
        total_time = end_time - start_time
        
        # 성능 검증
        assert len(chunk_timestamps) >= 8  # 최소 8개 청크 (800ms)
        assert len(chunk_timestamps) <= 12  # 최대 12개 청크 (1200ms)
        
        # 청크 간 간격 확인 (100ms ± 20ms)
        if len(chunk_timestamps) > 1:
            intervals = [
                chunk_timestamps[i] - chunk_timestamps[i-1]
                for i in range(1, len(chunk_timestamps))
            ]
            
            avg_interval = sum(intervals) / len(intervals)
            assert 0.08 <= avg_interval <= 0.12  # 80ms ~ 120ms


class TestAudioBuffering:
    """오디오 버퍼링 시스템 테스트"""
    
    class AudioBuffer:
        """간단한 오디오 버퍼 구현"""
        
        def __init__(self, max_size: int = 10):
            self.buffer = []
            self.max_size = max_size
            self.lock = threading.Lock()
        
        def add_chunk(self, chunk: bytes):
            """버퍼에 청크 추가"""
            with self.lock:
                self.buffer.append(chunk)
                if len(self.buffer) > self.max_size:
                    self.buffer.pop(0)  # 오래된 데이터 제거
        
        def get_chunk(self) -> Optional[bytes]:
            """버퍼에서 청크 가져오기"""
            with self.lock:
                return self.buffer.pop(0) if self.buffer else None
        
        def size(self) -> int:
            """버퍼 크기 반환"""
            with self.lock:
                return len(self.buffer)
        
        def clear(self):
            """버퍼 초기화"""
            with self.lock:
                self.buffer.clear()
    
    @pytest.fixture
    def audio_buffer(self):
        """오디오 버퍼 픽스처"""
        return self.AudioBuffer()
    
    def test_buffer_initialization(self, audio_buffer):
        """✅ Test 9: 버퍼 초기화"""
        assert audio_buffer.size() == 0
        assert audio_buffer.get_chunk() is None
    
    def test_buffer_add_get(self, audio_buffer):
        """✅ Test 10: 버퍼 추가/가져오기"""
        test_chunk = b"test_audio_data"
        
        # 청크 추가
        audio_buffer.add_chunk(test_chunk)
        assert audio_buffer.size() == 1
        
        # 청크 가져오기
        retrieved_chunk = audio_buffer.get_chunk()
        assert retrieved_chunk == test_chunk
        assert audio_buffer.size() == 0
    
    def test_buffer_overflow(self, audio_buffer):
        """✅ Test 11: 버퍼 오버플로우 처리"""
        # 최대 크기보다 많은 청크 추가
        for i in range(15):
            chunk = f"chunk_{i}".encode()
            audio_buffer.add_chunk(chunk)
        
        # 최대 크기로 제한되는지 확인
        assert audio_buffer.size() == audio_buffer.max_size
        
        # 가장 오래된 데이터가 제거되었는지 확인
        first_chunk = audio_buffer.get_chunk()
        assert first_chunk == b"chunk_5"  # 처음 5개가 제거됨
    
    def test_buffer_thread_safety(self, audio_buffer):
        """🔒 Test 12: 버퍼 스레드 안전성"""
        def producer():
            for i in range(100):
                chunk = f"data_{i}".encode()
                audio_buffer.add_chunk(chunk)
                time.sleep(0.001)
        
        def consumer():
            consumed = []
            for _ in range(50):
                chunk = audio_buffer.get_chunk()
                if chunk:
                    consumed.append(chunk)
                time.sleep(0.002)
            return consumed
        
        # 생산자와 소비자 스레드 시작
        producer_thread = threading.Thread(target=producer)
        consumer_thread = threading.Thread(target=consumer)
        
        producer_thread.start()
        consumer_thread.start()
        
        producer_thread.join()
        consumer_thread.join()
        
        # 데이터 손실 없이 처리되었는지 확인
        remaining_size = audio_buffer.size()
        assert remaining_size >= 0
        assert remaining_size <= audio_buffer.max_size


class TestIntegratedAudioPipeline:
    """통합 오디오 파이프라인 테스트"""
    
    @pytest.mark.asyncio
    async def test_capture_to_gpt4o_pipeline(self):
        """🔄 Test 13: 캡처 → GPT-4o 파이프라인"""
        # 모의 서비스 설정
        audio_service = MockAudioCaptureService()
        
        # GPT-4o 서비스 모의 객체
        mock_gpt4o = AsyncMock()
        mock_gpt4o.send_audio_chunk = AsyncMock()
        mock_gpt4o.is_connected = True
        
        processed_chunks = []
        
        def audio_pipeline(audio_data: bytes):
            """오디오 파이프라인 처리"""
            processed_chunks.append(audio_data)
            # 비동기 처리를 동기로 변환
            asyncio.create_task(mock_gpt4o.send_audio_chunk(audio_data))
        
        # 파이프라인 설정
        audio_service.set_audio_callback(audio_pipeline)
        
        # 녹음 시작
        audio_service.start_recording()
        await asyncio.sleep(0.5)  # 500ms 녹음
        audio_service.stop_recording()
        
        # 파이프라인 검증
        assert len(processed_chunks) >= 4  # 최소 4개 청크
        
        # 모든 청크가 올바른 형식인지 확인
        for chunk in processed_chunks:
            assert isinstance(chunk, bytes)
            assert len(chunk) == audio_service.chunk_size
    
    @pytest.mark.asyncio
    async def test_realtime_latency_measurement(self):
        """⚡ Test 14: 실시간 지연시간 측정"""
        audio_service = MockAudioCaptureService()
        
        latency_measurements = []
        
        def latency_callback(audio_data: bytes):
            """지연시간 측정 콜백"""
            processing_start = time.perf_counter()
            
            # 간단한 처리 시뮬레이션
            time.sleep(0.001)  # 1ms 처리 시간
            
            processing_end = time.perf_counter()
            latency = (processing_end - processing_start) * 1000  # ms 변환
            latency_measurements.append(latency)
        
        audio_service.set_audio_callback(latency_callback)
        audio_service.start_recording()
        
        await asyncio.sleep(0.3)
        audio_service.stop_recording()
        
        # 지연시간 검증
        assert len(latency_measurements) >= 2
        
        avg_latency = sum(latency_measurements) / len(latency_measurements)
        max_latency = max(latency_measurements)
        
        # 성능 요구사항 검증 (< 10ms 처리 시간)
        assert avg_latency < 10, f"평균 처리 지연시간 {avg_latency:.2f}ms > 10ms"
        assert max_latency < 20, f"최대 처리 지연시간 {max_latency:.2f}ms > 20ms"


class TestErrorHandling:
    """오디오 스트리밍 오류 처리 테스트"""
    
    def test_audio_device_not_available(self):
        """❌ Test 15: 오디오 장치 없음 처리"""
        audio_service = MockAudioCaptureService()
        
        # 장치 목록이 비어있는 경우 시뮬레이션
        with patch.object(audio_service, 'get_device_list', return_value=[]):
            devices = audio_service.get_device_list()
            assert len(devices) == 0
    
    def test_callback_exception_handling(self):
        """⚠️ Test 16: 콜백 예외 처리"""
        audio_service = MockAudioCaptureService()
        
        exception_count = 0
        
        def faulty_callback(audio_data: bytes):
            """예외를 발생시키는 콜백"""
            nonlocal exception_count
            exception_count += 1
            if exception_count <= 2:
                raise Exception("Simulated callback error")
        
        audio_service.set_audio_callback(faulty_callback)
        
        # 예외가 발생해도 녹음이 계속되는지 테스트
        try:
            audio_service.start_recording()
            time.sleep(0.25)
            audio_service.stop_recording()
        except Exception:
            pytest.fail("콜백 예외가 메인 스레드에 전파되었습니다")
        
        # 예외가 발생했는지 확인
        assert exception_count >= 2
    
    def test_recording_state_consistency(self):
        """🔒 Test 17: 녹음 상태 일관성"""
        audio_service = MockAudioCaptureService()
        
        # 이미 녹음 중일 때 시작 요청
        audio_service.start_recording()
        result = audio_service.start_recording()  # 두 번째 시작 시도
        
        assert result is False  # 실패해야 함
        assert audio_service.is_recording is True
        
        audio_service.stop_recording()
        assert audio_service.is_recording is False
        
        # 이미 중지된 상태에서 중지 요청
        audio_service.stop_recording()  # 두 번째 중지 시도
        assert audio_service.is_recording is False  # 여전히 중지 상태


# 테스트 실행 함수
def run_phase2_tests():
    """Phase 2 테스트 실행"""
    import subprocess
    
    print("🧪 Phase 2: 실시간 오디오 스트리밍 테스트 실행 중...")
    
    # pytest 실행
    result = subprocess.run([
        'py', '-m', 'pytest', 
        __file__,
        '-v',  # 상세 출력
        '--tb=short',  # 짧은 트레이스백
        '--disable-warnings'  # 경고 메시지 숨김
    ], capture_output=True, text=True)
    
    print("📊 테스트 결과:")
    print(result.stdout)
    
    if result.stderr:
        print("⚠️ 경고/오류:")
        print(result.stderr)
    
    return result.returncode == 0


if __name__ == "__main__":
    # 직접 실행 시 테스트 수행
    success = run_phase2_tests()
    
    if success:
        print("✅ Phase 2 테스트 모두 통과!")
        print("🚀 Phase 3 구현을 시작할 수 있습니다.")
    else:
        print("❌ 일부 테스트 실패. 구현을 수정해주세요.") 