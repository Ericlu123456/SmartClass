using System;
using System.Windows;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class NotificationInputDialog : Window
    {
        public string NotificationContent { get; private set; }

        public NotificationInputDialog()
        {
            InitializeComponent();

            OkBtn.Click += (s, e) =>
            {
                try
                {
                    NotificationContent = ContentTextBox.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(NotificationContent))
                    {
                        DialogResult = true;
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "NotificationInputDialog 确认按钮");
                }
            };

            CancelBtn.Click += (s, e) =>
            {
                try
                {
                    DialogResult = false;
                    Close();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "NotificationInputDialog 取消按钮");
                }
            };
        }
    }
}
