"""
GPT-4o 실시간 트랜스크립션 서비스 (websockets 라이브러리 기반)
VoiceMacro Pro를 위한 게임 명령어 최적화 음성 인식 시스템
"""

import os
import json
import base64
import logging
import asyncio
import threading
import websockets
from typing import Optional, Callable, Dict, Any
from datetime import datetime

class GPT4oTranscriptionService:
    """websockets 라이브러리를 사용한 GPT-4o 실시간 트랜스크립션 서비스"""
    
    def __init__(self, api_key: str):
        """
        GPT-4o 트랜스크립션 서비스 초기화
        
        Args:
            api_key (str): OpenAI API 키
        """
        # API 키 우선순위: 매개변수 > 환경변수 > .env 파일
        self.api_key = api_key or os.getenv('OPENAI_API_KEY')
        
        if not self.api_key:
            # .env 파일에서 시도
            try:
                from dotenv import load_dotenv
                load_dotenv()
                self.api_key = os.getenv('OPENAI_API_KEY')
            except ImportError:
                pass
        
        if not self.api_key:
            raise ValueError("OpenAI API 키가 설정되지 않았습니다. 매개변수나 환경변수로 제공해주세요.")
        
        self.websocket: Optional[websockets.WebSocketServerProtocol] = None
        self.session_id: Optional[str] = None
        self.is_connected = False
        self.transcription_callback: Optional[Callable] = None
        self.logger = logging.getLogger(__name__)
        
        # WebSocket 연결 설정
        self.url = "wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01"
        self.headers = {
            "Authorization": f"Bearer {self.api_key}",
            "OpenAI-Beta": "realtime=v1"
        }
        
        # 연결 상태 관리
        self.connection_lock = asyncio.Lock()
        self.is_connecting = False
        self._loop = None
        self._connection_task = None
        
        # 게임 명령어 최적화를 위한 세션 설정
        self.session_config = {
            "type": "session.update",
            "session": {
                "modalities": ["text", "audio"],
                "instructions": self._get_gaming_optimized_prompt(),
                "voice": "alloy",
                "input_audio_format": "pcm16",
                "output_audio_format": "pcm16",
                "input_audio_transcription": {
                    "model": "whisper-1"  # GPT-4o Realtime API의 공식 트랜스크립션 모델
                },
                "turn_detection": {
                    "type": "server_vad",
                    "threshold": 0.5,
                    "prefix_padding_ms": 300,
                    "silence_duration_ms": 500
                },
                "tools": [],
                "tool_choice": "auto",
                "temperature": 0.8,
                "max_response_output_tokens": "inf"
            }
        }
    
    def _get_gaming_optimized_prompt(self) -> str:
        """게임 명령어 인식 최적화를 위한 프롬프트"""
        return """
        게임 플레이어의 음성 명령어를 정확하게 인식하고 텍스트로 변환하세요.
        주요 명령어 패턴:
        - 공격: 공격, 어택, 때려, 치기, 공격해
        - 스킬: 스킬, 기술, 마법, 궁극기, 스페셜
        - 이동: 앞으로, 뒤로, 좌측, 우측, 점프, 달려
        - 아이템: 포션, 회복, 아이템, 사용, 먹기
        - 방어: 방어, 막기, 피하기, 회피
        짧고 명확한 게임 명령어를 우선 인식하세요.
        """
    
    async def connect(self) -> bool:
        """
        OpenAI Realtime API에 WebSocket 연결
        
        Returns:
            bool: 연결 성공 여부
        """
        async with self.connection_lock:
            if self.is_connecting or self.is_connected:
                self.logger.info("이미 연결 중이거나 연결되어 있습니다.")
                return self.is_connected
            
            self.is_connecting = True
            
        try:
            self.logger.info("GPT-4o Realtime API 연결 시도 중...")
            
            # WebSocket 연결 생성
            self.websocket = await websockets.connect(self.url, additional_headers=self.headers)
            
            # 별도 스레드에서 연결 실행
            self._connection_task = asyncio.create_task(self._run_connection())
            
            # 연결 완료까지 최대 10초 대기
            for _ in range(100):  # 0.1초씩 100번 = 10초
                if self.is_connected:
                    self.logger.info("GPT-4o 트랜스크립션 서비스 연결 성공")
                    return True
                await asyncio.sleep(0.1)
            
            self.logger.error("연결 타임아웃 (10초)")
            return False
            
        except Exception as e:
            self.logger.error(f"GPT-4o 서비스 연결 실패: {e}")
            return False
        finally:
            async with self.connection_lock:
                self.is_connecting = False
    
    async def _run_connection(self):
        """WebSocket 연결 실행 루프"""
        try:
            # 세션 설정 전송
            await self.websocket.send(json.dumps(self.session_config))
            self.logger.debug("세션 설정이 전송되었습니다.")
            
            while True:
                message = await self.websocket.recv()
                self.logger.debug(f"수신된 이벤트: {message}")
                
                # 이벤트 타입별 처리
                await self._handle_realtime_event(message)
                
        except Exception as e:
            self.logger.error(f"WebSocket 연결 실행 중 오류: {e}")
        finally:
            self.is_connected = False
            self.session_id = None
            self.logger.info("WebSocket 연결이 종료되었습니다.")
    
    async def _handle_realtime_event(self, message: str):
        """
        OpenAI Realtime API 이벤트 처리
        
        Args:
            message (str): 수신된 이벤트 데이터
        """
        try:
            data = json.loads(message)
            event_type = data.get("type")
            
            if event_type == "session.created":
                # 세션 생성 완료
                self.session_id = data.get("session", {}).get("id")
                self.is_connected = True
                self.logger.info(f"트랜스크립션 세션 생성됨: {self.session_id}")
                
            elif event_type == "session.updated":
                # 세션 업데이트 완료
                self.logger.debug("세션 설정이 업데이트되었습니다.")
                
            elif event_type == "input_audio_buffer.speech_started":
                # 음성 입력 시작 감지
                self.logger.debug("음성 입력 시작 감지됨")
                
            elif event_type == "input_audio_buffer.speech_stopped":
                # 음성 입력 종료 감지
                self.logger.debug("음성 입력 종료 감지됨")
                
            elif event_type == "conversation.item.input_audio_transcription.completed":
                # 트랜스크립션 완료
                transcript = data.get("transcript", "")
                item_id = data.get("item_id")
                
                if self.transcription_callback and transcript.strip():
                    # 신뢰도는 완료된 트랜스크립션에 대해 높게 설정
                    confidence = 0.9  # Realtime API에서는 별도 신뢰도 점수를 제공하지 않음
                    
                    await self.transcription_callback({
                        "type": "final",
                        "text": transcript,
                        "item_id": item_id,
                        "confidence": confidence,
                        "timestamp": datetime.now().isoformat()
                    })
                    
            elif event_type == "conversation.item.input_audio_transcription.failed":
                # 트랜스크립션 실패
                error_info = data.get("error", {})
                self.logger.warning(f"트랜스크립션 실패: {error_info}")
                
            elif event_type == "error":
                # API 오류
                error_info = data.get("error", {})
                error_msg = error_info.get("message", "알 수 없는 오류")
                self.logger.error(f"Realtime API 오류: {error_msg}")
                
            else:
                # 기타 이벤트
                self.logger.debug(f"처리되지 않은 이벤트: {event_type}")
            
        except Exception as e:
            self.logger.error(f"메시지 처리 오류: {e}")
    
    async def send_audio_chunk(self, audio_data: bytes):
        """
        실시간 오디오 데이터 전송
        
        Args:
            audio_data (bytes): PCM16 형식의 오디오 데이터 (24kHz)
        """
        if not self.is_connected or not self.websocket:
            raise ConnectionError("트랜스크립션 서비스에 연결되지 않음")
        
        try:
            # 오디오 데이터를 Base64로 인코딩
            audio_base64 = base64.b64encode(audio_data).decode('utf-8')
            
            message = {
                "type": "input_audio_buffer.append",
                "audio": audio_base64
            }
            
            await self.websocket.send(json.dumps(message))
            
        except Exception as e:
            self.logger.error(f"오디오 전송 실패: {e}")
    
    async def commit_audio_buffer(self):
        """수동으로 오디오 버퍼 커밋 (VAD 비활성화 시 사용)"""
        if not self.is_connected or not self.websocket:
            return
            
        try:
            message = {"type": "input_audio_buffer.commit"}
            await self.websocket.send(json.dumps(message))
            
        except Exception as e:
            self.logger.error(f"오디오 버퍼 커밋 실패: {e}")
    
    def set_transcription_callback(self, callback: Callable):
        """
        트랜스크립션 결과 콜백 함수 설정
        
        Args:
            callback (Callable): 트랜스크립션 결과를 처리할 함수
        """
        self.transcription_callback = callback
        self.logger.debug("트랜스크립션 콜백 함수가 설정되었습니다.")
    
    async def disconnect(self):
        """WebSocket 연결 종료"""
        try:
            if self.websocket:
                await self.websocket.close()
            self.is_connected = False
            self.session_id = None
            self.logger.info("GPT-4o 트랜스크립션 서비스 연결 해제")
            
        except Exception as e:
            self.logger.error(f"연결 해제 중 오류: {e}")


# 테스트용 예제 함수들
async def example_transcription_handler(transcription_data: Dict[str, Any]):
    """
    트랜스크립션 결과 처리 예제 함수
    
    Args:
        transcription_data (Dict[str, Any]): 트랜스크립션 결과 데이터
    """
    if transcription_data["type"] == "final":
        print(f"✅ 최종 인식: {transcription_data['text']} (신뢰도: {transcription_data['confidence']:.2f})")


async def test_gpt4o_service_websocket_client(api_key: str):
    """
    websockets 기반 GPT-4o 트랜스크립션 서비스 테스트 함수
    
    Args:
        api_key (str): OpenAI API 키
    """
    service = GPT4oTranscriptionService(api_key)
    service.set_transcription_callback(example_transcription_handler)
    
    if await service.connect():
        print("✅ GPT-4o 서비스 연결 성공!")
        print("🎤 오디오 데이터를 전송할 준비가 되었습니다.")
        
        # 여기서 실제 오디오 데이터를 전송할 수 있음
        # await service.send_audio_chunk(audio_data)
        
        try:
            # 연결 유지 (실제로는 오디오 스트림이 여기에 들어감)
            input("연결을 유지합니다. Enter를 눌러 종료하세요...")
        except KeyboardInterrupt:
            print("\n서비스 종료 중...")
        finally:
            await service.disconnect()
    else:
        print("❌ GPT-4o 서비스 연결 실패!")


if __name__ == "__main__":
    # 환경변수에서 API 키 가져오기
    api_key = os.getenv('OPENAI_API_KEY')
    if api_key:
        asyncio.run(test_gpt4o_service_websocket_client(api_key))
    else:
        print("❌ OPENAI_API_KEY 환경변수를 설정해주세요.")
        print("PowerShell: $env:OPENAI_API_KEY = 'your_api_key_here'")
        print("또는 .env 파일에 OPENAI_API_KEY=your_api_key_here 추가") 