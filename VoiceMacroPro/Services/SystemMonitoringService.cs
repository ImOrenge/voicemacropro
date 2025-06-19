using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VoiceMacroPro.Services
{
    /// <summary>
    /// 시스템 성능 모니터링 서비스
    /// CPU, 메모리, 디스크 사용률 등을 실시간으로 모니터링합니다.
    /// </summary>
    public class SystemMonitoringService : INotifyPropertyChanged
    {
        #region Private Fields
        
        private static SystemMonitoringService? _instance;
        private readonly DispatcherTimer _monitoringTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;
        private readonly Process _currentProcess;
        
        private double _cpuUsage = 0;
        private double _memoryUsage = 0;
        private double _memoryUsageMB = 0;
        private double _totalMemoryGB = 0;
        private bool _isMonitoring = false;
        private string _systemStatus = "정상";
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static SystemMonitoringService Instance => _instance ??= new SystemMonitoringService();
        
        /// <summary>
        /// CPU 사용률 (%)
        /// </summary>
        public double CpuUsage
        {
            get => _cpuUsage;
            private set
            {
                _cpuUsage = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 메모리 사용률 (%)
        /// </summary>
        public double MemoryUsage
        {
            get => _memoryUsage;
            private set
            {
                _memoryUsage = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 메모리 사용량 (MB)
        /// </summary>
        public double MemoryUsageMB
        {
            get => _memoryUsageMB;
            private set
            {
                _memoryUsageMB = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 전체 메모리 (GB)
        /// </summary>
        public double TotalMemoryGB
        {
            get => _totalMemoryGB;
            private set
            {
                _totalMemoryGB = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 모니터링 활성화 상태
        /// </summary>
        public bool IsMonitoring
        {
            get => _isMonitoring;
            private set
            {
                _isMonitoring = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 시스템 상태
        /// </summary>
        public string SystemStatus
        {
            get => _systemStatus;
            private set
            {
                _systemStatus = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 애플리케이션 실행 시간
        /// </summary>
        public TimeSpan Uptime => DateTime.Now - _currentProcess.StartTime;
        
        /// <summary>
        /// 현재 프로세스 메모리 사용량 (MB)
        /// </summary>
        public double ProcessMemoryMB => _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// SystemMonitoringService 생성자
        /// </summary>
        private SystemMonitoringService()
        {
            try
            {
                // Performance Counter 초기화
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _currentProcess = Process.GetCurrentProcess();
                
                // 전체 메모리 크기 계산 (GB)
                var totalMemoryMB = GetTotalPhysicalMemoryMB();
                TotalMemoryGB = totalMemoryMB / 1024.0;
                
                // 타이머 설정 (2초마다 업데이트)
                _monitoringTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                _monitoringTimer.Tick += MonitoringTimer_Tick;
                
                // 초기 데이터 수집
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000); // CPU 카운터 초기화 대기
                    UpdateSystemMetrics();
                });
                
                LoggingService.Instance.LogInfo("시스템 모니터링 서비스 초기화 완료");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"시스템 모니터링 서비스 초기화 실패: {ex.Message}");
                SystemStatus = "오류";
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 모니터링 시작
        /// </summary>
        public void StartMonitoring()
        {
            try
            {
                if (!IsMonitoring)
                {
                    _monitoringTimer.Start();
                    IsMonitoring = true;
                    SystemStatus = "모니터링 중";
                    LoggingService.Instance.LogInfo("시스템 모니터링 시작");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"모니터링 시작 실패: {ex.Message}");
                SystemStatus = "오류";
            }
        }
        
        /// <summary>
        /// 모니터링 중지
        /// </summary>
        public void StopMonitoring()
        {
            try
            {
                if (IsMonitoring)
                {
                    _monitoringTimer.Stop();
                    IsMonitoring = false;
                    SystemStatus = "정상";
                    LoggingService.Instance.LogInfo("시스템 모니터링 중지");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"모니터링 중지 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 수동으로 시스템 메트릭 업데이트
        /// </summary>
        public void RefreshMetrics()
        {
            UpdateSystemMetrics();
        }
        
        /// <summary>
        /// 시스템 정보 요약 문자열 반환
        /// </summary>
        public string GetSystemSummary()
        {
            return $"CPU: {CpuUsage:F1}%, 메모리: {MemoryUsage:F1}% ({MemoryUsageMB:F0}MB), " +
                   $"프로세스: {ProcessMemoryMB:F1}MB, 가동시간: {Uptime:hh\\:mm\\:ss}";
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 타이머 이벤트 핸들러
        /// </summary>
        private void MonitoringTimer_Tick(object sender, EventArgs e)
        {
            UpdateSystemMetrics();
        }
        
        /// <summary>
        /// 시스템 메트릭 업데이트
        /// </summary>
        private void UpdateSystemMetrics()
        {
            try
            {
                // CPU 사용률 업데이트
                var cpuUsage = _cpuCounter.NextValue();
                CpuUsage = Math.Round(cpuUsage, 1);
                
                // 메모리 사용률 계산
                var availableMemoryMB = _memoryCounter.NextValue();
                var totalMemoryMB = TotalMemoryGB * 1024;
                var usedMemoryMB = totalMemoryMB - availableMemoryMB;
                
                MemoryUsageMB = Math.Round(usedMemoryMB, 0);
                MemoryUsage = Math.Round((usedMemoryMB / totalMemoryMB) * 100, 1);
                
                // 시스템 상태 결정
                UpdateSystemStatus();
                
                // 가끔씩 프로세스 정보 새로고침
                _currentProcess.Refresh();
                
                OnPropertyChanged(nameof(Uptime));
                OnPropertyChanged(nameof(ProcessMemoryMB));
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"시스템 메트릭 업데이트 실패: {ex.Message}");
                SystemStatus = "오류";
            }
        }
        
        /// <summary>
        /// 시스템 상태 업데이트
        /// </summary>
        private void UpdateSystemStatus()
        {
            if (CpuUsage > 90 || MemoryUsage > 90)
            {
                SystemStatus = "높은 사용률";
            }
            else if (CpuUsage > 70 || MemoryUsage > 70)
            {
                SystemStatus = "보통";
            }
            else
            {
                SystemStatus = "정상";
            }
        }
        
        /// <summary>
        /// 전체 물리적 메모리 크기 가져오기 (MB)
        /// </summary>
        private double GetTotalPhysicalMemoryMB()
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    var totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    return totalBytes / (1024.0 * 1024.0); // Convert to MB
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"메모리 크기 조회 실패: {ex.Message}");
            }
            
            // 기본값 (8GB)
            return 8192;
        }
        
        #endregion
        
        #region INotifyPropertyChanged Implementation
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            try
            {
                StopMonitoring();
                _monitoringTimer?.Stop();
                _cpuCounter?.Dispose();
                _memoryCounter?.Dispose();
                _currentProcess?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"SystemMonitoringService Dispose 오류: {ex.Message}");
            }
        }
        
        #endregion
    }
} 