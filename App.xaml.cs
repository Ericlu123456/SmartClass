using System;
using System.Windows;
using System.Windows.Threading;
using smartClass.Services;

namespace smartClass
{
    public partial class App : System.Windows.Application
    {
        private SingleInstanceManager _singleInstanceManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 注册全局异常处理（在任何可能崩溃的操作之前）
            SetupGlobalExceptionHandlers();

            // 检查是否已有实例运行
            _singleInstanceManager = new SingleInstanceManager("SmartClass");
            if (_singleInstanceManager.IsAnotherInstanceRunning())
            {
                LogService.Log("检测到已有实例运行，退出");
                System.Windows.MessageBox.Show("应用程序已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            try
            {
                // 创建但不显示主窗口，进入隐藏模式
                var main = new MainWindow();
                main.InitializeHiddenMode();
                LogService.Log("应用启动成功，进入托盘隐藏模式");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "应用启动失败");
                System.Windows.MessageBox.Show(
                    $"程序启动失败:\n{ex.Message}\n\n详细信息已写入 error.log",
                    "启动错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
            }

            // 不调用 main.Show()
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LogService.Log("应用正常退出");
                _singleInstanceManager?.Dispose();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "退出清理异常");
            }
            base.OnExit(e);
        }

        /// <summary>
        /// 注册全局未处理异常处理器，确保所有崩溃都被记录
        /// </summary>
        private void SetupGlobalExceptionHandlers()
        {
            // WPF UI 线程未处理异常
            DispatcherUnhandledException += (s, e) =>
            {
                LogService.Log(e.Exception, "UI线程未处理异常");
                e.Handled = true; // 防止进程直接终止
                System.Windows.MessageBox.Show(
                    $"程序发生未预期错误:\n{e.Exception.Message}\n\n详细信息已写入 error.log，程序将尝试继续运行。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };

            // 非 UI 线程未处理异常
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    LogService.Log(ex, "非UI线程未处理异常(进程即将终止)");
                }
                else
                {
                    LogService.Log($"非UI线程未处理异常(非Exception对象): {e.ExceptionObject}");
                }
            };

            // 任务中未观察到的异常
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogService.Log(e.Exception, "未观察到的Task异常");
                e.SetObserved(); // 防止进程终止
            };
        }
    }
}
