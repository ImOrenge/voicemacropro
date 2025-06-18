# 게임 AI 음성 매크로 프로그램 PRD (Product Requirements Document)

## 1. 프로젝트 개요

### 1.1 프로젝트 명
**VoiceMacro Pro - 게임 AI 음성 매크로 프로그램**

### 1.2 프로젝트 목적
- 게이머들이 음성 명령을 통해 복잡한 키보드/마우스 조작을 자동화할 수 있는 매크로 프로그램
- 음성 인식 AI를 활용하여 직관적이고 편리한 매크로 제어 시스템 구축

### 1.3 대상 사용자
- PC 게임 플레이어 (특히 MMO, RTS, FPS 게임)
- 반복적인 작업이 많은 게임 사용자
- 신체적 제약으로 키보드 조작이 어려운 사용자

## 2. 기술 스택

### 2.1 백엔드 (Python)
- **OpenAI Whisper**: 음성 인식 및 텍스트 변환
- **PyAutoGUI**: 키보드/마우스 자동화
- **Flask/FastAPI**: 백엔드 API 서버
- **SQLite**: 로컬 데이터베이스 (매크로 저장)

### 2.2 프론트엔드 (C# WPF)
- **C# WPF**: 메인 애플리케이션 프레임워크
- **XAML**: UI 레이아웃 정의
- **CSS**: 스타일링 (WPF에서 사용 가능한 범위)

## 3. 핵심 기능 명세

### 3.1 기능 1: 매크로 관리 시스템

#### 3.1.1 기능 설명
매크로의 음성 명령어와 실행 동작을 체계적으로 관리하는 시스템

#### 3.1.2 세부 기능
- **매크로 목록 시트**: 
  - 음성 명령어, 동작 설명, 생성일, 최근 사용일 표시
  - 검색 및 필터링 기능
  - 정렬 기능 (이름순, 생성일순, 사용빈도순)

- **매크로 CRUD 기능**:
  - **추가**: 새로운 매크로 생성 및 등록
  - **수정**: 기존 매크로의 명령어 또는 동작 변경
  - **복사**: 기존 매크로를 복제하여 새로운 매크로 생성
  - **삭제**: 불필요한 매크로 제거

#### 3.1.3 사용자 인터페이스
```
[매크로 관리] 탭
├── 매크로 목록 DataGrid
│   ├── 음성 명령어 | 동작 설명 | 생성일 | 사용 횟수
│   └── 정렬/필터 옵션
├── 버튼 그룹
│   ├── [새 매크로 추가] 버튼
│   ├── [수정] 버튼
│   ├── [복사] 버튼
│   └── [삭제] 버튼
└── 검색 박스
```

### 3.2 기능 2: 음성 인식 및 매크로 매칭 시스템

#### 3.2.1 기능 설명
사용자의 음성을 실시간으로 녹음하고 분석하여 등록된 매크로 명령어와 매칭

#### 3.2.2 세부 기능
- **실시간 음성 녹음**: 
  - 마이크 권한 관리
  - 음성 입력 레벨 표시
  - 백그라운드 녹음 기능

- **음성 분석 및 텍스트 변환**:
  - OpenAI Whisper를 통한 고정밀 음성 인식
  - 노이즈 필터링
  - 다국어 지원 (한국어, 영어)

- **명령어 매칭**:
  - 유사도 기반 매크로 검색
  - 부분 일치 및 동의어 처리
  - 확신도 표시

#### 3.2.3 사용자 인터페이스
```
[음성 인식] 탭
├── 음성 입력 상태 표시
│   ├── 마이크 레벨 게이지
│   ├── 녹음 상태 LED
│   └── 인식된 텍스트 표시창
├── 설정 그룹
│   ├── 마이크 선택 드롭다운
│   ├── 감도 조절 슬라이더
│   └── 언어 선택 옵션
└── 매칭 결과 표시
    ├── 매칭된 매크로 목록
    └── 확신도 표시
```

### 3.3 기능 3: 매크로 동작 메커니즘 설정

#### 3.3.1 기능 설명
다양한 게임 상황에 맞는 매크로 동작 패턴 설정

#### 3.3.2 세부 기능
- **콤보**: 여러 키를 순차적으로 입력
  - 키 간 딜레이 설정
  - 콤보 순서 편집기

- **연사**: 특정 키를 빠르게 반복 입력
  - 연사 속도 설정 (CPS - Clicks Per Second)
  - 연사 지속 시간 설정

- **홀드**: 키를 눌러서 유지하는 동작
  - 홀드 지속 시간 설정
  - 자동 해제 조건 설정

- **토글**: 키를 한 번 누르면 ON, 다시 누르면 OFF
  - 토글 상태 표시
  - 토글 해제 명령어 설정

- **반복**: 특정 동작을 지정된 횟수만큼 반복
  - 반복 횟수 설정
  - 반복 간격 설정

#### 3.3.3 사용자 인터페이스
```
[매크로 설정] 탭
├── 매크로 타입 선택 RadioButton 그룹
│   ├── ○ 콤보 ○ 연사 ○ 홀드 ○ 토글 ○ 반복
├── 타입별 설정 패널
│   ├── [콤보 설정] - 키 순서, 딜레이
│   ├── [연사 설정] - 속도, 지속시간
│   ├── [홀드 설정] - 지속시간, 해제조건
│   ├── [토글 설정] - 상태표시, 해제명령
│   └── [반복 설정] - 횟수, 간격
├── 키 입력 설정
│   ├── 키 조합 입력기
│   └── 마우스 좌표 설정
└── 미리보기/테스트 버튼
```

### 3.4 기능 4: 프리셋 관리 시스템

#### 3.4.1 기능 설명
매크로 집합을 프리셋으로 관리하여 게임별 또는 상황별로 빠르게 전환

#### 3.4.2 세부 기능
- **프리셋 파일 관리**:
  - JSON 형태로 프리셋 저장
  - 프리셋 내보내기 (.json, .xml)
  - 프리셋 가져오기 (파일 선택)

- **프리셋 CRUD**:
  - 프리셋 생성/수정/삭제/복사
  - 프리셋 이름 및 설명 관리
  - 프리셋 미리보기

- **프리셋 적용**:
  - 원클릭 프리셋 적용
  - 프리셋 전환 히스토리
  - 즐겨찾기 프리셋

#### 3.4.3 사용자 인터페이스
```
[프리셋 관리] 탭
├── 프리셋 목록 ListBox
│   ├── 프리셋 이름 | 매크로 개수 | 생성일
│   └── 즐겨찾기 표시
├── 프리셋 조작 버튼 그룹
│   ├── [새 프리셋] [수정] [복사] [삭제]
│   ├── [내보내기] [가져오기]
│   └── [적용] [즐겨찾기 추가/제거]
└── 프리셋 미리보기 패널
    └── 포함된 매크로 목록 표시
```

### 3.5 기능 5: 로그 및 모니터링 시스템

#### 3.5.1 기능 설명
매크로 실행 상태를 실시간으로 모니터링하고 로그를 관리

#### 3.5.2 세부 기능
- **실시간 로그 출력**:
  - 매크로 실행 시작/완료 로그
  - 음성 인식 결과 로그
  - 오류 및 경고 메시지

- **로그 관리**:
  - 로그 레벨 설정 (Debug, Info, Warning, Error)
  - 로그 필터링 및 검색
  - 로그 파일 저장 및 내보내기

- **성능 모니터링**:
  - 매크로 실행 성공률 통계
  - 음성 인식 정확도 통계
  - 시스템 리소스 사용량 표시

#### 3.5.3 사용자 인터페이스
```
[로그 및 모니터링] 탭
├── 실시간 로그 출력창
│   ├── 시간 | 레벨 | 메시지
│   └── 자동 스크롤 옵션
├── 로그 제어 패널
│   ├── 로그 레벨 선택
│   ├── 필터 검색박스
│   └── [로그 저장] [로그 지우기] 버튼
└── 통계 패널
    ├── 매크로 실행 성공률 차트
    ├── 음성 인식 정확도 표시
    └── 시스템 리소스 사용량 게이지
```

### 3.6 기능 6: 커스텀 매크로 스크립팅 시스템

#### 3.6.1 기능 설명
사용자가 자유롭게 매크로 동작을 정의할 수 있는 직관적인 스크립팅 언어 시스템

#### 3.6.2 매크로 스크립팅 언어 (MSL - Macro Scripting Language)

##### **기본 문법 규칙**
- **한 줄 스크립트**: 모든 매크로 동작을 한 줄로 표현
- **직관적 기호**: 게이머가 쉽게 이해할 수 있는 기호 사용
- **체이닝 지원**: 여러 동작을 연결하여 복합 매크로 생성
- **타이밍 제어**: 정밀한 시간 제어 기능

##### **핵심 연산자**

| 연산자 | 설명 | 예시 | 동작 |
|--------|------|------|------|
| `,` | 순차 실행 | `W,A,S,D` | W → A → S → D 순서대로 실행 |
| `+` | 동시 실행 | `W+A+S+D` | W, A, S, D를 동시에 누름 |
| `>` | 홀드 연결 | `W>A>S>D` | W 누르고 유지하면서 A, S, D 순차 실행 |
| `\|` | 병렬 실행 | `W\|A\|S\|D` | 독립적인 4개 스레드로 실행 |
| `~` | 토글 | `~CapsLock` | CapsLock ON/OFF 전환 |
| `*` | 반복 | `W*5` | W키를 5번 반복 |
| `&` | 연속 입력 | `Space&100` | Space를 100ms 간격으로 연속 입력 |
| `%` | 확률 실행 | `W%50` | 50% 확률로 W키 실행 |

##### **타이밍 제어**

| 표현 | 설명 | 예시 | 동작 |
|------|------|------|------|
| `(숫자)` | 지연 시간 (ms) | `W(500)A` | W 누르고 500ms 대기 후 A |
| `[숫자]` | 홀드 시간 (ms) | `W[1000]` | W키를 1초간 홀드 |
| `{숫자}` | 반복 간격 (ms) | `W*5{200}` | W키를 200ms 간격으로 5번 |
| `<숫자>` | 페이드 시간 (ms) | `W<100>A` | W에서 A로 100ms에 걸쳐 전환 |

##### **고급 기능**

| 기능 | 문법 | 예시 | 설명 |
|------|------|------|------|
| **그룹화** | `(...)` | `(W+A),S,D` | W+A를 그룹으로 묶어 순차 실행 |
| **조건부 실행** | `?조건:` | `?HP<50:Heal` | HP가 50% 미만일 때만 힐 실행 |
| **변수 사용** | `$변수명` | `$combo1,W,A` | 미리 정의된 콤보 사용 |
| **마우스 제어** | `@좌표` | `@(100,200)` | 마우스를 (100,200) 좌표로 이동 |
| **휠 제어** | `wheel+/-` | `wheel+3` | 마우스 휠 3번 위로 스크롤 |

#### 3.6.3 실제 사용 예시

##### **기본 예시**
```
# 순차적으로 키 누르기
W,A,S,D

# 동시에 키 누르기  
W+A+S+D

# 순차적으로 누르면서 홀드
W>A>S>D

# 지연 시간을 포함한 콤보
Q(100)W(150)E(200)R

# 키를 홀드하면서 다른 키 실행
Shift[2000]+(W,A,S,D)

# 반복 실행
Space*10{50}

# 확률적 실행 (크리티컬 스킬)
CriticalSkill%30

# 토글 기능
~CapsLock,(W,A,S,D),~CapsLock
```

##### **고급 예시**
```
# 복합 콤보 (격투 게임)
Down+Forward,Punch(50)Kick(100)*3

# MMO 스킬 로테이션
Skill1(1000)Skill2(800)Skill3(1200)Buff%20

# FPS 리코일 제어
MouseDown[100]@(0,-5)@(0,-3)@(0,-2)MouseUp

# RTS 빌드 오더
B(50)H(50)P*5{200}(100)B(50)S

# 자동 파밍 (조건부)
?MP>80:FireBall,?MP<20:ManaPotion

# 팀 전투 매크로
ChatTeam:"Attack!"(500)+Attack+Move@EnemyBase

# 연속 점프 (플랫포머)
Space[200]+(Left*3)(300)Space[200]+(Right*3)
```

#### 3.6.4 스크립팅 시스템 아키텍처

##### **파서 및 인터프리터**
- **어휘 분석**: 스크립트를 토큰으로 분해
- **구문 분석**: AST(Abstract Syntax Tree) 생성
- **의미 분석**: 유효성 검사 및 최적화
- **실행 엔진**: 실시간 매크로 실행

##### **실행 시간 최적화**
- **컴파일 캐싱**: 자주 사용되는 스크립트 미리 컴파일
- **병렬 처리**: 독립적인 동작의 멀티스레딩
- **지연 로딩**: 필요 시점에만 리소스 로드
- **메모리 풀링**: 객체 재사용으로 GC 압박 최소화

#### 3.6.5 개발자 도구

##### **스크립트 에디터**
- **실시간 구문 강조**: 키워드, 연산자, 숫자별 색상 구분
- **자동 완성**: 키 이름, 함수, 변수 자동 완성
- **오류 검출**: 실시간 문법 검사 및 오류 표시
- **스마트 들여쓰기**: 자동 코드 포맷팅

##### **디버깅 도구**
- **단계별 실행**: 스크립트를 한 단계씩 실행
- **중단점 설정**: 특정 지점에서 실행 일시 정지
- **변수 감시**: 실행 중 변수 값 실시간 모니터링
- **실행 로그**: 각 단계의 실행 시간 및 결과 기록

##### **성능 분석기**
- **실행 시간 측정**: 각 구문의 실행 시간 분석
- **병목점 탐지**: 느린 구간 자동 탐지
- **메모리 사용량**: 스크립트별 메모리 사용량 추적
- **최적화 제안**: AI 기반 성능 개선 제안

#### 3.6.6 사용자 인터페이스

```
[커스텀 스크립팅] 탭
├── 스크립트 에디터 영역
│   ├── 문법 강조 텍스트 에디터
│   ├── 줄 번호 표시
│   ├── 자동 완성 팝업
│   └── 오류 밑줄 표시
├── 도구 모음
│   ├── [새 스크립트] [열기] [저장] [실행] 버튼
│   ├── [디버그] [단계 실행] [중단점] 버튼
│   └── [문법 검사] [성능 분석] 버튼
├── 미리보기 패널
│   ├── 파싱된 AST 트리 표시
│   ├── 예상 실행 시간 계산
│   └── 키 입력 시뮬레이션 애니메이션
├── 템플릿 라이브러리
│   ├── 게임별 스크립트 템플릿
│   ├── 사용자 정의 템플릿
│   └── 커뮤니티 공유 스크립트
└── 하단 상태창
    ├── 파싱 결과 및 오류 메시지
    ├── 실행 상태 및 진행률
    └── 성능 통계 (실행 시간, 메모리 사용량)
```

#### 3.6.7 확장 기능

##### **스크립트 라이브러리**
- **게임별 프리셋**: 인기 게임의 최적화된 스크립트 모음
- **사용자 기여**: 커뮤니티에서 제작한 스크립트 공유
- **프로 게이머 스크립트**: 프로 선수들이 실제 사용하는 스크립트
- **버전 관리**: Git 기반 스크립트 버전 관리

##### **AI 어시스턴트**
- **자연어 변환**: "3번 점프한 후 공격" → `Space*3{200}Attack`
- **스크립트 최적화**: 비효율적인 구문 자동 개선
- **게임 인식**: 실행 중인 게임에 맞는 스크립트 제안
- **학습 기능**: 사용자 패턴 학습 후 맞춤 스크립트 생성

##### **통합 개발 환경**
- **프로젝트 관리**: 여러 스크립트 파일을 프로젝트 단위로 관리
- **버전 제어**: 스크립트 변경사항 추적 및 복원
- **팀 협업**: 스크립트 공동 편집 및 리뷰 시스템
- **CI/CD**: 스크립트 자동 테스트 및 배포

#### 3.6.8 보안 및 안전성

##### **샌드박스 실행**
- **권한 제한**: 시스템 파일 접근 금지
- **리소스 제한**: CPU, 메모리 사용량 제한
- **타임아웃**: 무한 루프 방지
- **격리 실행**: 다른 프로세스에 영향 없는 실행

##### **코드 검증**
- **정적 분석**: 컴파일 시점 보안 취약점 검사
- **동적 분석**: 실행 중 비정상 동작 감지
- **화이트리스트**: 허용된 API만 사용 가능
- **디지털 서명**: 신뢰할 수 있는 스크립트 검증

## 4. 시스템 아키텍처

### 4.1 전체 구조도
```
┌─────────────────┐    HTTP/WebSocket    ┌─────────────────┐
│   C# WPF UI     │ ◄─────────────────► │   Python API    │
│   (프론트엔드)    │                     │   (백엔드)       │
│  ┌─────────────┐ │                     │  ┌─────────────┐│
│  │Script Editor│ │                     │  │MSL Compiler │││
│  │Syntax Highlight│                    │  │& Interpreter│││
│  │Auto Complete│ │                     │  └─────────────┘│
│  └─────────────┘ │                     └─────────────────┘
└─────────────────┘                              │
         │                                       │
         │                                       ▼
         ▼                              ┌─────────────────┐
┌─────────────────┐                     │ Script Engine   │
│   Local Config  │                     │ ┌─────────────┐ │
│   Files (.json) │                     │ │  AST Parser │ │
│ ┌─────────────┐ │                     │ │ Performance │ │
│ │Script Cache │ │                     │ │ Optimizer   │ │
│ │Templates    │ │                     │ │ Security    │ │
│ └─────────────┘ │                     │ │ Validator   │ │
└─────────────────┘                     │ └─────────────┘ │
                                        └─────────────────┘
                                                 │
                                                 ▼
                                       ┌─────────────────┐
                                       │   SQLite DB     │
                                       │ ┌─────────────┐ │
                                       │ │   Macros    │ │
                                       │ │Custom Scripts│ │
                                       │ │ Templates   │ │
                                       │ │ Variables   │ │
                                       │ │Performance  │ │
                                       │ └─────────────┘ │
                                       └─────────────────┘
                                                 │
                                                 ▼
                                       ┌─────────────────┐
                                       │  Audio Processing│
                                       │  (Whisper AI)   │
                                       └─────────────────┘
                                                 │
                                                 ▼
                                       ┌─────────────────┐
                                       │ Execution Engine│
                                       │ ┌─────────────┐ │
                                       │ │MSL Runtime  │ │
                                       │ │PyAutoGUI    │ │
                                       │ │Input Control│ │
                                       │ │Multi-thread │ │
                                       │ │Safety Check │ │
                                       │ └─────────────┘ │
                                       └─────────────────┘
```

### 4.2 데이터베이스 스키마
```sql
-- 매크로 테이블 (기존 5가지 타입 + 커스텀 스크립팅 지원)
CREATE TABLE macros (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    voice_command TEXT NOT NULL,
    action_type TEXT NOT NULL, -- combo, rapid, hold, toggle, repeat, custom_script
    key_sequence TEXT NOT NULL, -- JSON format 또는 스크립트 코드
    settings TEXT, -- JSON format for type-specific settings
    script_language TEXT DEFAULT 'MSL', -- 스크립팅 언어 타입 (MSL, 향후 확장 가능)
    is_script BOOLEAN DEFAULT FALSE, -- 스크립트 기반 매크로 여부
    script_version TEXT DEFAULT '1.0', -- 스크립트 언어 버전
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    usage_count INTEGER DEFAULT 0,
    execution_time_avg REAL DEFAULT 0, -- 평균 실행 시간 (ms)
    success_rate REAL DEFAULT 100 -- 실행 성공률 (%)
);

-- 커스텀 스크립트 테이블
CREATE TABLE custom_scripts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    macro_id INTEGER NOT NULL,
    script_code TEXT NOT NULL, -- MSL 스크립트 코드
    compiled_code BLOB, -- 컴파일된 바이트코드 (성능 최적화)
    ast_tree TEXT, -- JSON 형태의 AST 트리
    dependencies TEXT, -- JSON 배열: 의존성 목록
    variables TEXT, -- JSON 객체: 스크립트 변수
    performance_data TEXT, -- JSON: 성능 분석 데이터
    security_hash TEXT, -- 스크립트 무결성 검증용 해시
    is_validated BOOLEAN DEFAULT FALSE, -- 보안 검증 완료 여부
    validation_date DATETIME, -- 마지막 검증 날짜
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (macro_id) REFERENCES macros (id) ON DELETE CASCADE
);

-- 스크립트 템플릿 테이블
CREATE TABLE script_templates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    category TEXT NOT NULL, -- game_type, combo_type, etc.
    game_title TEXT, -- 특정 게임용 템플릿인 경우
    template_code TEXT NOT NULL, -- MSL 템플릿 코드
    parameters TEXT, -- JSON: 템플릿 매개변수 정의
    preview_gif BLOB, -- 실행 미리보기 애니메이션
    difficulty_level INTEGER DEFAULT 1, -- 1(초급) ~ 5(고급)
    popularity_score INTEGER DEFAULT 0, -- 인기도 점수
    author_name TEXT,
    is_official BOOLEAN DEFAULT FALSE, -- 공식 템플릿 여부
    is_public BOOLEAN DEFAULT TRUE, -- 공개 여부
    download_count INTEGER DEFAULT 0,
    rating_average REAL DEFAULT 0, -- 평균 평점
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 스크립트 실행 로그 테이블
CREATE TABLE script_execution_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    macro_id INTEGER NOT NULL,
    script_id INTEGER,
    execution_start DATETIME DEFAULT CURRENT_TIMESTAMP,
    execution_end DATETIME,
    execution_time_ms INTEGER, -- 실행 시간 (밀리초)
    success BOOLEAN DEFAULT FALSE,
    error_message TEXT, -- 실패 시 오류 메시지
    input_parameters TEXT, -- JSON: 실행 시 입력 매개변수
    output_result TEXT, -- JSON: 실행 결과
    performance_metrics TEXT, -- JSON: 성능 메트릭
    memory_usage_kb INTEGER, -- 메모리 사용량 (KB)
    cpu_usage_percent REAL, -- CPU 사용률
    FOREIGN KEY (macro_id) REFERENCES macros (id) ON DELETE CASCADE,
    FOREIGN KEY (script_id) REFERENCES custom_scripts (id) ON DELETE SET NULL
);

-- 프리셋 테이블
CREATE TABLE presets (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    macro_ids TEXT NOT NULL, -- JSON array of macro IDs
    is_favorite BOOLEAN DEFAULT FALSE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 로그 테이블
CREATE TABLE logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    level TEXT NOT NULL, -- DEBUG, INFO, WARNING, ERROR
    message TEXT NOT NULL,
    macro_id INTEGER,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (macro_id) REFERENCES macros (id)
);

-- 스크립트 변수 저장소 테이블
CREATE TABLE script_variables (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    variable_name TEXT NOT NULL UNIQUE,
    variable_value TEXT NOT NULL, -- JSON 형태로 저장
    variable_type TEXT NOT NULL, -- string, number, boolean, object, array
    scope TEXT DEFAULT 'global', -- global, session, macro
    description TEXT,
    is_persistent BOOLEAN DEFAULT TRUE, -- 재시작 후에도 유지 여부
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 스크립트 성능 분석 테이블
CREATE TABLE script_performance_analysis (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    script_id INTEGER NOT NULL,
    analysis_date DATETIME DEFAULT CURRENT_TIMESTAMP,
    total_executions INTEGER DEFAULT 0,
    average_execution_time REAL, -- 평균 실행 시간 (ms)
    min_execution_time REAL, -- 최소 실행 시간 (ms)
    max_execution_time REAL, -- 최대 실행 시간 (ms)
    success_rate REAL, -- 성공률 (%)
    memory_efficiency_score REAL, -- 메모리 효율성 점수
    cpu_efficiency_score REAL, -- CPU 효율성 점수
    bottleneck_operations TEXT, -- JSON: 병목 구간 분석
    optimization_suggestions TEXT, -- JSON: 최적화 제안
    FOREIGN KEY (script_id) REFERENCES custom_scripts (id) ON DELETE CASCADE
);

-- 인덱스 생성 (성능 최적화)
CREATE INDEX idx_macros_action_type ON macros(action_type);
CREATE INDEX idx_macros_is_script ON macros(is_script);
CREATE INDEX idx_custom_scripts_macro_id ON custom_scripts(macro_id);
CREATE INDEX idx_script_templates_category ON script_templates(category);
CREATE INDEX idx_script_templates_game_title ON script_templates(game_title);
CREATE INDEX idx_script_execution_logs_macro_id ON script_execution_logs(macro_id);
CREATE INDEX idx_script_execution_logs_execution_start ON script_execution_logs(execution_start);
CREATE INDEX idx_script_variables_scope ON script_variables(scope);
CREATE INDEX idx_script_performance_analysis_script_id ON script_performance_analysis(script_id);
```

## 5. 개발 계획 및 우선순위

### 5.1 1단계: 기본 매크로 관리 시스템 (2주)
- 매크로 CRUD 기능 구현
- 기본 UI 프레임워크 구축
- 데이터베이스 스키마 설계 및 구현

### 5.2 2단계: 음성 인식 시스템 (3주)
- OpenAI Whisper 통합
- 실시간 음성 녹음 기능
- 명령어 매칭 알고리즘 구현

### 5.3 3단계: 매크로 동작 메커니즘 (2주)
- PyAutoGUI를 통한 키보드/마우스 제어
- 다양한 매크로 타입 구현
- 매크로 실행 엔진 개발

### 5.4 4단계: 프리셋 관리 시스템 (1주)
- 프리셋 저장/로드 기능
- 파일 내보내기/가져오기
- 프리셋 UI 완성

### 5.5 5단계: 로깅 및 최적화 (1주)
- 로그 시스템 구현
- 성능 모니터링 기능
- 버그 수정 및 최적화

### 5.6 6단계: 커스텀 매크로 스크립팅 시스템 (3주)

#### 5.6.1 1주차: 스크립팅 언어 설계 및 파서 구현
- **MSL 문법 정의**: 연산자, 구문, 키워드 명세
- **어휘 분석기**: 토큰화 및 기본 파싱
- **구문 분석기**: AST 생성 및 문법 검증
- **기본 테스트**: 단순 스크립트 파싱 테스트

#### 5.6.2 2주차: 실행 엔진 및 UI 구현
- **MSL 인터프리터**: AST 기반 스크립트 실행
- **스크립트 에디터**: 구문 강조, 자동 완성
- **실행 환경**: 샌드박스, 보안 제약
- **기본 연산자**: `,`, `+`, `>`, `*`, `()`, `[]` 구현

#### 5.6.3 3주차: 고급 기능 및 통합
- **고급 연산자**: `|`, `~`, `&`, `%`, `<>` 구현
- **변수 시스템**: 전역/지역 변수, 타입 시스템
- **템플릿 시스템**: 게임별 템플릿, 라이브러리
- **성능 최적화**: 컴파일 캐싱, 실행 최적화
- **보안 강화**: 코드 검증, 권한 제한
- **통합 테스트**: 전체 시스템 통합 및 테스트

## 6. 심화 기술 구현 및 고도화

### 6.1 고급 보안 및 권한 관리 시스템

#### 6.1.1 안티치트 회피 기술
- **게임 프로세스 분석 및 우회**:
  - 실시간 게임 프로세스 모니터링 및 동적 매크로 조정
  - 하드웨어 레벨 입력 시뮬레이션 (WinAPI SendInput 대신 직접 드라이버 통신)
  - 무작위 지연 및 인간적 입력 패턴 모방
  - 메모리 패치 감지 회피를 위한 프로세스 격리

- **스텔스 모드 구현**:
  - 시스템 후킹 감지 회피
  - DLL 인젝션 방식 대신 외부 프로세스 방식 채택
  - 가상 키보드/마우스 드라이버 구현
  - 게임별 안티치트 시그니처 분석 및 대응

#### 6.1.2 권한 관리 및 사용자 인증
- **다단계 보안 시스템**:
  - 하드웨어 기반 장치 인증 (HWID)
  - 라이선스 키 기반 사용자 인증
  - 클라우드 기반 사용자 프로필 동기화
  - 세션 토큰 및 만료 관리

- **권한별 기능 제한**:
  - 무료/프리미엄/프로 버전별 기능 차별화
  - 매크로 복잡도 제한 (무료: 3단계, 프리미엄: 10단계, 프로: 무제한)
  - 동시 실행 매크로 수 제한
  - 음성 인식 정확도 및 응답 속도 차등 제공

#### 6.1.3 데이터 보호 및 개인정보
- **음성 데이터 보안**:
  - 음성 데이터 로컬 암호화 저장 (AES-256)
  - 클라우드 전송 시 종단간 암호화
  - 음성 데이터 자동 삭제 옵션 (설정 가능한 보존 기간)
  - 음성 프로파일 익명화 기술

### 6.2 고급 성능 최적화 및 AI 가속

#### 6.2.1 멀티스레딩 및 비동기 처리
- **병렬 처리 아키텍처**:
  - 음성 인식용 전용 스레드 풀
  - 매크로 실행용 독립 스레드
  - UI 반응성 보장을 위한 비동기 패턴
  - 백그라운드 작업 큐 시스템

- **메모리 최적화**:
  - 객체 풀링을 통한 GC 압박 최소화
  - 음성 버퍼 스트리밍 처리
  - 매크로 캐싱 시스템
  - 동적 메모리 할당 최적화

#### 6.2.2 AI 모델 최적화 및 가속
- **Whisper AI 최적화**:
  - ONNX Runtime을 이용한 모델 최적화
  - GPU 가속 (CUDA/DirectML) 지원
  - 모델 양자화를 통한 추론 속도 향상
  - 실시간 VAD(Voice Activity Detection) 구현

- **커스텀 음성 인식 모델**:
  - 게임별 특화 음성 모델 훈련
  - 사용자별 음성 프로파일 학습
  - 노이즈 환경 대응 모델
  - 다국어 혼용 명령어 지원

#### 6.2.3 게임별 최적화 프로파일
- **게임 감지 및 자동 설정**:
  - 실행 중인 게임 자동 감지
  - 게임별 최적화된 매크로 프로파일 자동 로드
  - 게임 해상도/프레임레이트별 타이밍 조정
  - 게임 엔진별 입력 레이턴시 보상

### 6.3 엔터프라이즈급 호환성 및 확장성

#### 6.3.1 크로스 플랫폼 지원
- **운영체제별 지원**:
  - Windows 10/11 (기본)
  - Linux 지원 (Mono/Wine 환경)
  - 가상머신 환경 지원
  - 클라우드 게이밍 플랫폼 대응

#### 6.3.2 하드웨어 호환성
- **다양한 입력 장치 지원**:
  - 기계식/멤브레인 키보드별 최적화
  - 게이밍 마우스 DPI 자동 감지
  - 헤드셋별 마이크 특성 프로파일
  - 외부 오디오 인터페이스 지원

#### 6.3.3 게임 엔진별 대응
- **주요 게임 엔진 지원**:
  - Unreal Engine 4/5 최적화
  - Unity 게임 대응
  - Source Engine 지원
  - 커스텀 엔진 게임 분석 도구

### 6.4 고급 매크로 시스템

#### 6.4.1 AI 기반 매크로 생성
- **자동 매크로 학습**:
  - 사용자 플레이 패턴 분석
  - 반복 작업 자동 감지 및 매크로 제안
  - 게임 상황별 최적 매크로 추천
  - 머신러닝 기반 매크로 성능 예측

#### 6.4.2 동적 매크로 조정
- **실시간 환경 적응**:
  - 게임 상태 실시간 분석 (화면 인식)
  - 네트워크 레이턴시에 따른 타이밍 자동 조정
  - FPS/성능에 따른 매크로 속도 조절
  - 게임 업데이트 감지 및 매크로 자동 수정

#### 6.4.3 협업 매크로 시스템
- **팀 매크로 동기화**:
  - 팀원 간 매크로 공유 시스템
  - 실시간 매크로 실행 상태 공유
  - 팀 전략에 따른 매크로 순서 조정
  - 리더 기반 매크로 동기화

### 6.5 클라우드 통합 및 데이터 분석

#### 6.5.1 클라우드 백엔드 시스템
- **스케일러블 인프라**:
  - AWS/Azure 기반 마이크로서비스 아키텍처
  - 실시간 매크로 동기화 서비스
  - 사용자별 설정 클라우드 백업
  - 글로벌 CDN을 통한 빠른 데이터 접근

#### 6.5.2 빅데이터 분석 및 인사이트
- **사용 패턴 분석**:
  - 매크로 사용 통계 실시간 수집
  - 게임별 인기 매크로 순위
  - 사용자별 개선 제안 시스템
  - A/B 테스팅을 통한 기능 최적화

## 7. 차세대 사용자 경험 (UX) 및 인터페이스

### 7.1 AI 기반 개인화 시스템

#### 7.1.1 적응형 사용자 인터페이스
- **스마트 UI 레이아웃**:
  - 사용자 행동 패턴 기반 UI 자동 조정
  - 자주 사용하는 기능 자동 배치
  - 컨텍스트 인식 메뉴 시스템
  - 음성 명령을 통한 UI 제어

- **개인화된 추천 시스템**:
  - AI 기반 매크로 추천 엔진
  - 게임 스타일 분석 후 맞춤 설정 제안
  - 학습 곡선에 따른 기능 단계적 노출
  - 사용자 피드백 기반 추천 정확도 향상

#### 7.1.2 고급 시각적 피드백
- **실시간 시각화 시스템**:
  - 3D 음성 파형 실시간 표시
  - 매크로 실행 과정 애니메이션
  - 게임 화면 오버레이 정보 표시
  - AR/VR 지원을 위한 3D 인터페이스 준비

### 7.2 접근성 및 사용성 혁신

#### 7.2.1 고급 접근성 기능
- **다감각 인터페이스**:
  - 시각 장애인을 위한 완전한 음성 내비게이션
  - 청각 장애인을 위한 시각적 피드백 강화
  - 신체 장애인을 위한 대체 입력 방식
  - 인지 장애 고려한 단순화 모드

#### 7.2.2 다국어 및 문화 적응
- **글로벌 사용자 지원**:
  - 50개 이상 언어 지원
  - 문화권별 UI/UX 최적화
  - 지역별 게임 특성 반영
  - 실시간 다국어 음성 인식

### 7.3 고급 사용자 상호작용

#### 7.3.1 자연어 기반 매크로 생성
- **대화형 매크로 빌더**:
  - "공격 후 0.5초 뒤에 스킬 사용" 같은 자연어 명령 처리
  - 음성으로 매크로 로직 설명하면 자동 생성
  - 복잡한 조건문도 일반 언어로 입력 가능
  - 실시간 매크로 수정 및 피드백

#### 7.3.2 고급 제스처 및 모션 인식
- **멀티모달 인터페이스**:
  - 웹캠 기반 손동작 인식
  - 시선 추적을 통한 매크로 트리거
  - 안면 표정 인식 연동
  - 체감형 게임 컨트롤러 통합

### 7.4 소셜 및 커뮤니티 기능

#### 7.4.1 매크로 공유 플랫폼
- **커뮤니티 기반 생태계**:
  - 매크로 마켓플레이스 구축
  - 사용자 평점 및 리뷰 시스템
  - 프로게이머 인증 매크로 판매
  - 커뮤니티 챌린지 및 이벤트

#### 7.4.2 실시간 협업 도구
- **팀 플레이 최적화**:
  - 팀원 간 실시간 매크로 공유
  - 음성 채팅 통합 매크로 트리거
  - 전략 공유 및 실시간 조정
  - 팀 성과 분석 및 개선 제안

### 7.5 게이미피케이션 및 성장 시스템

#### 7.5.1 사용자 성장 추적
- **스킬 발전 시스템**:
  - 매크로 사용 숙련도 레벨링
  - 게임별 전문성 뱃지 시스템
  - 성취도 기반 보상 시스템
  - 개인 발전 로드맵 제공

#### 7.5.2 경쟁 및 순위 시스템
- **글로벌 리더보드**:
  - 게임별 매크로 효율성 순위
  - 월간/시즌별 챌린지
  - 프로게이머와의 비교 분석
  - 실시간 토너먼트 및 대회

## 8. 차세대 테스트 및 품질 보증

### 8.1 AI 기반 자동화 테스트

#### 8.1.1 머신러닝 기반 테스트 생성
- **자동 테스트 케이스 생성**:
  - 사용자 행동 패턴 분석 후 테스트 시나리오 자동 생성
  - 게임 업데이트 감지 후 자동 회귀 테스트 실행
  - AI 기반 엣지 케이스 발견 및 테스트
  - 성능 저하 예측 및 사전 최적화

#### 8.1.2 실시간 모니터링 및 자가치유
- **자율 복구 시스템**:
  - 런타임 오류 자동 감지 및 복구
  - 성능 저하 시 자동 설정 조정
  - 네트워크 이슈 감지 및 대안 경로 제공
  - 사용자별 최적 설정 자동 추천

### 8.2 고급 성능 및 보안 테스트

#### 8.2.1 스트레스 테스트 자동화
- **대규모 부하 테스트**:
  - 수천 명 동시 사용자 시뮬레이션
  - 다양한 하드웨어 환경 에뮬레이션
  - 네트워크 레이턴시 변화 대응 테스트
  - 메모리 누수 및 성능 저하 자동 감지

#### 8.2.2 보안 침투 테스트
- **화이트해커 시뮬레이션**:
  - 자동화된 보안 취약점 스캔
  - 가상의 공격 시나리오 테스트
  - 암호화 강도 및 인증 시스템 검증
  - 개인정보 보호 컴플라이언스 자동 체크

이 심화 PRD를 바탕으로 세계적 수준의 게임 AI 음성 매크로 프로그램을 개발할 수 있습니다. 각 기능별 구체적인 구현 방법이나 기술적 세부사항이 필요하시면 언제든 문의해 주세요! 