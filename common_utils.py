"""
VoiceMacro Pro 공통 유틸리티 모듈
프로젝트 전반에서 사용되는 공통 함수들을 관리합니다.
코드 중복을 방지하고 일관성을 유지하기 위한 모듈입니다.
"""

import json
import logging
import sys
from datetime import datetime
from typing import Dict, Any, List, Union

# ============================================================================
# 로깅 관련 유틸리티
# ============================================================================

def get_logger(name: str) -> logging.Logger:
    """
    로거 인스턴스를 생성하고 반환하는 함수
    
    Args:
        name (str): 로거 이름 (보통 __name__ 사용)
    
    Returns:
        logging.Logger: 설정된 로거 인스턴스
    
    Example:
        >>> logger = get_logger(__name__)
        >>> logger.info("테스트 메시지")
    """
    logger = logging.getLogger(name)
    
    # 이미 핸들러가 설정되어 있다면 그대로 반환
    if logger.handlers:
        return logger
    
    # 로거 레벨 설정
    logger.setLevel(logging.DEBUG)
    
    # 콘솔 핸들러 생성
    console_handler = logging.StreamHandler(sys.stdout)
    console_handler.setLevel(logging.INFO)
    
    # 포맷터 설정
    formatter = logging.Formatter(
        fmt='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        datefmt='%Y-%m-%d %H:%M:%S'
    )
    console_handler.setFormatter(formatter)
    
    # 핸들러 추가
    logger.addHandler(console_handler)
    
    # 중복 로그 방지
    logger.propagate = False
    
    return logger

# ============================================================================
# 날짜/시간 관련 유틸리티
# ============================================================================

def format_datetime(dt: datetime, format_type: str = "default") -> str:
    """
    날짜시간을 지정된 형식으로 포맷팅하는 함수
    
    Args:
        dt (datetime): 포맷할 날짜시간 객체
        format_type (str): 포맷 타입 ('default', 'short', 'long', 'iso')
    
    Returns:
        str: 포맷된 날짜시간 문자열
    
    Example:
        >>> dt = datetime.now()
        >>> format_datetime(dt, 'default')
        '2024-01-01 12:00:00'
        >>> format_datetime(dt, 'short')
        '2024-01-01'
    """
    if not isinstance(dt, datetime):
        return str(dt)
    
    formats = {
        'default': "%Y-%m-%d %H:%M:%S",
        'short': "%Y-%m-%d",
        'long': "%Y년 %m월 %d일 %H시 %M분 %S초",
        'iso': "%Y-%m-%dT%H:%M:%S",
        'korean': "%Y년 %m월 %d일"
    }
    
    return dt.strftime(formats.get(format_type, formats['default']))

def parse_datetime(date_string: str) -> datetime:
    """
    문자열을 datetime 객체로 변환하는 함수
    
    Args:
        date_string (str): 변환할 날짜시간 문자열
    
    Returns:
        datetime: 변환된 datetime 객체
    
    Raises:
        ValueError: 올바르지 않은 날짜 형식인 경우
    """
    try:
        # 일반적인 형식들을 시도
        formats = [
            "%Y-%m-%d %H:%M:%S",
            "%Y-%m-%d",
            "%Y-%m-%dT%H:%M:%S",
            "%Y/%m/%d %H:%M:%S",
            "%Y/%m/%d"
        ]
        
        for fmt in formats:
            try:
                return datetime.strptime(date_string, fmt)
            except ValueError:
                continue
        
        raise ValueError(f"지원하지 않는 날짜 형식입니다: {date_string}")
    
    except Exception as e:
        raise ValueError(f"날짜 파싱 오류: {e}")

# ============================================================================
# 데이터 검증 유틸리티
# ============================================================================

def validate_required_fields(data: Dict[str, Any], required_fields: List[str]) -> None:
    """
    필수 필드가 모두 존재하는지 검증하는 함수
    
    Args:
        data (Dict): 검증할 데이터 딕셔너리
        required_fields (List[str]): 필수 필드 목록
    
    Raises:
        ValueError: 필수 필드가 누락된 경우
    
    Example:
        >>> data = {"name": "테스트", "age": 25}
        >>> validate_required_fields(data, ["name", "age"])  # 성공
        >>> validate_required_fields(data, ["name", "email"])  # ValueError 발생
    """
    missing_fields = []
    
    for field in required_fields:
        if field not in data or data[field] is None or data[field] == "":
            missing_fields.append(field)
    
    if missing_fields:
        raise ValueError(f"필수 필드가 누락되었습니다: {', '.join(missing_fields)}")

def validate_data_types(data: Dict[str, Any], type_rules: Dict[str, type]) -> None:
    """
    데이터 타입을 검증하는 함수
    
    Args:
        data (Dict): 검증할 데이터 딕셔너리
        type_rules (Dict): 필드별 타입 규칙
    
    Raises:
        TypeError: 올바르지 않은 데이터 타입인 경우
    
    Example:
        >>> data = {"name": "테스트", "age": 25}
        >>> rules = {"name": str, "age": int}
        >>> validate_data_types(data, rules)  # 성공
    """
    for field, expected_type in type_rules.items():
        if field in data and data[field] is not None:
            if not isinstance(data[field], expected_type):
                raise TypeError(f"필드 '{field}'의 타입이 올바르지 않습니다. "
                              f"기대값: {expected_type.__name__}, "
                              f"실제값: {type(data[field]).__name__}")

# ============================================================================
# JSON 관련 유틸리티
# ============================================================================

def safe_json_load(json_string: str) -> Union[Dict, List, None]:
    """
    안전하게 JSON 문자열을 파싱하는 함수
    
    Args:
        json_string (str): 파싱할 JSON 문자열
    
    Returns:
        Union[Dict, List, None]: 파싱된 객체 또는 None (실패시)
    
    Example:
        >>> safe_json_load('{"name": "test"}')
        {'name': 'test'}
        >>> safe_json_load('invalid json')
        None
    """
    if not json_string or not isinstance(json_string, str):
        return None
    
    try:
        return json.loads(json_string)
    except (json.JSONDecodeError, TypeError):
        return None

def safe_json_dump(data: Any) -> str:
    """
    안전하게 객체를 JSON 문자열로 변환하는 함수
    
    Args:
        data (Any): JSON으로 변환할 객체
    
    Returns:
        str: JSON 문자열 (실패시 빈 문자열)
    
    Example:
        >>> safe_json_dump({"name": "test"})
        '{"name": "test"}'
        >>> safe_json_dump(set())  # set은 JSON 직렬화 불가
        ''
    """
    try:
        return json.dumps(data, ensure_ascii=False, indent=2)
    except (TypeError, ValueError):
        return ""

# ============================================================================
# 문자열 처리 유틸리티
# ============================================================================

def sanitize_string(text: str, max_length: int = 255) -> str:
    """
    문자열을 정리하고 길이를 제한하는 함수
    
    Args:
        text (str): 정리할 문자열
        max_length (int): 최대 길이 (기본값: 255)
    
    Returns:
        str: 정리된 문자열
    
    Example:
        >>> sanitize_string("  Hello World!  ")
        'Hello World!'
        >>> sanitize_string("A" * 300, 10)
        'AAAAAAAAAA'
    """
    if not isinstance(text, str):
        text = str(text)
    
    # 앞뒤 공백 제거 및 길이 제한
    cleaned = text.strip()[:max_length]
    
    return cleaned

def is_valid_voice_command(command: str) -> bool:
    """
    음성 명령어가 유효한지 검증하는 함수
    
    Args:
        command (str): 검증할 음성 명령어
    
    Returns:
        bool: 유효성 여부
    
    Example:
        >>> is_valid_voice_command("공격")
        True
        >>> is_valid_voice_command("")
        False
        >>> is_valid_voice_command("a" * 100)
        False
    """
    if not command or not isinstance(command, str):
        return False
    
    # 길이 제한 (1-50자)
    if len(command.strip()) < 1 or len(command.strip()) > 50:
        return False
    
    # 특수문자 제한 (기본 한글, 영문, 숫자, 공백만 허용)
    import re
    pattern = r'^[가-힣a-zA-Z0-9\s]+$'
    
    return bool(re.match(pattern, command.strip()))

# ============================================================================
# API 응답 유틸리티
# ============================================================================

def create_api_response(success: bool = True, data: Any = None, 
                       message: str = "", error: str = None) -> Dict[str, Any]:
    """
    통일된 API 응답 형식을 생성하는 함수
    
    Args:
        success (bool): 성공 여부
        data (Any): 응답 데이터
        message (str): 응답 메시지
        error (str): 오류 메시지
    
    Returns:
        Dict: 표준화된 API 응답
    
    Example:
        >>> create_api_response(True, {"id": 1}, "매크로 생성 성공")
        {
            'success': True,
            'data': {'id': 1},
            'message': '매크로 생성 성공',
            'error': None,
            'timestamp': '2024-01-01T12:00:00'
        }
    """
    return {
        'success': success,
        'data': data,
        'message': message,
        'error': error,
        'timestamp': datetime.now().isoformat()
    }

def create_error_response(error_message: str, error_code: int = None) -> Dict[str, Any]:
    """
    오류 응답을 생성하는 함수
    
    Args:
        error_message (str): 오류 메시지
        error_code (int): 오류 코드 (선택사항)
    
    Returns:
        Dict: 오류 응답
    """
    error_data = {
        'message': error_message,
        'code': error_code
    } if error_code else error_message
    
    return create_api_response(
        success=False,
        data=None,
        message="요청 처리 중 오류가 발생했습니다.",
        error=error_data
    )

# ============================================================================
# 로깅 유틸리티
# ============================================================================

def create_log_entry(level: str, message: str, macro_id: int = None) -> Dict[str, Any]:
    """
    로그 엔트리를 생성하는 함수
    
    Args:
        level (str): 로그 레벨 (DEBUG, INFO, WARNING, ERROR)
        message (str): 로그 메시지
        macro_id (int): 관련 매크로 ID (선택사항)
    
    Returns:
        Dict: 로그 엔트리 데이터
    
    Example:
        >>> create_log_entry("INFO", "매크로 실행 완료", 1)
        {
            'level': 'INFO',
            'message': '매크로 실행 완료',
            'macro_id': 1,
            'timestamp': '2024-01-01T12:00:00'
        }
    """
    valid_levels = ['DEBUG', 'INFO', 'WARNING', 'ERROR']
    
    if level.upper() not in valid_levels:
        level = 'INFO'
    
    return {
        'level': level.upper(),
        'message': sanitize_string(message, 1000),
        'macro_id': macro_id,
        'timestamp': datetime.now().isoformat()
    }

# ============================================================================
# 매크로 관련 유틸리티
# ============================================================================

def validate_action_type(action_type: str) -> bool:
    """
    매크로 동작 타입이 유효한지 검증하는 함수
    
    Args:
        action_type (str): 검증할 동작 타입
    
    Returns:
        bool: 유효성 여부
    """
    valid_types = ['combo', 'rapid', 'hold', 'toggle', 'repeat']
    return action_type.lower() in valid_types

def parse_key_sequence(key_sequence: str) -> List[str]:
    """
    키 시퀀스 문자열을 리스트로 파싱하는 함수
    
    Args:
        key_sequence (str): 파싱할 키 시퀀스 (예: "Q,W,E,R")
    
    Returns:
        List[str]: 파싱된 키 리스트
    
    Example:
        >>> parse_key_sequence("Q,W,E,R")
        ['Q', 'W', 'E', 'R']
        >>> parse_key_sequence("Ctrl+C")
        ['Ctrl+C']
    """
    if not key_sequence or not isinstance(key_sequence, str):
        return []
    
    # 쉼표로 분리된 키들을 파싱
    keys = [key.strip() for key in key_sequence.split(',')]
    
    # 빈 문자열 제거
    return [key for key in keys if key]

# ============================================================================
# 설정 관련 유틸리티
# ============================================================================

def merge_settings(default_settings: Dict, user_settings: Dict) -> Dict:
    """
    기본 설정과 사용자 설정을 병합하는 함수
    
    Args:
        default_settings (Dict): 기본 설정
        user_settings (Dict): 사용자 설정
    
    Returns:
        Dict: 병합된 설정
    
    Example:
        >>> default = {"delay": 100, "repeat": 1}
        >>> user = {"delay": 200}
        >>> merge_settings(default, user)
        {'delay': 200, 'repeat': 1}
    """
    merged = default_settings.copy()
    
    if user_settings and isinstance(user_settings, dict):
        merged.update(user_settings)
    
    return merged

def validate_settings(settings: Dict, action_type: str) -> bool:
    """
    매크로 설정이 동작 타입에 맞는지 검증하는 함수
    
    Args:
        settings (Dict): 검증할 설정
        action_type (str): 매크로 동작 타입
    
    Returns:
        bool: 유효성 여부
    """
    if not settings or not isinstance(settings, dict):
        return True  # 설정이 없어도 기본값으로 동작 가능
    
    # 동작 타입별 필수 설정 검증
    required_keys = {
        'combo': [],  # 콤보는 특별한 설정 불필요
        'rapid': ['speed'],  # 연사는 속도 설정 필요
        'hold': ['duration'],  # 홀드는 지속시간 설정 필요
        'toggle': [],  # 토글은 특별한 설정 불필요
        'repeat': ['count']  # 반복은 횟수 설정 필요
    }
    
    required = required_keys.get(action_type.lower(), [])
    
    for key in required:
        if key not in settings:
            return False
    
    return True 