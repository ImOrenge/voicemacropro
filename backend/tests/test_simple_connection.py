"""
간단한 OpenAI API 키 유효성 테스트
GPT-4o 연결 문제 진단용
"""

import os
import asyncio
import sys

# 프로젝트 경로 추가
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

async def test_openai_api_key():
    """OpenAI API 키 유효성 간단 테스트"""
    
    print("🔍 OpenAI API 키 테스트 시작")
    print("=" * 50)
    
    # 1. 환경변수 확인
    api_key = os.getenv('OPENAI_API_KEY')
    
    if not api_key:
        print("❌ OPENAI_API_KEY 환경변수가 설정되지 않음")
        print("\n🔧 해결 방법:")
        print("PowerShell: $env:OPENAI_API_KEY = 'your_real_api_key'")
        return False
    
    print(f"✅ API 키 발견 (길이: {len(api_key)}자)")
    
    # 2. API 키 형식 확인
    if not api_key.startswith('sk-'):
        print(f"⚠️ API 키 형식 이상: '{api_key[:10]}...'")
        print("💡 OpenAI API 키는 'sk-'로 시작해야 합니다")
        if api_key == "sk-your-actual-api-key-here":
            print("❌ 더미 API 키입니다! 실제 API 키로 교체하세요")
            return False
    else:
        print("✅ API 키 형식 정상")
    
    # 3. 기본 OpenAI API 테스트 (간단한 요청)
    try:
        import openai
        client = openai.OpenAI(api_key=api_key)
        
        print("🔄 OpenAI API 기본 연결 테스트 중...")
        
        # 간단한 모델 목록 요청으로 API 키 유효성 확인
        models = client.models.list()
        print("✅ OpenAI API 키 유효성 확인됨")
        
        # GPT-4o 모델 사용 가능 여부 확인
        model_names = [model.id for model in models.data]
        if 'gpt-4o' in model_names:
            print("✅ GPT-4o 모델 접근 가능")
        else:
            print("⚠️ GPT-4o 모델 접근 불가 (계정 권한 확인 필요)")
        
        return True
        
    except Exception as e:
        print(f"❌ OpenAI API 연결 실패: {e}")
        print("\n🔧 해결 방법:")
        print("1. API 키가 유효한지 확인")
        print("2. OpenAI 계정에 결제 정보가 설정되어 있는지 확인")
        print("3. 계정이 정지되지 않았는지 확인")
        return False

async def test_gpt4o_websocket():
    """GPT-4o WebSocket 연결 테스트"""
    
    print("\n🌐 GPT-4o WebSocket 연결 테스트")
    print("=" * 50)
    
    api_key = os.getenv('OPENAI_API_KEY')
    if not api_key or api_key == "sk-your-actual-api-key-here":
        print("❌ 유효한 API 키 필요")
        return False
    
    try:
        from services.gpt4o_transcription_service import GPT4oTranscriptionService
        
        service = GPT4oTranscriptionService(api_key)
        print("🔄 GPT-4o 트랜스크립션 서비스 연결 시도...")
        
        connected = await service.connect()
        
        if connected:
            print("✅ GPT-4o WebSocket 연결 성공!")
            await service.disconnect()
            return True
        else:
            print("❌ GPT-4o WebSocket 연결 실패")
            return False
            
    except Exception as e:
        print(f"❌ GPT-4o 테스트 실패: {e}")
        return False

async def main():
    """메인 테스트 실행"""
    
    print("🚀 VoiceMacro Pro - API 연결 진단 도구")
    print("=" * 60)
    
    # 1. 기본 API 키 테스트
    basic_test = await test_openai_api_key()
    
    if basic_test:
        # 2. GPT-4o WebSocket 테스트
        websocket_test = await test_gpt4o_websocket()
        
        if websocket_test:
            print("\n🎉 모든 테스트 통과!")
            print("✅ GPT-4o 트랜스크립션 사용 준비 완료")
        else:
            print("\n⚠️ WebSocket 연결 문제")
            print("💡 네트워크 설정이나 방화벽을 확인하세요")
    else:
        print("\n❌ API 키 설정 필요")
        print("📖 자세한 설정 방법: API_KEY_SETUP_GUIDE.md 참조")

if __name__ == "__main__":
    asyncio.run(main()) 