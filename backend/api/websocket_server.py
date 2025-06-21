"""
VoiceMacro Pro - WebSocket API 서버
GPT-4o 실시간 음성인식을 위한 WebSocket 통신 서버
"""

import asyncio
import websockets
import json
import logging
import base64
from typing import Dict, Set, Optional, Callable
from datetime import datetime
import threading
import queue

from backend.services.voice_service import VoiceRecognitionService
from backend.services.macro_service import MacroService
from backend.services.macro_matching_service import MacroMatchingService
from backend.utils.config import Config
from backend.utils.common_utils import get_logger


class WebSocketVoiceServer:
    """
    WebSocket 기반 실시간 음성인식 서버
    - C# WPF 클라이언트와 실시간 통신
    - GPT-4o 트랜스크립션 결과 전송
    - 매크로 매칭 및 실행 결과 전송
    """
    
    def __init__(self):
        """WebSocket 서버 초기화"""
        self.logger = get_logger(__name__)
        
        # 서버 설정
        self.host = Config.get_websocket_config()['host']
        self.port = Config.get_websocket_config()['port']
        
        # 연결된 클라이언트 관리
        self.connected_clients: Set[websockets.WebSocketServerProtocol] = set()
        self.client_sessions: Dict[str, Dict] = {}
        
        # 서비스 인스턴스
        self.voice_service: Optional[VoiceRecognitionService] = None
        self.macro_service: Optional[MacroService] = None
        self.macro_matching_service: Optional[MacroMatchingService] = None
        
        # 서버 상태
        self.is_running = False
        self.server = None
        
        # 이벤트 핸들러
        self.transcription_handlers: Dict[str, Callable] = {}
        self.macro_execution_handlers: Dict[str, Callable] = {}
        
        self.logger.info(f"WebSocket 서버 초기화 완료 ({self.host}:{self.port})")
    
    async def initialize_services(self):
        """백엔드 서비스들 초기화"""
        try:
            # 음성 인식 서비스 초기화
            self.voice_service = VoiceRecognitionService()
            self.voice_service.set_transcription_callback(self._on_transcription_result)
            
            # 매크로 서비스 초기화
            from backend.services.macro_service import get_macro_service
            self.macro_service = get_macro_service()
            
            # 매크로 매칭 서비스 초기화
            from backend.services.macro_matching_service import get_macro_matching_service
            self.macro_matching_service = get_macro_matching_service()
            
            self.logger.info("모든 백엔드 서비스 초기화 완료")
            
        except Exception as e:
            self.logger.error(f"서비스 초기화 실패: {e}")
            raise
    
    async def start_server(self):
        """WebSocket 서버 시작"""
        try:
            # 백엔드 서비스 초기화
            await self.initialize_services()
            
            # WebSocket 서버 시작
            self.server = await websockets.serve(
                self.handle_client,
                self.host,
                self.port,
                ping_interval=20,  # 20초마다 ping
                ping_timeout=10,   # 10초 ping 타임아웃
                max_size=10 * 1024 * 1024  # 10MB 최대 메시지 크기
            )
            
            self.is_running = True
            self.logger.info(f"WebSocket 서버가 {self.host}:{self.port}에서 시작되었습니다.")
            
            # 서버 실행 대기
            await self.server.wait_closed()
            
        except Exception as e:
            self.logger.error(f"WebSocket 서버 시작 실패: {e}")
            raise
    
    async def stop_server(self):
        """WebSocket 서버 종료"""
        try:
            self.is_running = False
            
            # 모든 클라이언트 연결 종료
            if self.connected_clients:
                await asyncio.gather(
                    *[client.close() for client in self.connected_clients.copy()],
                    return_exceptions=True
                )
            
            # 서버 종료
            if self.server:
                self.server.close()
                await self.server.wait_closed()
            
            self.logger.info("WebSocket 서버가 종료되었습니다.")
            
        except Exception as e:
            self.logger.error(f"WebSocket 서버 종료 실패: {e}")
    
    async def handle_client(self, websocket, path):
        """
        클라이언트 연결 처리
        
        Args:
            websocket: WebSocket 연결
            path: 연결 경로
        """
        client_id = f"{websocket.remote_address[0]}:{websocket.remote_address[1]}"
        self.logger.info(f"새 클라이언트 연결: {client_id}")
        
        try:
            # 클라이언트 등록
            self.connected_clients.add(websocket)
            self.client_sessions[client_id] = {
                'websocket': websocket,
                'connected_at': datetime.now(),
                'is_recording': False,
                'last_activity': datetime.now()
            }
            
            # 연결 성공 메시지 전송
            await self.send_message(websocket, {
                'type': 'connection_established',
                'client_id': client_id,
                'server_time': datetime.now().isoformat(),
                'features': ['gpt4o_transcription', 'macro_matching', 'real_time_audio']
            })
            
            # 클라이언트 메시지 처리 루프
            async for message in websocket:
                await self._handle_client_message(websocket, client_id, message)
                
        except websockets.exceptions.ConnectionClosed:
            self.logger.info(f"클라이언트 연결 종료: {client_id}")
        except Exception as e:
            self.logger.error(f"클라이언트 처리 오류 ({client_id}): {e}")
        finally:
            # 클라이언트 정리
            await self._cleanup_client(websocket, client_id)
    
    async def _handle_client_message(self, websocket, client_id: str, message: str):
        """
        클라이언트 메시지 처리
        
        Args:
            websocket: WebSocket 연결
            client_id: 클라이언트 ID
            message: 수신된 메시지
        """
        try:
            # JSON 메시지 파싱
            data = json.loads(message)
            message_type = data.get('type')
            
            # 클라이언트 활동 시간 업데이트
            if client_id in self.client_sessions:
                self.client_sessions[client_id]['last_activity'] = datetime.now()
            
            self.logger.debug(f"메시지 수신 ({client_id}): {message_type}")
            
            # 메시지 타입별 처리
            if message_type == 'start_recording':
                await self._handle_start_recording(websocket, client_id, data)
            elif message_type == 'stop_recording':
                await self._handle_stop_recording(websocket, client_id, data)
            elif message_type == 'audio_chunk':
                await self._handle_audio_chunk(websocket, client_id, data)
            elif message_type == 'get_macros':
                await self._handle_get_macros(websocket, client_id, data)
            elif message_type == 'match_voice_command':
                await self._handle_match_voice_command(websocket, client_id, data)
            elif message_type == 'ping':
                await self._handle_ping(websocket, client_id, data)
            else:
                await self.send_error(websocket, f"알 수 없는 메시지 타입: {message_type}")
                
        except json.JSONDecodeError:
            await self.send_error(websocket, "JSON 파싱 실패")
        except Exception as e:
            self.logger.error(f"메시지 처리 오류 ({client_id}): {e}")
            await self.send_error(websocket, f"메시지 처리 오류: {str(e)}")
    
    async def _handle_start_recording(self, websocket, client_id: str, data: Dict):
        """음성 녹음 시작 처리"""
        try:
            if not self.voice_service:
                await self.send_error(websocket, "음성 서비스가 초기화되지 않았습니다.")
                return
            
            # 녹음 시작
            success = self.voice_service.start_recording()
            
            if success:
                self.client_sessions[client_id]['is_recording'] = True
                await self.send_message(websocket, {
                    'type': 'recording_started',
                    'message': '음성 녹음이 시작되었습니다.',
                    'gpt4o_enabled': self.voice_service.gpt4o_enabled
                })
                self.logger.info(f"음성 녹음 시작: {client_id}")
            else:
                await self.send_error(websocket, "음성 녹음 시작 실패")
                
        except Exception as e:
            await self.send_error(websocket, f"녹음 시작 오류: {str(e)}")
    
    async def _handle_stop_recording(self, websocket, client_id: str, data: Dict):
        """음성 녹음 중지 처리"""
        try:
            if not self.voice_service:
                await self.send_error(websocket, "음성 서비스가 초기화되지 않았습니다.")
                return
            
            # 녹음 중지
            success = self.voice_service.stop_recording()
            
            if success:
                self.client_sessions[client_id]['is_recording'] = False
                await self.send_message(websocket, {
                    'type': 'recording_stopped',
                    'message': '음성 녹음이 중지되었습니다.'
                })
                self.logger.info(f"음성 녹음 중지: {client_id}")
            else:
                await self.send_error(websocket, "음성 녹음 중지 실패")
                
        except Exception as e:
            await self.send_error(websocket, f"녹음 중지 오류: {str(e)}")
    
    async def _handle_audio_chunk(self, websocket, client_id: str, data: Dict):
        """오디오 청크 처리 (현재는 GPT-4o가 자동으로 처리)"""
        try:
            # GPT-4o는 실시간 오디오 스트림을 자동으로 처리하므로
            # 클라이언트에서 별도로 오디오 청크를 보낼 필요 없음
            await self.send_message(websocket, {
                'type': 'audio_chunk_received',
                'message': 'GPT-4o가 실시간으로 오디오를 처리 중입니다.'
            })
            
        except Exception as e:
            await self.send_error(websocket, f"오디오 처리 오류: {str(e)}")
    
    async def _handle_get_macros(self, websocket, client_id: str, data: Dict):
        """매크로 목록 조회 처리"""
        try:
            if not self.macro_service:
                await self.send_error(websocket, "매크로 서비스가 초기화되지 않았습니다.")
                return
            
            # 매크로 목록 조회
            macros = self.macro_service.get_all_macros()
            
            await self.send_message(websocket, {
                'type': 'macros_list',
                'macros': macros,
                'count': len(macros)
            })
            
        except Exception as e:
            await self.send_error(websocket, f"매크로 조회 오류: {str(e)}")
    
    async def _handle_match_voice_command(self, websocket, client_id: str, data: Dict):
        """음성 명령어 매칭 처리"""
        try:
            voice_text = data.get('voice_text', '').strip()
            if not voice_text:
                await self.send_error(websocket, "음성 텍스트가 비어있습니다.")
                return
            
            if not self.macro_matching_service:
                await self.send_error(websocket, "매크로 매칭 서비스가 초기화되지 않았습니다.")
                return
            
            # 매크로 매칭 실행
            match_result = self.macro_matching_service.find_best_match(voice_text)
            
            await self.send_message(websocket, {
                'type': 'macro_match_result',
                'voice_text': voice_text,
                'match_result': match_result
            })
            
        except Exception as e:
            await self.send_error(websocket, f"매크로 매칭 오류: {str(e)}")
    
    async def _handle_ping(self, websocket, client_id: str, data: Dict):
        """Ping 처리"""
        await self.send_message(websocket, {
            'type': 'pong',
            'timestamp': datetime.now().isoformat(),
            'client_id': client_id
        })
    
    async def _on_transcription_result(self, transcription_data: Dict):
        """
        GPT-4o 트랜스크립션 결과 처리 콜백
        
        Args:
            transcription_data: 트랜스크립션 결과 데이터
        """
        try:
            # 모든 연결된 클라이언트에게 트랜스크립션 결과 전송
            if self.connected_clients:
                message = {
                    'type': 'transcription_result',
                    'transcript': transcription_data.get('transcript', ''),
                    'confidence': transcription_data.get('confidence', 0.0),
                    'timestamp': transcription_data.get('timestamp', datetime.now().isoformat()),
                    'success': transcription_data.get('success', False)
                }
                
                # 매크로 매칭 시도
                if transcription_data.get('success') and self.macro_matching_service:
                    transcript = transcription_data.get('transcript', '').strip()
                    if transcript:
                        match_result = self.macro_matching_service.find_best_match(transcript)
                        message['macro_match'] = match_result
                
                # 모든 클라이언트에 브로드캐스트
                await self.broadcast_message(message)
                
                self.logger.info(f"트랜스크립션 결과 브로드캐스트: {transcription_data.get('transcript', '')}")
                
        except Exception as e:
            self.logger.error(f"트랜스크립션 결과 처리 오류: {e}")
    
    async def send_message(self, websocket, message: Dict):
        """
        특정 클라이언트에게 메시지 전송
        
        Args:
            websocket: 대상 WebSocket 연결
            message: 전송할 메시지 딕셔너리
        """
        try:
            json_message = json.dumps(message, ensure_ascii=False)
            await websocket.send(json_message)
        except Exception as e:
            self.logger.error(f"메시지 전송 실패: {e}")
    
    async def send_error(self, websocket, error_message: str):
        """
        클라이언트에게 에러 메시지 전송
        
        Args:
            websocket: 대상 WebSocket 연결
            error_message: 에러 메시지
        """
        await self.send_message(websocket, {
            'type': 'error',
            'message': error_message,
            'timestamp': datetime.now().isoformat()
        })
    
    async def broadcast_message(self, message: Dict):
        """
        모든 연결된 클라이언트에게 메시지 브로드캐스트
        
        Args:
            message: 브로드캐스트할 메시지 딕셔너리
        """
        if not self.connected_clients:
            return
        
        # 연결이 끊어진 클라이언트 제거를 위한 복사본 생성
        clients_copy = self.connected_clients.copy()
        
        for client in clients_copy:
            try:
                await self.send_message(client, message)
            except Exception as e:
                self.logger.warning(f"브로드캐스트 실패 (클라이언트 제거): {e}")
                self.connected_clients.discard(client)
    
    async def _cleanup_client(self, websocket, client_id: str):
        """
        클라이언트 정리
        
        Args:
            websocket: WebSocket 연결
            client_id: 클라이언트 ID
        """
        try:
            # 클라이언트 제거
            self.connected_clients.discard(websocket)
            
            # 세션 정보 제거
            if client_id in self.client_sessions:
                session = self.client_sessions.pop(client_id)
                
                # 녹음 중이었다면 중지
                if session.get('is_recording') and self.voice_service:
                    self.voice_service.stop_recording()
                    self.logger.info(f"클라이언트 연결 해제로 인한 녹음 중지: {client_id}")
            
            self.logger.info(f"클라이언트 정리 완료: {client_id}")
            
        except Exception as e:
            self.logger.error(f"클라이언트 정리 오류: {e}")
    
    def get_server_status(self) -> Dict:
        """
        서버 상태 정보 반환
        
        Returns:
            Dict: 서버 상태 정보
        """
        return {
            'is_running': self.is_running,
            'host': self.host,
            'port': self.port,
            'connected_clients': len(self.connected_clients),
            'client_sessions': list(self.client_sessions.keys()),
            'gpt4o_enabled': self.voice_service.gpt4o_enabled if self.voice_service else False
        }


# 전역 서버 인스턴스
_websocket_server: Optional[WebSocketVoiceServer] = None

def get_websocket_server() -> WebSocketVoiceServer:
    """
    WebSocket 서버 인스턴스 반환 (싱글톤)
    
    Returns:
        WebSocketVoiceServer: 서버 인스턴스
    """
    global _websocket_server
    if _websocket_server is None:
        _websocket_server = WebSocketVoiceServer()
    return _websocket_server


async def main():
    """WebSocket 서버 실행"""
    server = get_websocket_server()
    try:
        await server.start_server()
    except KeyboardInterrupt:
        print("\n서버 종료 중...")
        await server.stop_server()


if __name__ == "__main__":
    # WebSocket 서버 실행
    asyncio.run(main()) 