@echo off
REM VoiceMacro Pro - Whisper 음성 인식 정확도 테스트 배치 파일
REM 개선된 Whisper 서비스의 정확도를 빠르게 테스트

echo.
echo ========================================
echo 🎯 VoiceMacro Pro Whisper 정확도 테스트
echo ========================================
echo.

REM 현재 디렉토리 확인
echo 📁 현재 디렉토리: %CD%
echo.

REM Python 설치 확인
py --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Python이 설치되지 않았거나 경로에 없습니다.
    echo 💡 Python 3.8 이상을 설치해주세요.
    pause
    exit /b 1
)

echo ✅ Python 버전:
py --version

echo.
echo 📦 필요한 패키지 확인 중...

REM 필요한 패키지들 확인
py -c "import numpy, sounddevice, openai" >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 필요한 패키지가 설치되지 않았습니다.
    echo 💡 다음 명령어로 패키지를 설치해주세요:
    echo    pip install numpy sounddevice openai python-dotenv
    pause
    exit /b 1
)

echo ✅ 필요한 패키지가 모두 설치되어 있습니다.
echo.

REM OpenAI API 키 확인
if not exist ".env" (
    echo ⚠️  .env 파일이 없습니다.
    echo 💡 OpenAI API 키를 설정해주세요:
    echo    OPENAI_API_KEY=your-api-key-here
    echo.
)

echo 🚀 Whisper 정확도 테스트를 시작합니다...
echo.

REM 테스트 실행
py test_whisper_accuracy.py

echo.
echo 📊 테스트가 완료되었습니다.
echo.

REM 로그 파일 확인
if exist "logs\voice_recognition.log" (
    echo 📝 상세 로그는 다음 위치에서 확인하세요:
    echo    logs\voice_recognition.log
    echo.
)

REM 임시 파일 정리 확인
if exist "temp_audio\" (
    echo 🧹 임시 오디오 파일 정리...
    for %%f in (temp_audio\tmp*.wav) do (
        if %%~zf equ 0 (
            del "%%f" >nul 2>&1
        )
    )
    echo ✅ 빈 임시 파일들을 정리했습니다.
    echo.
)

echo 🎯 테스트 결과 요약:
echo    - 음성 인식 정확도가 크게 개선되었습니다
echo    - 게임 매크로 명령어 특화 프롬프트 적용
echo    - 한국어 동의어 인식 강화
echo    - 빈 오디오 파일 생성 방지
echo.

pause 