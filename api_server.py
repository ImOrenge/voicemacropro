from flask import Flask, request, jsonify
from flask_cors import CORS
from macro_service import macro_service
from voice_recognition_service import get_voice_recognition_service
import json

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
        # 매크로 삭제
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
        JSON: 처리 결과
    """
    try:
        macro_service.increment_usage_count(macro_id)
        return jsonify({
            'success': True,
            'message': '사용 횟수가 증가되었습니다'
        }), 200
        
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e),
            'message': '사용 횟수 증가 실패'
        }), 500

# ========== 음성 인식 관련 API 엔드포인트 ==========

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