# -*- coding: utf-8 -*-
from flask import Flask, request, jsonify
from flask_cors import CORS
from macro_service import macro_service
from voice_recognition_service import get_voice_recognition_service
from whisper_service import whisper_service
from macro_execution_service import macro_execution_service
import json
import numpy as np
import base64
import asyncio
import threading
from database import DatabaseManager

# Flask 애플리케이션 초기화
app = Flask(__name__)
CORS(app)  # CORS 설정 (프론트엔드와의 통신을 위해)

@app.route('/api/macros', methods=['GET'])
def get_macros():
    """
    모든 매크로를 조회하는 API 엔드포인트
    GET 요청을 처리하여 매크로 목록을 반환합니다.
    
    쿼리 파라미터:
    - search: 검색어 (선택사항)
    - sort: 정렬 기준 (name, created_at, usage_count)
    
    Returns:
        JSON: 매크로 목록
    """
    try:
        # 쿼리 파라미터 추출
        search_term = request.args.get('search', None)
        sort_by = request.args.get('sort', 'name')
        
        # 매크로 목록 조회
        macros = macro_service.get_all_macros(search_term, sort_by)
        
        return jsonify({
            'success': True,
            'data': macros,
            'message': '매크로 목록 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 목록 조회 실패'
        }), 500

@app.route('/api/macros/<int:macro_id>', methods=['GET'])
def get_macro(macro_id):
    """
    특정 매크로를 조회하는 API 엔드포인트
    
    Args:
        macro_id (int): 조회할 매크로 ID
        
    Returns:
        JSON: 매크로 정보
    """
    try:
        macro = macro_service.get_macro_by_id(macro_id)
        
        if macro:
            return jsonify({
                'success': True,
                'data': macro,
                'message': '매크로 조회 성공'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '매크로를 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 조회 실패'
        }), 500

@app.route('/api/macros', methods=['POST'])
def create_macro():
    """
    새로운 매크로를 생성하는 API 엔드포인트
    
    요청 본문:
        name (str): 매크로 이름
        voice_command (str): 음성 명령어
        action_type (str): 동작 타입
        key_sequence (str): 키 시퀀스
        settings (dict): 추가 설정 (선택사항)
        
    Returns:
        JSON: 생성된 매크로 ID
    """
    try:
        data = request.get_json()
        
        # 필수 필드 검증
        required_fields = ['name', 'voice_command', 'action_type', 'key_sequence']
        for field in required_fields:
            if not data.get(field):
                return jsonify({
                    'success': False,
                    'message': f'필수 필드가 누락되었습니다: {field}'
                }), 400
        
        # 매크로 생성
        macro_id = macro_service.create_macro(
            name=data['name'],
            voice_command=data['voice_command'],
            action_type=data['action_type'],
            key_sequence=data['key_sequence'],
            settings=data.get('settings')
        )
        
        return jsonify({
            'success': True,
            'data': {'id': macro_id},
            'message': '매크로가 성공적으로 생성되었습니다'
        }), 201
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 생성 실패'
        }), 500

@app.route('/api/macros/<int:macro_id>', methods=['PUT'])
def update_macro(macro_id):
    """
    기존 매크로를 수정하는 API 엔드포인트
    
    Args:
        macro_id (int): 수정할 매크로 ID
        
    요청 본문:
        수정할 필드들 (모든 필드는 선택사항)
        
    Returns:
        JSON: 수정 결과
    """
    try:
        data = request.get_json()
        
        # 매크로 수정
        success = macro_service.update_macro(
            macro_id=macro_id,
            name=data.get('name'),
            voice_command=data.get('voice_command'),
            action_type=data.get('action_type'),
            key_sequence=data.get('key_sequence'),
            settings=data.get('settings')
        )
        
        if success:
            return jsonify({
                'success': True,
                'message': '매크로가 성공적으로 수정되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '매크로를 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 수정 실패'
        }), 500

@app.route('/api/macros/<int:macro_id>/copy', methods=['POST'])
def copy_macro(macro_id):
    """
    매크로를 복사하는 API 엔드포인트
    
    Args:
        macro_id (int): 복사할 매크로 ID
        
    요청 본문:
        new_name (str): 새로운 매크로 이름 (선택사항)
        
    Returns:
        JSON: 복사된 매크로 ID
    """
    try:
        data = request.get_json()
        new_name = data.get('new_name') if data else None
        
        # 매크로 복사
        new_macro_id = macro_service.copy_macro(macro_id, new_name)
        
        if new_macro_id:
            return jsonify({
                'success': True,
                'data': {'id': new_macro_id},
                'message': '매크로가 성공적으로 복사되었습니다'
            }), 201
        else:
            return jsonify({
                'success': False,
                'message': '매크로를 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 복사 실패'
        }), 500

@app.route('/api/macros/<int:macro_id>', methods=['DELETE'])
def delete_macro(macro_id):
    """
    매크로를 삭제하는 API 엔드포인트
    
    Args:
        macro_id (int): 삭제할 매크로 ID
        
    Returns:
        JSON: 삭제 결과
    """
    try:
        success = macro_service.delete_macro(macro_id)
        
        if success:
            return jsonify({
                'success': True,
                'message': '매크로가 성공적으로 삭제되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '매크로를 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 삭제 실패'
        }), 500

@app.route('/api/macros/<int:macro_id>/usage', methods=['PUT'])
def increment_usage(macro_id):
    """
    매크로 사용 횟수를 증가시키는 API 엔드포인트
    
    Args:
        macro_id (int): 사용된 매크로 ID
        
    Returns:
        JSON: 업데이트 결과
    """
    try:
        success = macro_service.increment_usage_count(macro_id)
        
        if success:
            return jsonify({
                'success': True,
                'message': '사용 횟수가 업데이트되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '매크로를 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '사용 횟수 업데이트 실패'
        }), 500

# ========== 매크로 실행 관련 API 엔드포인트 ==========

@app.route('/api/macros/<int:macro_id>/execute', methods=['POST'])
def execute_macro(macro_id):
    """
    매크로를 실제로 실행하는 API 엔드포인트
    새로운 비동기 매크로 실행 서비스를 사용합니다.
    
    Args:
        macro_id (int): 실행할 매크로 ID
        
    Returns:
        JSON: 실행 결과
    """
    try:
        # 매크로 정보 조회
        macro = macro_service.get_macro_by_id(macro_id)
        if not macro:
            return jsonify({
                'success': False,
                'message': f'매크로를 찾을 수 없습니다: ID {macro_id}'
            }), 404
        
        # 비동기 함수를 동기 함수에서 실행하기 위한 래퍼
        def run_async_macro():
            loop = asyncio.new_event_loop()
            asyncio.set_event_loop(loop)
            try:
                result = loop.run_until_complete(macro_execution_service.execute_macro(macro))
                return result
            finally:
                loop.close()
        
        # 백그라운드 스레드에서 매크로 실행
        thread = threading.Thread(target=run_async_macro)
        thread.daemon = True
        thread.start()
        
        # 즉시 응답 반환 (비동기 실행)
        return jsonify({
            'success': True,
            'data': {
                'macro_id': macro_id,
                'macro_name': macro['name'],
                'action_type': macro['action_type'],
                'status': 'started'
            },
            'message': '매크로 실행이 시작되었습니다'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 실행 중 오류 발생'
        }), 500

@app.route('/api/macros/<int:macro_id>/stop', methods=['POST'])
def stop_macro(macro_id):
    """
    실행 중인 매크로를 중지하는 API 엔드포인트
    
    Args:
        macro_id (int): 중지할 매크로 ID
        
    Returns:
        JSON: 중지 결과
    """
    try:
        success = macro_execution_service.stop_macro(macro_id)
        
        if success:
            return jsonify({
                'success': True,
                'message': f'매크로 {macro_id}가 중지되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': f'매크로 {macro_id}가 실행 중이지 않습니다'
            }), 400
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 중지 실패'
        }), 500

@app.route('/api/macros/execution/stop-all', methods=['POST'])
def stop_all_macros():
    """
    모든 실행 중인 매크로를 중지하는 API 엔드포인트 (비상 정지)
    
    Returns:
        JSON: 중지 결과
    """
    try:
        running_macros = macro_execution_service.get_running_macros()
        stopped_count = 0
        
        for macro_id in running_macros:
            if macro_execution_service.stop_macro(macro_id):
                stopped_count += 1
        
        return jsonify({
            'success': True,
            'data': {
                'stopped_count': stopped_count,
                'total_running': len(running_macros)
            },
            'message': f'총 {stopped_count}개의 매크로가 중지되었습니다'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 중지 실패'
        }), 500

@app.route('/api/macros/execution/status', methods=['GET'])
def get_execution_status():
    """
    매크로 실행 상태를 조회하는 API 엔드포인트
    
    Returns:
        JSON: 실행 상태 정보
    """
    try:
        running_macros = macro_execution_service.get_running_macros()
        
        # 실행 중인 매크로 상세 정보 조회
        running_details = []
        for macro_id in running_macros:
            macro = macro_service.get_macro_by_id(macro_id)
            if macro:
                running_details.append({
                    'id': macro_id,
                    'name': macro['name'],
                    'action_type': macro['action_type']
                })
        
        # 토글 상태 정보 수집
        toggle_states = {}
        for macro in macro_service.get_all_macros():
            if macro['action_type'] == 'toggle':
                toggle_states[macro['id']] = macro_execution_service.get_toggle_state(macro['id'])
        
        status = {
            'service_active': True,
            'running_macros_count': len(running_macros),
            'running_macros': running_details,
            'toggle_states': toggle_states,
            'failsafe_enabled': True,  # PyAutoGUI.FAILSAFE
            'timestamp': macro_service.get_current_timestamp()
        }
        
        return jsonify({
            'success': True,
            'data': status,
            'message': '매크로 실행 상태 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 실행 상태 조회 실패'
        }), 500

@app.route('/api/macros/<int:macro_id>/toggle-state', methods=['GET'])
def get_toggle_state(macro_id):
    """
    토글 매크로의 현재 상태를 조회하는 API 엔드포인트
    
    Args:
        macro_id (int): 조회할 매크로 ID
        
    Returns:
        JSON: 토글 상태
    """
    try:
        # 매크로가 토글 타입인지 확인
        macro = macro_service.get_macro_by_id(macro_id)
        if not macro:
            return jsonify({
                'success': False,
                'message': '매크로를 찾을 수 없습니다'
            }), 404
        
        if macro['action_type'] != 'toggle':
            return jsonify({
                'success': False,
                'message': '토글 매크로가 아닙니다'
            }), 400
        
        state = macro_execution_service.get_toggle_state(macro_id)
        
        return jsonify({
            'success': True,
            'data': {
                'macro_id': macro_id,
                'macro_name': macro['name'],
                'is_on': state
            },
            'message': '토글 상태 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '토글 상태 조회 실패'
        }), 500

@app.route('/api/macros/<int:macro_id>/toggle-state', methods=['POST'])
def set_toggle_state(macro_id):
    """
    토글 매크로의 상태를 직접 설정하는 API 엔드포인트
    
    Args:
        macro_id (int): 설정할 매크로 ID
        
    요청 본문:
        state (bool): 설정할 상태 (True=ON, False=OFF)
        
    Returns:
        JSON: 설정 결과
    """
    try:
        data = request.get_json()
        state = data.get('state')
        
        if state is None:
            return jsonify({
                'success': False,
                'message': 'state 값이 필요합니다 (true 또는 false)'
            }), 400
        
        # 매크로가 토글 타입인지 확인
        macro = macro_service.get_macro_by_id(macro_id)
        if not macro:
            return jsonify({
                'success': False,
                'message': '매크로를 찾을 수 없습니다'
            }), 404
        
        if macro['action_type'] != 'toggle':
            return jsonify({
                'success': False,
                'message': '토글 매크로가 아닙니다'
            }), 400
        
        macro_execution_service.set_toggle_state(macro_id, bool(state))
        
        return jsonify({
            'success': True,
            'data': {
                'macro_id': macro_id,
                'macro_name': macro['name'],
                'is_on': bool(state)
            },
            'message': '토글 상태 설정 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '토글 상태 설정 실패'
        }), 500

# ========== 음성 인식 관련 API 엔드포인트==========

@app.route('/api/voice/devices', methods=['GET'])
def get_voice_devices():
    """
    사용 가능한 마이크 장치 목록을 반환하는 API 엔드포인트
    
    Returns:
        JSON: 마이크 장치 목록
    """
    try:
        voice_service = get_voice_recognition_service()
        devices = voice_service.get_available_devices()
        
        return jsonify({
            'success': True,
            'data': devices,
            'message': '마이크 장치 목록 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '마이크 장치 목록 조회 실패'
        }), 500

@app.route('/api/voice/device', methods=['POST'])
def set_voice_device():
    """
    마이크 장치를 설정하는 API 엔드포인트
    
    요청 본문:
        device_id (int): 설정할 장치 ID
        
    Returns:
        JSON: 설정 결과
    """
    try:
        data = request.get_json()
        device_id = data.get('device_id')
        
        if device_id is None:
            return jsonify({
                'success': False,
                'message': 'device_id가 필요합니다'
            }), 400
        
        voice_service = get_voice_recognition_service()
        success = voice_service.set_device(device_id)
        
        if success:
            return jsonify({
                'success': True,
                'message': '마이크 장치 설정 성공'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '유효하지 않은 장치 ID입니다'
            }), 400
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '마이크 장치 설정 실패'
        }), 500

@app.route('/api/voice/recording/start', methods=['POST'])
def start_voice_recording():
    """
    음성 녹음을 시작하는 API 엔드포인트
    
    Returns:
        JSON: 녹음 시작 결과
    """
    try:
        voice_service = get_voice_recognition_service()
        success = voice_service.start_recording()
        
        if success:
            return jsonify({
                'success': True,
                'message': '음성 녹음이 시작되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '음성 녹음 시작에 실패했습니다'
            }), 400
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '음성 녹음 시작 실패'
        }), 500

@app.route('/api/voice/recording/stop', methods=['POST'])
def stop_voice_recording():
    """
    음성 녹음을 중지하는 API 엔드포인트
    
    Returns:
        JSON: 녹음 중지 결과
    """
    try:
        voice_service = get_voice_recognition_service()
        success = voice_service.stop_recording()
        
        if success:
            return jsonify({
                'success': True,
                'message': '음성 녹음이 중지되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '음성 녹음 중지에 실패했습니다'
            }), 400
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '음성 녹음 중지 실패'
        }), 500

@app.route('/api/voice/status', methods=['GET'])
def get_voice_status():
    """
    음성 녹음 상태를 조회하는 API 엔드포인트
    
    Returns:
        JSON: 녹음 상태 정보
    """
    try:
        voice_service = get_voice_recognition_service()
        status = voice_service.get_recording_status()
        
        return jsonify({
            'success': True,
            'data': status,
            'message': '음성 녹음 상태 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '음성 녹음 상태 조회 실패'
        }), 500

@app.route('/api/voice/test', methods=['POST'])
def test_microphone():
    """
    마이크 테스트를 수행하는 API 엔드포인트
    
    Returns:
        JSON: 테스트 결과
    """
    try:
        voice_service = get_voice_recognition_service()
        test_result = voice_service.test_microphone()
        
        return jsonify({
            'success': test_result['success'],
            'data': test_result,
            'message': '마이크 테스트 완료' if test_result['success'] else test_result.get('error_message', '마이크 테스트 실패')
        }), 200 if test_result['success'] else 400
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '마이크 테스트 수행 실패'
        }), 500

# ==================== OpenAI Whisper 관련 API ====================

@app.route('/api/whisper/transcribe', methods=['POST'])
def whisper_transcribe():
    """
    오디오 데이터를 OpenAI Whisper API로 텍스트 변환하는 엔드포인트
    
    요청 본문:
        audio_data (str): Base64 인코딩된 오디오 데이터 (numpy array)
        
    Returns:
        JSON: 변환된 텍스트 결과
    """
    try:
        data = request.get_json()
        
        if not data or 'audio_data' not in data:
            return jsonify({
                'success': False,
                'message': 'audio_data가 필요합니다'
            }), 400
        
        # Base64 디코딩하여 numpy 배열로 변환
        try:
            audio_bytes = base64.b64decode(data['audio_data'])
            audio_array = np.frombuffer(audio_bytes, dtype=np.float32)
        except Exception as e:
            return jsonify({
                'success': False,
                'message': f'오디오 데이터 디코딩 실패: {str(e)}'
            }), 400
        
        # Whisper API로 텍스트 변환
        recognized_text = whisper_service.transcribe_audio(audio_array)
        
        if recognized_text:
            return jsonify({
                'success': True,
                'data': {
                    'recognized_text': recognized_text
                },
                'message': '음성 인식 성공'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '음성 인식에 실패했습니다'
            }), 400
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '음성 인식 처리 실패'
        }), 500

@app.route('/api/whisper/process', methods=['POST'])
def whisper_process_voice_command():
    """
    음성 명령 전체 처리 파이프라인 (음성 인식 + 매크로 매칭)
    
    요청 본문:
        audio_data (str): Base64 인코딩된 오디오 데이터
        
    Returns:
        JSON: 음성 인식 결과 및 매칭된 매크로 목록
    """
    try:
        data = request.get_json()
        
        if not data or 'audio_data' not in data:
            return jsonify({
                'success': False,
                'message': 'audio_data가 필요합니다'
            }), 400
        
        # Base64 디코딩하여 numpy 배열로 변환
        try:
            audio_bytes = base64.b64decode(data['audio_data'])
            audio_array = np.frombuffer(audio_bytes, dtype=np.float32)
        except Exception as e:
            return jsonify({
                'success': False,
                'message': f'오디오 데이터 디코딩 실패: {str(e)}'
            }), 400
        
        # 전체 음성 명령 처리 파이프라인 실행
        result = whisper_service.process_voice_command(audio_array)
        
        if result['success']:
            return jsonify({
                'success': True,
                'data': {
                    'recognized_text': result['recognized_text'],
                    'matched_macros': result['matched_macros'],
                    'processing_time': result['processing_time']
                },
                'message': '음성 명령 처리 성공'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': result.get('error', '음성 명령 처리 실패')
            }), 400
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '음성 명령 처리 실패'
        }), 500

@app.route('/api/whisper/match', methods=['POST'])
def whisper_match_macros():
    """
    텍스트와 매크로 명령어 매칭
    
    요청 본문:
        text (str): 매칭할 텍스트
        
    Returns:
        JSON: 매칭된 매크로 목록
    """
    try:
        data = request.get_json()
        
        if not data or 'text' not in data:
            return jsonify({
                'success': False,
                'message': 'text가 필요합니다'
            }), 400
        
        text = data['text'].strip()
        if not text:
            return jsonify({
                'success': False,
                'message': '빈 텍스트는 처리할 수 없습니다'
            }), 400
        
        # 매크로 매칭 수행
        matched_macros = whisper_service.find_matching_macros(text)
        
        return jsonify({
            'success': True,
            'data': {
                'input_text': text,
                'matched_macros': matched_macros,
                'match_count': len(matched_macros)
            },
            'message': f'매크로 매칭 완료: {len(matched_macros)}개 발견'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '매크로 매칭 실패'
        }), 500

@app.route('/api/whisper/status', methods=['GET'])
def whisper_status():
    """
    Whisper 서비스 상태 조회
    
    Returns:
        JSON: Whisper 서비스 상태 정보
    """
    try:
        status = whisper_service.get_service_status()
        
        return jsonify({
            'success': True,
            'data': status,
            'message': 'Whisper 서비스 상태 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': 'Whisper 서비스 상태 조회 실패'
        }), 500

@app.route('/api/voice/record-and-process', methods=['POST'])
def record_and_process_voice():
    """
    실시간 녹음된 오디오를 가져와서 Whisper로 처리
    
    요청 본문:
        duration (float): 녹음할 시간(초) - 기본값 3.0초
        
    Returns:
        JSON: 음성 인식 및 매크로 매칭 결과
    """
    try:
        data = request.get_json()
        duration = data.get('duration', 3.0) if data else 3.0
        
        # 음성 인식 서비스에서 오디오 데이터 가져오기
        voice_service = get_voice_recognition_service()
        audio_data = voice_service.get_audio_data(duration)
        
        if audio_data is None:
            return jsonify({
                'success': False,
                'message': '녹음된 오디오 데이터가 없습니다. 먼저 녹음을 시작해주세요.'
            }), 400
        
        # Whisper로 전체 처리 파이프라인 실행
        result = whisper_service.process_voice_command(audio_data)
        
        if result['success']:
            return jsonify({
                'success': True,
                'data': {
                    'recognized_text': result['recognized_text'],
                    'matched_macros': result['matched_macros'],
                    'processing_time': result['processing_time'],
                    'audio_duration': duration
                },
                'message': '음성 명령 처리 성공'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': result.get('error', '음성 명령 처리 실패')
            }), 400
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '음성 녹음 및 처리 실패'
        }), 500

@app.route('/api/health', methods=['GET'])
def health_check():
    """
    서버 상태를 확인하는 API 엔드포인트
    
    Returns:
        JSON: 서버 상태 정보
    """
    return jsonify({
        'status': 'healthy',
        'message': 'VoiceMacro API 서버가 정상 작동 중입니다'
    }), 200

if __name__ == '__main__':
    """
    메인 실행 함수
    Flask 서버를 시작합니다.
    """
    print("VoiceMacro API 서버를 시작합니다...")
    print("서버 주소: http://localhost:5000")
    print("API 문서: http://localhost:5000/api/health")
    
    # 디버그 모드로 서버 실행
    app.run(debug=True, host='0.0.0.0', port=5000) 
