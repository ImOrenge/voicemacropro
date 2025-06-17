#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
데이터베이스 매크로 확인 스크립트
"""

from database import DatabaseManager

def main():
    """데이터베이스에 생성된 매크로들을 확인"""
    try:
        # 데이터베이스 연결
        db = DatabaseManager()
        
        # 모든 매크로 조회
        macros = db.get_all_macros()
        
        print(f"🎮 총 {len(macros)}개의 매크로가 생성되었습니다!\n")
        
        if macros:
            print("📋 매크로 목록:")
            print("-" * 80)
            for i, macro in enumerate(macros[:10], 1):  # 처음 10개만 표시
                print(f"{i:2d}. 이름: {macro['name']}")
                print(f"    음성명령: '{macro['voice_command']}'")
                print(f"    타입: {macro['action_type']}")
                print(f"    키시퀀스: {macro['key_sequence']}")
                print()
            
            if len(macros) > 10:
                print(f"... 외 {len(macros) - 10}개 더 있습니다.")
        else:
            print("❌ 매크로가 없습니다. 먼저 매크로를 생성해주세요.")
            
    except Exception as e:
        print(f"❌ 오류 발생: {e}")

if __name__ == "__main__":
    main() 