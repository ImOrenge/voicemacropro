# VoiceMacro Pro - 게임 AI 음성 매크로 프로그램

## 프로젝트 개요

VoiceMacro Pro는 PRD(Product Requirements Document)에 따라 개발된 게임용 AI 음성 매크로 프로그램입니다. 
음성 명령을 통해 복잡한 키보드/마우스 조작을 자동화할 수 있는 매크로 관리 시스템을 제공합니다.

### 현재 구현된 기능: 기능 1 - 매크로 관리 시스템

- ✅ 매크로 목록 시트 (DataGrid)
- ✅ 매크로 CRUD 기능 (추가, 수정, 복사, 삭제)
- ✅ 검색 및 필터링 기능
- ✅ 정렬 기능 (이름순, 생성일순, 사용빈도순)
- ✅ 직관적인 UI/UX 디자인
- ✅ Python 백엔드 API 서버
- ✅ SQLite 데이터베이스

## 기술 스택

### 백엔드 (Python)
- **Flask**: REST API 서버 프레임워크
- **SQLite**: 로컬 데이터베이스
- **Flask-CORS**: 프론트엔드와의 통신을 위한 CORS 설정

### 프론트엔드 (C# WPF)
- **C# WPF**: 메인 애플리케이션 프레임워크
- **XAML**: UI 레이아웃 정의
- **Newtonsoft.Json**: JSON 데이터 처리

## 프로젝트 구조

```
VoiceMacro Pro/
├── Python 백엔드/
│   ├── database.py          # 데이터베이스 관리
│   ├── macro_service.py     # 매크로 비즈니스 로직
│   ├── api_server.py        # Flask API 서버
│   └── requirements.txt     # Python 의존성
│
└── C# WPF 프론트엔드/
    ├── VoiceMacroPro/
    │   ├── Models/
    │   │   └── Macro.cs     # 매크로 데이터 모델
    │   ├── Services/
    │   │   └── ApiService.cs # API 통신 서비스
    │   ├── Views/
    │   │   ├── MacroEditWindow.xaml     # 매크로 편집 윈도우
    │   │   └── MacroEditWindow.xaml.cs
    │   ├── MainWindow.xaml              # 메인 윈도우
    │   ├── MainWindow.xaml.cs
    │   ├── App.xaml                     # 애플리케이션 설정
    │   ├── App.xaml.cs
    │   └── VoiceMacroPro.csproj        # 프로젝트 파일
    └── README.md
```

## 실행 방법

### 1. Python 백엔드 서버 실행

먼저 Python 의존성을 설치합니다:

```bash
pip install -r requirements.txt
```

API 서버를 시작합니다:

```bash
python api_server.py
```

서버가 성공적으로 시작되면 다음과 같은 메시지가 출력됩니다:
```
VoiceMacro API 서버를 시작합니다...
서버 주소: http://localhost:5000
API 문서: http://localhost:5000/api/health
```

### 2. C# WPF 애플리케이션 실행

Visual Studio 또는 Visual Studio Code를 사용하여 `VoiceMacroPro.csproj` 파일을 열고 실행합니다.

또는 명령줄에서 실행:

```bash
cd VoiceMacroPro
dotnet run
```

## 사용 방법

### 1. 애플리케이션 시작
- 애플리케이션을 실행하면 자동으로 백엔드 서버 연결을 확인합니다
- 하단 상태바에서 서버 연결 상태를 확인할 수 있습니다
  - 🟢 초록색: 서버 연결됨
  - 🔴 빨간색: 서버 연결 실패

### 2. 매크로 관리 기능

#### 새 매크로 추가
1. "➕ 새 매크로 추가" 버튼 클릭
2. 매크로 정보 입력:
   - **매크로 이름**: 식별할 수 있는 고유한 이름
   - **음성 명령어**: 음성으로 인식될 명령어
   - **동작 타입**: 콤보, 연사, 홀드, 토글, 반복 중 선택
   - **키 시퀀스**: 실행할 키보드 입력 (예: Ctrl+C, Space 등)
   - **추가 설정**: 지연 시간, 반복 횟수
3. "🧪 테스트" 버튼으로 설정 확인
4. "💾 저장" 버튼으로 매크로 저장

#### 매크로 수정
1. 목록에서 수정할 매크로 선택
2. "✏️ 수정" 버튼 클릭
3. 정보 수정 후 저장

#### 매크로 복사
1. 목록에서 복사할 매크로 선택
2. "📋 복사" 버튼 클릭
3. 자동으로 "_복사" 접미사가 붙은 새 매크로 생성

#### 매크로 삭제
1. 목록에서 삭제할 매크로 선택
2. "🗑️ 삭제" 버튼 클릭
3. 확인 대화상자에서 "예" 선택

### 3. 검색 및 정렬

#### 검색
- 검색창에 키워드 입력 후 "검색" 버튼 클릭
- 매크로 이름과 음성 명령어에서 검색

#### 정렬
- 정렬 드롭다운에서 기준 선택:
  - **이름순**: 매크로 이름 알파벳 순서
  - **생성일순**: 최근 생성된 순서
  - **사용빈도순**: 많이 사용된 순서

## API 엔드포인트

백엔드 서버에서 제공하는 REST API:

| 메서드 | 엔드포인트 | 설명 |
|--------|------------|------|
| GET | `/api/health` | 서버 상태 확인 |
| GET | `/api/macros` | 매크로 목록 조회 |
| GET | `/api/macros/{id}` | 특정 매크로 조회 |
| POST | `/api/macros` | 새 매크로 생성 |
| PUT | `/api/macros/{id}` | 매크로 수정 |
| POST | `/api/macros/{id}/copy` | 매크로 복사 |
| DELETE | `/api/macros/{id}` | 매크로 삭제 |

## 데이터베이스 스키마

SQLite 데이터베이스에 저장되는 테이블:

### macros 테이블
```sql
CREATE TABLE macros (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    voice_command TEXT NOT NULL,
    action_type TEXT NOT NULL,
    key_sequence TEXT NOT NULL,
    settings TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    usage_count INTEGER DEFAULT 0
);
```

## 향후 구현 예정 기능

PRD에 따라 다음 기능들이 순차적으로 구현될 예정입니다:

- 🎤 **기능 2**: 음성 인식 및 매크로 매칭 시스템
- ⚙️ **기능 3**: 매크로 동작 메커니즘 설정
- 📁 **기능 4**: 프리셋 관리 시스템
- 📊 **기능 5**: 로그 및 모니터링 시스템

## 문제 해결

### 서버 연결 실패
- Python 백엔드 서버가 실행 중인지 확인
- 포트 5000이 사용 가능한지 확인
- 방화벽 설정 확인

### 매크로 저장 실패
- 모든 필수 필드가 입력되었는지 확인
- 매크로 이름의 중복 여부 확인
- 서버 연결 상태 확인

### UI 응답 없음
- 서버 응답 시간 확인 (타임아웃: 30초)
- 대용량 데이터 처리 시 잠시 대기

## 개발자 정보

이 프로젝트는 초보 개발자도 이해할 수 있도록 상세한 주석과 함께 작성되었습니다.
각 함수와 클래스에는 기능 설명이 포함되어 있으며, 오류 처리와 사용자 경험을 고려한 설계가 적용되었습니다.

## 라이선스

이 프로젝트는 교육 목적으로 개발되었습니다. 