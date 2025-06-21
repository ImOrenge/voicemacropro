# VoiceMacro Pro GPT-4o 실시간 음성인식 시스템 개발명세서

## 📋 프롬프트 엔지니어링 기반 개발 가이드

### 🎯 Role-based Prompting (역할 기반 접근법)

당신은 **VoiceMacro Pro의 시니어 음성인식 개발자**입니다. 다음 역할을 수행해야 합니다:

```
역할 정의:
- OpenAI GPT-4o 실시간 트랜스크립션 전문가
- Python 백엔드 및 C# WPF 프론트엔드 아키텍트  
- 게임 매크로 음성 명령 최적화 엔지니어
- 실시간 WebSocket 통신 설계자
```

### 🔄 Chain of Thought (단계별 사고 과정)

#### 1단계: 현재 상황 분석
```markdown
기존 시스템: Whisper 기반 음성인식 → 한계점 존재
├── 지연시간: 음성 파일 업로드 후 처리 (3-5초)
├── 정확도: 게임 전문 용어 인식 한계
└── 실시간성: 배치 처리 방식의 한계

목표 시스템: GPT-4o 실시간 트랜스크립션
├── 지연시간: 실시간 스트리밍 (100ms 미만)
├── 정확도: 게임 명령어 특화 프롬프트 적용
└── 실시간성: WebSocket 기반 연속 처리
```

#### 2단계: 기술적 요구사항 도출
```python
# 기술 스택 결정 과정
if 실시간_처리_필요:
    transcription_method = "GPT-4o-transcribe"  # WebSocket 기반
    audio_format = "pcm16"  # 24kHz 샘플링
    buffer_size = 100  # ms 단위
else:
    transcription_method = "Whisper"  # 기존 방식 유지
```

#### 3단계: 아키텍처 설계 결정
```mermaid
실시간 음성인식 플로우:
마이크 입력 → 오디오 버퍼링 → Base64 인코딩 → WebSocket 전송 
→ GPT-4o 처리 → 텍스트 반환 → 매크로 매칭 → 키보드 실행
```

### 📚 Few-shot Learning (예시 기반 학습)

#### 예시 1: 음성 명령어 처리 패턴
```python
# 입력 음성: "공격 스킬 사용해"
# GPT-4o 트랜스크립션 결과: "공격 스킬 사용해"
# 신뢰도: 0.95
# 매칭된 매크로: attack_skill_combo
# 실행 결과: Q키 → 0.1초 대기 → W키 → E키

voice_command_examples = {
    "게임 공격": {
        "input_audio": "공격해, 어택, 때려",
        "transcription": "공격해",
        "confidence": 0.92,
        "matched_macro": "basic_attack",
        "key_sequence": "Space"
    },
    "스킬 콤보": {
        "input_audio": "스킬 콤보 써, 궁극기 발동",
        "transcription": "스킬 콤보 써",
        "confidence": 0.88,
        "matched_macro": "skill_combo",
        "key_sequence": "Q,W,E,R"
    },
    "이동 명령": {
        "input_audio": "앞으로 가, 뒤로 물러서",
        "transcription": "앞으로 가",
        "confidence": 0.91,
        "matched_macro": "move_forward",
        "key_sequence": "W[1000]"
    }
}
```

#### 예시 2: 에러 처리 패턴
```python
error_handling_examples = {
    "낮은 신뢰도": {
        "confidence": 0.65,
        "action": "재시도 요청",
        "message": "음성을 다시 말씀해 주세요"
    },
    "연결 실패": {
        "error": "WebSocket connection failed",
        "fallback": "Whisper 모델로 전환",
        "retry_count": 3
    },
    "API 한도 초과": {
        "error": "Rate limit exceeded", 
        "action": "대기 후 재시도",
        "wait_time": "60초"
    }
}
```

### 🔧 Task Decomposition (작업 분해)

#### 주요 작업 1: Python 백엔드 구현
```python
class GPT4oImplementationTasks:
    """GPT-4o 구현을 위한 세부 작업 분해"""
    
    def task_1_create_transcription_service(self):
        """
        작업 1: GPT-4o 트랜스크립션 서비스 생성
        └── 1.1: WebSocket 연결 관리자 구현
        └── 1.2: 오디오 데이터 인코딩/디코딩
        └── 1.3: 실시간 이벤트 핸들러 구현
        └── 1.4: 신뢰도 계산 알고리즘 구현
        """
        subtasks = [
            "WebSocket 연결 및 인증 처리",
            "PCM16 오디오 데이터 Base64 인코딩",
            "delta/completed 이벤트 분리 처리", 
            "logprobs 기반 신뢰도 계산"
        ]
        return subtasks
    
    def task_2_integrate_voice_service(self):
        """
        작업 2: 기존 음성 서비스 통합
        └── 2.1: voice_service.py 리팩토링
        └── 2.2: 매크로 매칭 서비스 연동
        └── 2.3: 에러 핸들링 및 폴백 구현
        └── 2.4: 로깅 및 모니터링 추가
        """
        pass
    
    def task_3_websocket_api_development(self):
        """
        작업 3: WebSocket API 개발
        └── 3.1: Flask-SocketIO 서버 설정
        └── 3.2: 실시간 오디오 스트리밍 엔드포인트
        └── 3.3: 트랜스크립션 결과 브로드캐스팅
        └── 3.4: 연결 상태 관리 및 재연결 로직
        """
        pass
```

#### 주요 작업 2: C# WPF 프론트엔드 구현
```csharp
public class CSharpImplementationTasks
{
    /*
    작업 4: C# 음성 인식 래퍼 서비스 업데이트
    └── 4.1: SocketIOClient 통합
    └── 4.2: NAudio 기반 실시간 오디오 캡처
    └── 4.3: 비동기 이벤트 핸들링 구현
    └── 4.4: UI 상태 관리 및 사용자 피드백
    
    작업 5: UI 컴포넌트 업데이트
    └── 5.1: 실시간 트랜스크립션 표시
    └── 5.2: 신뢰도 및 연결 상태 인디케이터
    └── 5.3: 음성 입력 레벨 시각화
    └── 5.4: 에러 메시지 및 재시도 UI
    */
}
```

### ⚙️ Constraint-based Prompting (제약 조건 명시)

#### 기술적 제약사항
```yaml
# 필수 준수사항
constraints:
  performance:
    - 음성 인식 지연시간: 100ms 이하
    - 메모리 사용량: 512MB 이하  
    - CPU 사용률: 30% 이하
    
  compatibility:
    - Python 3.8+ 호환성 유지
    - .NET Framework 4.8+ 지원
    - Windows 10/11 환경 최적화
    
  security:
    - OpenAI API 키 환경변수 저장 필수
    - 음성 데이터 로컬 암호화
    - HTTPS/WSS 연결만 허용
    
  reliability:
    - 99.5% 이상 가동률 목표
    - 자동 재연결 및 폴백 메커니즘
    - 실시간 헬스체크 구현
```

#### 개발 제약사항
```python
# 코딩 표준 및 규칙
DEVELOPMENT_CONSTRAINTS = {
    "코드 품질": {
        "함수별 주석": "모든 public 함수에 docstring 필수",
        "타입 힌팅": "Python 3.8+ typing 모듈 사용",
        "에러 처리": "모든 외부 API 호출에 try-catch 적용",
        "로깅": "DEBUG/INFO/WARNING/ERROR 레벨 구분"
    },
    "성능 최적화": {
        "비동기 처리": "I/O 작업은 async/await 패턴 사용",
        "메모리 관리": "큰 객체는 명시적 해제",
        "캐싱": "자주 사용되는 데이터 메모리 캐싱"
    },
    "테스트": {
        "단위 테스트": "80% 이상 코드 커버리지",
        "통합 테스트": "실제 음성 데이터로 E2E 테스트",
        "성능 테스트": "부하 테스트 및 메모리 누수 검사"
    }
}
```

### 📋 Template-based Prompting (템플릿 기반 구현)

#### 템플릿 1: GPT-4o 서비스 클래스 구조
```python
# 템플릿: backend/services/gpt4o_transcription_service.py
class GPT4oTranscriptionService:
    """
    GPT-4o 실시간 트랜스크립션 서비스 템플릿
    
    Template Parameters:
    - api_key: OpenAI API 키
    - language: 트랜스크립션 언어 (기본값: 'ko')
    - model: 사용할 모델 (기본값: 'gpt-4o-transcribe')
    """
    
    def __init__(self, api_key: str, language: str = "ko"):
        """초기화 템플릿"""
        self.api_key = api_key
        self.language = language
        self.session_config = self._create_session_config()
        
    def _create_session_config(self) -> dict:
        """세션 설정 템플릿 - 게임 명령어 최적화"""
        return {
            "type": "transcription_session.update",
            "input_audio_format": "pcm16",
            "input_audio_transcription": {
                "model": "gpt-4o-transcribe",
                "prompt": self._get_gaming_prompt(),  # 게임 특화 프롬프트
                "language": self.language
            },
            "turn_detection": {
                "type": "server_vad",
                "threshold": 0.5,  # 음성 감지 임계값
                "prefix_padding_ms": 300,
                "silence_duration_ms": 500
            },
            "input_audio_noise_reduction": {
                "type": "near_field"  # 게임용 헤드셋 최적화
            },
            "include": ["item.input_audio_transcription.logprobs"]
        }
    
    def _get_gaming_prompt(self) -> str:
        """게임 명령어 특화 프롬프트 템플릿"""
        return """
        게임 음성 명령어를 정확하게 인식해주세요. 
        다음과 같은 패턴을 우선적으로 인식합니다:
        - 공격 관련: 공격, 어택, 때려, 치기
        - 스킬 관련: 스킬, 기술, 마법, 궁극기
        - 이동 관련: 앞으로, 뒤로, 좌측, 우측, 점프
        - 아이템 관련: 포션, 회복, 아이템, 사용
        게임 전문 용어와 짧은 명령어를 정확히 인식해주세요.
        """
```

#### 템플릿 2: C# 이벤트 핸들링 구조
```csharp
// 템플릿: VoiceMacroPro/Services/VoiceRecognitionWrapperService.cs
public class VoiceRecognitionWrapperService : IDisposable
{
    // Template Fields
    private SocketIOClient.SocketIO _socket;
    private WaveInEvent _waveIn;
    private readonly string _serverUrl;
    
    // Template Events
    public event EventHandler<TranscriptionResult> TranscriptionReceived;
    public event EventHandler<VoiceError> ErrorOccurred;
    public event EventHandler<ConnectionStatus> ConnectionChanged;
    
    // Template Constructor
    public VoiceRecognitionWrapperService(string serverUrl = "http://localhost:5000")
    {
        _serverUrl = serverUrl;
        InitializeComponents();
    }
    
    // Template Method: 연결 초기화
    public async Task<bool> InitializeAsync()
    {
        try
        {
            await ConnectToServer();
            SetupEventHandlers();
            InitializeAudioCapture();
            return true;
        }
        catch (Exception ex)
        {
            HandleInitializationError(ex);
            return false;
        }
    }
    
    // Template Method: 실시간 오디오 처리
    private async void OnAudioDataAvailable(object sender, WaveInEventArgs e)
    {
        if (_isRecording && _socket.Connected)
        {
            var audioData = ProcessAudioBuffer(e.Buffer, e.BytesRecorded);
            await SendAudioToServer(audioData);
        }
    }
}
```

### 🧪 Implementation Verification (구현 검증 템플릿)

#### 검증 체크리스트
```markdown
## 구현 완료 검증 항목

### Phase 1: 백엔드 구현 검증
- [ ] GPT4oTranscriptionService 클래스 구현 완료
- [ ] WebSocket 연결 및 인증 정상 동작
- [ ] 실시간 오디오 스트리밍 처리 확인
- [ ] 트랜스크립션 결과 콜백 정상 동작
- [ ] 에러 핸들링 및 재연결 로직 테스트

### Phase 2: 프론트엔드 구현 검증  
- [ ] VoiceRecognitionWrapperService 업데이트 완료
- [ ] SocketIOClient 통합 및 통신 확인
- [ ] NAudio 실시간 녹음 기능 동작
- [ ] UI 이벤트 처리 및 상태 표시 확인
- [ ] 사용자 피드백 및 에러 메시지 표시

### Phase 3: 통합 테스트 검증
- [ ] 전체 파이프라인 E2E 테스트 통과
- [ ] 다양한 음성 명령어 인식 정확도 확인
- [ ] 성능 요구사항 충족 (100ms 이하 지연)
- [ ] 안정성 테스트 (장시간 연속 사용)
- [ ] 보안 검증 (API 키 안전한 저장)
```

### 🚀 Deployment Strategy (배포 전략 템플릿)

```python
# 배포 단계별 전략
deployment_phases = {
    "Phase 1 - 개발 환경": {
        "목표": "GPT-4o 기본 기능 구현 및 테스트",
        "기간": "1주",
        "성공 기준": [
            "GPT-4o API 연결 성공",
            "기본 음성 인식 동작 확인",
            "간단한 매크로 실행 테스트"
        ]
    },
    "Phase 2 - 스테이징 환경": {
        "목표": "성능 최적화 및 안정성 확보", 
        "기간": "1주",
        "성공 기준": [
            "지연시간 100ms 이하 달성",
            "24시간 연속 동작 안정성",
            "다양한 게임 시나리오 테스트"
        ]
    },
    "Phase 3 - 프로덕션 배포": {
        "목표": "사용자 대상 점진적 배포",
        "기간": "1주", 
        "성공 기준": [
            "99.5% 가동률 달성",
            "사용자 만족도 90% 이상",
            "기존 Whisper 대비 성능 향상"
        ]
    }
}
```

## 📖 구체적 구현 가이드

### 1. Python 백엔드 구현

#### 1.1 GPT-4o 트랜스크립션 서비스 생성
```python
# backend/services/gpt4o_transcription_service.py
import asyncio
import websockets
import json
import base64
import logging
from typing import Optional, Callable, Dict, Any
from datetime import datetime

class GPT4oTranscriptionService:
    """실시간 음성 인식을 위한 GPT-4o 트랜스크립션 서비스"""
    
    def __init__(self, api_key: str):
        """
        GPT-4o 트랜스크립션 서비스 초기화
        
        Args:
            api_key (str): OpenAI API 키
        """
        self.api_key = api_key
        self.websocket: Optional[websockets.WebSocketServerProtocol] = None
        self.session_id: Optional[str] = None
        self.is_connected = False
        self.transcription_callback: Optional[Callable] = None
        self.logger = logging.getLogger(__name__)
        
        # 게임 명령어 최적화를 위한 세션 설정
        self.session_config = {
            "type": "transcription_session.update",
            "input_audio_format": "pcm16",  # 24kHz PCM16 오디오
            "input_audio_transcription": {
                "model": "gpt-4o-transcribe",
                "prompt": self._get_gaming_optimized_prompt(),
                "language": "ko"  # 한국어 게임 명령어
            },
            "turn_detection": {
                "type": "server_vad",
                "threshold": 0.5,
                "prefix_padding_ms": 300,
                "silence_duration_ms": 500
            },
            "input_audio_noise_reduction": {
                "type": "near_field"  # 헤드셋 마이크 최적화
            },
            "include": ["item.input_audio_transcription.logprobs"]
        }
    
    def _get_gaming_optimized_prompt(self) -> str:
        """게임 명령어 인식 최적화를 위한 프롬프트"""
        return """
        게임 플레이어의 음성 명령어를 정확하게 인식하세요.
        주요 명령어 패턴:
        - 공격: 공격, 어택, 때려, 치기, 공격해
        - 스킬: 스킬, 기술, 마법, 궁극기, 스페셜
        - 이동: 앞으로, 뒤로, 좌측, 우측, 점프, 달려
        - 아이템: 포션, 회복, 아이템, 사용, 먹기
        - 방어: 방어, 막기, 피하기, 회피
        짧고 명확한 게임 명령어를 우선 인식하세요.
        """
    
    async def connect(self) -> bool:
        """OpenAI Realtime API에 WebSocket 연결"""
        try:
            uri = "wss://api.openai.com/v1/realtime"
            headers = {
                "Authorization": f"Bearer {self.api_key}",
                "OpenAI-Beta": "realtime=v1"
            }
            
            self.websocket = await websockets.connect(uri, extra_headers=headers)
            await self._initialize_session()
            self.is_connected = True
            self.logger.info("GPT-4o 트랜스크립션 서비스 연결 성공")
            return True
            
        except Exception as e:
            self.logger.error(f"GPT-4o 서비스 연결 실패: {e}")
            return False
    
    async def _initialize_session(self):
        """트랜스크립션 세션 초기화"""
        await self.websocket.send(json.dumps(self.session_config))
        
        # 세션 생성 확인 대기
        response = await self.websocket.recv()
        session_data = json.loads(response)
        
        if session_data.get("type") == "transcription_session.created":
            self.session_id = session_data.get("id")
            self.logger.info(f"트랜스크립션 세션 생성됨: {self.session_id}")
    
    async def send_audio_chunk(self, audio_data: bytes):
        """실시간 오디오 데이터 전송"""
        if not self.is_connected or not self.websocket:
            raise ConnectionError("트랜스크립션 서비스에 연결되지 않음")
        
        # 오디오 데이터를 Base64로 인코딩
        audio_base64 = base64.b64encode(audio_data).decode('utf-8')
        
        message = {
            "type": "input_audio_buffer.append",
            "audio": audio_base64
        }
        
        await self.websocket.send(json.dumps(message))
    
    async def listen_for_transcriptions(self):
        """트랜스크립션 결과 실시간 수신"""
        try:
            async for message in self.websocket:
                data = json.loads(message)
                await self._handle_transcription_event(data)
                
        except websockets.exceptions.ConnectionClosed:
            self.logger.warning("트랜스크립션 연결이 종료됨")
            self.is_connected = False
        except Exception as e:
            self.logger.error(f"트랜스크립션 수신 오류: {e}")
    
    async def _handle_transcription_event(self, event: Dict[str, Any]):
        """트랜스크립션 이벤트 처리"""
        event_type = event.get("type")
        
        if event_type == "conversation.item.input_audio_transcription.delta":
            # 실시간 부분 트랜스크립션
            delta_text = event.get("delta", "")
            item_id = event.get("item_id")
            
            if self.transcription_callback:
                await self.transcription_callback({
                    "type": "partial",
                    "text": delta_text,
                    "item_id": item_id,
                    "timestamp": datetime.now().isoformat()
                })
        
        elif event_type == "conversation.item.input_audio_transcription.completed":
            # 완료된 트랜스크립션 결과
            transcript = event.get("transcript", "")
            item_id = event.get("item_id")
            confidence_score = self._calculate_confidence(event.get("logprobs", []))
            
            if self.transcription_callback:
                await self.transcription_callback({
                    "type": "final",
                    "text": transcript,
                    "item_id": item_id,
                    "confidence": confidence_score,
                    "timestamp": datetime.now().isoformat()
                })
        
        elif event_type == "error":
            # API 오류 처리
            error_msg = event.get("error", {}).get("message", "알 수 없는 오류")
            self.logger.error(f"트랜스크립션 API 오류: {error_msg}")
    
    def _calculate_confidence(self, logprobs: list) -> float:
        """로그 확률을 기반으로 신뢰도 계산"""
        if not logprobs:
            return 0.0
        
        # 로그 확률을 확률로 변환하고 평균 계산
        probs = [min(1.0, max(0.0, 2 ** logprob)) for logprob in logprobs if logprob is not None]
        return sum(probs) / len(probs) if probs else 0.0
    
    def set_transcription_callback(self, callback: Callable):
        """트랜스크립션 결과 콜백 함수 설정"""
        self.transcription_callback = callback
    
    async def disconnect(self):
        """WebSocket 연결 종료"""
        if self.websocket and not self.websocket.closed:
            await self.websocket.close()
        self.is_connected = False
        self.session_id = None
        self.logger.info("GPT-4o 트랜스크립션 서비스 연결 해제")
```

#### 1.2 음성 서비스 통합 업데이트
```python
# backend/services/voice_service.py 업데이트
import asyncio
from .gpt4o_transcription_service import GPT4oTranscriptionService
from .macro_matching_service import MacroMatchingService
from ..utils.config import get_openai_api_key

class VoiceService:
    """GPT-4o 실시간 트랜스크립션을 사용하는 개선된 음성 서비스"""
    
    def __init__(self):
        """음성 서비스 초기화"""
        self.transcription_service = GPT4oTranscriptionService(get_openai_api_key())
        self.macro_matching_service = MacroMatchingService()
        self.current_transcription = ""
        self.is_recording = False
        self.confidence_threshold = 0.7  # 신뢰도 임계값
        
    async def initialize(self):
        """음성 서비스 초기화 및 트랜스크립션 연결 설정"""
        success = await self.transcription_service.connect()
        if success:
            self.transcription_service.set_transcription_callback(self._handle_transcription_result)
            # 백그라운드에서 트랜스크립션 수신 시작
            asyncio.create_task(self.transcription_service.listen_for_transcriptions())
        return success
    
    async def _handle_transcription_result(self, transcription_data):
        """트랜스크립션 결과 처리 및 매크로 매칭"""
        if transcription_data["type"] == "final":
            transcript = transcription_data["text"].strip()
            confidence = transcription_data["confidence"]
            
            self.logger.info(f"음성 인식 결과: '{transcript}' (신뢰도: {confidence:.2f})")
            
            # 고신뢰도 트랜스크립션만 처리
            if confidence > self.confidence_threshold:
                # 매크로 매칭 서비스에 전달
                await self.macro_matching_service.process_voice_command(
                    transcript, confidence
                )
                
                # 성공적인 트랜스크립션 로그 기록
                self._log_successful_transcription(transcript, confidence)
            else:
                self.logger.warning(f"낮은 신뢰도로 인한 무시: {confidence:.2f}")
    
    async def start_recording(self):
        """음성 녹음 시작"""
        self.is_recording = True
        self.logger.info("음성 녹음 시작")
        # 실제 마이크 캡처는 C# 프론트엔드에서 처리
    
    async def stop_recording(self):
        """음성 녹음 중지"""
        self.is_recording = False
        # 남은 오디오 버퍼 커밋
        await self.transcription_service.commit_audio_buffer()
        self.logger.info("음성 녹음 중지")
    
    def _log_successful_transcription(self, transcript: str, confidence: float):
        """성공적인 트랜스크립션 로그 기록"""
        # 로깅 서비스에 기록 (구현 필요)
        pass
```

### 2. C# WPF 프론트엔드 구현

#### 2.1 음성 인식 래퍼 서비스 업데이트
```csharp
// VoiceMacroPro/Services/VoiceRecognitionWrapperService.cs
using System;
using System.Threading.Tasks;
using SocketIOClient;
using NAudio.Wave;
using System.IO;

public class VoiceRecognitionWrapperService : IDisposable
{
    private SocketIOClient.SocketIO _socket;
    private WaveInEvent _waveIn;
    private bool _isRecording = false;
    private readonly string _serverUrl;
    
    // 이벤트 정의
    public event EventHandler<TranscriptionResult> TranscriptionReceived;
    public event EventHandler<string> ErrorOccurred;
    public event EventHandler<ConnectionStatus> ConnectionChanged;
    
    public VoiceRecognitionWrapperService(string serverUrl = "http://localhost:5000")
    {
        _serverUrl = serverUrl;
    }
    
    /// <summary>
    /// 음성 인식 서비스 초기화
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            // Python 백엔드 WebSocket 연결
            _socket = new SocketIOClient.SocketIO(_serverUrl);
            
            // 이벤트 핸들러 설정
            _socket.On("transcription_result", HandleTranscriptionResult);
            _socket.On("voice_recognition_error", HandleError);
            _socket.On("connection_status", HandleConnectionStatus);
            
            await _socket.ConnectAsync();
            
            // 오디오 캡처 초기화
            InitializeAudioCapture();
            
            ConnectionChanged?.Invoke(this, new ConnectionStatus { IsConnected = true });
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"초기화 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 실시간 오디오 캡처 설정
    /// </summary>
    private void InitializeAudioCapture()
    {
        _waveIn = new WaveInEvent();
        // GPT-4o에 최적화된 오디오 설정: 24kHz, 16-bit, 모노
        _waveIn.WaveFormat = new WaveFormat(24000, 16, 1);
        _waveIn.BufferMilliseconds = 100; // 100ms 버퍼로 실시간 처리
        
        _waveIn.DataAvailable += OnAudioDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;
    }
    
    /// <summary>
    /// 오디오 데이터 실시간 처리 및 전송
    /// </summary>
    private async void OnAudioDataAvailable(object sender, WaveInEventArgs e)
    {
        if (_isRecording && _socket.Connected)
        {
            try
            {
                // 오디오 데이터를 Base64로 인코딩하여 전송
                string audioBase64 = Convert.ToBase64String(e.Buffer, 0, e.BytesRecorded);
                
                await _socket.EmitAsync("audio_chunk", new { audio = audioBase64 });
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"오디오 전송 오류: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 음성 녹음 시작
    /// </summary>
    public async Task StartRecordingAsync()
    {
        if (!_isRecording)
        {
            _isRecording = true;
            _waveIn.StartRecording();
            await _socket.EmitAsync("start_voice_recognition");
        }
    }
    
    /// <summary>
    /// 음성 녹음 중지
    /// </summary>
    public async Task StopRecordingAsync()
    {
        if (_isRecording)
        {
            _isRecording = false;
            _waveIn.StopRecording();
            await _socket.EmitAsync("stop_voice_recognition");
        }
    }
    
    /// <summary>
    /// 트랜스크립션 결과 처리
    /// </summary>
    private void HandleTranscriptionResult(SocketIOResponse response)
    {
        try
        {
            var data = response.GetValue<TranscriptionData>();
            
            var result = new TranscriptionResult
            {
                Type = data.type,
                Text = data.text,
                Confidence = data.confidence,
                Timestamp = DateTime.Parse(data.timestamp)
            };
            
            TranscriptionReceived?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"트랜스크립션 결과 처리 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 오류 처리
    /// </summary>
    private void HandleError(SocketIOResponse response)
    {
        var error = response.GetValue<ErrorData>();
        ErrorOccurred?.Invoke(this, error.error);
    }
    
    /// <summary>
    /// 연결 상태 변경 처리
    /// </summary>
    private void HandleConnectionStatus(SocketIOResponse response)
    {
        var status = response.GetValue<ConnectionStatus>();
        ConnectionChanged?.Invoke(this, status);
    }
    
    /// <summary>
    /// 녹음 중지 이벤트 처리
    /// </summary>
    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            ErrorOccurred?.Invoke(this, $"녹음 중지 오류: {e.Exception.Message}");
        }
    }
    
    /// <summary>
    /// 리소스 해제
    /// </summary>
    public void Dispose()
    {
        _waveIn?.Dispose();
        _socket?.DisconnectAsync();
    }
}

// 데이터 전송 객체들
public class TranscriptionResult
{
    public string Type { get; set; }
    public string Text { get; set; }
    public double Confidence { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TranscriptionData
{
    public string type { get; set; }
    public string text { get; set; }
    public double confidence { get; set; }
    public string timestamp { get; set; }
}

public class ErrorData
{
    public string error { get; set; }
}

public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public string Status { get; set; }
}
```

### 3. 설정 및 종속성 관리

#### 3.1 환경 변수 설정
```python
# backend/utils/config.py 업데이트
import os
from typing import Dict, Any

def get_openai_api_key() -> str:
    """환경 변수에서 OpenAI API 키 가져오기"""
    api_key = os.getenv('OPENAI_API_KEY')
    if not api_key:
        raise ValueError("OPENAI_API_KEY 환경변수가 설정되지 않았습니다")
    return api_key

def get_transcription_config() -> Dict[str, Any]:
    """트랜스크립션 설정 반환"""
    return {
        'model': 'gpt-4o-transcribe',
        'language': 'ko',  # 한국어 VoiceMacro Pro용
        'noise_reduction': 'near_field',
        'vad_threshold': 0.5,
        'confidence_threshold': 0.7,
        'buffer_size_ms': 100,
        'sample_rate': 24000
    }

def get_websocket_config() -> Dict[str, Any]:
    """WebSocket 설정 반환"""
    return {
        'host': 'localhost',
        'port': 5000,
        'cors_allowed_origins': "*",
        'reconnect_attempts': 3,
        'reconnect_delay': 5
    }
```

#### 3.2 종속성 업데이트
```txt
# requirements.txt 업데이트
websockets>=11.0.0
openai>=1.3.0
flask>=2.3.0
flask-socketio>=5.3.0
asyncio>=3.4.3
numpy>=1.24.0
logging>=0.4.9.6
```

```xml
<!-- VoiceMacroPro/VoiceMacroPro.csproj 업데이트 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="SocketIOClient" Version="3.0.6" />
    <PackageReference Include="NAudio" Version="2.2.1" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.5.4" />
  </ItemGroup>
</Project>
```

## 📊 성능 모니터링 및 최적화

### 실시간 성능 추적
```python
# backend/utils/performance_monitor.py
import time
import psutil
import logging
from dataclasses import dataclass
from typing import Dict, List

@dataclass
class PerformanceMetrics:
    """성능 메트릭 데이터 클래스"""
    timestamp: float
    latency_ms: float
    cpu_usage: float
    memory_usage_mb: float
    transcription_accuracy: float
    connection_status: bool

class PerformanceMonitor:
    """실시간 성능 모니터링 클래스"""
    
    def __init__(self):
        self.metrics_history: List[PerformanceMetrics] = []
        self.logger = logging.getLogger(__name__)
        self.start_time = time.time()
    
    def record_transcription_latency(self, start_time: float, end_time: float):
        """트랜스크립션 지연시간 기록"""
        latency_ms = (end_time - start_time) * 1000
        
        metrics = PerformanceMetrics(
            timestamp=time.time(),
            latency_ms=latency_ms,
            cpu_usage=psutil.cpu_percent(),
            memory_usage_mb=psutil.Process().memory_info().rss / 1024 / 1024,
            transcription_accuracy=0.0,  # 별도 계산 필요
            connection_status=True
        )
        
        self.metrics_history.append(metrics)
        
        # 성능 경고 체크
        if latency_ms > 100:  # 100ms 초과시 경고
            self.logger.warning(f"높은 지연시간 감지: {latency_ms:.2f}ms")
    
    def get_average_performance(self, last_n_minutes: int = 5) -> Dict:
        """최근 N분간 평균 성능 반환"""
        cutoff_time = time.time() - (last_n_minutes * 60)
        recent_metrics = [m for m in self.metrics_history if m.timestamp > cutoff_time]
        
        if not recent_metrics:
            return {}
        
        return {
            'avg_latency_ms': sum(m.latency_ms for m in recent_metrics) / len(recent_metrics),
            'avg_cpu_usage': sum(m.cpu_usage for m in recent_metrics) / len(recent_metrics),
            'avg_memory_mb': sum(m.memory_usage_mb for m in recent_metrics) / len(recent_metrics),
            'total_transcriptions': len(recent_metrics)
        }
```

이 개발명세서는 **프롬프트 엔지니어링의 6가지 핵심 기법**을 활용하여 VoiceMacro Pro의 GPT-4o 음성인식 시스템 구현을 위한 **실행 가능한 로드맵**을 제공합니다. 각 단계별로 구체적인 구현 가이드, 예시 코드, 검증 방법을 포함하여 개발자가 단계별로 따라갈 수 있도록 설계되었습니다. 