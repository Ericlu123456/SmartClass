using System;
using System.Windows;
using System.Windows.Threading;

namespace SamrtClass.Windows
{
    public partial class AutoCloseDialog : Window
    {
        private DispatcherTimer _closeTimer;
        public AutoCloseDialog(string message, TimeSpan timeout)
        {
            InitializeComponent();
            MessageText.Text = message;
            YesBtn.Click += (s, e) => { DialogResult = true; Close(); };
            NoBtn.Click += (s, e) => { DialogResult = false; Close(); };

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = timeout;
            _closeTimer.Tick += (s, e) => { _closeTimer.Stop(); DialogResult = null; Close(); };
            _closeTimer.Start();
        }
    }
}
