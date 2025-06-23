"""
사용자 제공 코드 기반 간단한 GPT-4o WebSocket 테스트
"""

import os
import json
from dotenv import load_dotenv

# 환경변수 로드
load_dotenv()

# 사용자가 제공한 원본 코드를 기반으로 한 간단한 테스트
def simple_websocket_test():
    """간단한 WebSocket 연결 테스트"""
    
    OPENAI_API_KEY = os.environ.get("OPENAI_API_KEY")
    
    if not OPENAI_API_KEY:
        print("❌ OPENAI_API_KEY 환경변수가 설정되지 않았습니다.")
        return False
    
    print(f"🔑 API 키 확인: {OPENAI_API_KEY[:7]}...{OPENAI_API_KEY[-4:]}")
    
    # websocket-client가 설치 안 되어 있으니 requests로 HTTP 테스트
    try:
        import requests
        
        # OpenAI API 기본 연결 테스트
        headers = {
            "Authorization": f"Bearer {OPENAI_API_KEY}",
            "Content-Type": "application/json"
        }
        
        # 간단한 API 호출로 키 유효성 확인
        response = requests.get(
            "https://api.openai.com/v1/models",
            headers=headers,
            timeout=10
        )
        
        if response.status_code == 200:
            print("✅ OpenAI API 연결 성공!")
            
            models = response.json()
            gpt4_models = [m['id'] for m in models['data'] if 'gpt-4' in m['id']]
            print(f"🔍 사용 가능한 GPT-4 모델: {len(gpt4_models)}개")
            
            # Realtime API 사용 가능 여부 확인 (간접적)
            if any('gpt-4o' in model for model in gpt4_models):
                print("✅ GPT-4o 모델 접근 가능")
                print("🌐 Realtime API 테스트는 WebSocket이 필요합니다.")
                print("📝 현재는 HTTP API로만 연결 확인")
                return True
            else:
                print("⚠️ GPT-4o 모델 접근 제한")
                return False
                
        else:
            print(f"❌ API 연결 실패: {response.status_code}")
            print(f"📄 응답: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ 연결 테스트 실패: {e}")
        return False

def test_with_basic_websockets():
    """기존 websockets 라이브러리로 테스트"""
    try:
        import asyncio
        import websockets
        import json
        
        print("🌐 websockets 라이브러리로 연결 테스트 시도...")
        
        async def test_connection():
            OPENAI_API_KEY = os.environ.get("OPENAI_API_KEY")
            
            if not OPENAI_API_KEY:
                return False
            
            uri = "wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01"
            headers = {
                "Authorization": f"Bearer {OPENAI_API_KEY}",
                "OpenAI-Beta": "realtime=v1"
            }
            
            try:
                # 최신 websockets 라이브러리 방식
                websocket = await websockets.connect(
                    uri,
                    additional_headers=headers
                )
                
                print("✅ WebSocket 연결 성공!")
                
                # 간단한 세션 설정 전송
                session_config = {
                    "type": "session.update",
                    "session": {
                        "modalities": ["text", "audio"],
                        "instructions": "안녕하세요! 게임 음성 명령을 인식해주세요.",
                        "voice": "alloy",
                        "input_audio_format": "pcm16",
                        "output_audio_format": "pcm16"
                    }
                }
                
                await websocket.send(json.dumps(session_config))
                print("📤 세션 설정 전송됨")
                
                # 응답 대기
                try:
                    response = await asyncio.wait_for(websocket.recv(), timeout=5.0)
                    data = json.loads(response)
                    print(f"📥 응답 수신: {data.get('type', 'unknown')}")
                    
                    if data.get('type') == 'session.created':
                        print("🎉 GPT-4o Realtime API 연결 성공!")
                        await websocket.close()
                        return True
                    else:
                        print(f"⚠️ 예상과 다른 응답: {data}")
                        
                except asyncio.TimeoutError:
                    print("⏱️ 응답 타임아웃 (5초)")
                    
                await websocket.close()
                return False
                
            except Exception as e:
                print(f"❌ WebSocket 연결 실패: {e}")
                return False
        
        # 비동기 함수 실행
        return asyncio.run(test_connection())
        
    except ImportError:
        print("❌ websockets 라이브러리가 설치되지 않았습니다.")
        return False
    except Exception as e:
        print(f"❌ WebSocket 테스트 실패: {e}")
        return False

if __name__ == "__main__":
    print("🚀 GPT-4o 연결 테스트 시작")
    print("=" * 50)
    
    # 1. 기본 HTTP API 테스트
    print("1️⃣ HTTP API 연결 테스트:")
    http_success = simple_websocket_test()
    
    print("\n" + "=" * 50)
    
    # 2. WebSocket 테스트 (websockets 라이브러리)
    print("2️⃣ WebSocket 연결 테스트:")
    if http_success:
        ws_success = test_with_basic_websockets()
        
        if ws_success:
            print("\n🎉 모든 테스트 통과!")
            print("✅ GPT-4o Realtime API 사용 준비 완료")
        else:
            print("\n⚠️ WebSocket 연결 문제")
            print("💡 API 키에 GPT-4o Realtime API 베타 권한이 필요할 수 있습니다")
    else:
        print("\n❌ 기본 API 연결 실패")
        print("�� API 키 설정을 확인해주세요") 