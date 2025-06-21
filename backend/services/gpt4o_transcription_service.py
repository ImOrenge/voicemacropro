"""
GPT-4o 실시간 트랜스크립션 서비스
VoiceMacro Pro를 위한 게임 명령어 최적화 음성 인식 시스템
"""

import asyncio
import websockets
import json
import base64
import logging
from typing import Optional, Callable, Dict, Any
from datetime import datetime

class GPT4oTranscriptionService:
    """실시간 음성 인식을 위한 GPT-4o 트랜스크립션 서비스"""
    
    def __init__(self, api_key: str):
        """
        GPT-4o 트랜스크립션 서비스 초기화
        
        Args:
            api_key (str): OpenAI API 키
        """
        self.api_key = api_key
        self.websocket: Optional[websockets.WebSocketServerProtocol] = None
        self.session_id: Optional[str] = None
        self.is_connected = False
        self.transcription_callback: Optional[Callable] = None
        self.logger = logging.getLogger(__name__)
        
        # 게임 명령어 최적화를 위한 세션 설정
        self.session_config = {
            "type": "transcription_session.update",
            "input_audio_format": "pcm16",  # 24kHz PCM16 오디오
            "input_audio_transcription": {
                "model": "gpt-4o-transcribe",
                "prompt": self._get_gaming_optimized_prompt(),
                "language": "ko"  # 한국어 게임 명령어
            },
            "turn_detection": {
                "type": "server_vad",
                "threshold": 0.5,
                "prefix_padding_ms": 300,
                "silence_duration_ms": 500
            },
            "input_audio_noise_reduction": {
                "type": "near_field"  # 헤드셋 마이크 최적화
            },
            "include": ["item.input_audio_transcription.logprobs"]
        }
    
    def _get_gaming_optimized_prompt(self) -> str:
        """게임 명령어 인식 최적화를 위한 프롬프트"""
        return """
        게임 플레이어의 음성 명령어를 정확하게 인식하세요.
        주요 명령어 패턴:
        - 공격: 공격, 어택, 때려, 치기, 공격해
        - 스킬: 스킬, 기술, 마법, 궁극기, 스페셜
        - 이동: 앞으로, 뒤로, 좌측, 우측, 점프, 달려
        - 아이템: 포션, 회복, 아이템, 사용, 먹기
        - 방어: 방어, 막기, 피하기, 회피
        짧고 명확한 게임 명령어를 우선 인식하세요.
        """
    
    async def connect(self) -> bool:
        """OpenAI Realtime API에 WebSocket 연결"""
        try:
            uri = "wss://api.openai.com/v1/realtime"
            headers = {
                "Authorization": f"Bearer {self.api_key}",
                "OpenAI-Beta": "realtime=v1"
            }
            
            self.websocket = await websockets.connect(uri, extra_headers=headers)
            await self._initialize_session()
            self.is_connected = True
            self.logger.info("GPT-4o 트랜스크립션 서비스 연결 성공")
            return True
            
        except Exception as e:
            self.logger.error(f"GPT-4o 서비스 연결 실패: {e}")
            return False
    
    async def _initialize_session(self):
        """트랜스크립션 세션 초기화"""
        await self.websocket.send(json.dumps(self.session_config))
        
        # 세션 생성 확인 대기
        response = await self.websocket.recv()
        session_data = json.loads(response)
        
        if session_data.get("type") == "transcription_session.created":
            self.session_id = session_data.get("id")
            self.logger.info(f"트랜스크립션 세션 생성됨: {self.session_id}")
    
    async def send_audio_chunk(self, audio_data: bytes):
        """
        실시간 오디오 데이터 전송
        
        Args:
            audio_data (bytes): PCM16 형식의 오디오 데이터 (24kHz)
        """
        if not self.is_connected or not self.websocket:
            raise ConnectionError("트랜스크립션 서비스에 연결되지 않음")
        
        # 오디오 데이터를 Base64로 인코딩
        audio_base64 = base64.b64encode(audio_data).decode('utf-8')
        
        message = {
            "type": "input_audio_buffer.append",
            "audio": audio_base64
        }
        
        await self.websocket.send(json.dumps(message))
    
    async def commit_audio_buffer(self):
        """수동으로 오디오 버퍼 커밋 (VAD 비활성화 시 사용)"""
        if not self.is_connected or not self.websocket:
            return
            
        message = {"type": "input_audio_buffer.commit"}
        await self.websocket.send(json.dumps(message))
    
    async def listen_for_transcriptions(self):
        """트랜스크립션 결과 실시간 수신"""
        try:
            async for message in self.websocket:
                data = json.loads(message)
                await self._handle_transcription_event(data)
                
        except websockets.exceptions.ConnectionClosed:
            self.logger.warning("트랜스크립션 연결이 종료됨")
            self.is_connected = False
        except Exception as e:
            self.logger.error(f"트랜스크립션 수신 오류: {e}")
    
    async def _handle_transcription_event(self, event: Dict[str, Any]):
        """
        트랜스크립션 이벤트 처리
        
        Args:
            event (Dict[str, Any]): OpenAI Realtime API 이벤트 데이터
        """
        event_type = event.get("type")
        
        if event_type == "conversation.item.input_audio_transcription.delta":
            # 실시간 부분 트랜스크립션
            delta_text = event.get("delta", "")
            item_id = event.get("item_id")
            
            if self.transcription_callback:
                await self.transcription_callback({
                    "type": "partial",
                    "text": delta_text,
                    "item_id": item_id,
                    "timestamp": datetime.now().isoformat()
                })
        
        elif event_type == "conversation.item.input_audio_transcription.completed":
            # 완료된 트랜스크립션 결과
            transcript = event.get("transcript", "")
            item_id = event.get("item_id")
            confidence_score = self._calculate_confidence(event.get("logprobs", []))
            
            if self.transcription_callback:
                await self.transcription_callback({
                    "type": "final",
                    "text": transcript,
                    "item_id": item_id,
                    "confidence": confidence_score,
                    "timestamp": datetime.now().isoformat()
                })
        
        elif event_type == "input_audio_buffer.committed":
            # 오디오 버퍼 커밋 완료
            self.logger.debug(f"오디오 버퍼 커밋됨: {event.get('item_id')}")
        
        elif event_type == "error":
            # API 오류 처리
            error_msg = event.get("error", {}).get("message", "알 수 없는 오류")
            self.logger.error(f"트랜스크립션 API 오류: {error_msg}")
    
    def _calculate_confidence(self, logprobs: list) -> float:
        """
        로그 확률을 기반으로 신뢰도 계산
        
        Args:
            logprobs (list): 토큰별 로그 확률 리스트
            
        Returns:
            float: 0.0 ~ 1.0 사이의 신뢰도 점수
        """
        if not logprobs:
            return 0.0
        
        # 로그 확률을 확률로 변환하고 평균 계산
        probs = [min(1.0, max(0.0, 2 ** logprob)) for logprob in logprobs if logprob is not None]
        return sum(probs) / len(probs) if probs else 0.0
    
    def set_transcription_callback(self, callback: Callable):
        """
        트랜스크립션 결과 콜백 함수 설정
        
        Args:
            callback (Callable): 트랜스크립션 결과를 처리할 비동기 함수
        """
        self.transcription_callback = callback
    
    async def disconnect(self):
        """WebSocket 연결 종료"""
        if self.websocket and not self.websocket.closed:
            await self.websocket.close()
        self.is_connected = False
        self.session_id = None
        self.logger.info("GPT-4o 트랜스크립션 서비스 연결 해제")


# 테스트용 예제 함수들
async def example_transcription_handler(transcription_data: Dict[str, Any]):
    """
    트랜스크립션 결과 처리 예제 함수
    
    Args:
        transcription_data (Dict[str, Any]): 트랜스크립션 결과 데이터
    """
    if transcription_data["type"] == "partial":
        print(f"🎙️ 부분 인식: {transcription_data['text']}")
    elif transcription_data["type"] == "final":
        print(f"✅ 최종 인식: {transcription_data['text']} (신뢰도: {transcription_data['confidence']:.2f})")


async def test_gpt4o_service(api_key: str):
    """
    GPT-4o 트랜스크립션 서비스 테스트 함수
    
    Args:
        api_key (str): OpenAI API 키
    """
    service = GPT4oTranscriptionService(api_key)
    service.set_transcription_callback(example_transcription_handler)
    
    if await service.connect():
        print("GPT-4o 서비스 연결 성공!")
        # 백그라운드에서 트랜스크립션 수신
        listen_task = asyncio.create_task(service.listen_for_transcriptions())
        
        # 여기서 실제 오디오 데이터를 전송할 수 있음
        # await service.send_audio_chunk(audio_data)
        
        try:
            await listen_task
        except KeyboardInterrupt:
            print("서비스 종료 중...")
            await service.disconnect()
    else:
        print("GPT-4o 서비스 연결 실패!")


if __name__ == "__main__":
    import os
    api_key = os.getenv('OPENAI_API_KEY')
    if api_key:
        asyncio.run(test_gpt4o_service(api_key))
    else:
        print("OPENAI_API_KEY 환경변수를 설정해주세요.") 