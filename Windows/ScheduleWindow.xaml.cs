using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using smartClass.Models;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class ScheduleWindow : Window
    {
        private AppState _state;

        // 窗口置底
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        public ScheduleWindow(AppState state)
        {
            InitializeComponent();

            _state = state ?? new AppState();

            this.SizeToContent = SizeToContent.WidthAndHeight;

            // 工具栏按钮事件
            ToolSettingsBtn.Click += (s, e) =>
            {
                try { OpenSettings(); }
                catch (Exception ex) { LogService.Log(ex, "ScheduleWindow 工具栏-设置"); }
            };
            ToolAddNotificationBtn.Click += (s, e) =>
            {
                try { OpenAddNotification(); }
                catch (Exception ex) { LogService.Log(ex, "ScheduleWindow 工具栏-添加提醒"); }
            };
            ToolExitBtn.Click += (s, e) =>
            {
                try
                {
                    LogService.Log("从课程表窗口退出程序");
                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex) { LogService.Log(ex, "ScheduleWindow 工具栏-退出"); }
            };

            // 拖拽移动
            this.MouseLeftButtonDown += ScheduleWindow_MouseLeftButtonDown;
            // 拖拽后保存位置
            this.MouseLeftButtonUp += (s, e) => SavePosition();

            // 窗口激活时推回 Z 轴底层
            this.Activated += (s, e) => SendToBottom();

            try
            {
                UpdateUI();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 初始 UpdateUI 失败");
            }

            this.UpdateLayout();

            // 延迟定位并置底
            this.Loaded += (s, e) =>
            {
                try
                {
                    if (!RestorePosition())
                        PositionWindowBottom();
                    SendToBottom();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "ScheduleWindow 定位失败");
                    PositionWindowBottom();
                }
            };
        }

        /// <summary>
        /// 将窗口推至 Z 轴最底层（桌面图标上方）
        /// </summary>
        private void SendToBottom()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 置底失败");
            }
        }

        #region 工具栏

        private void OpenSettings()
        {
            var win = new SettingsWindow();
            win.Owner = this;
            win.ShowDialog();
            _state = StorageService.Load();
            UpdateUI();
            this.UpdateLayout();
            try
            {
                var main = System.Windows.Application.Current?.MainWindow as MainWindow;
                main?.ReloadState();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 同步主窗口状态失败");
            }
        }

        private void OpenAddNotification()
        {
            var dlg = new NotificationInputDialog();
            dlg.Owner = this;
            var result = dlg.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(dlg.NotificationContent))
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = dlg.NotificationContent,
                    CreatedAt = DateTime.Now
                };
                _state.Notifications.Add(notification);
                StorageService.Save(_state);

                var win = new NotificationWindow(notification, _state.FontSize);
                win.Show();
            }
        }

        #endregion

        #region 窗口定位

        private bool RestorePosition()
        {
            try
            {
                if (_state.ScheduleWindowLeft >= 0 && _state.ScheduleWindowTop >= 0)
                {
                    var screen = SystemParameters.WorkArea;
                    if (_state.ScheduleWindowLeft >= screen.Left - 50 &&
                        _state.ScheduleWindowLeft < screen.Right - 50 &&
                        _state.ScheduleWindowTop >= screen.Top - 50 &&
                        _state.ScheduleWindowTop < screen.Bottom - 50)
                    {
                        Left = _state.ScheduleWindowLeft;
                        Top = _state.ScheduleWindowTop;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 恢复位置失败");
            }
            return false;
        }

        private void SavePosition()
        {
            try
            {
                if (!double.IsNaN(Left) && !double.IsNaN(Top))
                {
                    _state.ScheduleWindowLeft = Left;
                    _state.ScheduleWindowTop = Top;
                    StorageService.Save(_state);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 保存位置失败");
            }
        }

        private void PositionWindowBottom()
        {
            var screen = SystemParameters.WorkArea;

            UpdateLayout();

            double h = ActualHeight;
            if (double.IsNaN(h) || h <= 0) h = Height;
            if (double.IsNaN(h) || h <= 0) h = 200;

            Left = screen.Left + 20;
            Top = screen.Bottom - h - 20;

            if (double.IsNaN(Top) || Top < screen.Top)
                Top = screen.Top + 20;
        }

        #endregion

        #region 拖拽

        private void ScheduleWindow_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 拖拽失败");
            }
        }

        #endregion

        #region UI 更新

        public void UpdateState(AppState state)
        {
            if (state == null) return;
            _state = state;

            try
            {
                UpdateUI();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow UpdateUI 失败");
            }

            UpdateLayout();
        }

        private void UpdateUI()
        {
            var map = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            var today = DateTime.Now.DayOfWeek;
            var todayText = map[(int)today];

            CoursesList.ItemsSource =
                _state.Courses.Where(c => c.DayOfWeek == todayText).ToList();

            var duty = _state.DailyDuties.FirstOrDefault(d => d.Date.Date == DateTime.Today);
            if (duty == null)
            {
                DutyList.ItemsSource = null;
            }
            else
            {
                var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
                if (group == null)
                {
                    DutyList.ItemsSource = null;
                }
                else
                {
                    DutyList.ItemsSource = group.Members
                        .Select(m => new
                        {
                            Name = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "",
                            Role = m.Role ?? ""
                        })
                        .ToList();
                }
            }

            try
            {
                var fs = _state.FontSize;
                if (!double.IsNaN(fs) && !double.IsInfinity(fs) && fs > 0)
                {
                    CoursesList.FontSize = fs;
                    DutyList.FontSize = fs;
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 字体大小设置失败");
            }
        }

        #endregion
    }
}
