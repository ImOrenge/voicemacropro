#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Socket.IO 연결 테스트 스크립트
VoiceMacro Pro의 Socket.IO 서버 연결을 테스트합니다.
"""

import socketio
import time
import threading
import sys
import os

# 백엔드 모듈을 임포트할 수 있도록 경로 추가
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

class SocketIOTestClient:
    """Socket.IO 클라이언트 테스트 클래스"""
    
    def __init__(self, server_url='http://localhost:5000'):
        """
        테스트 클라이언트 초기화
        
        Args:
            server_url (str): 테스트할 서버 URL
        """
        self.server_url = server_url
        self.sio = socketio.Client(
            logger=True, 
            engineio_logger=True
        )
        self.connected = False
        self.setup_event_handlers()
    
    def setup_event_handlers(self):
        """이벤트 핸들러 설정"""
        
        @self.sio.event
        def connect():
            """서버 연결 성공"""
            print("✅ Socket.IO 서버에 연결됨!")
            self.connected = True
            
        @self.sio.event
        def disconnect():
            """서버 연결 해제"""
            print("❌ Socket.IO 서버 연결 해제됨")
            self.connected = False
            
        @self.sio.event
        def connection_established(data):
            """서버로부터 연결 확인 메시지"""
            print(f"🎉 연결 확인: {data}")
            
        @self.sio.event
        def voice_recognition_started(data):
            """음성인식 시작 확인"""
            print(f"🎤 음성인식 시작: {data}")
            
        @self.sio.event
        def voice_recognition_stopped(data):
            """음성인식 중지 확인"""
            print(f"🛑 음성인식 중지: {data}")
            
        @self.sio.event
        def pong(data):
            """핑 응답"""
            print(f"🏓 Pong 수신: {data}")
            
        @self.sio.on('*')
        def catch_all(event, data):
            """모든 이벤트 캐치"""
            print(f"📡 이벤트 수신: {event} -> {data}")
    
    def test_connection(self):
        """기본 연결 테스트"""
        try:
            print(f"🔌 Socket.IO 서버 연결 시도: {self.server_url}")
            self.sio.connect(
                self.server_url, 
                wait_timeout=10,
                transports=['polling', 'websocket']
            )
            
            if self.connected:
                print("✅ 연결 성공!")
                return True
            else:
                print("❌ 연결 실패: 연결되지 않음")
                return False
                
        except Exception as e:
            print(f"❌ 연결 실패: {e}")
            return False
    
    def test_voice_recognition_events(self):
        """음성인식 이벤트 테스트"""
        if not self.connected:
            print("❌ 서버에 연결되지 않음")
            return False
        
        try:
            print("\n🧪 음성인식 이벤트 테스트 시작...")
            
            # 음성인식 시작
            print("📤 start_voice_recognition 이벤트 전송")
            self.sio.emit('start_voice_recognition')
            time.sleep(2)
            
            # 핑 테스트
            print("📤 ping 이벤트 전송")
            self.sio.emit('ping', {'test': True})
            time.sleep(1)
            
            # 더미 오디오 데이터 전송
            print("📤 더미 오디오 청크 전송")
            dummy_audio = b'\x00' * 1024  # 1KB 더미 데이터
            import base64
            self.sio.emit('audio_chunk', {
                'audio': base64.b64encode(dummy_audio).decode('utf-8')
            })
            time.sleep(1)
            
            # 음성인식 중지
            print("📤 stop_voice_recognition 이벤트 전송")
            self.sio.emit('stop_voice_recognition')
            time.sleep(2)
            
            print("✅ 음성인식 이벤트 테스트 완료")
            return True
            
        except Exception as e:
            print(f"❌ 음성인식 이벤트 테스트 실패: {e}")
            return False
    
    def run_comprehensive_test(self):
        """종합 테스트 실행"""
        print("🚀 Socket.IO 종합 테스트 시작\n")
        
        # 1. 연결 테스트
        if not self.test_connection():
            print("❌ 연결 테스트 실패 - 테스트 중단")
            return False
        
        time.sleep(2)
        
        # 2. 음성인식 이벤트 테스트
        if not self.test_voice_recognition_events():
            print("❌ 음성인식 이벤트 테스트 실패")
            return False
        
        time.sleep(1)
        
        # 3. 연결 해제
        print("\n🔌 연결 해제 중...")
        self.sio.disconnect()
        
        print("\n✅ 모든 테스트가 성공적으로 완료되었습니다!")
        return True
    
    def disconnect(self):
        """연결 해제"""
        if self.connected:
            self.sio.disconnect()

def main():
    """메인 테스트 함수"""
    print("=" * 60)
    print("🧪 VoiceMacro Pro Socket.IO 연결 테스트")
    print("=" * 60)
    
    # 서버 실행 확인
    import requests
    try:
        response = requests.get('http://localhost:5000/api/health', timeout=5)
        print("✅ VoiceMacro Pro 서버가 실행 중입니다")
        print(f"   서버 상태: {response.status_code}")
    except requests.exceptions.RequestException:
        print("❌ VoiceMacro Pro 서버가 실행되지 않음")
        print("   먼저 'py run_server.py' 명령으로 서버를 시작하세요!")
        return False
    
    # Socket.IO 테스트 실행
    test_client = SocketIOTestClient()
    
    try:
        success = test_client.run_comprehensive_test()
        return success
    except KeyboardInterrupt:
        print("\n⚠️ 사용자에 의해 테스트가 중단됨")
        test_client.disconnect()
        return False
    except Exception as e:
        print(f"\n❌ 예상치 못한 오류: {e}")
        test_client.disconnect()
        return False

if __name__ == '__main__':
    success = main()
    exit(0 if success else 1) 