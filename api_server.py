# -*- coding: utf-8 -*-
from flask import Flask, request, jsonify
from flask_cors import CORS
from macro_service import macro_service
from voice_recognition_service import get_voice_recognition_service
from whisper_service import whisper_service
from macro_execution_service import macro_execution_service
from preset_service import preset_service
from custom_script_service import custom_script_service
import json
import numpy as np
import base64
import asyncio
import threading
import os
import sqlite3
from werkzeug.utils import secure_filename
from database import DatabaseManager

# Flask 애플리케이션 초기화
app = Flask(__name__)
CORS(app)  # CORS 설정 (프론트엔드와의 통신을 위해)

# 데이터베이스 설정
db_path = "voice_macro.db"

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

# ========== 프리셋 관리 API 엔드포인트 ==========

@app.route('/api/presets', methods=['GET'])
def get_presets():
    """
    모든 프리셋을 조회하는 API 엔드포인트
    
    쿼리 파라미터:
    - search: 검색어 (선택사항)
    - favorites_only: 즐겨찾기만 조회 (true/false)
    
    Returns:
        JSON: 프리셋 목록
    """
    try:
        search_term = request.args.get('search', None)
        favorites_only = request.args.get('favorites_only', 'false').lower() == 'true'
        
        if favorites_only:
            presets = preset_service.get_favorite_presets()
        elif search_term:
            presets = preset_service.search_presets(search_term)
        else:
            presets = preset_service.get_all_presets()
        
        return jsonify({
            'success': True,
            'data': presets,
            'message': '프리셋 목록 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 목록 조회 실패'
        }), 500

@app.route('/api/presets/<int:preset_id>', methods=['GET'])
def get_preset(preset_id):
    """
    특정 프리셋을 조회하는 API 엔드포인트
    
    Args:
        preset_id (int): 조회할 프리셋 ID
        
    Returns:
        JSON: 프리셋 정보
    """
    try:
        preset = preset_service.get_preset_by_id(preset_id)
        
        if preset:
            # 포함된 매크로들의 상세 정보도 함께 조회
            macro_details = []
            for macro_id in preset['macro_ids']:
                macro = macro_service.get_macro_by_id(macro_id)
                if macro:
                    macro_details.append(macro)
            
            preset['macros'] = macro_details
            
            return jsonify({
                'success': True,
                'data': preset,
                'message': '프리셋 조회 성공'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '프리셋을 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 조회 실패'
        }), 500

@app.route('/api/presets', methods=['POST'])
def create_preset():
    """
    새로운 프리셋을 생성하는 API 엔드포인트
    
    요청 본문:
        name (str): 프리셋 이름
        description (str): 프리셋 설명 (선택사항)
        macro_ids (List[int]): 포함할 매크로 ID 목록
        is_favorite (bool): 즐겨찾기 여부 (선택사항)
        
    Returns:
        JSON: 생성된 프리셋 ID
    """
    try:
        data = request.get_json()
        
        # 필수 필드 검증
        if not data.get('name'):
            return jsonify({
                'success': False,
                'message': '프리셋 이름은 필수입니다'
            }), 400
        
        # 프리셋 생성
        preset_id = preset_service.create_preset(
            name=data['name'],
            description=data.get('description', ''),
            macro_ids=data.get('macro_ids', []),
            is_favorite=data.get('is_favorite', False)
        )
        
        return jsonify({
            'success': True,
            'data': {'id': preset_id},
            'message': '프리셋이 성공적으로 생성되었습니다'
        }), 201
        
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 생성 실패'
        }), 400
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 생성 중 오류 발생'
        }), 500

@app.route('/api/presets/<int:preset_id>', methods=['PUT'])
def update_preset(preset_id):
    """
    기존 프리셋을 수정하는 API 엔드포인트
    
    Args:
        preset_id (int): 수정할 프리셋 ID
        
    요청 본문:
        수정할 필드들 (모든 필드는 선택사항)
        
    Returns:
        JSON: 수정 결과
    """
    try:
        data = request.get_json()
        
        # 프리셋 수정
        success = preset_service.update_preset(
            preset_id=preset_id,
            name=data.get('name'),
            description=data.get('description'),
            macro_ids=data.get('macro_ids'),
            is_favorite=data.get('is_favorite')
        )
        
        if success:
            return jsonify({
                'success': True,
                'message': '프리셋이 성공적으로 수정되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '프리셋을 찾을 수 없습니다'
            }), 404
            
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 수정 실패'
        }), 400
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 수정 중 오류 발생'
        }), 500

@app.route('/api/presets/<int:preset_id>/copy', methods=['POST'])
def copy_preset(preset_id):
    """
    프리셋을 복사하는 API 엔드포인트
    
    Args:
        preset_id (int): 복사할 프리셋 ID
        
    요청 본문:
        new_name (str): 새로운 프리셋 이름 (선택사항)
        
    Returns:
        JSON: 복사된 프리셋 ID
    """
    try:
        data = request.get_json()
        new_name = data.get('new_name') if data else None
        
        # 프리셋 복사
        new_preset_id = preset_service.copy_preset(preset_id, new_name)
        
        return jsonify({
            'success': True,
            'data': {'id': new_preset_id},
            'message': '프리셋이 성공적으로 복사되었습니다'
        }), 201
            
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 복사 실패'
        }), 400
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 복사 중 오류 발생'
        }), 500

@app.route('/api/presets/<int:preset_id>', methods=['DELETE'])
def delete_preset(preset_id):
    """
    프리셋을 삭제하는 API 엔드포인트
    
    Args:
        preset_id (int): 삭제할 프리셋 ID
        
    쿼리 파라미터:
        hard_delete: 완전 삭제 여부 (true/false, 기본값: false)
        
    Returns:
        JSON: 삭제 결과
    """
    try:
        hard_delete = request.args.get('hard_delete', 'false').lower() == 'true'
        success = preset_service.delete_preset(preset_id, hard_delete)
        
        if success:
            return jsonify({
                'success': True,
                'message': '프리셋이 성공적으로 삭제되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '프리셋을 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 삭제 실패'
        }), 500

@app.route('/api/presets/<int:preset_id>/toggle-favorite', methods=['POST'])
def toggle_preset_favorite(preset_id):
    """
    프리셋 즐겨찾기 상태를 토글하는 API 엔드포인트
    
    Args:
        preset_id (int): 토글할 프리셋 ID
        
    Returns:
        JSON: 새로운 즐겨찾기 상태
    """
    try:
        new_favorite_status = preset_service.toggle_favorite(preset_id)
        
        return jsonify({
            'success': True,
            'data': {'is_favorite': new_favorite_status},
            'message': f'프리셋이 {"즐겨찾기에 추가" if new_favorite_status else "즐겨찾기에서 제거"}되었습니다'
        }), 200
            
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋을 찾을 수 없습니다'
        }), 404
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '즐겨찾기 토글 실패'
        }), 500

@app.route('/api/presets/<int:preset_id>/apply', methods=['POST'])
def apply_preset(preset_id):
    """
    프리셋을 적용하는 API 엔드포인트
    
    Args:
        preset_id (int): 적용할 프리셋 ID
        
    Returns:
        JSON: 적용 결과
    """
    try:
        success = preset_service.apply_preset(preset_id)
        
        if success:
            preset = preset_service.get_preset_by_id(preset_id)
            return jsonify({
                'success': True,
                'data': {
                    'preset_id': preset_id,
                    'preset_name': preset['name'] if preset else 'Unknown',
                    'macro_count': len(preset['macro_ids']) if preset else 0
                },
                'message': '프리셋이 성공적으로 적용되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'message': '프리셋을 찾을 수 없습니다'
            }), 404
            
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 적용 실패'
        }), 500

@app.route('/api/presets/<int:preset_id>/export', methods=['POST'])
def export_preset(preset_id):
    """
    프리셋을 JSON 파일로 내보내는 API 엔드포인트
    
    Args:
        preset_id (int): 내보낼 프리셋 ID
        
    요청 본문:
        file_path (str): 저장할 파일 경로 (선택사항)
        
    Returns:
        JSON: 내보낸 파일 경로
    """
    try:
        data = request.get_json()
        file_path = data.get('file_path') if data else None
        
        exported_file_path = preset_service.export_preset_to_json(preset_id, file_path)
        
        return jsonify({
            'success': True,
            'data': {
                'file_path': exported_file_path,
                'file_name': os.path.basename(exported_file_path)
            },
            'message': '프리셋이 성공적으로 내보내졌습니다'
        }), 200
            
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 내보내기 실패'
        }), 404
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 내보내기 중 오류 발생'
        }), 500

@app.route('/api/presets/import', methods=['POST'])
def import_preset():
    """
    JSON 파일에서 프리셋을 가져오는 API 엔드포인트
    
    요청 본문:
        file_path (str): 가져올 JSON 파일 경로
        preset_name (str): 새로운 프리셋 이름 (선택사항)
        
    Returns:
        JSON: 가져온 프리셋 ID
    """
    try:
        data = request.get_json()
        
        if not data.get('file_path'):
            return jsonify({
                'success': False,
                'message': '파일 경로가 필요합니다'
            }), 400
        
        preset_id = preset_service.import_preset_from_json(
            file_path=data['file_path'],
            preset_name=data.get('preset_name')
        )
        
        return jsonify({
            'success': True,
            'data': {'id': preset_id},
            'message': '프리셋이 성공적으로 가져와졌습니다'
        }), 201
            
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 가져오기 실패'
        }), 400
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 가져오기 중 오류 발생'
        }), 500

@app.route('/api/presets/statistics', methods=['GET'])
def get_preset_statistics():
    """
    프리셋 통계 정보를 조회하는 API 엔드포인트
    
    Returns:
        JSON: 프리셋 통계
    """
    try:
        stats = preset_service.get_preset_statistics()
        
        return jsonify({
            'success': True,
            'data': stats,
            'message': '프리셋 통계 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '프리셋 통계 조회 실패'
        }), 500

# =============================================================================
# 커스텀 스크립팅 API 엔드포인트 (MSL - Macro Scripting Language)
# =============================================================================

@app.route('/api/scripts/validate', methods=['POST'])
def validate_script():
    """
    MSL 스크립트 코드를 검증하는 API 엔드포인트
    
    요청 본문:
        script_code (str): 검증할 MSL 스크립트 코드
        
    Returns:
        JSON: 검증 결과
    """
    try:
        data = request.get_json()
        
        if not data.get('script_code'):
            return jsonify({
                'success': False,
                'message': '스크립트 코드가 필요합니다'
            }), 400
        
        validation_result = custom_script_service.validate_script(data['script_code'])
        
        return jsonify({
            'success': True,
            'data': validation_result,
            'message': '스크립트 검증 완료'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '스크립트 검증 실패'
        }), 500

@app.route('/api/scripts', methods=['GET'])
def get_custom_scripts():
    """
    커스텀 스크립트 목록을 조회하는 API 엔드포인트
    
    쿼리 파라미터:
        category (str): 카테고리 필터 (선택사항)
        game_title (str): 게임 타이틀 필터 (선택사항)
        search (str): 검색어 (선택사항)
        
    Returns:
        JSON: 스크립트 목록
    """
    try:
        # 쿼리 파라미터 추출
        category = request.args.get('category')
        game_title = request.args.get('game_title')
        search = request.args.get('search')
        
        # 데이터베이스에서 스크립트 목록 조회
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # 기본 쿼리
        base_query = """
        SELECT cs.id, m.name as macro_name, cs.script_code, m.voice_command,
               cs.created_at, cs.updated_at, cs.is_validated, spa.success_rate,
               spa.average_execution_time, m.action_type, 
               COALESCE(st.category, '기타') as category,
               COALESCE(st.game_title, '공통') as game_title,
               COALESCE(st.description, '') as description
        FROM custom_scripts cs
        JOIN macros m ON cs.macro_id = m.id
        LEFT JOIN script_templates st ON cs.id = st.id
        LEFT JOIN script_performance_analysis spa ON cs.id = spa.script_id
        WHERE 1=1
        """
        
        params = []
        
        # 필터 조건 추가
        if category and category != '':
            base_query += " AND COALESCE(st.category, '기타') = ?"
            params.append(category)
        
        if game_title and game_title != '':
            base_query += " AND COALESCE(st.game_title, '공통') = ?"
            params.append(game_title)
        
        if search and search.strip():
            base_query += " AND (m.name LIKE ? OR cs.script_code LIKE ? OR st.description LIKE ?)"
            search_term = f"%{search.strip()}%"
            params.extend([search_term, search_term, search_term])
        
        # 정렬 추가
        base_query += " ORDER BY cs.updated_at DESC"
        
        cursor.execute(base_query, params)
        scripts = cursor.fetchall()
        
        # 결과 포맷팅
        script_list = []
        for script in scripts:
            script_data = {
                'id': script[0],
                'name': script[1],
                'script_code': script[2],
                'voice_command': script[3],
                'created_at': script[4],
                'updated_at': script[5],
                'is_validated': bool(script[6]),
                'success_rate': script[7] if script[7] is not None else 100.0,
                'average_execution_time': script[8] if script[8] is not None else 0.0,
                'action_type': script[9],
                'category': script[10],
                'game_title': script[11],
                'description': script[12],
                'status_text': '검증됨' if script[6] else '미검증',
                'status_color': '#28A745' if script[6] else '#DC3545',
                'success_rate_text': f"{script[7]:.1f}%" if script[7] is not None else "N/A",
                'success_rate_color': '#28A745' if (script[7] or 100) >= 90 else '#FFC107' if (script[7] or 100) >= 70 else '#DC3545'
            }
            script_list.append(script_data)
        
        conn.close()
        
        return jsonify({
            'success': True,
            'data': script_list,
            'count': len(script_list),
            'message': '커스텀 스크립트 목록 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '스크립트 목록 조회 실패'
        }), 500

@app.route('/api/scripts', methods=['POST'])
def create_custom_script():
    """
    새로운 커스텀 스크립트를 생성하는 API 엔드포인트
    
    요청 본문:
        macro_id (int): 연결될 매크로 ID
        script_code (str): MSL 스크립트 코드
        variables (dict): 스크립트 변수 (선택사항)
        
    Returns:
        JSON: 생성된 스크립트 ID
    """
    try:
        data = request.get_json()
        
        # 필수 필드 검증
        if not data.get('macro_id'):
            return jsonify({
                'success': False,
                'message': '매크로 ID가 필요합니다'
            }), 400
        
        if not data.get('script_code'):
            return jsonify({
                'success': False,
                'message': '스크립트 코드가 필요합니다'
            }), 400
        
        # 스크립트 생성
        result = custom_script_service.create_custom_script(
            macro_id=data['macro_id'],
            script_code=data['script_code'],
            variables=data.get('variables')
        )
        
        if result['success']:
            return jsonify({
                'success': True,
                'data': {
                    'script_id': result['script_id'],
                    'validation_result': result['validation_result']
                },
                'message': '커스텀 스크립트가 성공적으로 생성되었습니다'
            }), 201
        else:
            return jsonify({
                'success': False,
                'error': result['error'],
                'message': '커스텀 스크립트 생성 실패'
            }), 400
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '커스텀 스크립트 생성 중 오류 발생'
        }), 500

@app.route('/api/scripts/<int:script_id>/execute', methods=['POST'])
def execute_custom_script(script_id):
    """
    커스텀 스크립트를 실행하는 API 엔드포인트
    
    Args:
        script_id (int): 실행할 스크립트 ID
        
    요청 본문:
        context (dict): 실행 컨텍스트 (선택사항)
        
    Returns:
        JSON: 실행 결과
    """
    try:
        data = request.get_json()
        context = data.get('context') if data else None
        
        # 비동기 실행을 위한 스레드 처리
        def run_async_script():
            return custom_script_service.execute_script(script_id, context)
        
        # 스크립트 실행
        result = run_async_script()
        
        if result['success']:
            return jsonify({
                'success': True,
                'data': {
                    'result': result['result'],
                    'execution_time_ms': result['execution_time_ms'],
                    'log_id': result['log_id']
                },
                'message': '스크립트가 성공적으로 실행되었습니다'
            }), 200
        else:
            return jsonify({
                'success': False,
                'error': result['error'],
                'execution_time_ms': result.get('execution_time_ms', 0),
                'message': '스크립트 실행 실패'
            }), 400
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '스크립트 실행 중 오류 발생'
        }), 500

@app.route('/api/scripts/<int:script_id>/performance', methods=['GET'])
def get_script_performance(script_id):
    """
    스크립트 성능 통계를 조회하는 API 엔드포인트
    
    Args:
        script_id (int): 조회할 스크립트 ID
        
    Returns:
        JSON: 성능 통계
    """
    try:
        performance_stats = custom_script_service.get_script_performance_stats(script_id)
        
        return jsonify({
            'success': True,
            'data': performance_stats,
            'message': '스크립트 성능 통계 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '성능 통계 조회 실패'
        }), 500

@app.route('/api/scripts/templates', methods=['GET'])
def get_script_templates():
    """
    스크립트 템플릿 목록을 조회하는 API 엔드포인트
    
    쿼리 파라미터:
        category (str): 카테고리 필터 (선택사항)
        game_title (str): 게임 타이틀 필터 (선택사항)
        
    Returns:
        JSON: 템플릿 목록
    """
    try:
        category = request.args.get('category')
        game_title = request.args.get('game_title')
        
        templates = custom_script_service.get_script_templates(category, game_title)
        
        return jsonify({
            'success': True,
            'data': templates,
            'message': '스크립트 템플릿 목록 조회 성공'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '템플릿 목록 조회 실패'
        }), 500

@app.route('/api/scripts/templates', methods=['POST'])
def create_script_template():
    """
    새로운 스크립트 템플릿을 생성하는 API 엔드포인트
    
    요청 본문:
        name (str): 템플릿 이름
        description (str): 템플릿 설명
        category (str): 카테고리
        template_code (str): 템플릿 코드
        game_title (str): 게임 타이틀 (선택사항)
        parameters (dict): 매개변수 정의 (선택사항)
        difficulty_level (int): 난이도 레벨 (선택사항)
        author_name (str): 작성자 (선택사항)
        is_official (bool): 공식 템플릿 여부 (선택사항)
        
    Returns:
        JSON: 생성된 템플릿 ID
    """
    try:
        data = request.get_json()
        
        # 필수 필드 검증
        required_fields = ['name', 'description', 'category', 'template_code']
        for field in required_fields:
            if not data.get(field):
                return jsonify({
                    'success': False,
                    'message': f'필수 필드가 누락되었습니다: {field}'
                }), 400
        
        # 템플릿 생성
        result = custom_script_service.create_script_template(
            name=data['name'],
            description=data['description'],
            category=data['category'],
            template_code=data['template_code'],
            game_title=data.get('game_title'),
            parameters=data.get('parameters', {}),
            difficulty_level=data.get('difficulty_level', 1),
            author_name=data.get('author_name'),
            is_official=data.get('is_official', False)
        )
        
        if result['success']:
            return jsonify({
                'success': True,
                'data': {'template_id': result['template_id']},
                'message': '스크립트 템플릿이 성공적으로 생성되었습니다'
            }), 201
        else:
            return jsonify({
                'success': False,
                'error': result['error'],
                'message': '템플릿 생성 실패'
            }), 400
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '템플릿 생성 중 오류 발생'
        }), 500

@app.route('/api/scripts/test', methods=['POST'])
def test_script_syntax():
    """
    MSL 스크립트 문법을 테스트하는 API 엔드포인트
    (실제 실행 없이 구문 분석만 수행)
    
    요청 본문:
        script_code (str): 테스트할 스크립트 코드
        
    Returns:
        JSON: 구문 분석 결과
    """
    try:
        data = request.get_json()
        
        if not data.get('script_code'):
            return jsonify({
                'success': False,
                'message': '스크립트 코드가 필요합니다'
            }), 400
        
        # 기본 문법 검증
        validation_result = custom_script_service.validate_script(data['script_code'])
        
        if validation_result['valid']:
            # 추가 분석 정보 제공
            from msl_lexer import MSLLexer
            from msl_parser import MSLParser
            
            lexer = MSLLexer()
            parser = MSLParser()
            
            tokens = lexer.tokenize(data['script_code'])
            ast = parser.parse(tokens)
            
            # AST 정보 수집
            analysis_result = {
                'valid': True,
                'tokens': [{'type': token.type.name, 'value': token.value} for token in tokens[:10]],  # 처음 10개만
                'ast_summary': str(ast)[:200] + '...' if len(str(ast)) > 200 else str(ast),
                'complexity_score': validation_result.get('ast_nodes', 1),
                'estimated_execution_time': validation_result.get('estimated_execution_time', 0)
            }
            
            return jsonify({
                'success': True,
                'data': analysis_result,
                'message': '스크립트 문법 테스트 성공'
            }), 200
        else:
            return jsonify({
                'success': False,
                'data': validation_result,
                'message': '스크립트 문법 오류 발견'
            }), 400
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '스크립트 테스트 중 오류 발생'
        }), 500

@app.route('/api/health', methods=['GET'])
def health_check():
    """
    서버 상태를 확인하는 API 엔드포인트
    
    Returns:
        JSON: 서버 상태 정보
    """
    try:
        # 데이터베이스 연결 테스트
        db_manager = DatabaseManager()
        macros_count = len(macro_service.get_all_macros())
        presets_count = len(preset_service.get_all_presets())
        
        return jsonify({
            'success': True,
            'data': {
                'status': 'healthy',
                'service': 'VoiceMacro Pro API',
                'version': '1.0.0',
                'macros_count': macros_count,
                'presets_count': presets_count,
                'whisper_status': 'ready' if whisper_service else 'not_initialized'
            },
            'message': '서버가 정상적으로 동작 중입니다'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '서버 상태 확인 실패'
        }), 500

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
