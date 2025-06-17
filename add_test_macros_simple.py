import sqlite3
import json

# 테스트 매크로 데이터
test_macros = [
    ('공격_매크로', '공격해', 'combo', 'space', '{"delay": 0.1}'),
    ('치료_매크로', '치료하기', 'single', 'h', '{"priority": "high"}'),
    ('스킬_사용_매크로', '스킬 사용', 'combo', 'q,w,e', '{"delay": 0.2, "repeat": false}'),
    ('아이템_사용_매크로', '아이템 사용', 'single', '1', '{"consumable": true}'),
    ('빠른_이동_매크로', '빠른 이동', 'hold', 'shift+w', '{"duration": 2.0}'),
    ('맵_열기_매크로', '맵 열기', 'single', 'm', '{"window": "map"}'),
    ('인벤토리_매크로', '인벤토리 열기', 'single', 'i', '{"window": "inventory"}'),
    ('메뉴_열기_매크로', '메뉴 열기', 'single', 'escape', '{"window": "menu"}'),
    ('저장_매크로', '저장하기', 'combo', 'ctrl+s', '{"auto_save": true}'),
    ('종료_매크로', '종료하기', 'combo', 'alt+f4', '{"confirm": true}'),
    ('설정_열기_매크로', '설정 열기', 'single', 'f10', '{"window": "settings"}'),
    ('도움말_매크로', '도움말 보기', 'single', 'f1', '{"window": "help"}'),
    ('점프_매크로', '점프', 'single', 'space', '{"physics": true}'),
    ('달리기_매크로', '달려', 'hold', 'shift', '{"stamina": true}'),
    ('리로드_매크로', '재장전', 'single', 'r', '{"weapon": "gun"}'),
    ('웅크리기_매크로', '웅크려', 'toggle', 'ctrl', '{"stealth": true}'),
    ('채팅_매크로', '채팅 열기', 'single', 'enter', '{"communication": true}'),
    ('스크린샷_매크로', '스크린샷', 'single', 'f12', '{"capture": true}')
]

try:
    # 데이터베이스 연결
    conn = sqlite3.connect('voice_macro.db')
    cursor = conn.cursor()
    
    # 기존 테스트 매크로들 삭제
    cursor.execute("DELETE FROM macros WHERE name LIKE '%_매크로'")
    print("기존 테스트 매크로들을 삭제했습니다.")
    
    # 새로운 테스트 매크로들 추가
    for macro in test_macros:
        cursor.execute("""
            INSERT INTO macros (name, voice_command, action_type, key_sequence, settings, is_active)
            VALUES (?, ?, ?, ?, ?, 1)
        """, macro)
    
    # 커밋
    conn.commit()
    
    # 결과 확인
    cursor.execute("SELECT COUNT(*) FROM macros WHERE name LIKE '%_매크로'")
    count = cursor.fetchone()[0]
    
    print(f"\n✅ {count}개의 테스트 매크로가 성공적으로 추가되었습니다!")
    
    # 추가된 매크로 목록 확인
    cursor.execute("SELECT name, voice_command, key_sequence FROM macros WHERE name LIKE '%_매크로' ORDER BY id")
    macros = cursor.fetchall()
    
    print("\n=== 추가된 테스트 매크로 목록 ===")
    for i, (name, voice_cmd, key_seq) in enumerate(macros, 1):
        print(f"{i:2d}. {name} - '{voice_cmd}' -> {key_seq}")
    
    conn.close()
    
    print(f"\n음성 인식 UI에서 다음 명령어들을 테스트해보세요:")
    print("'공격해', '치료하기', '스킬 사용', '맵 열기' 등")
    
except Exception as e:
    print(f"❌ 오류 발생: {e}") 