using System;
using System.Windows;
using System.Windows.Threading;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class AutoCloseDialog : Window
    {
        private DispatcherTimer _closeTimer;
        private bool _isClosed = false;

        public AutoCloseDialog(string message, TimeSpan timeout)
        {
            InitializeComponent();
            MessageText.Text = message;

            YesBtn.Click += (s, e) =>
            {
                try
                {
                    StopTimer();
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "AutoCloseDialog Yes 按钮");
                }
            };

            NoBtn.Click += (s, e) =>
            {
                try
                {
                    StopTimer();
                    DialogResult = false;
                    Close();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "AutoCloseDialog No 按钮");
                }
            };

            // 窗口关闭时确保定时器停止
            this.Closing += (s, e) =>
            {
                StopTimer();
            };

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = timeout;
            _closeTimer.Tick += (s, e) =>
            {
                try
                {
                    if (!_isClosed)
                    {
                        StopTimer();
                        _isClosed = true;
                        DialogResult = true; // 超时默认视为"是"（已擦黑板）
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "AutoCloseDialog 超时关闭");
                }
            };
            _closeTimer.Start();
        }

        private void StopTimer()
        {
            try
            {
                if (_closeTimer != null && _closeTimer.IsEnabled)
                {
                    _closeTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "AutoCloseDialog 停止定时器");
            }
        }
    }
}
