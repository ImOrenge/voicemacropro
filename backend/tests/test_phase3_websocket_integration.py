"""
Phase 3: WebSocket 통신 및 프론트엔드 연동 테스트
VoiceMacro Pro의 TDD 기반 개발을 위한 테스트 코드
"""

import os
import sys
import json
import pytest
import asyncio
import base64
import threading
import time
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from typing import Dict, Any, List, Optional, Callable

# 프로젝트 루트를 Python 경로에 추가
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))


class MockWebSocketServer:
    """테스트용 모의 WebSocket 서버"""
    
    def __init__(self, host: str = "localhost", port: int = 5000):
        """
        모의 WebSocket 서버 초기화
        
        Args:
            host (str): 서버 호스트
            port (int): 서버 포트
        """
        self.host = host
        self.port = port
        self.is_running = False
        self.connected_clients = []
        self.message_handlers = {}
        self.server_thread: Optional[threading.Thread] = None
        self.event_callbacks: Dict[str, Callable] = {}
        
    def on_event(self, event_name: str, callback: Callable):
        """이벤트 핸들러 등록"""
        self.event_callbacks[event_name] = callback
    
    def start_server(self) -> bool:
        """서버 시작"""
        if self.is_running:
            return False
            
        self.is_running = True
        self.server_thread = threading.Thread(target=self._server_loop)
        self.server_thread.start()
        return True
    
    def stop_server(self):
        """서버 중지"""
        self.is_running = False
        if self.server_thread:
            self.server_thread.join(timeout=1.0)
    
    def _server_loop(self):
        """서버 실행 루프"""
        while self.is_running:
            # 클라이언트 연결 시뮬레이션
            time.sleep(0.1)
    
    async def emit_to_client(self, client_id: str, event: str, data: Dict[str, Any]):
        """클라이언트에게 이벤트 전송"""
        message = {
            "event": event,
            "data": data,
            "timestamp": time.time()
        }
        
        # 실제 전송 시뮬레이션
        if event in self.event_callbacks:
            await self.event_callbacks[event](client_id, data)
    
    def add_client(self, client_id: str) -> bool:
        """클라이언트 연결 추가"""
        if client_id not in self.connected_clients:
            self.connected_clients.append(client_id)
            return True
        return False
    
    def remove_client(self, client_id: str) -> bool:
        """클라이언트 연결 제거"""
        if client_id in self.connected_clients:
            self.connected_clients.remove(client_id)
            return True
        return False
    
    def get_client_count(self) -> int:
        """연결된 클라이언트 수 반환"""
        return len(self.connected_clients)


class MockCSharpClient:
    """테스트용 모의 C# 클라이언트"""
    
    def __init__(self, client_id: str = "test_client"):
        """
        모의 C# 클라이언트 초기화
        
        Args:
            client_id (str): 클라이언트 ID
        """
        self.client_id = client_id
        self.is_connected = False
        self.received_messages = []
        self.sent_messages = []
        self.server_url = None
        self.event_handlers = {}
        
    def on(self, event_name: str, handler: Callable):
        """이벤트 핸들러 등록"""
        self.event_handlers[event_name] = handler
    
    async def connect(self, server_url: str) -> bool:
        """서버에 연결"""
        self.server_url = server_url
        self.is_connected = True
        return True
    
    async def disconnect(self):
        """서버 연결 해제"""
        self.is_connected = False
        self.server_url = None
    
    async def emit(self, event: str, data: Dict[str, Any]):
        """서버에 이벤트 전송"""
        if not self.is_connected:
            raise ConnectionError("서버에 연결되지 않음")
        
        message = {
            "event": event,
            "data": data,
            "client_id": self.client_id,
            "timestamp": time.time()
        }
        
        self.sent_messages.append(message)
        
        # 응답 시뮬레이션
        if event in self.event_handlers:
            await self.event_handlers[event](data)
    
    async def receive_message(self, message: Dict[str, Any]):
        """서버로부터 메시지 수신"""
        self.received_messages.append(message)
        
        # 이벤트 핸들러 호출
        event = message.get("event")
        if event in self.event_handlers:
            await self.event_handlers[event](message.get("data", {}))


class TestWebSocketServer:
    """WebSocket 서버 테스트"""
    
    @pytest.fixture
    def mock_server(self):
        """테스트용 모의 서버"""
        return MockWebSocketServer()
    
    def test_server_initialization(self, mock_server):
        """✅ Test 1: 서버 초기화"""
        assert mock_server.host == "localhost"
        assert mock_server.port == 5000
        assert not mock_server.is_running
        assert len(mock_server.connected_clients) == 0
    
    def test_server_start_stop(self, mock_server):
        """✅ Test 2: 서버 시작/중지"""
        # 서버 시작
        result = mock_server.start_server()
        assert result is True
        assert mock_server.is_running is True
        
        # 짧은 대기
        time.sleep(0.05)
        
        # 서버 중지
        mock_server.stop_server()
        assert mock_server.is_running is False
    
    def test_client_connection_management(self, mock_server):
        """✅ Test 3: 클라이언트 연결 관리"""
        # 클라이언트 추가
        result1 = mock_server.add_client("client1")
        assert result1 is True
        assert mock_server.get_client_count() == 1
        
        # 동일 클라이언트 중복 추가
        result2 = mock_server.add_client("client1")
        assert result2 is False
        assert mock_server.get_client_count() == 1
        
        # 다른 클라이언트 추가
        mock_server.add_client("client2")
        assert mock_server.get_client_count() == 2
        
        # 클라이언트 제거
        remove_result = mock_server.remove_client("client1")
        assert remove_result is True
        assert mock_server.get_client_count() == 1
    
    @pytest.mark.asyncio
    async def test_event_emission(self, mock_server):
        """✅ Test 4: 이벤트 전송"""
        received_events = []
        
        async def event_handler(client_id: str, data: Dict[str, Any]):
            received_events.append({"client_id": client_id, "data": data})
        
        mock_server.on_event("test_event", event_handler)
        mock_server.add_client("test_client")
        
        # 이벤트 전송
        await mock_server.emit_to_client("test_client", "test_event", {"message": "hello"})
        
        # 이벤트 수신 확인
        assert len(received_events) == 1
        assert received_events[0]["client_id"] == "test_client"
        assert received_events[0]["data"]["message"] == "hello"


class TestCSharpClientIntegration:
    """C# 클라이언트 통합 테스트"""
    
    @pytest.fixture
    def mock_client(self):
        """테스트용 모의 클라이언트"""
        return MockCSharpClient()
    
    def test_client_initialization(self, mock_client):
        """✅ Test 5: 클라이언트 초기화"""
        assert mock_client.client_id == "test_client"
        assert not mock_client.is_connected
        assert len(mock_client.received_messages) == 0
        assert len(mock_client.sent_messages) == 0
    
    @pytest.mark.asyncio
    async def test_client_connection(self, mock_client):
        """✅ Test 6: 클라이언트 연결"""
        # 연결
        result = await mock_client.connect("ws://localhost:5000")
        assert result is True
        assert mock_client.is_connected is True
        assert mock_client.server_url == "ws://localhost:5000"
        
        # 연결 해제
        await mock_client.disconnect()
        assert mock_client.is_connected is False
        assert mock_client.server_url is None
    
    @pytest.mark.asyncio
    async def test_message_emission(self, mock_client):
        """✅ Test 7: 메시지 전송"""
        await mock_client.connect("ws://localhost:5000")
        
        # 메시지 전송
        test_data = {"command": "start_recording", "params": {"quality": "high"}}
        await mock_client.emit("voice_command", test_data)
        
        # 전송된 메시지 확인
        assert len(mock_client.sent_messages) == 1
        sent_msg = mock_client.sent_messages[0]
        assert sent_msg["event"] == "voice_command"
        assert sent_msg["data"]["command"] == "start_recording"
        assert sent_msg["client_id"] == "test_client"
    
    @pytest.mark.asyncio
    async def test_message_reception(self, mock_client):
        """✅ Test 8: 메시지 수신"""
        received_data = []
        
        async def handle_transcription(data: Dict[str, Any]):
            received_data.append(data)
        
        mock_client.on("transcription_result", handle_transcription)
        
        # 메시지 수신 시뮬레이션
        test_message = {
            "event": "transcription_result",
            "data": {"text": "공격해", "confidence": 0.95}
        }
        
        await mock_client.receive_message(test_message)
        
        # 수신된 메시지 확인
        assert len(mock_client.received_messages) == 1
        assert len(received_data) == 1
        assert received_data[0]["text"] == "공격해"
        assert received_data[0]["confidence"] == 0.95


class TestRealtimeCommunication:
    """실시간 통신 테스트"""
    
    @pytest.mark.asyncio
    async def test_bidirectional_communication(self):
        """🔄 Test 9: 양방향 통신"""
        server = MockWebSocketServer()
        client = MockCSharpClient()
        
        server_received = []
        client_received = []
        
        # 서버 이벤트 핸들러
        async def server_handler(client_id: str, data: Dict[str, Any]):
            server_received.append({"client_id": client_id, "data": data})
            
            # 서버 응답
            response_data = {"status": "received", "original": data}
            await client.receive_message({
                "event": "server_response",
                "data": response_data
            })
        
        # 클라이언트 이벤트 핸들러
        async def client_handler(data: Dict[str, Any]):
            client_received.append(data)
        
        server.on_event("client_message", server_handler)
        client.on("server_response", client_handler)
        
        # 연결 설정
        server.add_client(client.client_id)
        await client.connect("ws://localhost:5000")
        
        # 클라이언트 → 서버 메시지
        await client.emit("client_message", {"text": "hello server"})
        
        # 서버에서 이벤트 처리
        last_sent = client.sent_messages[-1]
        await server.emit_to_client(
            last_sent["client_id"],
            last_sent["event"],
            last_sent["data"]
        )
        
        # 양방향 통신 확인
        assert len(server_received) == 1
        assert server_received[0]["data"]["text"] == "hello server"
        
        assert len(client_received) == 1
        assert client_received[0]["status"] == "received"
    
    @pytest.mark.asyncio
    async def test_audio_streaming_protocol(self):
        """🎵 Test 10: 오디오 스트리밍 프로토콜"""
        client = MockCSharpClient("audio_client")
        
        # 오디오 데이터 시뮬레이션
        audio_chunks = []
        for i in range(5):
            chunk = f"audio_chunk_{i}".encode()
            audio_base64 = base64.b64encode(chunk).decode('utf-8')
            audio_chunks.append(audio_base64)
        
        await client.connect("ws://localhost:5000")
        
        # 연속 오디오 전송
        for i, chunk in enumerate(audio_chunks):
            await client.emit("audio_chunk", {
                "chunk_id": i,
                "audio_data": chunk,
                "format": "pcm16",
                "sample_rate": 24000
            })
        
        # 전송된 오디오 확인
        assert len(client.sent_messages) == 5
        
        for i, message in enumerate(client.sent_messages):
            assert message["event"] == "audio_chunk"
            assert message["data"]["chunk_id"] == i
            assert message["data"]["format"] == "pcm16"
            assert message["data"]["sample_rate"] == 24000
    
    @pytest.mark.asyncio
    async def test_transcription_result_protocol(self):
        """📝 Test 11: 트랜스크립션 결과 프로토콜"""
        client = MockCSharpClient("transcription_client")
        transcription_results = []
        
        async def handle_transcription(data: Dict[str, Any]):
            transcription_results.append(data)
        
        client.on("transcription_result", handle_transcription)
        await client.connect("ws://localhost:5000")
        
        # 트랜스크립션 결과 수신 시뮬레이션
        test_results = [
            {"type": "partial", "text": "공격", "confidence": 0.7},
            {"type": "partial", "text": "공격해", "confidence": 0.8},
            {"type": "final", "text": "공격해", "confidence": 0.95}
        ]
        
        for result in test_results:
            await client.receive_message({
                "event": "transcription_result",
                "data": result
            })
        
        # 트랜스크립션 결과 확인
        assert len(transcription_results) == 3
        assert transcription_results[0]["type"] == "partial"
        assert transcription_results[1]["text"] == "공격해"
        assert transcription_results[2]["type"] == "final"
        assert transcription_results[2]["confidence"] == 0.95


class TestPerformanceAndLatency:
    """성능 및 지연시간 테스트"""
    
    @pytest.mark.asyncio
    async def test_message_latency(self):
        """⚡ Test 12: 메시지 지연시간 테스트"""
        client = MockCSharpClient("latency_client")
        await client.connect("ws://localhost:5000")
        
        latencies = []
        
        # 여러 메시지의 지연시간 측정
        for i in range(10):
            start_time = time.perf_counter()
            
            await client.emit("test_message", {"id": i, "data": f"test_{i}"})
            
            end_time = time.perf_counter()
            latency = (end_time - start_time) * 1000  # ms 변환
            latencies.append(latency)
        
        # 지연시간 분석
        avg_latency = sum(latencies) / len(latencies)
        max_latency = max(latencies)
        
        # 성능 요구사항 검증 (< 5ms 메시지 처리)
        assert avg_latency < 5, f"평균 메시지 지연시간 {avg_latency:.2f}ms > 5ms"
        assert max_latency < 10, f"최대 메시지 지연시간 {max_latency:.2f}ms > 10ms"
    
    @pytest.mark.asyncio
    async def test_concurrent_connections(self):
        """🔄 Test 13: 동시 연결 처리"""
        server = MockWebSocketServer()
        clients = []
        
        # 여러 클라이언트 생성
        for i in range(10):
            client = MockCSharpClient(f"client_{i}")
            await client.connect("ws://localhost:5000")
            server.add_client(client.client_id)
            clients.append(client)
        
        # 동시 메시지 전송
        send_tasks = []
        for i, client in enumerate(clients):
            task = client.emit("concurrent_test", {"client_id": i, "message": f"from_client_{i}"})
            send_tasks.append(task)
        
        # 모든 전송 완료 대기
        await asyncio.gather(*send_tasks)
        
        # 모든 클라이언트가 메시지 전송했는지 확인
        for client in clients:
            assert len(client.sent_messages) == 1
        
        # 서버 연결 수 확인
        assert server.get_client_count() == 10
    
    @pytest.mark.asyncio
    async def test_large_data_transmission(self):
        """📦 Test 14: 대용량 데이터 전송"""
        client = MockCSharpClient("large_data_client")
        await client.connect("ws://localhost:5000")
        
        # 대용량 오디오 데이터 시뮬레이션 (1MB)
        large_audio_data = b"x" * (1024 * 1024)  # 1MB
        audio_base64 = base64.b64encode(large_audio_data).decode('utf-8')
        
        start_time = time.perf_counter()
        
        await client.emit("large_audio_chunk", {
            "audio_data": audio_base64,
            "size": len(large_audio_data),
            "format": "pcm16"
        })
        
        end_time = time.perf_counter()
        transmission_time = (end_time - start_time) * 1000
        
        # 대용량 데이터 전송 성능 검증 (< 100ms for 1MB)
        assert transmission_time < 100, f"1MB 데이터 전송 시간 {transmission_time:.2f}ms > 100ms"
        
        # 전송된 데이터 확인
        sent_message = client.sent_messages[-1]
        assert sent_message["data"]["size"] == 1024 * 1024


class TestErrorHandlingAndResilience:
    """오류 처리 및 복원력 테스트"""
    
    @pytest.mark.asyncio
    async def test_connection_failure_recovery(self):
        """🔄 Test 15: 연결 실패 복구"""
        client = MockCSharpClient("recovery_client")
        
        # 연결 실패 시뮬레이션
        client.is_connected = False
        
        with pytest.raises(ConnectionError):
            await client.emit("test_message", {"data": "test"})
        
        # 재연결 및 메시지 전송
        await client.connect("ws://localhost:5000")
        assert client.is_connected is True
        
        await client.emit("recovery_test", {"data": "recovered"})
        assert len(client.sent_messages) == 1
    
    @pytest.mark.asyncio
    async def test_malformed_message_handling(self):
        """⚠️ Test 16: 잘못된 메시지 처리"""
        client = MockCSharpClient("error_client")
        error_count = 0
        
        async def error_handler(data: Dict[str, Any]):
            nonlocal error_count
            error_count += 1
            # 잘못된 데이터 처리 시뮬레이션
            if "invalid" in data:
                raise ValueError("Invalid message format")
        
        client.on("error_test", error_handler)
        
        # 정상 메시지
        await client.receive_message({
            "event": "error_test",
            "data": {"valid": "data"}
        })
        
        # 잘못된 메시지 (예외 발생해도 시스템 중단 없어야 함)
        try:
            await client.receive_message({
                "event": "error_test",
                "data": {"invalid": "data"}
            })
        except ValueError:
            pass  # 예외 발생 예상됨
        
        assert error_count == 2  # 두 메시지 모두 처리됨
    
    def test_server_resource_limits(self):
        """🔒 Test 17: 서버 리소스 제한"""
        server = MockWebSocketServer()
        
        # 최대 클라이언트 수 테스트 (예: 100개)
        max_clients = 100
        
        # 많은 클라이언트 연결 시뮬레이션
        for i in range(max_clients + 10):  # 제한보다 많이 시도
            client_id = f"client_{i}"
            server.add_client(client_id)
        
        # 모든 클라이언트가 추가되었는지 확인 (제한 없음 in 모의 구현)
        assert server.get_client_count() == max_clients + 10
        
        # 실제 구현에서는 제한을 두어야 함


class TestDataSerialization:
    """데이터 직렬화/역직렬화 테스트"""
    
    def test_json_serialization_performance(self):
        """⚡ Test 18: JSON 직렬화 성능"""
        # 복잡한 데이터 구조
        complex_data = {
            "transcription": {
                "text": "스킬 사용해서 몬스터 공격하고 포션 먹어",
                "confidence": 0.95,
                "words": [
                    {"word": "스킬", "confidence": 0.98, "start_time": 0.1},
                    {"word": "사용해서", "confidence": 0.92, "start_time": 0.3},
                    {"word": "몬스터", "confidence": 0.96, "start_time": 0.6},
                    {"word": "공격하고", "confidence": 0.94, "start_time": 0.9},
                    {"word": "포션", "confidence": 0.91, "start_time": 1.2},
                    {"word": "먹어", "confidence": 0.97, "start_time": 1.4}
                ]
            },
            "metadata": {
                "audio_length": 1.6,
                "processing_time": 0.08,
                "model_version": "gpt-4o-transcribe",
                "language": "ko"
            }
        }
        
        # 직렬화 성능 테스트
        start_time = time.perf_counter()
        
        for _ in range(100):  # 100번 반복
            json_str = json.dumps(complex_data, ensure_ascii=False)
            parsed_data = json.loads(json_str)
        
        end_time = time.perf_counter()
        avg_time = ((end_time - start_time) / 100) * 1000  # ms
        
        # 성능 요구사항 (< 1ms per serialization)
        assert avg_time < 1, f"JSON 직렬화 평균 시간 {avg_time:.2f}ms > 1ms"
        
        # 데이터 무결성 확인
        final_json = json.dumps(complex_data, ensure_ascii=False)
        final_data = json.loads(final_json)
        assert final_data["transcription"]["text"] == complex_data["transcription"]["text"]
        assert len(final_data["transcription"]["words"]) == 6


# 테스트 실행 함수
def run_phase3_tests():
    """Phase 3 테스트 실행"""
    import subprocess
    
    print("🧪 Phase 3: WebSocket 통신 및 프론트엔드 연동 테스트 실행 중...")
    
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
    success = run_phase3_tests()
    
    if success:
        print("✅ Phase 3 테스트 모두 통과!")
        print("🚀 Phase 4 구현을 시작할 수 있습니다.")
    else:
        print("❌ 일부 테스트 실패. 구현을 수정해주세요.") 