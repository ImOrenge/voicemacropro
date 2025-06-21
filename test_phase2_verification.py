#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
VoiceMacro Pro - Phase 2 (C# WPF 프론트엔드) 구현 검증 스크립트

이 스크립트는 GPT-4o 실시간 음성인식 Phase 2 구현이 올바르게 완료되었는지 검증합니다.
검증 항목:
1. C# 프로젝트 의존성 패키지 (SocketIOClient, NAudio)
2. GPT-4o 관련 모델 클래스들
3. VoiceRecognitionWrapperService WebSocket 통합
4. VoiceRecognitionView UI 업데이트
5. 전체 프론트엔드 아키텍처 검증

작성자: VoiceMacro Pro 개발팀
작성일: 2025-01-22
"""

import os
import sys
import re
import json
import xml.etree.ElementTree as ET
from typing import Dict, List, Tuple, Any
from pathlib import Path

class Phase2Verifier:
    """Phase 2 C# WPF 프론트엔드 구현 검증 클래스"""
    
    def __init__(self):
        """검증 클래스 초기화"""
        self.results = {
            'total_tests': 0,
            'passed_tests': 0,
            'failed_tests': 0,
            'test_details': []
        }
        self.base_path = Path(__file__).parent
        self.csharp_project_path = self.base_path / "VoiceMacroPro"
        
    def log_test(self, test_name: str, passed: bool, details: str = ""):
        """테스트 결과를 로깅하는 함수"""
        self.results['total_tests'] += 1
        if passed:
            self.results['passed_tests'] += 1
            status = "✅ PASS"
        else:
            self.results['failed_tests'] += 1
            status = "❌ FAIL"
            
        self.results['test_details'].append({
            'name': test_name,
            'status': status,
            'details': details
        })
        
        print(f"{status} | {test_name}")
        if details:
            print(f"      └─ {details}")
    
    def verify_csproj_dependencies(self) -> bool:
        """VoiceMacroPro.csproj의 NuGet 패키지 의존성 검증"""
        try:
            csproj_path = self.csharp_project_path / "VoiceMacroPro.csproj"
            
            if not csproj_path.exists():
                self.log_test("C# 프로젝트 파일 존재 확인", False, f"VoiceMacroPro.csproj 파일이 없습니다: {csproj_path}")
                return False
            
            # XML 파싱
            tree = ET.parse(csproj_path)
            root = tree.getroot()
            
            # 필수 패키지 목록
            required_packages = {
                'SocketIOClient': '3.0.6',
                'NAudio': '2.2.1', 
                'System.Threading.Tasks.Extensions': '4.5.4'
            }
            
            found_packages = {}
            
            # PackageReference 요소들 검색
            for item_group in root.findall('.//ItemGroup'):
                for package_ref in item_group.findall('PackageReference'):
                    include = package_ref.get('Include')
                    version = package_ref.get('Version')
                    if include and version:
                        found_packages[include] = version
            
            # 필수 패키지 검증
            missing_packages = []
            for package, expected_version in required_packages.items():
                if package not in found_packages:
                    missing_packages.append(f"{package} (누락)")
                elif found_packages[package] != expected_version:
                    missing_packages.append(f"{package} (버전 불일치: {found_packages[package]} != {expected_version})")
            
            if missing_packages:
                self.log_test("NuGet 패키지 의존성 검증", False, f"누락/불일치 패키지: {', '.join(missing_packages)}")
                return False
            else:
                self.log_test("NuGet 패키지 의존성 검증", True, f"모든 필수 패키지 확인됨: {len(required_packages)}개")
                return True
                
        except Exception as e:
            self.log_test("NuGet 패키지 의존성 검증", False, f"검증 중 오류: {str(e)}")
            return False
    
    def verify_gpt4o_models(self) -> bool:
        """GPT-4o 관련 모델 클래스들 검증"""
        try:
            models_path = self.csharp_project_path / "Models" / "Gpt4oModels.cs"
            
            if not models_path.exists():
                self.log_test("GPT-4o 모델 파일 존재 확인", False, f"Gpt4oModels.cs 파일이 없습니다: {models_path}")
                return False
            
            # 파일 내용 읽기
            with open(models_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 필수 클래스들 검증
            required_classes = [
                'TranscriptionResult',
                'TranscriptionData', 
                'ConnectionStatus',
                'ErrorData',
                'AudioCaptureSettings',
                'VoiceSession',
                'VoiceRecognitionStats'
            ]
            
            missing_classes = []
            for class_name in required_classes:
                if f"class {class_name}" not in content:
                    missing_classes.append(class_name)
            
            # 중요한 프로퍼티들 검증
            critical_properties = [
                'IsHighConfidence',  # TranscriptionResult의 계산 프로퍼티
                'ConfidenceColor',   # UI 바인딩용
                'SessionStats'       # 세션 통계
            ]
            
            missing_properties = []
            for prop in critical_properties:
                if prop not in content:
                    missing_properties.append(prop)
            
            if missing_classes:
                self.log_test("GPT-4o 모델 클래스 검증", False, f"누락된 클래스: {', '.join(missing_classes)}")
                return False
            elif missing_properties:
                self.log_test("GPT-4o 모델 프로퍼티 검증", False, f"누락된 중요 프로퍼티: {', '.join(missing_properties)}")
                return False
            else:
                self.log_test("GPT-4o 모델 클래스 검증", True, f"모든 필수 클래스 확인됨: {len(required_classes)}개")
                return True
                
        except Exception as e:
            self.log_test("GPT-4o 모델 클래스 검증", False, f"검증 중 오류: {str(e)}")
            return False
    
    def verify_voice_wrapper_service(self) -> bool:
        """VoiceRecognitionWrapperService WebSocket 통합 검증"""
        try:
            service_path = self.csharp_project_path / "Services" / "VoiceRecognitionWrapperService.cs"
            
            if not service_path.exists():
                self.log_test("음성인식 래퍼 서비스 파일 존재 확인", False, f"VoiceRecognitionWrapperService.cs 파일이 없습니다: {service_path}")
                return False
            
            # 파일 내용 읽기
            with open(service_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # WebSocket 통합 검증
            websocket_features = [
                'SocketIOClient.SocketIO',      # SocketIO 클라이언트
                'NAudio.Wave',                  # NAudio 네임스페이스
                'WaveInEvent',                  # 실시간 오디오 캡처
                'TranscriptionReceived',        # 트랜스크립션 이벤트
                'ConnectionChanged',            # 연결 상태 이벤트
                'AudioLevelChanged',            # 오디오 레벨 이벤트
                'InitializeAsync',              # 비동기 초기화
                'StartRecordingAsync',          # 비동기 녹음 시작
                'OnAudioDataAvailable'          # 오디오 데이터 처리
            ]
            
            missing_features = []
            for feature in websocket_features:
                if feature not in content:
                    missing_features.append(feature)
            
            # GPT-4o 특화 설정 검증
            gpt4o_settings = [
                '24000',           # 24kHz 샘플링
                '100',             # 100ms 버퍼
                'audio_chunk',     # WebSocket 이벤트명
                'transcription_result',  # 트랜스크립션 결과 이벤트
                'IDisposable'      # 리소스 관리
            ]
            
            missing_settings = []
            for setting in gpt4o_settings:
                if setting not in content:
                    missing_settings.append(setting)
            
            if missing_features:
                self.log_test("WebSocket 통합 기능 검증", False, f"누락된 기능: {', '.join(missing_features)}")
                return False
            elif missing_settings:
                self.log_test("GPT-4o 최적화 설정 검증", False, f"누락된 설정: {', '.join(missing_settings)}")
                return False
            else:
                self.log_test("음성인식 래퍼 서비스 검증", True, f"WebSocket 통합 및 GPT-4o 최적화 확인됨")
                return True
                
        except Exception as e:
            self.log_test("음성인식 래퍼 서비스 검증", False, f"검증 중 오류: {str(e)}")
            return False
    
    def verify_voice_recognition_view_cs(self) -> bool:
        """VoiceRecognitionView.xaml.cs 코드비하인드 검증"""
        try:
            view_cs_path = self.csharp_project_path / "Views" / "VoiceRecognitionView.xaml.cs"
            
            if not view_cs_path.exists():
                self.log_test("음성인식 View 코드비하인드 파일 존재 확인", False, f"VoiceRecognitionView.xaml.cs 파일이 없습니다: {view_cs_path}")
                return False
            
            # 파일 내용 읽기
            with open(view_cs_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # 실시간 트랜스크립션 UI 기능 검증
            ui_features = [
                'TranscriptionResults',          # 트랜스크립션 결과 컬렉션
                'MacroResults',                  # 매크로 실행 결과 컬렉션
                'CurrentTranscription',          # 현재 트랜스크립션 텍스트
                'CurrentConfidence',             # 현재 신뢰도
                'IsConnected',                   # 연결 상태
                'AudioLevel',                    # 오디오 레벨
                'OnTranscriptionReceived',       # 트랜스크립션 수신 핸들러
                'OnConnectionChanged',           # 연결 상태 변경 핸들러
                'RecordingToggleButton_Click',   # 녹음 토글 버튼
                'SetupEventHandlers'             # 이벤트 핸들러 설정
            ]
            
            missing_features = []
            for feature in ui_features:
                if feature not in content:
                    missing_features.append(feature)
            
            # UI 바인딩 관련 검증
            binding_features = [
                'ConfidenceColor',      # 신뢰도 색상 바인딩
                'SessionStats',         # 세션 통계 바인딩
                'Dispatcher.Invoke',    # UI 스레드 동기화
                'INotifyPropertyChanged'  # 프로퍼티 변경 알림
            ]
            
            missing_bindings = []
            for binding in binding_features:
                if binding not in content:
                    missing_bindings.append(binding)
            
            if missing_features:
                self.log_test("실시간 UI 기능 검증", False, f"누락된 UI 기능: {', '.join(missing_features)}")
                return False
            elif missing_bindings:
                self.log_test("UI 바인딩 기능 검증", False, f"누락된 바인딩: {', '.join(missing_bindings)}")
                return False
            else:
                self.log_test("음성인식 View 코드비하인드 검증", True, f"실시간 UI 및 바인딩 기능 확인됨")
                return True
                
        except Exception as e:
            self.log_test("음성인식 View 코드비하인드 검증", False, f"검증 중 오류: {str(e)}")
            return False
    
    def verify_voice_recognition_view_xaml(self) -> bool:
        """VoiceRecognitionView.xaml UI 마크업 검증"""
        try:
            view_xaml_path = self.csharp_project_path / "Views" / "VoiceRecognitionView.xaml"
            
            if not view_xaml_path.exists():
                self.log_test("음성인식 View XAML 파일 존재 확인", False, f"VoiceRecognitionView.xaml 파일이 없습니다: {view_xaml_path}")
                return False
            
            # 파일 내용 읽기
            with open(view_xaml_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # GPT-4o UI 요소들 검증
            ui_elements = [
                'GPT-4o 실시간 음성인식',      # 헤더 텍스트
                'OpenAI 최신 트랜스크립션',    # 서브 헤더
                'TranscriptionResults',        # 트랜스크립션 결과 바인딩
                'MacroResults',                # 매크로 결과 바인딩
                'CurrentTranscription',        # 현재 트랜스크립션 바인딩
                'ConfidenceColor',             # 신뢰도 색상 바인딩
                'ConnectionStatusText',        # 연결 상태 텍스트
                'RecordingToggleButton',       # 녹음 토글 버튼
                'ReconnectButton',             # 재연결 버튼
                'DropShadowEffect'             # 그림자 효과
            ]
            
            missing_elements = []
            for element in ui_elements:
                if element not in content:
                    missing_elements.append(element)
            
            # 현대적 UI 디자인 요소 검증
            design_elements = [
                'CornerRadius',          # 둥근 모서리
                'LinearGradientBrush',   # 그라데이션 (아니면 Linear로 검사)
                'DataTrigger',           # 조건부 스타일
                'Storyboard',            # 애니메이션
                'F8FAFC',               # 현대적 색상 코드
                '1400'                   # 넓은 화면 지원
            ]
            
            # LinearGradientBrush 대신 Linear로 검사 (색상 정의에서)
            if 'LinearGradientBrush' not in content and 'Linear#' not in content:
                missing_elements.append('LinearGradientBrush/Linear')
            
            missing_design = []
            for design in design_elements:
                if design == 'LinearGradientBrush':  # 이미 위에서 처리
                    continue
                if design not in content:
                    missing_design.append(design)
            
            if missing_elements:
                self.log_test("GPT-4o UI 요소 검증", False, f"누락된 UI 요소: {', '.join(missing_elements)}")
                return False
            elif missing_design:
                self.log_test("현대적 UI 디자인 검증", False, f"누락된 디자인 요소: {', '.join(missing_design)}")
                return False
            else:
                self.log_test("음성인식 View XAML 검증", True, f"GPT-4o UI 및 현대적 디자인 확인됨")
                return True
                
        except Exception as e:
            self.log_test("음성인식 View XAML 검증", False, f"검증 중 오류: {str(e)}")
            return False
    
    def verify_project_structure(self) -> bool:
        """전체 프로젝트 구조 검증"""
        try:
            # 필수 파일들 검증
            required_files = [
                "VoiceMacroPro/VoiceMacroPro.csproj",
                "VoiceMacroPro/Models/Gpt4oModels.cs",
                "VoiceMacroPro/Services/VoiceRecognitionWrapperService.cs",
                "VoiceMacroPro/Views/VoiceRecognitionView.xaml",
                "VoiceMacroPro/Views/VoiceRecognitionView.xaml.cs"
            ]
            
            missing_files = []
            existing_files = []
            
            for file_path in required_files:
                full_path = self.base_path / file_path
                if full_path.exists():
                    existing_files.append(file_path)
                else:
                    missing_files.append(file_path)
            
            if missing_files:
                self.log_test("프로젝트 구조 검증", False, f"누락된 파일: {', '.join(missing_files)}")
                return False
            else:
                self.log_test("프로젝트 구조 검증", True, f"모든 필수 파일 확인됨: {len(existing_files)}개")
                return True
                
        except Exception as e:
            self.log_test("프로젝트 구조 검증", False, f"검증 중 오류: {str(e)}")
            return False
    
    def verify_integration_readiness(self) -> bool:
        """Phase 1과 Phase 2 통합 준비 상태 검증"""
        try:
            # Phase 1 백엔드 파일들 확인
            phase1_files = [
                "backend/services/gpt4o_transcription_service.py",
                "backend/api/websocket_server.py", 
                "backend/services/voice_service.py"
            ]
            
            # Phase 2 프론트엔드 파일들 확인
            phase2_files = [
                "VoiceMacroPro/Services/VoiceRecognitionWrapperService.cs",
                "VoiceMacroPro/Models/Gpt4oModels.cs"
            ]
            
            missing_backend = []
            missing_frontend = []
            
            for file_path in phase1_files:
                if not (self.base_path / file_path).exists():
                    missing_backend.append(file_path)
            
            for file_path in phase2_files:
                if not (self.base_path / file_path).exists():
                    missing_frontend.append(file_path)
            
            if missing_backend:
                self.log_test("Phase 1 백엔드 파일 확인", False, f"누락된 백엔드 파일: {', '.join(missing_backend)}")
                return False
            elif missing_frontend:
                self.log_test("Phase 2 프론트엔드 파일 확인", False, f"누락된 프론트엔드 파일: {', '.join(missing_frontend)}")
                return False
            else:
                self.log_test("통합 준비 상태 검증", True, f"Phase 1 + Phase 2 모든 파일 준비됨")
                return True
                
        except Exception as e:
            self.log_test("통합 준비 상태 검증", False, f"검증 중 오류: {str(e)}")
            return False
    
    def run_all_verifications(self) -> Dict[str, Any]:
        """모든 검증을 실행하는 메인 함수"""
        print("=" * 80)
        print("🤖 VoiceMacro Pro - Phase 2 (C# WPF 프론트엔드) 구현 검증")
        print("=" * 80)
        print()
        
        # 검증 항목들 실행
        verification_methods = [
            self.verify_project_structure,
            self.verify_csproj_dependencies, 
            self.verify_gpt4o_models,
            self.verify_voice_wrapper_service,
            self.verify_voice_recognition_view_cs,
            self.verify_voice_recognition_view_xaml,
            self.verify_integration_readiness
        ]
        
        for verification_method in verification_methods:
            try:
                verification_method()
            except Exception as e:
                self.log_test(f"{verification_method.__name__} 실행 오류", False, str(e))
        
        # 결과 요약 출력
        print()
        print("=" * 80)
        print("📊 검증 결과 요약")
        print("=" * 80)
        
        total = self.results['total_tests']
        passed = self.results['passed_tests']
        failed = self.results['failed_tests']
        success_rate = (passed / total * 100) if total > 0 else 0
        
        print(f"총 테스트: {total}개")
        print(f"성공: {passed}개")
        print(f"실패: {failed}개")
        print(f"성공률: {success_rate:.1f}%")
        print()
        
        # 성공률에 따른 결과 메시지
        if success_rate >= 95:
            print("🎉 우수! Phase 2 구현이 거의 완벽합니다.")
        elif success_rate >= 85:
            print("✅ 양호! Phase 2 구현이 잘 되었습니다.")
        elif success_rate >= 70:
            print("⚠️  보통! 몇 가지 문제를 수정해야 합니다.")
        else:
            print("❌ 문제 발견! Phase 2 구현을 다시 검토해주세요.")
        
        print()
        print("📋 세부 테스트 결과:")
        for test in self.results['test_details']:
            print(f"  {test['status']} {test['name']}")
            if test['details']:
                print(f"    └─ {test['details']}")
        
        return self.results

def main():
    """메인 함수"""
    verifier = Phase2Verifier()
    results = verifier.run_all_verifications()
    
    # 성공률이 90% 이상이면 성공 코드 반환
    success_rate = (results['passed_tests'] / results['total_tests'] * 100) if results['total_tests'] > 0 else 0
    exit_code = 0 if success_rate >= 90 else 1
    
    print(f"\n🔄 Phase 2 검증 완료 (종료 코드: {exit_code})")
    return exit_code

if __name__ == "__main__":
    sys.exit(main()) 