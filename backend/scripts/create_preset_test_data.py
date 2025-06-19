#!/usr/bin/env python3
"""
프리셋 테스트 데이터 생성 스크립트
"""

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..', '..'))

from backend.database.database_manager import db_manager
from backend.services.macro_service import MacroService
from backend.services.preset_service import PresetService

def create_test_data():
    """테스트용 매크로와 프리셋 데이터를 생성하는 함수"""
    try:
        print("🔄 테스트 데이터 생성 시작...")
        
        # 서비스 초기화
        macro_service = MacroService()
        preset_service = PresetService()
        
        # 1. 기본 매크로 생성
        print("📝 테스트 매크로 생성 중...")
        
        macro_ids = []
        
        # 리그 오브 레전드 매크로들
        lol_macros = [
            {
                'name': 'Q스킬 연타',
                'voice_command': '큐 스킬',
                'action_type': 'rapid',
                'key_sequence': 'Q',
                'settings': '{"cps": 5, "duration": 2000}'
            },
            {
                'name': 'W스킬 홀드',
                'voice_command': '더블유 스킬',
                'action_type': 'hold',
                'key_sequence': 'W',
                'settings': '{"hold_duration": 1500}'
            },
            {
                'name': '궁극기 콤보',
                'voice_command': '궁극기',
                'action_type': 'combo',
                'key_sequence': 'R,Q,W,E',
                'settings': '{"delays": [100, 200, 150]}'
            },
        ]
        
        for macro_data in lol_macros:
            macro_id = macro_service.create_macro(**macro_data)
            macro_ids.append(macro_id)
            print(f"  ✅ 매크로 생성: {macro_data['name']} (ID: {macro_id})")
        
        # FPS 게임 매크로들
        fps_macros = [
            {
                'name': '빠른 사격',
                'voice_command': '빠른 사격',
                'action_type': 'rapid',
                'key_sequence': 'mouse_left',
                'settings': '{"cps": 10, "duration": 1000}'
            },
            {
                'name': '장전',
                'voice_command': '장전',
                'action_type': 'combo',
                'key_sequence': 'R',
                'settings': '{}'
            },
            {
                'name': '점프샷',
                'voice_command': '점프샷',
                'action_type': 'combo',
                'key_sequence': 'Space,mouse_left',
                'settings': '{"delays": [50]}'
            },
        ]
        
        for macro_data in fps_macros:
            macro_id = macro_service.create_macro(**macro_data)
            macro_ids.append(macro_id)
            print(f"  ✅ 매크로 생성: {macro_data['name']} (ID: {macro_id})")
        
        # RTS 게임 매크로들
        rts_macros = [
            {
                'name': '빌드 오더',
                'voice_command': '빌드 오더',
                'action_type': 'combo',
                'key_sequence': 'B,H,P,P,P',
                'settings': '{"delays": [100, 200, 100, 100]}'
            },
            {
                'name': '유닛 선택',
                'voice_command': '유닛 선택',
                'action_type': 'combo',
                'key_sequence': 'Ctrl+A',
                'settings': '{}'
            },
        ]
        
        for macro_data in rts_macros:
            macro_id = macro_service.create_macro(**macro_data)
            macro_ids.append(macro_id)
            print(f"  ✅ 매크로 생성: {macro_data['name']} (ID: {macro_id})")
        
        # 2. 프리셋 생성
        print("\n📁 테스트 프리셋 생성 중...")
        
        presets = [
            {
                'name': '리그 오브 레전드 기본',
                'description': 'LOL 게임용 기본 매크로 모음',
                'macro_ids': macro_ids[:3],  # 처음 3개 매크로
                'is_favorite': True
            },
            {
                'name': 'FPS 게임용',
                'description': 'FPS 게임에 최적화된 매크로 세트',
                'macro_ids': macro_ids[3:6],  # 다음 3개 매크로
                'is_favorite': False
            },
            {
                'name': 'RTS 전략게임',
                'description': '실시간 전략 게임용 매크로',
                'macro_ids': macro_ids[6:8],  # 마지막 2개 매크로
                'is_favorite': True
            },
            {
                'name': '종합 매크로 세트',
                'description': '모든 게임에 사용 가능한 종합 매크로',
                'macro_ids': macro_ids,  # 모든 매크로
                'is_favorite': False
            },
        ]
        
        for preset_data in presets:
            preset_id = preset_service.create_preset(**preset_data)
            print(f"  ✅ 프리셋 생성: {preset_data['name']} (ID: {preset_id})")
        
        print(f"\n🎉 테스트 데이터 생성 완료!")
        print(f"   📝 매크로: {len(macro_ids)}개")
        print(f"   📁 프리셋: {len(presets)}개")
        print(f"   ⭐ 즐겨찾기: {sum(1 for p in presets if p['is_favorite'])}개")
        
        return True
        
    except Exception as e:
        print(f"❌ 테스트 데이터 생성 실패: {e}")
        return False

if __name__ == "__main__":
    create_test_data() 