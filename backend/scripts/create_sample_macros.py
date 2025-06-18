#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
VoiceMacro Pro - 샘플 매크로 생성 스크립트
테스트 및 데모용 매크로 데이터를 생성합니다.
"""

from backend.services.macro_service import macro_service
import json

def create_sample_macros():
    """
    다양한 게임 상황을 위한 샘플 매크로들을 생성합니다.
    """
    
    sample_macros = [
        # FPS 게임용 매크로
        {
            "name": "공격 콤보",
            "voice_command": "공격",
            "action_type": "combo",
            "key_sequence": "q,w,e",
            "settings": {"delay": 0.1}
        },
        {
            "name": "점프 공격",
            "voice_command": "점프 공격",
            "action_type": "combo", 
            "key_sequence": "space,left_click",
            "settings": {"delay": 0.05}
        },
        {
            "name": "연사 사격",
            "voice_command": "연사",
            "action_type": "rapid",
            "key_sequence": "left_click",
            "settings": {"rate": 10, "duration": 2.0}
        },
        {
            "name": "스킬 1번",
            "voice_command": "스킬 원",
            "action_type": "combo",
            "key_sequence": "1",
            "settings": {"delay": 0.0}
        },
        {
            "name": "스킬 2번",
            "voice_command": "스킬 투",
            "action_type": "combo",
            "key_sequence": "2", 
            "settings": {"delay": 0.0}
        },
        
        # MMO 게임용 매크로
        {
            "name": "힐링 포션",
            "voice_command": "힐링",
            "action_type": "combo",
            "key_sequence": "h",
            "settings": {"delay": 0.0}
        },
        {
            "name": "마나 포션",
            "voice_command": "마나",
            "action_type": "combo",
            "key_sequence": "m",
            "settings": {"delay": 0.0}
        },
        {
            "name": "궁극기",
            "voice_command": "궁극기",
            "action_type": "combo",
            "key_sequence": "r",
            "settings": {"delay": 0.0}
        },
        {
            "name": "텔레포트",
            "voice_command": "텔레포트",
            "action_type": "combo",
            "key_sequence": "t",
            "settings": {"delay": 0.0}
        },
        
        # 전략 게임용 매크로
        {
            "name": "모든 선택",
            "voice_command": "모두 선택",
            "action_type": "combo",
            "key_sequence": "ctrl+a",
            "settings": {"delay": 0.0}
        },
        {
            "name": "빠른 저장",
            "voice_command": "저장",
            "action_type": "combo", 
            "key_sequence": "ctrl+s",
            "settings": {"delay": 0.0}
        },
        {
            "name": "일시정지",
            "voice_command": "일시정지",
            "action_type": "combo",
            "key_sequence": "space",
            "settings": {"delay": 0.0}
        },
        
        # 일반 기능 매크로
        {
            "name": "스크린샷",
            "voice_command": "스크린샷",
            "action_type": "combo",
            "key_sequence": "f12",
            "settings": {"delay": 0.0}
        },
        {
            "name": "채팅창 열기",
            "voice_command": "채팅",
            "action_type": "combo",
            "key_sequence": "enter",
            "settings": {"delay": 0.0}
        },
        {
            "name": "인벤토리",
            "voice_command": "인벤토리",
            "action_type": "combo",
            "key_sequence": "i",
            "settings": {"delay": 0.0}
        },
        
        # 음성 인식 테스트용 매크로
        {
            "name": "테스트 명령",
            "voice_command": "테스트",
            "action_type": "combo",
            "key_sequence": "f1",
            "settings": {"delay": 0.0}
        },
        {
            "name": "안녕하세요",
            "voice_command": "안녕",
            "action_type": "combo",
            "key_sequence": "f2",
            "settings": {"delay": 0.0}
        },
        {
            "name": "감사합니다",
            "voice_command": "감사",
            "action_type": "combo", 
            "key_sequence": "f3",
            "settings": {"delay": 0.0}
        },
        
        # 홀드 타입 매크로
        {
            "name": "달리기",
            "voice_command": "달리기",
            "action_type": "hold",
            "key_sequence": "shift",
            "settings": {"duration": 3.0}
        },
        {
            "name": "웅크리기", 
            "voice_command": "웅크리기",
            "action_type": "hold",
            "key_sequence": "ctrl",
            "settings": {"duration": 2.0}
        },
        
        # 토글 타입 매크로
        {
            "name": "자동 달리기",
            "voice_command": "오토런",
            "action_type": "toggle",
            "key_sequence": "shift",
            "settings": {"toggle_key": "shift"}
        }
    ]
    
    print("🚀 샘플 매크로 생성을 시작합니다...")
    
    created_count = 0
    failed_count = 0
    
    for macro_data in sample_macros:
        try:
            macro_id = macro_service.create_macro(
                name=macro_data["name"],
                voice_command=macro_data["voice_command"], 
                action_type=macro_data["action_type"],
                key_sequence=macro_data["key_sequence"],
                settings=macro_data["settings"]
            )
            
            print(f"✅ 매크로 생성 성공: {macro_data['name']} (ID: {macro_id})")
            created_count += 1
            
        except Exception as e:
            print(f"❌ 매크로 생성 실패: {macro_data['name']} - {str(e)}")
            failed_count += 1
    
    print(f"\n📊 매크로 생성 완료!")
    print(f"   성공: {created_count}개")
    print(f"   실패: {failed_count}개")
    print(f"   총합: {len(sample_macros)}개")
    
    # 생성된 매크로 목록 출력
    print(f"\n📋 생성된 매크로 목록:")
    all_macros = macro_service.get_all_macros()
    
    for macro in all_macros:
        print(f"   • ID {macro['id']}: \"{macro['voice_command']}\" → {macro['name']}")
    
    print(f"\n🎤 음성 인식 테스트 추천 명령어:")
    print(f"   • \"공격\" - 공격 콤보")
    print(f"   • \"힐링\" - 힐링 포션") 
    print(f"   • \"테스트\" - 테스트 명령")
    print(f"   • \"안녕\" - 안녕하세요")
    print(f"   • \"저장\" - 빠른 저장")


if __name__ == "__main__":
    print("=" * 60)
    print("🎮 VoiceMacro Pro - 샘플 매크로 생성기")
    print("=" * 60)
    
    try:
        create_sample_macros()
        print(f"\n✨ 모든 작업이 완료되었습니다!")
        print(f"이제 VoiceMacro Pro 애플리케이션을 실행하여 음성 인식을 테스트해보세요.")
        
    except Exception as e:
        print(f"\n💥 오류 발생: {str(e)}")
        print(f"데이터베이스 연결이나 매크로 서비스에 문제가 있을 수 있습니다.")
    
    print("=" * 60) 