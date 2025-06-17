# 🎤 VoiceMacro Pro - OpenAI Whisper API 설정 가이드

이 문서는 VoiceMacro Pro에서 실제 OpenAI Whisper API를 사용하여 음성 인식을 하기 위한 설정 방법을 설명합니다.

## 📋 요구사항

- Python 3.8 이상
- OpenAI API 계정 및 API 키
- 인터넷 연결

## 🔧 설정 단계

### 1. 필요한 패키지 설치

```bash
# 프로젝트 루트 디렉토리에서 실행
pip install -r requirements.txt
```

### 2. OpenAI API 키 획득

1. [OpenAI 웹사이트](https://platform.openai.com/)에 가입
2. [API Keys 페이지](https://platform.openai.com/api-keys)로 이동
3. "Create new secret key" 클릭하여 새 API 키 생성
4. 생성된 API 키를 복사 (한 번만 표시됨)

### 3. 환경 변수 설정

#### 방법 1: .env 파일 생성 (권장)

프로젝트 루트 디렉토리에 `.env` 파일을 생성하고 다음 내용을 입력:

```bash
# OpenAI API 설정
OPENAI_API_KEY=your_openai_api_key_here

# Whisper 모델 설정 (기본값: whisper-1)
WHISPER_MODEL=whisper-1

# 음성 인식 언어 설정 (기본값: 한국어)
VOICE_RECOGNITION_LANGUAGE=ko

# 기타 설정
VOICE_RECOGNITION_TIMEOUT=30
VOICE_RECOGNITION_MAX_FILE_SIZE=25
LOG_LEVEL=INFO
```

#### 방법 2: Windows 환경 변수 설정

1. `Win + R` → `sysdm.cpl` 실행
2. "고급" 탭 → "환경 변수" 클릭
3. "시스템 변수"에서 "새로 만들기" 클릭
4. 변수 이름: `OPENAI_API_KEY`
5. 변수 값: 복사한 API 키 입력

### 4. 디렉토리 생성

프로젝트 실행 시 자동으로 생성되지만, 미리 생성할 수도 있습니다:

```bash
mkdir temp_audio
mkdir logs
```

## 🚀 실행 방법

### 1. 백엔드 서버 시작

```bash
# 프로젝트 루트 디렉토리에서
python api_server.py
```

서버가 정상 실행되면 다음과 같은 메시지가 표시됩니다:
```
VoiceMacro API 서버를 시작합니다...
서버 주소: http://localhost:5000
API 문서: http://localhost:5000/api/health
 * Running on all addresses (0.0.0.0)
 * Running on http://127.0.0.1:5000
 * Running on http://[::1]:5000
```

### 2. C# 애플리케이션 실행

Visual Studio 또는 명령줄에서 C# WPF 애플리케이션을 실행합니다.

## 🧪 테스트 방법

### 1. 서버 상태 확인

브라우저에서 http://localhost:5000/api/health 접속하여 서버 상태 확인

### 2. Whisper 서비스 상태 확인

브라우저에서 http://localhost:5000/api/whisper/status 접속하여 Whisper 설정 확인

예상 응답:
```json
{
  "success": true,
  "data": {
    "client_initialized": true,
    "api_key_configured": true,
    "model": "whisper-1",
    "language": "ko",
    "sample_rate": 16000,
    "temp_dir": "temp_audio",
    "temp_dir_exists": true,
    "macro_cache_size": 0,
    "cache_last_updated": null
  },
  "message": "Whisper 서비스 상태 조회 성공"
}
```

### 3. 애플리케이션에서 테스트

1. VoiceMacro Pro 실행
2. "🎤 음성 인식" 탭으로 이동
3. "🎤 녹음 시작" 버튼 클릭
4. 명확하게 음성 명령 발화
5. "⏹️ 녹음 중지" 버튼 클릭
6. Whisper가 음성을 분석하고 결과 표시

## 📊 사용량 및 비용

- OpenAI Whisper API는 사용량에 따라 과금됩니다
- 현재 요금: $0.006 per minute (분당 0.006달러)
- 예상 비용: 1분 음성 인식 시 약 7원 (환율에 따라 변동)

## ⚠️ 주의사항

### 보안
- API 키를 코드에 직접 하드코딩하지 마세요
- .env 파일을 Git에 커밋하지 마세요
- API 키는 안전한 곳에 보관하세요

### 성능
- 음성 파일은 25MB 이하로 제한됩니다
- 긴 음성보다는 짧고 명확한 음성이 더 정확합니다
- 네트워크 상태에 따라 응답 시간이 달라질 수 있습니다

### 지원 언어
- 한국어 (ko) - 기본값
- 영어 (en)
- 기타 언어는 config.py에서 설정 가능

## 🔧 문제 해결

### 1. "OpenAI API 키가 설정되지 않았습니다" 오류

- .env 파일의 API 키 확인
- 환경 변수 설정 확인
- API 키 오타 확인

### 2. "음성 인식에 실패했습니다" 오류

- 마이크 권한 확인
- 마이크 장치 연결 상태 확인
- 주변 소음 레벨 확인
- 음성 명령의 명확성 확인

### 3. 서버 연결 오류

- 백엔드 서버 실행 상태 확인
- 포트 5000번 사용 가능 여부 확인
- 방화벽 설정 확인

### 4. 임시 파일 관련 오류

- temp_audio 디렉토리 권한 확인
- 디스크 여유 공간 확인

## 📚 추가 리소스

- [OpenAI Whisper API 문서](https://platform.openai.com/docs/guides/speech-to-text)
- [OpenAI API 요금 정보](https://openai.com/pricing)
- [프로젝트 개발 규칙](DEVELOPMENT_RULES.md)

## 💡 팁

1. **매크로 명령어 설정 시 팁:**
   - 짧고 명확한 단어 사용
   - 발음하기 쉬운 명령어 선택
   - 비슷한 발음의 명령어 피하기

2. **음성 인식 정확도 향상:**
   - 조용한 환경에서 사용
   - 마이크와 적절한 거리 유지
   - 일정한 톤과 속도로 발화

3. **비용 절약:**
   - 불필요한 긴 녹음 피하기
   - 테스트 시에만 사용하고 평소엔 로컬 처리 활용
   - API 사용량 정기적으로 모니터링 