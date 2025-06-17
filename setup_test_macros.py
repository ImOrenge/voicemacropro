"""
음성 인식 UI 테스트를 위한 기본 매크로 설정 스크립트

이 스크립트는 음성 인식 UI가 제대로 작동하는지 테스트하기 위해
다양한 게임 매크로들을 데이터베이스에 추가합니다.
"""

import sqlite3
import json
from datetime import datetime
from common_utils import get_logger

# 로깅 설정
logger = get_logger(__name__)

def setup_test_macros():
    """
    테스트용 매크로들을 데이터베이스에 추가하는 함수
    """
    # 테스트 매크로 데이터
    test_macros = [
        {
            'name': '공격_매크로',
            'voice_command': '공격해',
            'action_type': 'combo',
            'key_sequence': 'space',
            'settings': json.dumps({'delay': 0.1})
        },
        {
            'name': '치료_매크로',
            'voice_command': '치료하기',
            'action_type': 'single',
            'key_sequence': 'h',
            'settings': json.dumps({'priority': 'high'})
        },
        {
            'name': '스킬_사용_매크로',
            'voice_command': '스킬 사용',
            'action_type': 'combo',
            'key_sequence': 'q,w,e',
            'settings': json.dumps({'delay': 0.2, 'repeat': False})
        },
        {
            'name': '아이템_사용_매크로',
            'voice_command': '아이템 사용',
            'action_type': 'single',
            'key_sequence': '1',
            'settings': json.dumps({'consumable': True})
        },
        {
            'name': '빠른_이동_매크로',
            'voice_command': '빠른 이동',
            'action_type': 'hold',
            'key_sequence': 'shift+w',
            'settings': json.dumps({'duration': 2.0})
        },
        {
            'name': '맵_열기_매크로',
            'voice_command': '맵 열기',
            'action_type': 'single',
            'key_sequence': 'm',
            'settings': json.dumps({'window': 'map'})
        },
        {
            'name': '인벤토리_매크로',
            'voice_command': '인벤토리 열기',
            'action_type': 'single',
            'key_sequence': 'i',
            'settings': json.dumps({'window': 'inventory'})
        },
        {
            'name': '메뉴_열기_매크로',
            'voice_command': '메뉴 열기',
            'action_type': 'single',
            'key_sequence': 'escape',
            'settings': json.dumps({'window': 'menu'})
        },
        {
            'name': '저장_매크로',
            'voice_command': '저장하기',
            'action_type': 'combo',
            'key_sequence': 'ctrl+s',
            'settings': json.dumps({'auto_save': True})
        },
        {
            'name': '종료_매크로',
            'voice_command': '종료하기',
            'action_type': 'combo',
            'key_sequence': 'alt+f4',
            'settings': json.dumps({'confirm': True})
        },
        {
            'name': '설정_열기_매크로',
            'voice_command': '설정 열기',
            'action_type': 'single',
            'key_sequence': 'f10',
            'settings': json.dumps({'window': 'settings'})
        },
        {
            'name': '도움말_매크로',
            'voice_command': '도움말 보기',
            'action_type': 'single',
            'key_sequence': 'f1',
            'settings': json.dumps({'window': 'help'})
        },
        {
            'name': '점프_매크로',
            'voice_command': '점프',
            'action_type': 'single',
            'key_sequence': 'space',
            'settings': json.dumps({'physics': True})
        },
        {
            'name': '달리기_매크로',
            'voice_command': '달려',
            'action_type': 'hold',
            'key_sequence': 'shift',
            'settings': json.dumps({'stamina': True})
        },
        {
            'name': '리로드_매크로',
            'voice_command': '재장전',
            'action_type': 'single',
            'key_sequence': 'r',
            'settings': json.dumps({'weapon': 'gun'})
        },
        {
            'name': '웅크리기_매크로',
            'voice_command': '웅크려',
            'action_type': 'toggle',
            'key_sequence': 'ctrl',
            'settings': json.dumps({'stealth': True})
        },
        {
            'name': '채팅_매크로',
            'voice_command': '채팅 열기',
            'action_type': 'single',
            'key_sequence': 'enter',
            'settings': json.dumps({'communication': True})
        },
        {
            'name': '스크린샷_매크로',
            'voice_command': '스크린샷',
            'action_type': 'single',
            'key_sequence': 'f12',
            'settings': json.dumps({'capture': True})
        }
    ]
    
    try:
        # 데이터베이스 연결
        conn = sqlite3.connect('voice_macro.db')
        cursor = conn.cursor()
        
        # 기존 테스트 매크로들 삭제 (선택사항)
        cursor.execute("DELETE FROM macros WHERE name LIKE '%_매크로'")
        logger.info("기존 테스트 매크로들을 삭제했습니다.")
        
        # 새로운 테스트 매크로들 추가
        for macro in test_macros:
            cursor.execute("""
                INSERT INTO macros (name, voice_command, action_type, key_sequence, settings, is_active)
                VALUES (?, ?, ?, ?, ?, 1)
            """, (
                macro['name'],
                macro['voice_command'],
                macro['action_type'],
                macro['key_sequence'],
                macro['settings']
            ))
        
        # 커밋 및 연결 종료
        conn.commit()
        conn.close()
        
        logger.info(f"{len(test_macros)}개의 테스트 매크로가 성공적으로 추가되었습니다.")
        
        # 추가된 매크로 목록 출력
        print("\n=== 추가된 테스트 매크로 목록 ===")
        for i, macro in enumerate(test_macros, 1):
            print(f"{i:2d}. {macro['name']} - '{macro['voice_command']}' -> {macro['key_sequence']}")
        
        print(f"\n총 {len(test_macros)}개의 매크로가 데이터베이스에 추가되었습니다.")
        print("음성 인식 UI에서 '공격해', '치료하기', '스킬 사용' 등의 명령어를 테스트해보세요!")
        
        return True
        
    except Exception as e:
        logger.error(f"테스트 매크로 추가 실패: {e}")
        print(f"오류 발생: {e}")
        return False

def main():
    """
    메인 실행 함수
    """
    print("VoiceMacro Pro - 테스트 매크로 설정")
    print("=" * 50)
    
    success = setup_test_macros()
    
    if success:
        print("\n✅ 테스트 매크로 설정이 완료되었습니다!")
        print("이제 C# 애플리케이션을 실행하고 음성 인식 탭에서 테스트해보세요.")
    else:
        print("\n❌ 테스트 매크로 설정에 실패했습니다.")

if __name__ == '__main__':
    main() 