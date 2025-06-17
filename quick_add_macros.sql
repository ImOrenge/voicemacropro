-- 음성 인식 UI 테스트용 매크로 추가 SQL

-- 기존 테스트 매크로들 삭제
DELETE FROM macros WHERE name LIKE '%_매크로';

-- 새로운 테스트 매크로들 추가
INSERT INTO macros (name, voice_command, action_type, key_sequence, settings, is_active) VALUES
('공격_매크로', '공격해', 'combo', 'space', '{"delay": 0.1}', 1),
('치료_매크로', '치료하기', 'single', 'h', '{"priority": "high"}', 1),
('스킬_사용_매크로', '스킬 사용', 'combo', 'q,w,e', '{"delay": 0.2, "repeat": false}', 1),
('아이템_사용_매크로', '아이템 사용', 'single', '1', '{"consumable": true}', 1),
('빠른_이동_매크로', '빠른 이동', 'hold', 'shift+w', '{"duration": 2.0}', 1),
('맵_열기_매크로', '맵 열기', 'single', 'm', '{"window": "map"}', 1),
('인벤토리_매크로', '인벤토리 열기', 'single', 'i', '{"window": "inventory"}', 1),
('메뉴_열기_매크로', '메뉴 열기', 'single', 'escape', '{"window": "menu"}', 1),
('저장_매크로', '저장하기', 'combo', 'ctrl+s', '{"auto_save": true}', 1),
('종료_매크로', '종료하기', 'combo', 'alt+f4', '{"confirm": true}', 1),
('설정_열기_매크로', '설정 열기', 'single', 'f10', '{"window": "settings"}', 1),
('도움말_매크로', '도움말 보기', 'single', 'f1', '{"window": "help"}', 1),
('점프_매크로', '점프', 'single', 'space', '{"physics": true}', 1),
('달리기_매크로', '달려', 'hold', 'shift', '{"stamina": true}', 1),
('리로드_매크로', '재장전', 'single', 'r', '{"weapon": "gun"}', 1),
('웅크리기_매크로', '웅크려', 'toggle', 'ctrl', '{"stealth": true}', 1),
('채팅_매크로', '채팅 열기', 'single', 'enter', '{"communication": true}', 1),
('스크린샷_매크로', '스크린샷', 'single', 'f12', '{"capture": true}', 1);

-- 추가된 매크로 확인
SELECT COUNT(*) as '추가된_매크로_수' FROM macros WHERE name LIKE '%_매크로';
SELECT * FROM macros WHERE name LIKE '%_매크로' ORDER BY id; 