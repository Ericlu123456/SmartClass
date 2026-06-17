using System;
using System.Configuration;
using System.Data;
using System.Windows;
using smartClass.Services;

namespace smartClass
{
    public partial class App : System.Windows.Application
    {
        private SingleInstanceManager _singleInstanceManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 检查是否已有实例运行
            _singleInstanceManager = new SingleInstanceManager("SmartClass");
            if (_singleInstanceManager.IsAnotherInstanceRunning())
            {
                System.Windows.MessageBox.Show("应用程序已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            // 创建但不显示主窗口，进入隐藏模式
            var main = new MainWindow();
            main.InitializeHiddenMode();

            // 不调用 main.Show()
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _singleInstanceManager?.Dispose();
            base.OnExit(e);
        }
    }
}
