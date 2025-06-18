# VoiceMacro Pro 백엔드 구조

## 📁 디렉토리 구조

```
backend/
├── 📂 api/                 # API 서버 및 엔드포인트
│   ├── server.py           # Flask 기반 REST API 서버
│   └── __init__.py         # API 패키지 초기화
│
├── 📂 services/            # 비즈니스 로직 서비스
│   ├── macro_service.py                    # 매크로 관리 서비스
│   ├── custom_script_service.py            # 커스텀 스크립트 서비스
│   ├── preset_service.py                   # 프리셋 관리 서비스
│   ├── voice_service.py                    # 음성 인식 서비스
│   ├── voice_recognition_service_basic.py  # 기본 음성 인식
│   ├── whisper_service.py                  # Whisper AI 서비스
│   ├── macro_execution_service.py          # 매크로 실행 서비스
│   ├── macro_matching_service.py           # 매크로 매칭 서비스
│   ├── voice_analysis_service.py           # 음성 분석 서비스
│   └── __init__.py                         # 서비스 패키지 초기화
│
├── 📂 database/            # 데이터베이스 관리
│   ├── database_manager.py # SQLite 데이터베이스 관리자
│   └── __init__.py         # 데이터베이스 패키지 초기화
│
├── 📂 parsers/             # MSL 파서 및 인터프리터
│   ├── msl_lexer.py        # MSL 어휘 분석기
│   ├── msl_parser.py       # MSL 구문 분석기
│   ├── msl_interpreter.py  # MSL 인터프리터
│   ├── msl_ast.py          # MSL 추상 구문 트리
│   └── __init__.py         # 파서 패키지 초기화
│
├── 📂 utils/               # 공통 유틸리티
│   ├── common_utils.py     # 공통 유틸리티 함수
│   ├── config.py          # 설정 관리
│   └── __init__.py        # 유틸리티 패키지 초기화
│
├── 📂 tests/               # 테스트 파일들
│   ├── test_*.py          # 각종 테스트 파일들
│   └── __init__.py        # 테스트 패키지 초기화
│
├── 📂 scripts/             # 도구 스크립트들
│   ├── add_*.py           # 데이터 추가 스크립트
│   ├── setup_*.py         # 설정 스크립트
│   ├── check_*.py         # 체크 스크립트
│   └── __init__.py        # 스크립트 패키지 초기화
│
└── __init__.py            # 백엔드 패키지 초기화
```

## 🚀 서버 실행

### 메인 서버 실행
```bash
# 프로젝트 루트에서 실행
py run_server.py
```

### 개별 모듈 실행 (개발/디버깅용)
```bash
# API 서버 직접 실행
py backend/api/server.py

# 특정 테스트 실행
py backend/tests/test_api_validation.py

# 데이터베이스 초기화
py backend/scripts/setup_test_macros.py
```

## 📚 주요 모듈 설명

### 🔌 API 서버 (`backend/api/`)
- **server.py**: Flask 기반 REST API 서버
- 모든 클라이언트 요청의 진입점
- CORS 설정으로 C# WPF 클라이언트와 통신
- 엔드포인트: `/api/macros`, `/api/scripts`, `/api/presets` 등

### ⚙️ 서비스 레이어 (`backend/services/`)
- **macro_service.py**: 매크로 CRUD 비즈니스 로직
- **custom_script_service.py**: MSL 스크립트 관리 및 검증
- **preset_service.py**: 매크로 프리셋 관리
- **voice_service.py**: 음성 인식 통합 서비스
- **whisper_service.py**: OpenAI Whisper AI 연동
- **macro_execution_service.py**: 매크로 실행 엔진
- **macro_matching_service.py**: 음성-매크로 매칭 알고리즘
- **voice_analysis_service.py**: 음성 데이터 분석

### 🗄️ 데이터베이스 (`backend/database/`)
- **database_manager.py**: SQLite 데이터베이스 관리
- 연결 풀링, 트랜잭션 관리, 스키마 마이그레이션

### 🔍 MSL 파서 (`backend/parsers/`)
- **msl_lexer.py**: 토큰화 (문자열 → 토큰)
- **msl_parser.py**: 구문 분석 (토큰 → AST)
- **msl_interpreter.py**: 실행 (AST → 동작)
- **msl_ast.py**: 추상 구문 트리 노드 정의

### 🛠️ 유틸리티 (`backend/utils/`)
- **common_utils.py**: 로깅, 파일 처리, 시간 함수 등
- **config.py**: 환경 설정, API 키, 경로 설정

## 🔄 데이터 흐름

```
C# WPF Client ←→ Flask API Server ←→ Service Layer ←→ Database
                      ↓
              MSL Parser/Interpreter
                      ↓
            PyAutoGUI (매크로 실행)
```

## 📋 개발 규칙

### Import 규칙
```python
# ✅ 올바른 방법
from backend.services.macro_service import macro_service
from backend.database.database_manager import DatabaseManager
from backend.parsers.msl_parser import MSLParser

# ❌ 잘못된 방법
from macro_service import macro_service  # 상대 경로
import services.macro_service            # 불완전한 경로
```

### 패키지 구조 변경 금지
- 기존 디렉토리 구조를 함부로 변경하지 마세요
- 새 파일 추가 시 해당 기능에 맞는 디렉토리 사용
- import 경로 변경 시 모든 의존성 확인

### 테스트 실행
```bash
# 전체 테스트 실행
py -m pytest backend/tests/

# 특정 테스트 실행
py backend/tests/test_msl_complete.py
```

## 🔧 개발 도구

### Import 경로 수정 도구
```bash
# import 경로 자동 수정
py fix_imports.py
py fix_imports_v2.py
```

### 데이터베이스 관리 도구
```bash
# 데이터베이스 상태 확인
py backend/scripts/check_db.py

# 테스트 데이터 추가
py backend/scripts/setup_test_macros.py
```

## 📝 로그 및 모니터링

- 모든 서비스는 표준화된 로깅 사용
- 로그 레벨: DEBUG, INFO, WARNING, ERROR
- 로그 파일: `logs/` 디렉토리에 자동 저장
- API 요청/응답 로깅 자동화

## 🔒 보안 고려사항

- API 키는 환경 변수로 관리
- 데이터베이스 접근 권한 제한
- 사용자 입력 검증 및 살리타이징
- MSL 스크립트 샌드박스 실행

---

**버전**: 1.0.0  
**최종 업데이트**: 2025-01-18  
**작성자**: VoiceMacro Pro Development Team 