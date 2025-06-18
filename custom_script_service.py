"""
커스텀 스크립팅 관리 서비스
MSL(Macro Scripting Language) 스크립트의 생성, 컴파일, 실행, 관리를 담당합니다.
"""

import json
import hashlib
import time
import traceback
from datetime import datetime
from typing import Dict, List, Optional, Any
from database import db_manager
from msl_lexer import MSLLexer
from msl_parser import MSLParser
from msl_interpreter import MSLInterpreter
import threading
import logging

# 로깅 설정
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class CustomScriptService:
    """커스텀 스크립팅 서비스 클래스"""
    
    def __init__(self):
        """초기화"""
        self.lexer = MSLLexer()
        self.parser = MSLParser()
        self.interpreter = MSLInterpreter()
        self._script_cache = {}  # 컴파일된 스크립트 캐시
        self._execution_lock = threading.Lock()
    
    def create_custom_script(self, macro_id: int, script_code: str, 
                           variables: Dict = None) -> Dict[str, Any]:
        """
        새로운 커스텀 스크립트를 생성하는 함수
        
        Args:
            macro_id (int): 연결될 매크로 ID
            script_code (str): MSL 스크립트 코드
            variables (Dict): 스크립트 변수 (선택사항)
            
        Returns:
            Dict[str, Any]: 생성 결과
        """
        try:
            # 스크립트 검증 및 컴파일
            validation_result = self.validate_script(script_code)
            if not validation_result['valid']:
                return {
                    'success': False,
                    'error': f"스크립트 검증 실패: {validation_result['error']}",
                    'script_id': None
                }
            
            # AST 생성
            tokens = self.lexer.tokenize(script_code)
            ast = self.parser.parse(tokens)
            
            # AST를 JSON으로 안전하게 변환
            try:
                if hasattr(ast, 'to_dict'):
                    ast_json = json.dumps(ast.to_dict())
                elif isinstance(ast, list):
                    # AST가 리스트인 경우 각 노드를 문자열로 변환
                    ast_dict = [str(node) for node in ast]
                    ast_json = json.dumps(ast_dict)
                else:
                    ast_json = json.dumps(str(ast))
            except Exception as json_error:
                logger.warning(f"AST JSON 변환 실패: {json_error}, 문자열로 저장")
                ast_json = json.dumps(str(ast))
            
            # 보안 해시 생성
            security_hash = self._generate_security_hash(script_code)
            
            # 의존성 분석
            dependencies = self._analyze_dependencies(ast)
            
            # 데이터베이스에 저장
            conn = db_manager.get_connection()
            cursor = conn.cursor()
            
            cursor.execute('''
                INSERT INTO custom_scripts (
                    macro_id, script_code, ast_tree, dependencies, variables,
                    security_hash, is_validated, validation_date
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                macro_id,
                script_code,
                ast_json,
                json.dumps(dependencies),
                json.dumps(variables or {}),
                security_hash,
                True,
                datetime.now().isoformat()
            ))
            
            script_id = cursor.lastrowid
            
            # 매크로 테이블 업데이트 (스크립트 연결)
            cursor.execute('''
                UPDATE macros 
                SET is_script = TRUE, script_language = 'MSL', updated_at = ?
                WHERE id = ?
            ''', (datetime.now().isoformat(), macro_id))
            
            conn.commit()
            conn.close()
            
            # 캐시에 추가
            self._script_cache[script_id] = {
                'ast': ast,
                'code': script_code,
                'variables': variables or {},
                'dependencies': dependencies
            }
            
            logger.info(f"커스텀 스크립트 생성 완료: ID {script_id}, 매크로 ID {macro_id}")
            
            return {
                'success': True,
                'script_id': script_id,
                'message': '스크립트가 성공적으로 생성되었습니다.',
                'validation_result': validation_result
            }
            
        except Exception as e:
            logger.error(f"커스텀 스크립트 생성 실패: {str(e)}")
            return {
                'success': False,
                'error': f"스크립트 생성 중 오류 발생: {str(e)}",
                'script_id': None
            }
    
    def validate_script(self, script_code: str) -> Dict[str, Any]:
        """
        MSL 스크립트 코드를 검증하는 함수
        
        Args:
            script_code (str): 검증할 스크립트 코드
            
        Returns:
            Dict[str, Any]: 검증 결과
        """
        try:
            if not script_code or not script_code.strip():
                return {
                    'is_valid': False,
                    'errors': ['스크립트 코드가 비어있습니다'],
                    'warnings': ['스크립트 코드를 입력해주세요'],
                    'tokens': [],
                    'ast': None,
                    'validation_time_ms': 0.1
                }
            
            # 기본 구문 체크
            basic_syntax_issues = self._check_basic_syntax(script_code)
            if basic_syntax_issues:
                return {
                    'is_valid': False,
                    'errors': basic_syntax_issues,
                    'warnings': self._get_syntax_suggestions(script_code),
                    'tokens': [],
                    'ast': None,
                    'validation_time_ms': 1.0
                }
            
            # MSL 파싱 시도 (현재 제한된 구문만 지원)
            try:
                tokens = self.lexer.tokenize(script_code)
                
                # 토큰 유효성 검사
                errors = self.lexer.validate_tokens(tokens)
                if errors:
                    return {
                        'is_valid': False,
                        'errors': errors[:3],  # 처음 3개 오류만 표시
                        'warnings': self._get_token_suggestions(errors),
                        'tokens': [],
                        'ast': None,
                        'validation_time_ms': 2.0
                    }
                
                # 파싱 시도
                ast = self.parser.parse(script_code)
                
                # 파싱 성공 시 AST 분석
                node_count = self._count_ast_nodes(ast)
                execution_time = self._estimate_execution_time(ast)
                
                return {
                    'is_valid': True,
                    'errors': [],
                    'warnings': [],
                    'tokens': [],
                    'ast': None,
                    'validation_time_ms': 1.0
                }
                
            except Exception as parse_error:
                # 파싱 실패했지만 기본 구문은 올바른 경우
                # -> 고급 구문이 아직 지원되지 않음을 알림
                
                advanced_features = self._detect_advanced_features(script_code)
                if advanced_features:
                    return {
                        'is_valid': False,
                        'errors': [f'고급 구문이 아직 완전히 지원되지 않습니다: {", ".join(advanced_features)}'],
                        'warnings': [
                            '현재 지원되는 기본 구문을 사용해보세요:',
                            '• 순차 실행: W,A,S,D',
                            '• 동시 실행: W+A+S+D', 
                            '• 반복: Space*5',
                            '• 연속 입력: Attack&100',
                            '• 토글: ~CapsLock',
                            '• 홀드: Shift[2000]'
                        ],
                        'tokens': [],
                        'ast': None,
                        'validation_time_ms': 5.0
                    }
                else:
                    return {
                        'is_valid': False,
                        'errors': [f'파싱 오류: {str(parse_error)}'],
                        'warnings': self._get_parsing_suggestions(script_code, str(parse_error)),
                        'tokens': [],
                        'ast': None,
                        'validation_time_ms': 3.0
                    }
                    
        except Exception as e:
            logger.error(f"스크립트 검증 중 예외 발생: {e}")
            return {
                'is_valid': False,
                'errors': [f'검증 중 오류가 발생했습니다: {str(e)}'],
                'warnings': ['스크립트 코드를 다시 확인해주세요'],
                'tokens': [],
                'ast': None,
                'validation_time_ms': 0.5
            }
    
    def _check_basic_syntax(self, script_code: str) -> List[str]:
        """기본 구문 검사"""
        issues = []
        
        # 괄호 매칭 검사
        open_parens = script_code.count('(')
        close_parens = script_code.count(')')
        if open_parens != close_parens:
            issues.append('괄호가 매칭되지 않습니다')
        
        open_brackets = script_code.count('[')
        close_brackets = script_code.count(']')
        if open_brackets != close_brackets:
            issues.append('대괄호가 매칭되지 않습니다')
        
        open_braces = script_code.count('{')
        close_braces = script_code.count('}')
        if open_braces != close_braces:
            issues.append('중괄호가 매칭되지 않습니다')
        
        # 빈 연산자 검사
        if script_code.endswith((',', '+', '*', '&', '>', '|', '~')):
            issues.append('스크립트가 연산자로 끝납니다')
        
        if script_code.startswith((',', '+', '*', '&', '>', '|')):
            issues.append('스크립트가 연산자로 시작합니다')
        
        return issues
    
    def _detect_advanced_features(self, script_code: str) -> List[str]:
        """고급 기능 감지"""
        features = []
        
        # 연속 지연시간 패턴 감지 (Q(100)W 같은)
        import re
        if re.search(r'\w+\(\d+\)\w+', script_code):
            features.append('연속 지연시간 (예: Q(100)W)')
        
        # 마우스 좌표 감지
        if '@(' in script_code:
            features.append('마우스 좌표 제어')
        
        # 조건부 실행 감지
        if '?' in script_code and ':' in script_code:
            features.append('조건부 실행')
        
        # 변수 사용 감지
        if '$' in script_code:
            features.append('변수 사용')
        
        # 복잡한 그룹화 감지
        if ')(' in script_code:
            features.append('복잡한 그룹화')
        
        return features
    
    def _get_syntax_suggestions(self, script_code: str) -> List[str]:
        """구문 제안 생성"""
        suggestions = []
        
        if '(' in script_code and ')' not in script_code:
            suggestions.append('여는 괄호에 대응하는 닫는 괄호를 추가하세요')
        
        if '[' in script_code and ']' not in script_code:
            suggestions.append('여는 대괄호에 대응하는 닫는 대괄호를 추가하세요')
        
        if script_code.endswith(','):
            suggestions.append('쉼표 뒤에 다음 동작을 추가하세요')
        
        return suggestions
    
    def _get_token_suggestions(self, errors: List[str]) -> List[str]:
        """토큰 오류에 대한 제안"""
        suggestions = []
        
        if any('@' in error for error in errors):
            suggestions.append('마우스 좌표는 @(x,y) 형식으로 작성하세요 (예: @(100,200))')
        
        if any('-' in error for error in errors):
            suggestions.append('음수는 마우스 좌표 내에서만 사용할 수 있습니다')
        
        return suggestions
    
    def _get_parsing_suggestions(self, script_code: str, error_msg: str) -> List[str]:
        """파싱 오류에 대한 제안"""
        suggestions = []
        
        if 'unexpected token' in error_msg.lower():
            suggestions.append('예상치 못한 토큰이 있습니다. 구문을 다시 확인해주세요')
        
        if 'timing' in error_msg.lower():
            suggestions.append('타이밍 제어 구문을 확인해주세요 (예: W[1000], W*5{200})')
        
        return suggestions
    
    def execute_script(self, script_id: int, context: Dict = None) -> Dict[str, Any]:
        """
        스크립트를 실행하는 함수
        
        Args:
            script_id (int): 실행할 스크립트 ID
            context (Dict): 실행 컨텍스트 (선택사항)
            
        Returns:
            Dict[str, Any]: 실행 결과
        """
        execution_start = datetime.now()
        
        try:
            with self._execution_lock:
                # 스크립트 로드
                script_data = self._load_script(script_id)
                if not script_data:
                    return {
                        'success': False,
                        'error': f'스크립트 ID {script_id}를 찾을 수 없습니다.'
                    }
                
                # 실행 로그 시작 기록
                log_id = self._start_execution_log(script_id, context)
                
                try:
                    # AST로부터 실행
                    ast = script_data['ast']
                    variables = {**script_data['variables'], **(context or {})}
                    
                    # 인터프리터 실행
                    execution_result = self.interpreter.execute(ast, variables)
                    
                    execution_end = datetime.now()
                    execution_time = (execution_end - execution_start).total_seconds() * 1000
                    
                    # 실행 로그 완료 기록
                    self._finish_execution_log(log_id, True, execution_time, execution_result)
                    
                    # 성능 통계 업데이트
                    self._update_performance_stats(script_id, execution_time, True)
                    
                    logger.info(f"스크립트 실행 완료: ID {script_id}, 실행시간 {execution_time:.2f}ms")
                    
                    return {
                        'success': True,
                        'result': execution_result,
                        'execution_time_ms': execution_time,
                        'log_id': log_id
                    }
                    
                except Exception as e:
                    execution_end = datetime.now()
                    execution_time = (execution_end - execution_start).total_seconds() * 1000
                    error_message = str(e)
                    
                    # 실행 로그 오류 기록
                    self._finish_execution_log(log_id, False, execution_time, None, error_message)
                    
                    # 성능 통계 업데이트 (실패)
                    self._update_performance_stats(script_id, execution_time, False)
                    
                    logger.error(f"스크립트 실행 실패: ID {script_id}, 오류: {error_message}")
                    
                    return {
                        'success': False,
                        'error': f'스크립트 실행 중 오류: {error_message}',
                        'execution_time_ms': execution_time,
                        'log_id': log_id
                    }
                    
        except Exception as e:
            logger.error(f"스크립트 실행 준비 실패: {str(e)}")
            return {
                'success': False,
                'error': f'스크립트 실행 준비 중 오류: {str(e)}'
            }
    
    def get_script_templates(self, category: str = None, game_title: str = None) -> List[Dict]:
        """
        스크립트 템플릿 목록을 가져오는 함수
        
        Args:
            category (str): 카테고리 필터 (선택사항)
            game_title (str): 게임 타이틀 필터 (선택사항)
            
        Returns:
            List[Dict]: 템플릿 목록
        """
        try:
            conn = db_manager.get_connection()
            cursor = conn.cursor()
            
            query = "SELECT * FROM script_templates WHERE is_public = TRUE"
            params = []
            
            if category:
                query += " AND category = ?"
                params.append(category)
            
            if game_title:
                query += " AND game_title = ?"
                params.append(game_title)
            
            query += " ORDER BY popularity_score DESC, rating_average DESC"
            
            cursor.execute(query, params)
            templates = cursor.fetchall()
            conn.close()
            
            # 컬럼명과 함께 딕셔너리로 변환
            columns = [description[0] for description in cursor.description]
            result = []
            for template in templates:
                template_dict = dict(zip(columns, template))
                # JSON 필드 파싱
                if template_dict.get('parameters'):
                    template_dict['parameters'] = json.loads(template_dict['parameters'])
                result.append(template_dict)
            
            return result
            
        except Exception as e:
            logger.error(f"템플릿 목록 조회 실패: {str(e)}")
            return []
    
    def _generate_security_hash(self, script_code: str) -> str:
        """스크립트 보안 해시 생성"""
        return hashlib.sha256(script_code.encode('utf-8')).hexdigest()
    
    def _analyze_dependencies(self, ast) -> List[str]:
        """AST에서 의존성 분석"""
        # 기본 구현: 향후 확장 가능
        dependencies = []
        if hasattr(ast, 'get_dependencies'):
            dependencies = ast.get_dependencies()
        return dependencies
    
    def _count_ast_nodes(self, ast) -> int:
        """AST 노드 수 계산"""
        if hasattr(ast, 'count_nodes'):
            return ast.count_nodes()
        return 1
    
    def _estimate_execution_time(self, ast) -> float:
        """예상 실행 시간 계산 (ms)"""
        # 기본 추정: 노드 수 * 평균 실행 시간
        node_count = self._count_ast_nodes(ast)
        return node_count * 50  # 노드당 50ms 추정
    
    def _load_script(self, script_id: int) -> Optional[Dict]:
        """스크립트 데이터 로드 (캐시 우선)"""
        if script_id in self._script_cache:
            return self._script_cache[script_id]
        
        try:
            conn = db_manager.get_connection()
            cursor = conn.cursor()
            
            cursor.execute('''
                SELECT script_code, ast_tree, variables, dependencies
                FROM custom_scripts 
                WHERE id = ? AND is_validated = TRUE
            ''', (script_id,))
            
            result = cursor.fetchone()
            conn.close()
            
            if result:
                script_code, ast_json, variables_json, dependencies_json = result
                
                # AST 재생성 (JSON에서 복원은 복잡하므로 재파싱)
                tokens = self.lexer.tokenize(script_code)
                ast = self.parser.parse(tokens)
                
                script_data = {
                    'ast': ast,
                    'code': script_code,
                    'variables': json.loads(variables_json) if variables_json else {},
                    'dependencies': json.loads(dependencies_json) if dependencies_json else []
                }
                
                # 캐시에 저장
                self._script_cache[script_id] = script_data
                return script_data
            
            return None
            
        except Exception as e:
            logger.error(f"스크립트 로드 실패: {str(e)}")
            return None
    
    def _start_execution_log(self, script_id: int, context: Dict = None) -> int:
        """실행 로그 시작 기록"""
        try:
            conn = db_manager.get_connection()
            cursor = conn.cursor()
            
            # 매크로 ID 조회
            cursor.execute('SELECT macro_id FROM custom_scripts WHERE id = ?', (script_id,))
            result = cursor.fetchone()
            macro_id = result[0] if result else None
            
            cursor.execute('''
                INSERT INTO script_execution_logs (
                    macro_id, script_id, input_parameters, execution_start
                ) VALUES (?, ?, ?, ?)
            ''', (
                macro_id,
                script_id,
                json.dumps(context or {}),
                datetime.now().isoformat()
            ))
            
            log_id = cursor.lastrowid
            conn.commit()
            conn.close()
            
            return log_id
            
        except Exception as e:
            logger.error(f"실행 로그 시작 기록 실패: {str(e)}")
            return 0
    
    def _finish_execution_log(self, log_id: int, success: bool, execution_time: float,
                            result: Any = None, error_message: str = None):
        """실행 로그 완료 기록"""
        try:
            conn = db_manager.get_connection()
            cursor = conn.cursor()
            
            cursor.execute('''
                UPDATE script_execution_logs 
                SET execution_end = ?, execution_time_ms = ?, success = ?,
                    output_result = ?, error_message = ?
                WHERE id = ?
            ''', (
                datetime.now().isoformat(),
                int(execution_time),
                success,
                json.dumps(result) if result else None,
                error_message,
                log_id
            ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"실행 로그 완료 기록 실패: {str(e)}")
    
    def _update_performance_stats(self, script_id: int, execution_time: float, success: bool):
        """성능 통계 업데이트"""
        try:
            # 이 부분은 별도 스레드에서 비동기로 처리하는 것이 좋음
            threading.Thread(
                target=self._async_update_performance_stats,
                args=(script_id, execution_time, success)
            ).start()
            
        except Exception as e:
            logger.error(f"성능 통계 업데이트 실패: {str(e)}")
    
    def _async_update_performance_stats(self, script_id: int, execution_time: float, success: bool):
        """비동기 성능 통계 업데이트"""
        try:
            conn = db_manager.get_connection()
            cursor = conn.cursor()
            
            # 기존 통계 조회
            cursor.execute('''
                SELECT total_executions, average_execution_time, min_execution_time,
                       max_execution_time, success_rate
                FROM script_performance_analysis 
                WHERE script_id = ?
                ORDER BY analysis_date DESC LIMIT 1
            ''', (script_id,))
            
            existing_stats = cursor.fetchone()
            
            if existing_stats:
                # 기존 통계 업데이트
                total_exec, avg_time, min_time, max_time, success_rate = existing_stats
                
                new_total = total_exec + 1
                new_avg = ((avg_time * total_exec) + execution_time) / new_total
                new_min = min(min_time, execution_time) if min_time else execution_time
                new_max = max(max_time, execution_time) if max_time else execution_time
                
                # 성공률 계산
                current_success_count = int((success_rate / 100.0) * total_exec)
                new_success_count = current_success_count + (1 if success else 0)
                new_success_rate = (new_success_count / new_total) * 100
                
                cursor.execute('''
                    UPDATE script_performance_analysis 
                    SET total_executions = ?, average_execution_time = ?,
                        min_execution_time = ?, max_execution_time = ?,
                        success_rate = ?, analysis_date = ?
                    WHERE script_id = ?
                ''', (
                    new_total, new_avg, new_min, new_max, new_success_rate,
                    datetime.now().isoformat(), script_id
                ))
            else:
                # 새 통계 생성
                cursor.execute('''
                    INSERT INTO script_performance_analysis (
                        script_id, total_executions, average_execution_time,
                        min_execution_time, max_execution_time, success_rate
                    ) VALUES (?, ?, ?, ?, ?, ?)
                ''', (
                    script_id, 1, execution_time, execution_time, execution_time,
                    100.0 if success else 0.0
                ))
            
            conn.commit()
            conn.close()
            
        except Exception as e:
            logger.error(f"비동기 성능 통계 업데이트 실패: {str(e)}")


# 전역 서비스 인스턴스
custom_script_service = CustomScriptService()
