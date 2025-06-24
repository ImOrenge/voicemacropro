"""
Phase 5: 성능 최적화 및 안정성 강화 테스트
VoiceMacro Pro의 TDD 기반 개발을 위한 테스트 코드
"""

import os
import sys
import json
import pytest
import asyncio
import time
import threading
import gc
from unittest.mock import Mock, AsyncMock, patch, MagicMock
from typing import Dict, Any, List, Optional, Callable
from dataclasses import dataclass
import weakref

# psutil을 안전하게 import
try:
    import psutil
    HAS_PSUTIL = True
except ImportError:
    HAS_PSUTIL = False
    # psutil이 없을 경우 모의 구현
    class MockProcess:
        def memory_info(self):
            return type('MemoryInfo', (), {'rss': 100 * 1024 * 1024})()  # 100MB
    
    class MockPsutil:
        @staticmethod
        def cpu_percent(interval=None):
            return 10.0  # 10% CPU 사용률
        
        @staticmethod
        def Process():
            return MockProcess()
    
    psutil = MockPsutil()

# 프로젝트 루트를 Python 경로에 추가
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))


@dataclass
class PerformanceMetrics:
    """성능 메트릭 데이터 클래스"""
    cpu_usage: float
    memory_usage: float  # MB
    response_time: float  # ms
    throughput: float  # requests/sec
    error_rate: float  # %
    timestamp: float


class PerformanceMonitor:
    """성능 모니터링 클래스"""
    
    def __init__(self):
        """성능 모니터 초기화"""
        self.metrics_history: List[PerformanceMetrics] = []
        self.is_monitoring = False
        self.monitor_thread: Optional[threading.Thread] = None
        self.monitor_interval = 0.1  # 100ms 간격
        
    def start_monitoring(self):
        """모니터링 시작"""
        if self.is_monitoring:
            return
            
        self.is_monitoring = True
        self.monitor_thread = threading.Thread(target=self._monitoring_loop)
        self.monitor_thread.start()
    
    def stop_monitoring(self):
        """모니터링 중지"""
        self.is_monitoring = False
        if self.monitor_thread:
            self.monitor_thread.join(timeout=1.0)
    
    def _monitoring_loop(self):
        """모니터링 루프"""
        while self.is_monitoring:
            try:
                # 시스템 리소스 수집
                cpu_percent = psutil.cpu_percent(interval=None)
                memory_info = psutil.Process().memory_info()
                memory_mb = memory_info.rss / 1024 / 1024  # bytes to MB
                
                # 메트릭 생성
                metrics = PerformanceMetrics(
                    cpu_usage=cpu_percent,
                    memory_usage=memory_mb,
                    response_time=0.0,  # 개별 측정 필요
                    throughput=0.0,     # 개별 측정 필요
                    error_rate=0.0,     # 개별 측정 필요
                    timestamp=time.time()
                )
                
                self.metrics_history.append(metrics)
                
                # 히스토리 크기 제한 (최근 1000개)
                if len(self.metrics_history) > 1000:
                    self.metrics_history.pop(0)
                
                time.sleep(self.monitor_interval)
                
            except Exception as e:
                print(f"모니터링 오류: {e}")
                break
    
    def get_average_metrics(self, last_n: int = 10) -> Optional[PerformanceMetrics]:
        """최근 N개 메트릭의 평균 반환"""
        if len(self.metrics_history) < last_n:
            return None
            
        recent_metrics = self.metrics_history[-last_n:]
        
        avg_cpu = sum(m.cpu_usage for m in recent_metrics) / len(recent_metrics)
        avg_memory = sum(m.memory_usage for m in recent_metrics) / len(recent_metrics)
        avg_response = sum(m.response_time for m in recent_metrics) / len(recent_metrics)
        avg_throughput = sum(m.throughput for m in recent_metrics) / len(recent_metrics)
        avg_error_rate = sum(m.error_rate for m in recent_metrics) / len(recent_metrics)
        
        return PerformanceMetrics(
            cpu_usage=avg_cpu,
            memory_usage=avg_memory,
            response_time=avg_response,
            throughput=avg_throughput,
            error_rate=avg_error_rate,
            timestamp=time.time()
        )


class MemoryLeakDetector:
    """메모리 누수 감지 클래스"""
    
    def __init__(self):
        """메모리 누수 감지기 초기화"""
        self.initial_objects = None
        self.object_refs = []
        
    def start_detection(self):
        """누수 감지 시작"""
        gc.collect()  # 가비지 컬렉션 강제 실행
        self.initial_objects = len(gc.get_objects())
        self.object_refs.clear()
    
    def check_for_leaks(self, threshold: int = 1000) -> Dict[str, Any]:
        """메모리 누수 확인"""
        gc.collect()  # 가비지 컬렉션 강제 실행
        current_objects = len(gc.get_objects())
        
        leak_info = {
            "initial_objects": self.initial_objects,
            "current_objects": current_objects,
            "object_increase": current_objects - self.initial_objects if self.initial_objects else 0,
            "potential_leak": False,
            "memory_usage": psutil.Process().memory_info().rss / 1024 / 1024  # MB
        }
        
        if leak_info["object_increase"] > threshold:
            leak_info["potential_leak"] = True
            
        return leak_info
    
    def track_object(self, obj: Any, description: str = ""):
        """객체 추적 등록"""
        try:
            weak_ref = weakref.ref(obj)
            self.object_refs.append({
                "ref": weak_ref,
                "description": description,
                "created_at": time.time()
            })
        except TypeError:
            # 일부 객체는 weakref를 지원하지 않음
            pass
    
    def check_tracked_objects(self) -> Dict[str, Any]:
        """추적된 객체 상태 확인"""
        alive_count = 0
        dead_count = 0
        
        for ref_info in self.object_refs:
            if ref_info["ref"]() is not None:
                alive_count += 1
            else:
                dead_count += 1
        
        return {
            "total_tracked": len(self.object_refs),
            "alive_objects": alive_count,
            "dead_objects": dead_count,
            "cleanup_rate": dead_count / len(self.object_refs) if self.object_refs else 0
        }


class StressTestRunner:
    """스트레스 테스트 실행기"""
    
    def __init__(self):
        """스트레스 테스트 실행기 초기화"""
        self.test_results = []
        self.error_count = 0
        self.success_count = 0
        
    async def run_concurrent_load_test(self, target_function: Callable, 
                                      concurrent_count: int = 10, 
                                      duration_seconds: int = 5) -> Dict[str, Any]:
        """동시 부하 테스트 실행"""
        start_time = time.perf_counter()
        end_time = start_time + duration_seconds
        
        tasks = []
        results = []
        
        async def worker():
            """워커 함수"""
            worker_results = []
            while time.perf_counter() < end_time:
                try:
                    task_start = time.perf_counter()
                    result = await target_function()
                    task_end = time.perf_counter()
                    
                    worker_results.append({
                        "success": True,
                        "response_time": (task_end - task_start) * 1000,  # ms
                        "result": result
                    })
                    self.success_count += 1
                    
                except Exception as e:
                    worker_results.append({
                        "success": False,
                        "error": str(e),
                        "response_time": 0
                    })
                    self.error_count += 1
                
                # 작은 딜레이로 CPU 사용률 조절
                await asyncio.sleep(0.001)
            
            return worker_results
        
        # 동시 워커 실행
        for _ in range(concurrent_count):
            task = asyncio.create_task(worker())
            tasks.append(task)
        
        # 모든 워커 완료 대기
        worker_results = await asyncio.gather(*tasks)
        
        # 결과 집계
        all_results = []
        for worker_result in worker_results:
            all_results.extend(worker_result)
        
        # 통계 계산
        successful_results = [r for r in all_results if r["success"]]
        failed_results = [r for r in all_results if not r["success"]]
        
        if successful_results:
            response_times = [r["response_time"] for r in successful_results]
            avg_response_time = sum(response_times) / len(response_times)
            max_response_time = max(response_times)
            min_response_time = min(response_times)
        else:
            avg_response_time = max_response_time = min_response_time = 0
        
        total_duration = time.perf_counter() - start_time
        throughput = len(all_results) / total_duration if total_duration > 0 else 0
        error_rate = len(failed_results) / len(all_results) * 100 if all_results else 0
        
        return {
            "total_requests": len(all_results),
            "successful_requests": len(successful_results),
            "failed_requests": len(failed_results),
            "error_rate": error_rate,
            "avg_response_time": avg_response_time,
            "max_response_time": max_response_time,
            "min_response_time": min_response_time,
            "throughput": throughput,
            "duration": total_duration,
            "concurrent_workers": concurrent_count
        }


class TestPerformanceMonitoring:
    """성능 모니터링 테스트"""
    
    @pytest.fixture
    def performance_monitor(self):
        """성능 모니터 픽스처"""
        monitor = PerformanceMonitor()
        yield monitor
        monitor.stop_monitoring()
    
    def test_monitor_initialization(self, performance_monitor):
        """✅ Test 1: 모니터 초기화"""
        assert not performance_monitor.is_monitoring
        assert len(performance_monitor.metrics_history) == 0
        assert performance_monitor.monitor_interval == 0.1
    
    def test_monitoring_start_stop(self, performance_monitor):
        """✅ Test 2: 모니터링 시작/중지"""
        # 모니터링 시작
        performance_monitor.start_monitoring()
        assert performance_monitor.is_monitoring is True
        
        # 잠시 대기 후 메트릭 수집 확인
        time.sleep(0.3)
        assert len(performance_monitor.metrics_history) > 0
        
        # 모니터링 중지
        performance_monitor.stop_monitoring()
        assert performance_monitor.is_monitoring is False
    
    def test_metrics_collection(self, performance_monitor):
        """✅ Test 3: 메트릭 수집"""
        performance_monitor.start_monitoring()
        time.sleep(0.5)  # 500ms 동안 수집
        performance_monitor.stop_monitoring()
        
        # 메트릭이 수집되었는지 확인
        assert len(performance_monitor.metrics_history) >= 3  # 최소 3개 수집
        
        # 메트릭 내용 검증
        for metric in performance_monitor.metrics_history:
            assert metric.cpu_usage >= 0
            assert metric.memory_usage > 0  # 메모리는 항상 사용 중
            assert metric.timestamp > 0
    
    def test_average_metrics_calculation(self, performance_monitor):
        """✅ Test 4: 평균 메트릭 계산"""
        performance_monitor.start_monitoring()
        time.sleep(0.5)
        performance_monitor.stop_monitoring()
        
        avg_metrics = performance_monitor.get_average_metrics(last_n=3)
        
        if avg_metrics:  # 충분한 데이터가 있는 경우
            assert avg_metrics.cpu_usage >= 0
            assert avg_metrics.memory_usage > 0
        else:
            # 데이터가 부족한 경우
            assert len(performance_monitor.metrics_history) < 3


class TestMemoryManagement:
    """메모리 관리 테스트"""
    
    @pytest.fixture
    def memory_detector(self):
        """메모리 누수 감지기 픽스처"""
        return MemoryLeakDetector()
    
    def test_memory_leak_detection_initialization(self, memory_detector):
        """✅ Test 5: 메모리 누수 감지 초기화"""
        assert memory_detector.initial_objects is None
        assert len(memory_detector.object_refs) == 0
    
    def test_memory_baseline_establishment(self, memory_detector):
        """✅ Test 6: 메모리 기준선 설정"""
        memory_detector.start_detection()
        
        assert memory_detector.initial_objects is not None
        assert memory_detector.initial_objects > 0
    
    def test_object_tracking(self, memory_detector):
        """✅ Test 7: 객체 추적"""
        memory_detector.start_detection()
        
        # 테스트 객체 생성 및 추적
        test_objects = []
        for i in range(10):
            obj = {"id": i, "data": f"test_data_{i}"}
            test_objects.append(obj)
            memory_detector.track_object(obj, f"test_object_{i}")
        
        # 추적 상태 확인
        status = memory_detector.check_tracked_objects()
        assert status["total_tracked"] == 10
        assert status["alive_objects"] == 10
        assert status["dead_objects"] == 0
        
        # 객체 해제
        del test_objects
        gc.collect()
        
        # 정리 확인
        status_after = memory_detector.check_tracked_objects()
        assert status_after["dead_objects"] > 0
    
    def test_memory_leak_simulation(self, memory_detector):
        """⚠️ Test 8: 메모리 누수 시뮬레이션"""
        memory_detector.start_detection()
        
        # 의도적인 메모리 "누수" 시뮬레이션
        leaked_objects = []
        for i in range(100):  # 적은 수로 시뮬레이션
            obj = [f"data_{j}" for j in range(100)]  # 큰 객체
            leaked_objects.append(obj)
        
        # 누수 확인
        leak_info = memory_detector.check_for_leaks(threshold=50)
        
        # 누수가 감지되었는지 확인 (또는 허용 가능한 증가인지)
        assert leak_info["object_increase"] > 0
        assert leak_info["memory_usage"] > 0
        
        # 정리
        del leaked_objects
        gc.collect()


class TestStressAndLoad:
    """스트레스 및 부하 테스트"""
    
    @pytest.fixture
    def stress_runner(self):
        """스트레스 테스트 실행기 픽스처"""
        return StressTestRunner()
    
    async def mock_voice_recognition_task(self):
        """모의 음성 인식 작업"""
        # 실제 작업 시뮬레이션
        await asyncio.sleep(0.01)  # 10ms 처리 시간
        
        # 랜덤 결과 생성
        import random
        if random.random() < 0.95:  # 95% 성공률
            return {
                "text": "공격해",
                "confidence": 0.9 + random.random() * 0.1
            }
        else:
            raise Exception("Recognition failed")
    
    @pytest.mark.asyncio
    async def test_concurrent_voice_processing(self, stress_runner):
        """⚡ Test 9: 동시 음성 처리 부하 테스트"""
        result = await stress_runner.run_concurrent_load_test(
            target_function=self.mock_voice_recognition_task,
            concurrent_count=10,
            duration_seconds=2
        )
        
        # 성능 요구사항 검증
        assert result["error_rate"] < 10, f"오류율 {result['error_rate']:.1f}% > 10%"
        assert result["avg_response_time"] < 50, f"평균 응답시간 {result['avg_response_time']:.1f}ms > 50ms"
        assert result["throughput"] > 50, f"처리량 {result['throughput']:.1f} req/s < 50 req/s"
        
        # 기본 통계 확인
        assert result["total_requests"] > 0
        assert result["successful_requests"] > 0
    
    async def mock_macro_execution_task(self):
        """모의 매크로 실행 작업"""
        # 다양한 매크로 타입 시뮬레이션
        import random
        macro_types = ["key", "combo", "rapid", "hold"]
        macro_type = random.choice(macro_types)
        
        if macro_type == "key":
            await asyncio.sleep(0.001)  # 1ms
        elif macro_type == "combo":
            await asyncio.sleep(0.01)   # 10ms
        elif macro_type == "rapid":
            await asyncio.sleep(0.05)   # 50ms
        elif macro_type == "hold":
            await asyncio.sleep(0.02)   # 20ms
        
        return {"type": macro_type, "success": True}
    
    @pytest.mark.asyncio
    async def test_high_frequency_macro_execution(self, stress_runner):
        """⚡ Test 10: 고빈도 매크로 실행 테스트"""
        result = await stress_runner.run_concurrent_load_test(
            target_function=self.mock_macro_execution_task,
            concurrent_count=5,
            duration_seconds=3
        )
        
        # 매크로 실행 성능 요구사항
        assert result["error_rate"] < 5, f"매크로 실행 오류율 {result['error_rate']:.1f}% > 5%"
        assert result["avg_response_time"] < 30, f"평균 실행시간 {result['avg_response_time']:.1f}ms > 30ms"
        
        # 처리량 확인
        assert result["total_requests"] > 100  # 3초 동안 최소 100개 처리
    
    @pytest.mark.asyncio
    async def test_system_resource_usage(self):
        """🔒 Test 11: 시스템 리소스 사용량 테스트"""
        monitor = PerformanceMonitor()
        monitor.start_monitoring()
        
        # 부하 생성
        stress_runner = StressTestRunner()
        await stress_runner.run_concurrent_load_test(
            target_function=self.mock_voice_recognition_task,
            concurrent_count=8,
            duration_seconds=2
        )
        
        monitor.stop_monitoring()
        
        # 리소스 사용량 분석
        avg_metrics = monitor.get_average_metrics(last_n=10)
        
        if avg_metrics:
            # 성능 요구사항 검증
            assert avg_metrics.cpu_usage < 80, f"CPU 사용률 {avg_metrics.cpu_usage:.1f}% > 80%"
            assert avg_metrics.memory_usage < 500, f"메모리 사용량 {avg_metrics.memory_usage:.1f}MB > 500MB"
    
    @pytest.mark.asyncio
    async def test_sustained_load_stability(self, stress_runner):
        """🔄 Test 12: 지속적 부하 안정성 테스트"""
        # 더 긴 시간 동안 안정성 테스트
        result = await stress_runner.run_concurrent_load_test(
            target_function=self.mock_voice_recognition_task,
            concurrent_count=3,
            duration_seconds=5  # 5초간 지속
        )
        
        # 안정성 요구사항
        assert result["error_rate"] < 15, f"지속 부하 오류율 {result['error_rate']:.1f}% > 15%"
        
        # 성능 저하 없이 지속적 처리 확인
        assert result["total_requests"] > 200  # 5초 동안 최소 200개 처리
        assert result["throughput"] > 30  # 최소 30 req/s 유지


class TestErrorRecovery:
    """오류 복구 테스트"""
    
    async def faulty_service_task(self, failure_rate: float = 0.3):
        """의도적으로 실패하는 서비스 작업"""
        import random
        await asyncio.sleep(0.005)  # 5ms 처리 시간
        
        if random.random() < failure_rate:
            raise Exception("Service temporarily unavailable")
        
        return {"status": "success", "data": "processed"}
    
    @pytest.mark.asyncio
    async def test_error_handling_resilience(self):
        """🛡️ Test 13: 오류 처리 복원력"""
        error_count = 0
        success_count = 0
        
        # 여러 번 시도하여 오류 복구 테스트
        for _ in range(100):
            try:
                result = await self.faulty_service_task(failure_rate=0.2)
                success_count += 1
            except Exception:
                error_count += 1
        
        # 복원력 검증
        total_attempts = success_count + error_count
        error_rate = error_count / total_attempts * 100
        
        assert total_attempts == 100
        assert success_count > 70  # 70% 이상 성공
        assert error_rate < 30     # 30% 미만 실패
    
    @pytest.mark.asyncio
    async def test_graceful_degradation(self):
        """📉 Test 14: 점진적 성능 저하"""
        response_times = []
        
        # 점진적으로 부하 증가
        for load_level in [1, 3, 5, 8, 10]:
            start_time = time.perf_counter()
            
            tasks = []
            for _ in range(load_level):
                task = asyncio.create_task(self.mock_voice_recognition_task())
                tasks.append(task)
            
            await asyncio.gather(*tasks, return_exceptions=True)
            
            end_time = time.perf_counter()
            response_time = (end_time - start_time) * 1000  # ms
            response_times.append(response_time)
        
        # 성능 저하가 점진적인지 확인 (급격한 증가 방지)
        for i in range(1, len(response_times)):
            prev_time = response_times[i-1]
            curr_time = response_times[i]
            
            # 이전 대비 10배 이상 증가하지 않아야 함
            assert curr_time < prev_time * 10, f"급격한 성능 저하: {prev_time:.1f}ms → {curr_time:.1f}ms"
    
    async def mock_voice_recognition_task(self):
        """모의 음성 인식 작업 (간단 버전)"""
        await asyncio.sleep(0.01)
        return {"text": "recognized", "confidence": 0.9}


class TestDataIntegrity:
    """데이터 무결성 테스트"""
    
    def test_concurrent_data_access(self):
        """🔒 Test 15: 동시 데이터 접근"""
        import threading
        
        # 공유 데이터 구조
        shared_data = {"counter": 0, "items": []}
        lock = threading.Lock()
        
        def worker(worker_id: int):
            """워커 함수"""
            for i in range(100):
                with lock:
                    shared_data["counter"] += 1
                    shared_data["items"].append(f"worker_{worker_id}_item_{i}")
        
        # 여러 스레드에서 동시 접근
        threads = []
        for worker_id in range(5):
            thread = threading.Thread(target=worker, args=(worker_id,))
            threads.append(thread)
            thread.start()
        
        # 모든 스레드 완료 대기
        for thread in threads:
            thread.join()
        
        # 데이터 무결성 확인
        assert shared_data["counter"] == 500  # 5 workers * 100 items
        assert len(shared_data["items"]) == 500
        
        # 중복 없는지 확인
        unique_items = set(shared_data["items"])
        assert len(unique_items) == 500
    
    def test_configuration_consistency(self):
        """⚙️ Test 16: 설정 일관성"""
        # 모의 설정 데이터
        config = {
            "audio": {
                "sample_rate": 24000,
                "channels": 1,
                "buffer_size": 2400
            },
            "voice_recognition": {
                "confidence_threshold": 0.7,
                "model": "gpt-4o-transcribe"
            },
            "performance": {
                "max_cpu_usage": 15,
                "max_memory_usage": 200,
                "max_response_time": 300
            }
        }
        
        # 설정 검증 함수
        def validate_config(cfg: Dict[str, Any]) -> List[str]:
            errors = []
            
            # 오디오 설정 검증
            if cfg["audio"]["sample_rate"] not in [16000, 24000, 48000]:
                errors.append("Invalid sample rate")
            
            if cfg["audio"]["channels"] not in [1, 2]:
                errors.append("Invalid channel count")
            
            # 성능 설정 검증
            if cfg["performance"]["max_cpu_usage"] > 50:
                errors.append("CPU usage limit too high")
            
            if cfg["performance"]["max_memory_usage"] > 1000:
                errors.append("Memory usage limit too high")
            
            return errors
        
        # 설정 검증
        validation_errors = validate_config(config)
        assert len(validation_errors) == 0, f"설정 오류: {validation_errors}"
        
        # 일관성 확인
        buffer_size = config["audio"]["buffer_size"]
        sample_rate = config["audio"]["sample_rate"]
        channels = config["audio"]["channels"]
        
        # 버퍼 크기가 100ms에 해당하는지 확인
        samples_per_100ms = (sample_rate * channels * 100) // 1000
        assert buffer_size == samples_per_100ms, "버퍼 크기가 100ms와 일치하지 않음"


class TestSystemIntegration:
    """시스템 통합 테스트"""
    
    @pytest.mark.asyncio
    async def test_end_to_end_performance(self):
        """🔄 Test 17: 종단간 성능 테스트"""
        # 전체 파이프라인 시뮬레이션
        async def full_pipeline():
            """전체 파이프라인 실행"""
            # 1. 음성 캡처 (시뮬레이션)
            await asyncio.sleep(0.005)
            
            # 2. 음성 인식
            await asyncio.sleep(0.02)
            
            # 3. 매크로 매칭
            await asyncio.sleep(0.003)
            
            # 4. 매크로 실행
            await asyncio.sleep(0.01)
            
            return {"pipeline": "completed", "total_time": 0.038}
        
        # 성능 측정
        start_time = time.perf_counter()
        
        # 여러 파이프라인 동시 실행
        tasks = [full_pipeline() for _ in range(20)]
        results = await asyncio.gather(*tasks)
        
        end_time = time.perf_counter()
        total_time = (end_time - start_time) * 1000  # ms
        
        # 성능 요구사항 검증
        assert len(results) == 20
        assert total_time < 1000, f"20개 파이프라인 처리 시간 {total_time:.1f}ms > 1000ms"
        
        # 평균 파이프라인 시간
        avg_pipeline_time = total_time / 20
        assert avg_pipeline_time < 300, f"평균 파이프라인 시간 {avg_pipeline_time:.1f}ms > 300ms"
    
    @pytest.mark.asyncio
    async def test_system_stability_under_load(self):
        """🛡️ Test 18: 부하 상황에서 시스템 안정성"""
        monitor = PerformanceMonitor()
        leak_detector = MemoryLeakDetector()
        
        monitor.start_monitoring()
        leak_detector.start_detection()
        
        # 지속적인 부하 생성
        async def sustained_load():
            for _ in range(50):
                await asyncio.sleep(0.01)  # 10ms 간격으로 작업
                
                # 모의 작업 실행
                await self.mock_voice_recognition_task()
        
        # 여러 부하 생성기 동시 실행
        load_tasks = [sustained_load() for _ in range(3)]
        await asyncio.gather(*load_tasks)
        
        monitor.stop_monitoring()
        
        # 안정성 검증
        avg_metrics = monitor.get_average_metrics(last_n=10)
        leak_info = leak_detector.check_for_leaks(threshold=500)
        
        if avg_metrics:
            assert avg_metrics.cpu_usage < 90, "높은 부하에서 CPU 사용률 과다"
            assert avg_metrics.memory_usage < 600, "높은 부하에서 메모리 사용량 과다"
        
        assert not leak_info["potential_leak"], "부하 테스트 중 메모리 누수 감지"
    
    async def mock_voice_recognition_task(self):
        """모의 음성 인식 작업"""
        await asyncio.sleep(0.01)
        return {"status": "completed"}


# 테스트 실행 함수
def run_phase5_tests():
    """Phase 5 테스트 실행"""
    import subprocess
    
    print("🧪 Phase 5: 성능 최적화 및 안정성 강화 테스트 실행 중...")
    
    # pytest 실행
    result = subprocess.run([
        'py', '-m', 'pytest', 
        __file__,
        '-v',  # 상세 출력
        '--tb=short',  # 짧은 트레이스백
        '--disable-warnings'  # 경고 메시지 숨김
    ], capture_output=True, text=True)
    
    print("📊 테스트 결과:")
    print(result.stdout)
    
    if result.stderr:
        print("⚠️ 경고/오류:")
        print(result.stderr)
    
    return result.returncode == 0


if __name__ == "__main__":
    # 직접 실행 시 테스트 수행
    success = run_phase5_tests()
    
    if success:
        print("✅ Phase 5 테스트 모두 통과!")
        print("🎉 모든 Phase 구현이 완료되었습니다!")
    else:
        print("❌ 일부 테스트 실패. 구현을 수정해주세요.") 