"""
빠른 API 키 테스트 스크립트
API 키 설정 후 즉시 실행하여 연결을 확인합니다.
"""

import os

def quick_test():
    """빠른 API 키 테스트"""
    print("🚀 빠른 API 키 테스트")
    print("=" * 40)
    
    # 1. API 키 확인
    api_key = os.getenv('OPENAI_API_KEY')
    
    if not api_key:
        print("❌ OPENAI_API_KEY 환경변수가 없습니다!")
        return False
    
    print(f"✅ API 키 발견: {api_key[:7]}...{api_key[-4:]}")
    
    # 2. 더미 키 체크
    if api_key == "sk-your-actual-api-key-here":
        print("❌ 더미 API 키입니다! 실제 키로 교체하세요:")
        print("   $env:OPENAI_API_KEY = 'sk-실제API키'")
        return False
    
    # 3. 형식 확인
    if not api_key.startswith('sk-'):
        print("❌ API 키 형식이 올바르지 않습니다!")
        return False
    
    print("✅ API 키 형식 정상")
    
    # 4. 간단한 연결 테스트
    try:
        import openai
        client = openai.OpenAI(api_key=api_key)
        
        print("🔄 OpenAI API 연결 테스트 중...")
        models = client.models.list()
        print("✅ API 키 유효성 확인됨!")
        
        # GPT-4o 사용 가능 여부
        model_names = [model.id for model in models.data]
        if any('gpt-4' in model for model in model_names):
            print("✅ GPT-4o 사용 가능")
        else:
            print("⚠️ GPT-4 계열 모델 확인 필요")
        
        print("\n🎉 모든 테스트 통과!")
        print("✅ GPT-4o 트랜스크립션 사용 준비 완료")
        return True
        
    except Exception as e:
        print(f"❌ API 연결 실패: {e}")
        print("\n🔧 해결 방법:")
        print("1. API 키가 올바른지 확인")
        print("2. OpenAI 계정에 결제 정보 설정")
        print("3. 계정 상태 확인")
        return False

if __name__ == "__main__":
    success = quick_test()
    
    if success:
        print("\n🔥 이제 GPT-4o 테스트를 실행하세요:")
        print("   py backend/tests/test_gpt4o_transcription.py")
    else:
        print("\n📖 자세한 설정 방법: API_KEY_SETUP_GUIDE.md") 