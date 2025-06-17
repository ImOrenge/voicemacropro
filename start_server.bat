@echo off
echo VoiceMacro Pro 서버 시작 중...
echo.

REM Python 버전 확인
py --version
echo.

REM 의존성 확인
echo 의존성 확인 중...
py -c "import flask; print('✓ Flask:', flask.__version__)"
py -c "import flask_cors; print('✓ Flask-CORS')"
echo.

REM 데이터베이스 초기화
echo 데이터베이스 초기화 중...
py -c "import database; print('✓ 데이터베이스 초기화 완료')"
echo.

REM 서버 시작
echo 서버 시작 중...
py api_server.py

pause 