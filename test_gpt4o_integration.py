#!/usr/bin/env python3
"""
GPT-4o 트랜스크립션 서비스 통합 테스트
OpenAI API 키가 있는 경우 실제 연결까지 테스트
"""

import os
import sys
import asyncio
import time
from datetime import datetime

# 프로젝트 경로 추가
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from backend.services.gpt4o_transcription_service import GPT4oTranscriptionService
from backend.utils.config import Config

async def test_gpt4o_service():
    """GPT-4o 서비스 기본 테스트"""
    print("🚀 GPT-4o 트랜스크립션 서비스 테스트 시작")
    print("=" * 50)
    
    # 1. 설정 확인
    print(f"📋 설정 확인:")
    print(f"   - GPT4O_ENABLED: {Config.GPT4O_ENABLED}")
    print(f"   - GPT4O_PRIMARY: {Config.GPT4O_PRIMARY}")
    print(f"   - API 키 설정: {'✅' if Config.OPENAI_API_KEY else '❌'}")
    print()
    
    # 2. API 키 확인
    if not Config.OPENAI_API_KEY:
        print("⚠️ OpenAI API 키가 설정되지 않았습니다.")
        print("🔧 다음 중 하나의 방법으로 API 키를 설정하세요:")
        print("   1. PowerShell: $env:OPENAI_API_KEY = 'sk-your-key-here'")
        print("   2. .env 파일: OPENAI_API_KEY=sk-your-key-here")
        print("   3. 시스템 환경변수 설정")
        print()
        print("💡 API 키 없이는 서비스 초기화만 테스트합니다.")
        
        # API 키 없이도 객체 생성 테스트
        try:
            service = GPT4oTranscriptionService("dummy-key-for-test")
            print("✅ GPT-4o 서비스 객체 생성 성공")
            return False
        except Exception as e:
            print(f"❌ GPT-4o 서비스 객체 생성 실패: {e}")
            return False
    
    # 3. 실제 서비스 테스트 (API 키가 있는 경우)
    try:
        print("🔄 GPT-4o 서비스 초기화 중...")
        service = GPT4oTranscriptionService(Config.OPENAI_API_KEY)
        
        # 콜백 함수 설정
        async def test_callback(transcription_data):
            print(f"📝 트랜스크립션 결과: {transcription_data}")
        
        service.set_transcription_callback(test_callback)
        print("✅ 콜백 함수 설정 완료")
        
        # 연결 테스트
        print("🌐 OpenAI Realtime API 연결 시도...")
        connection_success = await service.connect()
        
        if connection_success:
            print("✅ GPT-4o 서비스 연결 성공!")
            print(f"   - 세션 ID: {service.session_id}")
            print(f"   - 연결 상태: {service.is_connected}")
            
            # 연결 해제
            await service.disconnect()
            print("🔌 서비스 연결 해제 완료")
            return True
        else:
            print("❌ GPT-4o 서비스 연결 실패")
            return False
            
    except Exception as e:
        print(f"❌ GPT-4o 서비스 테스트 실패: {e}")
        return False

def test_server_integration():
    """서버 통합 테스트"""
    print("\n🔧 서버 통합 테스트")
    print("-" * 30)
    
    try:
        from backend.api.server import gpt4o_service, Config
        
        if gpt4o_service:
            print("✅ 서버에서 GPT-4o 서비스 객체 확인됨")
            print(f"   - 타입: {type(gpt4o_service)}")
        else:
            print("⚠️ 서버에서 GPT-4o 서비스 객체가 None")
        
        print(f"📋 서버 설정:")
        print(f"   - GPT4O_ENABLED: {Config.GPT4O_ENABLED}")
        print(f"   - OPENAI_API_KEY 설정: {'✅' if Config.OPENAI_API_KEY else '❌'}")
        
        return True
        
    except Exception as e:
        print(f"❌ 서버 통합 테스트 실패: {e}")
        return False

def main():
    """메인 테스트 실행"""
    print(f"🎯 VoiceMacro Pro - GPT-4o 통합 테스트")
    print(f"⏰ 시작 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)
    
    # 비동기 테스트 실행
    gpt4o_success = asyncio.run(test_gpt4o_service())
    
    # 서버 통합 테스트
    server_success = test_server_integration()
    
    # 결과 요약
    print("\n" + "=" * 60)
    print("📊 테스트 결과 요약:")
    print(f"   - GPT-4o 서비스 테스트: {'✅ 성공' if gpt4o_success else '❌ 실패'}")
    print(f"   - 서버 통합 테스트: {'✅ 성공' if server_success else '❌ 실패'}")
    
    if gpt4o_success and server_success:
        print("\n🎉 모든 테스트 통과! GPT-4o 시스템이 준비되었습니다.")
        print("🚀 이제 VoiceMacro Pro 애플리케이션에서 GPT-4o를 사용할 수 있습니다.")
    else:
        print("\n⚠️ 일부 테스트 실패. API 키 설정을 확인하세요.")
    
    print(f"⏰ 완료 시간: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

if __name__ == "__main__":
    main() 