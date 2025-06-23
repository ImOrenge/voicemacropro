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
        """
        오디오 청크 처리 - Voice Activity Detection 추가 검증
        클라이언트에서 이미 VAD를 통과한 오디오만 받지만, 백엔드에서 추가 검증을 실시합니다.
        """
        try:
            # 오디오 데이터 추출
            audio_base64 = data.get('audio', '')
            audio_level = data.get('audio_level', 0.0)
            has_voice = data.get('has_voice', False)
            
            if not audio_base64:
                await self.send_error(websocket, "오디오 데이터가 비어있습니다.")
                return
            
            # Base64 디코딩 시도
            try:
                audio_bytes = base64.b64decode(audio_base64)
                audio_length = len(audio_bytes)
            except Exception as decode_error:
                self.logger.error(f"오디오 데이터 디코딩 실패: {decode_error}")
                await self.send_error(websocket, "오디오 데이터 디코딩 실패")
                return
            
            # 최소 오디오 길이 확인 (너무 짧은 데이터 필터링)
            MIN_AUDIO_LENGTH = 960  # 24kHz * 0.04초 (40ms) * 2bytes = 1920bytes 최소
            if audio_length < MIN_AUDIO_LENGTH:
                self.logger.debug(f"오디오 청크가 너무 짧음: {audio_length} bytes (최소: {MIN_AUDIO_LENGTH})")
                await self.send_message(websocket, {
                    'type': 'audio_chunk_received',
                    'success': False,
                    'reason': 'too_short',
                    'audio_length': audio_length
                })
                return
            
            # 클라이언트 VAD 결과 확인
            if not has_voice:
                self.logger.debug(f"클라이언트 VAD: 음성 없음 (레벨: {audio_level:.3f})")
                await self.send_message(websocket, {
                    'type': 'audio_chunk_received',
                    'success': False,
                    'reason': 'no_voice_detected',
                    'audio_level': audio_level,
                    'audio_length': audio_length
                })
                return
            
            # 백엔드 추가 검증
            backend_vad_result = self._validate_audio_chunk(audio_bytes, audio_level)
            
            if not backend_vad_result['is_valid']:
                self.logger.debug(f"백엔드 VAD 실패: {backend_vad_result['reason']}")
                await self.send_message(websocket, {
                    'type': 'audio_chunk_received',
                    'success': False,
                    'reason': f"backend_vad_{backend_vad_result['reason']}",
                    'audio_level': audio_level,
                    'audio_length': audio_length
                })
                return
            
            # 모든 검증 통과 - GPT-4o로 전송 (실제 트랜스크립션 로직은 별도 서비스에서 처리)
            if self.voice_service and hasattr(self.voice_service, 'process_audio_chunk'):
                try:
                    # 음성 서비스로 오디오 전달
                    await self.voice_service.process_audio_chunk(audio_bytes)
                    self.logger.debug(f"🎤 유효한 음성 감지: {audio_length} bytes (레벨: {audio_level:.3f})")
                except Exception as process_error:
                    self.logger.error(f"음성 처리 오류: {process_error}")
                    await self.send_error(websocket, f"음성 처리 오류: {str(process_error)}")
                    return
            
            # 성공 응답
            await self.send_message(websocket, {
                'type': 'audio_chunk_received',
                'success': True,
                'audio_length': audio_length,
                'audio_level': audio_level,
                'backend_validation': backend_vad_result,
                'timestamp': datetime.now().isoformat()
            })
            
        except Exception as e:
            self.logger.error(f"오디오 청크 처리 오류: {e}")
            await self.send_error(websocket, f"오디오 처리 오류: {str(e)}")
    
    def _validate_audio_chunk(self, audio_bytes: bytes, reported_level: float) -> Dict:
        """
        백엔드에서 오디오 청크 추가 검증
        
        Args:
            audio_bytes: 오디오 데이터 (PCM 16-bit)
            reported_level: 클라이언트에서 보고한 오디오 레벨
            
        Returns:
            Dict: 검증 결과 {'is_valid': bool, 'reason': str, 'calculated_level': float}
        """
        try:
            # 최소 바이트 수 확인
            if len(audio_bytes) < 4:
                return {'is_valid': False, 'reason': 'insufficient_data', 'calculated_level': 0.0}
            
            # 실제 오디오 레벨 재계산 (16-bit PCM)
            import struct
            samples = []
            for i in range(0, len(audio_bytes) - 1, 2):
                try:
                    sample = struct.unpack('<h', audio_bytes[i:i+2])[0]  # Little-endian 16-bit
                    samples.append(abs(sample))
                except:
                    continue
            
            if not samples:
                return {'is_valid': False, 'reason': 'no_valid_samples', 'calculated_level': 0.0}
            
            # RMS 계산
            avg_amplitude = sum(samples) / len(samples)
            calculated_level = min(1.0, avg_amplitude / 32768.0)  # 0.0 ~ 1.0 정규화
            
            # 레벨 차이 확인 (클라이언트 보고값과 비교)
            level_difference = abs(calculated_level - reported_level)
            MAX_LEVEL_DIFFERENCE = 0.1  # 10% 차이 허용
            
            if level_difference > MAX_LEVEL_DIFFERENCE:
                return {
                    'is_valid': False, 
                    'reason': 'level_mismatch', 
                    'calculated_level': calculated_level,
                    'reported_level': reported_level,
                    'difference': level_difference
                }
            
            # 최소 임계값 확인
            MIN_VALID_LEVEL = 0.01  # 1% 이상
            if calculated_level < MIN_VALID_LEVEL:
                return {
                    'is_valid': False, 
                    'reason': 'too_quiet', 
                    'calculated_level': calculated_level
                }
            
            # 최대 임계값 확인 (클리핑 방지)
            MAX_VALID_LEVEL = 0.98  # 98% 이하
            if calculated_level > MAX_VALID_LEVEL:
                return {
                    'is_valid': False, 
                    'reason': 'clipping_detected', 
                    'calculated_level': calculated_level
                }
            
            # 모든 검증 통과
            return {
                'is_valid': True, 
                'reason': 'valid', 
                'calculated_level': calculated_level,
                'sample_count': len(samples)
            }
            
        except Exception as e:
            return {
                'is_valid': False, 
                'reason': f'validation_error_{str(e)}', 
                'calculated_level': 0.0
            }
    
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