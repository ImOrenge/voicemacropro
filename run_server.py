#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
VoiceMacro Pro API 서버 런처

백엔드 API 서버를 실행하는 메인 진입점입니다.
"""

import sys
import os

# 백엔드 패키지를 Python 경로에 추가
project_root = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, project_root)

def main():
    """메인 실행 함수"""
    try:
        # 백엔드 API 서버 임포트 및 실행
        from backend.api.server import app, run_server
        
        print("🎮 VoiceMacro Pro API 서버를 시작합니다...")
        print("📁 프로젝트 루트:", project_root)
        print("🌐 서버 주소: http://localhost:5000")
        print("📚 API 문서: http://localhost:5000/api/health")
        print("-" * 50)
        
        # 서버 실행
        run_server()
        
    except ImportError as e:
        print(f"❌ 모듈 임포트 오류: {e}")
        print("백엔드 패키지 구조를 확인해주세요.")
        sys.exit(1)
    except Exception as e:
        print(f"❌ 서버 실행 오류: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main() 