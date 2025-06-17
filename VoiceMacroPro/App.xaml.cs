using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace VoiceMacroPro
{
    /// <summary>
    /// VoiceMacro Pro 애플리케이션의 메인 클래스
    /// 애플리케이션의 시작과 종료를 관리합니다.
    /// </summary>
    public partial class App : Application
    {
        // 콘솔 창을 할당하기 위한 Win32 API
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();

        /// <summary>
        /// 애플리케이션 시작 시 실행되는 이벤트 핸들러
        /// 필요한 초기화 작업을 수행합니다.
        /// </summary>
        /// <param name="e">시작 이벤트 인수</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // 디버그 모드에서 콘솔 창 할당
#if DEBUG
                AllocConsole();
                Console.WriteLine("=== VoiceMacro Pro Debug Console ===");
                Console.WriteLine($"애플리케이션 시작: {DateTime.Now}");
#endif

                // 전역 예외 처리기 등록
                DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // 기본 초기화 호출
                base.OnStartup(e);

                // 애플리케이션 시작 로그
                System.Diagnostics.Debug.WriteLine("VoiceMacro Pro 애플리케이션이 시작되었습니다.");
                Console.WriteLine("VoiceMacro Pro 애플리케이션이 시작되었습니다.");
            }
            catch (Exception ex)
            {
                // 시작 중 오류 발생 시 사용자에게 알림
                Console.WriteLine($"애플리케이션 시작 오류: {ex}");
                MessageBox.Show($"애플리케이션 시작 중 오류가 발생했습니다:\n{ex.Message}", 
                              "시작 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 애플리케이션 종료
                Shutdown(1);
            }
        }

        /// <summary>
        /// 애플리케이션 종료 시 실행되는 이벤트 핸들러
        /// 정리 작업을 수행합니다.
        /// </summary>
        /// <param name="e">종료 이벤트 인수</param>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 리소스 정리 작업 수행
                // (현재는 특별한 정리 작업 없음)
                
                // 애플리케이션 종료 로그
                System.Diagnostics.Debug.WriteLine("VoiceMacro Pro 애플리케이션이 종료되었습니다.");
                Console.WriteLine("VoiceMacro Pro 애플리케이션이 종료되었습니다.");

#if DEBUG
                // 콘솔 창 해제
                FreeConsole();
#endif
            }
            catch (Exception ex)
            {
                // 종료 중 오류가 발생해도 강제 종료
                System.Diagnostics.Debug.WriteLine($"종료 중 오류 발생: {ex.Message}");
                Console.WriteLine($"종료 중 오류 발생: {ex.Message}");
            }
            finally
            {
                // 기본 종료 처리 호출
                base.OnExit(e);
            }
        }

        /// <summary>
        /// WPF 디스패처에서 처리되지 않은 예외를 처리하는 함수
        /// UI 스레드에서 발생한 예외를 전역적으로 처리합니다.
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">예외 이벤트 인수</param>
        private void App_DispatcherUnhandledException(object sender, 
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                // 콘솔에 오류 출력
                Console.WriteLine($"UI 스레드 예외 발생: {e.Exception}");
                
                // 사용자에게 오류 메시지 표시
                string errorMessage = $"예상치 못한 오류가 발생했습니다:\n\n" +
                                     $"오류 내용: {e.Exception.Message}\n\n" +
                                     $"애플리케이션을 계속 사용하시겠습니까?";

                var result = MessageBox.Show(errorMessage, "오류 발생", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    // 사용자가 계속 사용하기를 원하면 예외를 처리된 것으로 표시
                    e.Handled = true;
                }
                else
                {
                    // 사용자가 종료를 원하면 애플리케이션 종료
                    Shutdown(1);
                }

                // 오류 로그 기록 (디버그 모드에서)
                System.Diagnostics.Debug.WriteLine($"UI 스레드 예외: {e.Exception}");
            }
            catch
            {
                // 예외 처리 중에도 오류가 발생하면 강제 종료
                Shutdown(1);
            }
        }

        /// <summary>
        /// 애플리케이션 도메인에서 처리되지 않은 예외를 처리하는 함수
        /// 백그라운드 스레드에서 발생한 예외를 전역적으로 처리합니다.
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">예외 이벤트 인수</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                string errorMessage = exception?.Message ?? "알 수 없는 오류가 발생했습니다.";

                // 콘솔에 오류 출력
                Console.WriteLine($"애플리케이션 도메인 예외 발생: {exception}");

                // 치명적 오류 메시지 표시
                MessageBox.Show($"치명적인 오류가 발생했습니다:\n\n{errorMessage}\n\n" +
                              "애플리케이션이 종료됩니다.", 
                              "치명적 오류", MessageBoxButton.OK, MessageBoxImage.Stop);

                // 오류 로그 기록 (디버그 모드에서)
                System.Diagnostics.Debug.WriteLine($"애플리케이션 도메인 예외: {exception}");
            }
            catch
            {
                // 예외 처리 중에도 오류가 발생하면 무시하고 종료
            }
            finally
            {
                // 치명적 오류이므로 강제 종료
                Environment.Exit(1);
            }
        }
    }
} 