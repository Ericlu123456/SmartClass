using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using smartClass.Models;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class NotificationWindow : Window
    {
        private readonly Notification _notification;

        public NotificationWindow(Notification notification, double fontSize)
        {
            InitializeComponent();

            _notification = notification ?? throw new ArgumentNullException(nameof(notification));

            // 设置内容和字体大小
            ContentText.Text = _notification.Content;
            if (!double.IsNaN(fontSize) && !double.IsInfinity(fontSize) && fontSize > 0)
            {
                ContentText.FontSize = fontSize;
            }

            // 关闭按钮
            CloseBtn.Click += (s, e) =>
            {
                try { CloseNotification(); }
                catch (Exception ex) { LogService.Log(ex, "NotificationWindow 关闭按钮"); }
            };

            // 拖拽移动
            this.MouseLeftButtonDown += (s, e) =>
            {
                try
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                        DragMove();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "NotificationWindow 拖拽失败");
                }
            };

            // 定位到屏幕右下角
            this.Loaded += (s, e) =>
            {
                try
                {
                    var screen = SystemParameters.WorkArea;
                    UpdateLayout();
                    var h = ActualHeight;
                    if (double.IsNaN(h) || h <= 0) h = 120;
                    var w = ActualWidth;
                    if (double.IsNaN(w) || w <= 0) w = 250;
                    Left = screen.Right - w - 20;
                    Top = screen.Bottom - h - 20;
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "NotificationWindow 定位失败");
                }
            };
        }

        private void CloseNotification()
        {
            try
            {
                var state = StorageService.Load();
                var toRemove = state.Notifications.FirstOrDefault(n => n.Id == _notification.Id);
                if (toRemove != null)
                {
                    state.Notifications.Remove(toRemove);
                    StorageService.Save(state);
                    LogService.Log($"通知已删除: {_notification.Content}");
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "NotificationWindow 移除通知失败");
            }
            finally
            {
                Close();
            }
        }
    }
}
