"""
Phase 1: GPT-4o API 연동 및 기본 연결 테스트
VoiceMacro Pro의 TDD 기반 개발을 위한 테스트 코드
"""

import os
import json
import pytest
import asyncio
import base64
from unittest.mock import Mock, AsyncMock, patch
from typing import Dict, Any, List

# 프로젝트 루트를 Python 경로에 추가
import sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from backend.services.gpt4o_transcription_service import GPT4oTranscriptionService


class TestGPT4oTranscriptionService:
    """GPT-4o 트랜스크립션 서비스 테스트 클래스"""
    
    @pytest.fixture
    def mock_api_key(self):
        """테스트용 모의 API 키"""
        return "test_api_key_sk-1234567890abcdef"
    
    @pytest.fixture
    def service(self, mock_api_key):
        """테스트용 서비스 인스턴스"""
        return GPT4oTranscriptionService(mock_api_key)
    
    def test_service_initialization_with_api_key(self, mock_api_key):
        """✅ Test 1: API 키로 서비스 초기화"""
        service = GPT4oTranscriptionService(mock_api_key)
        
        assert service.api_key == mock_api_key
        assert not service.is_connected
        assert service.session_id is None
        assert service.websocket is None
        assert service.transcription_callback is None
    
    @patch.dict(os.environ, {}, clear=True)  # 모든 환경변수 제거
    def test_service_initialization_without_api_key(self):
        """❌ Test 2: API 키 없이 서비스 초기화 시 예외 발생"""
        with pytest.raises(ValueError) as exc_info:
            GPT4oTranscriptionService(None)
        
        assert "OpenAI API 키가 설정되지 않았습니다" in str(exc_info.value)
    
    @patch.dict(os.environ, {'OPENAI_API_KEY': 'env_test_key'})
    def test_service_initialization_from_env(self):
        """✅ Test 3: 환경변수에서 API 키 가져오기"""
        service = GPT4oTranscriptionService(None)
        assert service.api_key == 'env_test_key'
    
    def test_gaming_optimized_prompt(self, service):
        """✅ Test 4: 게임 명령어 최적화 프롬프트 확인"""
        prompt = service._get_gaming_optimized_prompt()
        
        # 게임 관련 키워드들이 포함되어 있는지 확인
        gaming_keywords = ['공격', '스킬', '이동', '아이템', '방어']
        for keyword in gaming_keywords:
            assert keyword in prompt
    
    def test_session_config_structure(self, service):
        """✅ Test 5: 세션 설정 구조 검증"""
        config = service.session_config
        
        assert config["type"] == "session.update"
        assert "session" in config
        
        session = config["session"]
        assert session["input_audio_format"] == "pcm16"
        assert session["output_audio_format"] == "pcm16"
        assert "input_audio_transcription" in session
        assert "turn_detection" in session
    
    def test_callback_setting(self, service):
        """✅ Test 6: 트랜스크립션 콜백 함수 설정"""
        mock_callback = Mock()
        service.set_transcription_callback(mock_callback)
        
        assert service.transcription_callback == mock_callback
    
    @pytest.mark.asyncio
    async def test_send_audio_chunk_without_connection(self, service):
        """❌ Test 7: 연결 없이 오디오 전송 시 예외 발생"""
        audio_data = b"test_audio_data"
        
        with pytest.raises(ConnectionError) as exc_info:
            await service.send_audio_chunk(audio_data)
        
        assert "트랜스크립션 서비스에 연결되지 않음" in str(exc_info.value)
    
    @pytest.mark.asyncio
    async def test_commit_audio_buffer_without_connection(self, service):
        """✅ Test 8: 연결 없이 오디오 버퍼 커밋 (예외 없이 처리)"""
        # 연결되지 않은 상태에서 커밋 시도 - 예외 없이 조용히 처리됨
        await service.commit_audio_buffer()
        assert not service.is_connected


class TestGPT4oRealTimeEvents:
    """GPT-4o 실시간 이벤트 처리 테스트"""
    
    @pytest.fixture
    def service_with_callback(self):
        """콜백이 설정된 서비스"""
        service = GPT4oTranscriptionService("test_key")
        service.transcription_callback = AsyncMock()
        return service
    
    @pytest.mark.asyncio
    async def test_session_created_event(self, service_with_callback):
        """✅ Test 9: 세션 생성 이벤트 처리"""
        event_data = {
            "type": "session.created",
            "session": {"id": "test_session_123"}
        }
        
        await service_with_callback._handle_realtime_event(json.dumps(event_data))
        
        assert service_with_callback.session_id == "test_session_123"
        assert service_with_callback.is_connected
    
    @pytest.mark.asyncio
    async def test_transcription_completed_event(self, service_with_callback):
        """✅ Test 10: 트랜스크립션 완료 이벤트 처리"""
        event_data = {
            "type": "conversation.item.input_audio_transcription.completed",
            "transcript": "공격해",
            "item_id": "item_123"
        }
        
        await service_with_callback._handle_realtime_event(json.dumps(event_data))
        
        # 콜백이 호출되었는지 확인
        service_with_callback.transcription_callback.assert_called_once()
        
        # 호출된 인자 검증
        call_args = service_with_callback.transcription_callback.call_args[0][0]
        assert call_args["type"] == "final"
        assert call_args["text"] == "공격해"
        assert call_args["item_id"] == "item_123"
        assert call_args["confidence"] == 0.9
    
    @pytest.mark.asyncio
    async def test_transcription_failed_event(self, service_with_callback):
        """⚠️ Test 11: 트랜스크립션 실패 이벤트 처리"""
        event_data = {
            "type": "conversation.item.input_audio_transcription.failed",
            "error": {"message": "Audio quality too low"}
        }
        
        # 로깅 모의 객체 설정
        with patch.object(service_with_callback.logger, 'warning') as mock_warning:
            await service_with_callback._handle_realtime_event(json.dumps(event_data))
            mock_warning.assert_called_once()
    
    @pytest.mark.asyncio
    async def test_api_error_event(self, service_with_callback):
        """❌ Test 12: API 오류 이벤트 처리"""
        event_data = {
            "type": "error",
            "error": {"message": "Invalid API key"}
        }
        
        with patch.object(service_with_callback.logger, 'error') as mock_error:
            await service_with_callback._handle_realtime_event(json.dumps(event_data))
            mock_error.assert_called_once()


class TestGPT4oPerformance:
    """GPT-4o 서비스 성능 테스트"""
    
    @pytest.mark.asyncio
    async def test_audio_encoding_performance(self):
        """⚡ Test 13: 오디오 인코딩 성능 테스트 (< 10ms)"""
        import time
        
        # 1초 분량의 24kHz PCM16 오디오 데이터 시뮬레이션
        audio_data = b"x" * (24000 * 2)  # 24kHz * 2 bytes per sample
        
        start_time = time.perf_counter()
        encoded = base64.b64encode(audio_data).decode('utf-8')
        end_time = time.perf_counter()
        
        encoding_time = (end_time - start_time) * 1000  # 밀리초 변환
        
        assert encoding_time < 10, f"오디오 인코딩 시간 {encoding_time:.2f}ms > 10ms 성능 기준 초과"
        assert len(encoded) > 0
    
    @pytest.mark.asyncio
    async def test_json_serialization_performance(self):
        """⚡ Test 14: JSON 직렬화 성능 테스트 (< 1ms)"""
        import time
        
        # 큰 오디오 메시지 시뮬레이션
        message = {
            "type": "input_audio_buffer.append",
            "audio": "x" * 10000  # 10KB 오디오 데이터
        }
        
        start_time = time.perf_counter()
        json_data = json.dumps(message)
        end_time = time.perf_counter()
        
        serialization_time = (end_time - start_time) * 1000
        
        assert serialization_time < 1, f"JSON 직렬화 시간 {serialization_time:.2f}ms > 1ms 기준 초과"
        assert len(json_data) > 0


class TestGPT4oIntegration:
    """GPT-4o 통합 테스트 (실제 API 연결 없이)"""
    
    @pytest.mark.asyncio
    @patch('websockets.connect')
    async def test_connection_flow(self, mock_connect):
        """🔄 Test 15: 연결 플로우 테스트 (모의 WebSocket)"""
        # 모의 WebSocket 설정
        mock_websocket = AsyncMock()
        mock_connect.return_value = mock_websocket
        
        # 세션 생성 응답 시뮬레이션
        session_created_event = json.dumps({
            "type": "session.created",
            "session": {"id": "test_session_123"}
        })
        
        # recv를 한 번만 호출하도록 설정
        mock_websocket.recv.return_value = session_created_event
        
        service = GPT4oTranscriptionService("test_key")
        
        # 연결 시도를 별도 태스크로 실행
        connection_task = asyncio.create_task(service.connect())
        
        # 짧은 시간 대기 후 연결 상태 확인
        await asyncio.sleep(0.2)
        
        # 연결이 성공했는지 확인
        assert service.is_connected
        assert service.session_id == "test_session_123"
        
        # 정리
        connection_task.cancel()
        
        try:
            await connection_task
        except asyncio.CancelledError:
            pass
        
        await service.disconnect()
    
    @pytest.mark.asyncio
    @patch('websockets.connect')
    async def test_audio_send_flow(self, mock_connect):
        """🎵 Test 16: 오디오 전송 플로우 테스트"""
        mock_websocket = AsyncMock()
        mock_connect.return_value = mock_websocket
        
        service = GPT4oTranscriptionService("test_key")
        service.websocket = mock_websocket  # 직접 설정
        service.is_connected = True
        
        # 테스트 오디오 데이터
        audio_data = b"test_audio_sample_data"
        
        await service.send_audio_chunk(audio_data)
        
        # WebSocket send가 호출되었는지 확인
        mock_websocket.send.assert_called_once()
        
        # 전송된 메시지 검증
        sent_message = mock_websocket.send.call_args[0][0]
        parsed_message = json.loads(sent_message)
        
        assert parsed_message["type"] == "input_audio_buffer.append"
        assert "audio" in parsed_message
        
        # Base64 디코딩으로 원본 데이터 복원 확인
        decoded_audio = base64.b64decode(parsed_message["audio"])
        assert decoded_audio == audio_data


class TestGPT4oGameOptimization:
    """게임 최적화 기능 테스트"""
    
    @pytest.fixture
    def gaming_commands(self):
        """테스트용 게임 명령어 목록"""
        return [
            "공격해", "어택", "때려", "치기",
            "스킬", "기술", "마법", "궁극기",
            "앞으로", "뒤로", "좌측", "우측", "점프",
            "포션", "회복", "아이템", "사용",
            "방어", "막기", "피하기", "회피"
        ]
    
    @pytest.mark.asyncio
    async def test_gaming_command_recognition(self, gaming_commands):
        """🎮 Test 17: 게임 명령어 인식 테스트"""
        service = GPT4oTranscriptionService("test_key")
        recognized_commands: List[str] = []
        
        async def mock_callback(data: Dict[str, Any]):
            if data["type"] == "final":
                recognized_commands.append(data["text"])
        
        service.set_transcription_callback(mock_callback)
        
        # 각 게임 명령어에 대한 트랜스크립션 완료 이벤트 시뮬레이션
        for command in gaming_commands[:5]:  # 처음 5개만 테스트
            event_data = {
                "type": "conversation.item.input_audio_transcription.completed",
                "transcript": command,
                "item_id": f"item_{command}"
            }
            
            await service._handle_realtime_event(json.dumps(event_data))
        
        # 인식된 명령어 확인
        assert len(recognized_commands) == 5
        for i, command in enumerate(gaming_commands[:5]):
            assert recognized_commands[i] == command
    
    def test_session_config_gaming_optimization(self):
        """🎮 Test 18: 게임 최적화 세션 설정 확인"""
        service = GPT4oTranscriptionService("test_key")
        config = service.session_config
        
        # VAD 설정 확인 (게임 중 빠른 반응을 위해)
        vad_config = config["session"]["turn_detection"]
        assert vad_config["type"] == "server_vad"
        assert vad_config["threshold"] == 0.5
        assert vad_config["silence_duration_ms"] == 500  # 짧은 침묵 감지
        
        # 오디오 형식 확인
        assert config["session"]["input_audio_format"] == "pcm16"
        assert config["session"]["output_audio_format"] == "pcm16"


# 테스트 실행 함수
def run_phase1_tests():
    """Phase 1 테스트 실행"""
    import subprocess
    
    print("🧪 Phase 1: GPT-4o API 연동 테스트 실행 중...")
    
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
    success = run_phase1_tests()
    
    if success:
        print("✅ Phase 1 테스트 모두 통과!")
        print("🚀 Phase 2 구현을 시작할 수 있습니다.")
    else:
        print("❌ 일부 테스트 실패. 구현을 수정해주세요.") 