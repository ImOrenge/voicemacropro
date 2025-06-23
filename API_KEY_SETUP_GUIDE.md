# 🔑 OpenAI API 키 설정 가이드

## 📋 API 키 획득 방법

### 1. OpenAI 계정 생성
- https://platform.openai.com 방문
- 계정 생성 또는 로그인

### 2. API 키 생성
- 대시보드에서 "API Keys" 섹션 이동
- "Create new secret key" 클릭
- 생성된 키를 안전하게 복사 (다시 볼 수 없음)

### 3. 결제 정보 설정 (중요!)
- GPT-4o 트랜스크립션은 유료 서비스입니다
- "Billing" 섹션에서 결제 방법 추가
- 최소 $5-10 정도 크레딧 충전 권장

## 🔧 API 키 설정 방법

### 방법 1: 환경변수 설정 (권장)

#### Windows PowerShell:
```powershell
# 현재 세션용
$env:OPENAI_API_KEY = "sk-실제API키여기입력"

# 영구 설정 (시스템 환경변수)
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-실제API키여기입력", "User")
```

#### Windows 명령 프롬프트:
```cmd
set OPENAI_API_KEY=sk-실제API키여기입력
```

### 방법 2: .env 파일 생성

프로젝트 루트에 `.env` 파일 생성:
```env
OPENAI_API_KEY=sk-실제API키여기입력

# GPT-4o 설정
GPT4O_ENABLED=true
GPT4O_CONFIDENCE_THRESHOLD=0.7
GPT4O_VAD_THRESHOLD=0.5
GPT4O_NOISE_REDUCTION=near_field
GPT4O_BUFFER_SIZE_MS=100

# 로그 설정
LOG_LEVEL=INFO
VOICE_RECOGNITION_LANGUAGE=ko
```

## ⚠️ 보안 주의사항

1. **API 키 노출 금지**
   - .env 파일을 Git에 커밋하지 마세요
   - 코드에 직접 API 키를 하드코딩하지 마세요

2. **API 키 관리**
   - 정기적으로 키를 회전시키세요
   - 사용하지 않는 키는 즉시 삭제하세요

3. **비용 관리**
   - OpenAI 대시보드에서 사용량을 정기적으로 확인하세요
   - 비용 한도를 설정하여 예상치 못한 과금을 방지하세요

## 📊 예상 비용

GPT-4o 실시간 트랜스크립션 예상 비용:
- **입력 토큰**: $2.50 / 1M 토큰
- **출력 토큰**: $10.00 / 1M 토큰
- **일반적인 사용**: 시간당 $0.10-0.50 정도

## 🔍 API 키 테스트

API 키 설정 후 다음 명령으로 테스트:
```powershell
py backend/tests/test_gpt4o_transcription.py
```

정상 연결 시 "✅ PASS 실제 WebSocket 연결: 연결 성공" 메시지가 표시됩니다.

## 🆘 문제 해결

### API 키 오류 시:
1. API 키 형식 확인 (sk-로 시작하는 51자)
2. 계정 결제 정보 확인
3. OpenAI 계정 상태 확인 (정지되지 않았는지)

### 연결 오류 시:
1. 네트워크 연결 확인
2. 방화벽 설정 확인
3. VPN 사용 중인 경우 해제 후 테스트 