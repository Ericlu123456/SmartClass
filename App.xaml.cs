using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SamrtClass
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 创建但不显示主窗口，进入隐藏模式
            var main = new MainWindow();
            main.InitializeHiddenMode();

            // 不调用 main.Show()
        }
    }
}
