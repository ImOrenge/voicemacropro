#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
VoiceMacro Pro 백엔드 패키지

이 패키지는 VoiceMacro Pro의 모든 백엔드 기능을 포함합니다:
- API 서버 및 엔드포인트
- 비즈니스 로직 서비스들  
- 데이터베이스 관리
- MSL 파서 및 인터프리터
- 유틸리티 함수들
"""

__version__ = "1.0.0"
__author__ = "VoiceMacro Pro Development Team"
__email__ = "voicemacro.pro@example.com"

# 메인 모듈 임포트
from .api import *
from .services import *
from .database import *
from .parsers import *
from .utils import * 