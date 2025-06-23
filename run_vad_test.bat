@echo off
chcp 65001 > nul
echo.
echo 🔍 VoiceMacro Pro - Voice Activity Detection 테스트
echo ================================================
echo.
echo 이 스크립트는 음성 녹음 및 VAD 시스템의 문제를 진단합니다.
echo.

REM 현재 디렉토리 확인
if not exist "backend" (
    echo ❌ 오류: backend 폴더를 찾을 수 없습니다.
    echo    voicemacro 프로젝트 루트 디렉토리에서 실행하세요.
    pause
    exit /b 1
)

REM Python 실행 확인
python --version > nul 2>&1
if errorlevel 1 (
    echo ❌ 오류: Python이 설치되지 않았거나 PATH에 추가되지 않았습니다.
    echo    Python 3.8 이상을 설치하고 다시 시도하세요.
    pause
    exit /b 1
)

echo ✅ Python 환경 확인됨
echo.

REM 가상환경 활성화 시도 (선택사항)
if exist "venv\Scripts\activate.bat" (
    echo 🔧 가상환경 활성화 중...
    call venv\Scripts\activate.bat
)

REM VAD 테스트 실행
echo 🚀 VAD 테스트 시작...
echo.
python test_vad_validation.py

echo.
echo 📋 테스트 완료!
echo.
echo 🔧 추가 도움이 필요하시면:
echo    1. 서버 시작: py run_server.py
echo    2. 음성 인식 테스트: py backend/tests/test_voice_recording.py  
echo    3. 마이크 API 테스트: py backend/tests/test_microphone_api.py
echo.
pause 