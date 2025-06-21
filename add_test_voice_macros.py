#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
음성인식 테스트용 매크로 생성 스크립트
"""

import sys
import os

# 백엔드 경로 추가
sys.path.append('backend')

from backend.services.macro_service import macro_service

def create_test_voice_macros():
    """음성인식 테스트용 매크로들을 생성"""
    
    test_macros = [
        {
            "name": "테스트 안녕",
            "voice_command": "안녕",
            "action_type": "combo",
            "key_sequence": "ctrl+shift+h",
            "description": "음성인식 테스트: 안녕 명령어",
            "delay": 50,
            "is_enabled": True
        },
        {
            "name": "테스트 공격",
            "voice_command": "공격",
            "action_type": "combo", 
            "key_sequence": "space",
            "description": "음성인식 테스트: 공격 명령어",
            "delay": 50,
            "is_enabled": True
        },
        {
            "name": "테스트 점프",
            "voice_command": "점프",
            "action_type": "combo",
            "key_sequence": "space",
            "description": "음성인식 테스트: 점프 명령어", 
            "delay": 50,
            "is_enabled": True
        },
        {
            "name": "테스트 스킬",
            "voice_command": "스킬",
            "action_type": "combo",
            "key_sequence": "q",
            "description": "음성인식 테스트: 스킬 명령어",
            "delay": 50,
            "is_enabled": True
        },
        {
            "name": "테스트 울트라",
            "voice_command": "울트라",
            "action_type": "combo",
            "key_sequence": "r",
            "description": "음성인식 테스트: 울트라 명령어",
            "delay": 50,
            "is_enabled": True
        }
    ]
    
    print("🎮 음성인식 테스트 매크로 생성 중...")
    
    created_count = 0
    for macro_data in test_macros:
        try:
            settings = {
                "description": macro_data["description"],
                "delay": macro_data["delay"],
                "is_enabled": macro_data["is_enabled"]
            }
            
            macro_id = macro_service.create_macro(
                name=macro_data["name"],
                voice_command=macro_data["voice_command"],
                action_type=macro_data["action_type"],
                key_sequence=macro_data["key_sequence"],
                settings=settings
            )
            
            result = {'success': True, 'macro_id': macro_id}
            
            if result.get('success'):
                print(f"✅ 매크로 생성: {macro_data['name']} (음성: '{macro_data['voice_command']}')")
                created_count += 1
            else:
                print(f"❌ 매크로 생성 실패: {macro_data['name']} - {result.get('error')}")
                
        except Exception as e:
            print(f"❌ 매크로 생성 오류: {macro_data['name']} - {e}")
    
    print(f"\n🎯 총 {created_count}개의 테스트 매크로가 생성되었습니다!")
    print("\n📝 사용법:")
    print("1. VoiceMacro Pro 애플리케이션 실행")
    print("2. 음성인식 탭으로 이동")
    print("3. '녹음 시작' 버튼 클릭")
    print("4. 다음 음성 명령어 테스트:")
    for macro in test_macros:
        print(f"   - '{macro['voice_command']}' -> {macro['key_sequence']}")

if __name__ == '__main__':
    create_test_voice_macros() 