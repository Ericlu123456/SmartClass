using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Forms;
using smartClass.Models;
using smartClass.Services;
using smartClass.Windows;

namespace smartClass
{
    public partial class MainWindow : Window
    {
        private AppState _state = new AppState();
        private DispatcherTimer _timer;
        private DispatcherTimer _clockTimer;
        private NotifyIcon _notifyIcon;
        private ExamClockWindow? _examClock;
        private ScheduleWindow? _scheduleWindow;
        private bool _allowClose = false;
        private bool _isShuttingDown = false;


        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            // 阻止 Alt+F4 关闭
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.System && e.SystemKey == System.Windows.Input.Key.F4)
                {
                    e.Handled = true;
                }
            };

            // 点击时钟 → 全屏数字时钟
            ClockLabel.MouseLeftButtonDown += (s, e) =>
            {
                try { ShowExamClock(); }
                catch (Exception ex) { LogService.Log(ex, "全屏时钟打开失败"); }
            };
        }

        /// <summary>
        /// 顶栏模式初始化：加载数据、定位到屏幕顶部、启动定时器
        /// </summary>
        public void InitializeTopBar()
        {
            try
            {
                _state = StorageService.Load();
                LogService.Log($"数据加载完成: {_state.Students.Count} 学生, {_state.Courses.Count} 课程, {_state.DutyGroups.Count} 值日组");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "InitializeTopBar 加载数据失败");
                _state = new AppState();
            }

            try
            {
                SetupTrayIcon();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "托盘图标初始化失败");
            }

            try
            {
                StartTimers();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "定时器启动失败");
            }

            // 窗口句柄就绪后定位到屏幕顶部并预留工作区
            SourceInitialized += (s, e) =>
            {
                try { PositionAtTop(); }
                catch (Exception ex) { LogService.Log(ex, "顶栏定位失败"); }
            };

            // 延迟检查昨日值日
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { CheckYesterdayDuties(); }
                catch (Exception ex) { LogService.Log(ex, "昨日值日检查失败"); }
            }), DispatcherPriority.ApplicationIdle);

            // 延迟创建课程表浮窗
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { ShowScheduleWindow(); }
                catch (Exception ex) { LogService.Log(ex, "课程表窗口创建失败"); }
            }), DispatcherPriority.ApplicationIdle);

            // 首次刷新 UI
            RefreshUI();
        }

        #region 窗口定位

        private void PositionAtTop()
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (screen == null) return;
            var bounds = screen.Bounds;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = 70;
            Topmost = true;

            // HWND 就绪后注册为 AppBar（自动缩小工作区排开桌面图标）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { AppBarHelper.Register(this); }
                catch (Exception ex) { LogService.Log(ex, "AppBar 注册失败"); }
            }), DispatcherPriority.ApplicationIdle);
        }

        #endregion

        #region 定时器

        private void StartTimers()
        {
            // 业务逻辑定时器（60秒检查课程/值日）
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            LogService.Log("业务定时器已启动 (间隔60秒)");

            // UI 刷新定时器（1秒刷新时钟和倒计时）
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                RefreshUI();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "时钟刷新失败");
            }
        }

        #endregion

        #region UI 刷新

        /// <summary>
        /// 刷新顶栏所有 UI 元素（日期、时钟、倒计时、课程、值日）
        /// </summary>
        private void RefreshUI()
        {
            var now = DateTime.Now;

            // 日期：M月d日  ddd
            var weekMap = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            DateLabel.Text = $"{now.Month}月{now.Day}日  {weekMap[(int)now.DayOfWeek]}";

            // 时钟
            ClockLabel.Text = now.ToString("HH:mm:ss");

            // 倒计时
            var daysLeft = (_state.SemesterEndDate.Date - now.Date).Days;
            if (daysLeft > 0)
                CountdownLabel.Text = $"距期末 {daysLeft} 天";
            else if (daysLeft == 0)
                CountdownLabel.Text = "学期最后一天";
            else
                CountdownLabel.Text = "假期啦";

            // 今日课程
            var todayText = weekMap[(int)now.DayOfWeek];
            var todayCourses = _state.Courses
                .Where(c => c.DayOfWeek == todayText)
                .OrderBy(c => c.StartTime)
                .ToList();

            // 找到下一节和下二节
            string nextText = "", next2Text = "";
            foreach (var c in todayCourses)
            {
                if (TimeSpan.TryParse(c.StartTime, out var start))
                {
                    var courseTime = new TimeSpan(now.Hour, now.Minute, 0);
                    if (start > courseTime)
                    {
                        if (string.IsNullOrEmpty(nextText))
                            nextText = $"{c.Subject}  {c.StartTime}-{c.EndTime}";
                        else if (string.IsNullOrEmpty(next2Text))
                        {
                            next2Text = $"{c.Subject}  {c.StartTime}-{c.EndTime}";
                            break;
                        }
                    }
                }
            }
            NextCourseLabel.Text = string.IsNullOrEmpty(nextText) ? "今日课程已结束" : nextText;
            Next2CourseLabel.Text = next2Text;

        }

        #endregion

        #region 托盘图标

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = true;

            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                    _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "提取exe图标失败");
            }

            if (_notifyIcon.Icon == null)
                LogService.Log("托盘图标为 null，将使用系统默认图标");

            _notifyIcon.Text = "SmartClass";

            var menu = new ContextMenuStrip();
            var settingsItem = new ToolStripMenuItem("设置");
            settingsItem.Click += (s, e) =>
            {
                try { OpenSettings(); }
                catch (Exception ex) { LogService.Log(ex, "托盘菜单-设置"); }
            };
            var showSchedItem = new ToolStripMenuItem("显示/隐藏课程表");
            showSchedItem.Click += (s, e) =>
            {
                try { ToggleScheduleWindow(); }
                catch (Exception ex) { LogService.Log(ex, "托盘菜单-切换课程表"); }
            };
            var exportItem = new ToolStripMenuItem("导出报表");
            exportItem.Click += (s, e) =>
            {
                try { ExportReports(); }
                catch (Exception ex) { LogService.Log(ex, "托盘菜单-导出报表"); }
            };
            var exitItem = new ToolStripMenuItem("退出程序");
            exitItem.Click += (s, e) => { _allowClose = true; Close(); };
            var restartItem = new ToolStripMenuItem("重启程序");
            restartItem.Click += (s, e) =>
            {
                try { RestartApplication(); }
                catch (Exception ex) { LogService.Log(ex, "托盘菜单-重启"); }
            };

            menu.Items.Add(settingsItem);
            menu.Items.Add(showSchedItem);
            menu.Items.Add(exportItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(restartItem);
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        #endregion

        #region 设置

        private void OpenSettings()
        {
            Dispatcher.Invoke(() =>
            {
                var win = new SettingsWindow();
                win.Owner = this;
                win.ShowDialog();
                _state = StorageService.Load();
                RefreshUI();
                UpdateScheduleWindow();
            });
        }

        #endregion

        #region 课程表浮窗

        private void ShowScheduleWindow()
        {
            if (_scheduleWindow != null) return;
            Dispatcher.Invoke(() =>
            {
                try
                {
                    _scheduleWindow = new ScheduleWindow(_state);
                    _scheduleWindow.Closed += (s, e) => _scheduleWindow = null;
                    _scheduleWindow.Show();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "创建课程表窗口失败");
                    _scheduleWindow = null;
                }
            });
        }

        private void ToggleScheduleWindow()
        {
            if (_scheduleWindow == null)
                ShowScheduleWindow();
            else
            {
                try { _scheduleWindow.Close(); }
                catch (Exception ex) { LogService.Log(ex, "关闭课程表窗口"); }
                _scheduleWindow = null;
            }
        }

        public void UpdateScheduleWindow()
        {
            try
            {
                if (_scheduleWindow != null)
                    _scheduleWindow.UpdateState(_state);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "更新课程表窗口失败");
                _scheduleWindow = null;
                ShowScheduleWindow();
            }
        }

        #endregion

        #region 全屏时钟

        private void ShowExamClock()
        {
            if (_examClock != null && _examClock.IsVisible) return;

            _examClock = new ExamClockWindow();
            _examClock.Closed += (s, e) => _examClock = null;
            _examClock.Show();
        }

        #endregion

        #region 业务逻辑定时器

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_isShuttingDown) return;

                var now = DateTime.Now;

                foreach (var c in _state.Courses)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(c.StartTime) ||
                            string.IsNullOrWhiteSpace(c.EndTime) ||
                            string.IsNullOrWhiteSpace(c.DayOfWeek))
                            continue;

                        if (!MatchesToday(c.DayOfWeek)) continue;

                        if (TimeMatches(now, c.StartTime))
                            AskBoardCleaned(c);

                        if (TimeMatches(now, c.EndTime))
                            NotifyAfterClass(c);
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(ex, $"处理课程 '{c.Subject}' 时出错");
                    }
                }

                // 放学提醒
                if (now.Hour == 17 && now.Minute == 30)
                {
                    try { NotifyDutyAtEndOfDay(); }
                    catch (Exception ex) { LogService.Log(ex, "放学提醒失败"); }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "Timer_Tick 顶层异常(定时器继续运行)");
            }
        }

        private bool MatchesToday(string dayOfWeek)
        {
            var map = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            return dayOfWeek == map[(int)DateTime.Now.DayOfWeek];
        }

        private bool TimeMatches(DateTime now, string time)
        {
            if (!TimeSpan.TryParse(time, out var t)) return false;
            return now.Hour == t.Hours && now.Minute == t.Minutes;
        }

        #endregion

        #region 提醒 / 对话框

        private void NotifyAfterClass(Models.Course course)
        {
            try
            {
                var text = $"课程 {course.Subject} 下课，请老师确认并提醒值日生擦黑板。";
                _notifyIcon?.ShowBalloonTip(5000, "下课提醒", text, ToolTipIcon.Info);
                if (_state.EnableSpeech)
                    SpeechService.SpeakAsync($"下课了。{course.Subject}课程已结束，请提醒值日生擦黑板。");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, $"下课提醒失败: {course.Subject}");
            }
        }

        private void AskBoardCleaned(Models.Course course)
        {
            if (_state.EnableSpeech)
                SpeechService.SpeakAsync($"上课了。{course.Subject}课程开始，请检查黑板是否已擦干净。");

            Dispatcher.Invoke(() =>
            {
                try
                {
                    var dlg = new AutoCloseDialog(
                        $"课程 {course.Subject} 上课：黑板是否已擦？",
                        TimeSpan.FromMinutes(10));
                    var res = dlg.ShowDialog();
                    if (res == true)
                        ApplyBoardResult(course, true);
                    else if (res == false)
                        ApplyBoardResult(course, false);
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, $"黑板询问对话框失败: {course.Subject}");
                }
            });
        }

        private void ApplyBoardResult(Models.Course course, bool cleaned)
        {
            try
            {
                var today = DateTime.Today;
                var duty = _state.DailyDuties.FirstOrDefault(d => d.Date.Date == today);
                if (duty == null) return;
                var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
                if (group == null) return;

                foreach (var m in group.Members)
                {
                    var student = _state.Students.FirstOrDefault(s => s.Id == m.StudentId);
                    if (student == null) continue;
                    student.SocialCredits += cleaned ? 1 : -1;
                }

                StorageService.Save(_state);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, $"黑板评分失败: {course.Subject}, cleaned={cleaned}");
            }
        }

        private void NotifyDutyAtEndOfDay()
        {
            try
            {
                var today = DateTime.Today;
                var duty = _state.DailyDuties.FirstOrDefault(d => d.Date.Date == today);
                if (duty == null) return;
                var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
                if (group == null) return;

                var names = group.Members.Select(m =>
                    _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "未知");
                var text = "请值日生执行任务: " + string.Join(",", names);
                _notifyIcon?.ShowBalloonTip(7000, "放学提醒", text, ToolTipIcon.Info);
                if (_state.EnableSpeech)
                    SpeechService.SpeakAsync("放学时间到。请今天的值日生" +
                        string.Join("、", names) + "执行值日任务。");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "放学值日通知失败");
            }
        }

        private void CheckYesterdayDuties()
        {
            try
            {
                var yesterday = DateTime.Today.AddDays(-1);
                var duty = _state.DailyDuties.FirstOrDefault(d => d.Date.Date == yesterday);
                if (duty == null) return;

                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var res = System.Windows.MessageBox.Show(
                            "昨日值日是否已完成？\n点击 是=完成，否=未完成",
                            "昨日值日",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
                        if (group == null) return;
                        foreach (var m in group.Members)
                        {
                            var student = _state.Students.FirstOrDefault(s => s.Id == m.StudentId);
                            if (student == null) continue;
                            student.SocialCredits += (res == MessageBoxResult.Yes) ? 5 : -5;
                        }
                        StorageService.Save(_state);
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(ex, "昨日值日检查对话框");
                    }
                });
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "昨日值日检查失败");
            }
        }

        #endregion

        #region 报表 / 重启

        private void ExportReports()
        {
            try
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                var path = System.IO.Path.Combine(dir, "students.csv");
                using (var sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("Id,Name,SocialCredits");
                    foreach (var s in _state.Students)
                        sw.WriteLine($"{s.Id},{s.Name},{s.SocialCredits}");
                }
                _notifyIcon?.ShowBalloonTip(4000, "导出完成", "导出 students.csv 到应用目录", ToolTipIcon.Info);
                LogService.Log($"导出报表完成: {path}");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "导出报表失败");
                _notifyIcon?.ShowBalloonTip(4000, "导出失败", "导出 students.csv 失败，请查看 error.log", ToolTipIcon.Error);
            }
        }

        private void RestartApplication()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    System.Diagnostics.Process.Start(exePath);
                AppBarHelper.Unregister();
                _allowClose = true;
                _isShuttingDown = true;
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "重启失败");
                System.Windows.MessageBox.Show("重启失败：\n" + ex.Message, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 公开方法

        public void ShowTestNotification(string title, string text, int durationMs = 5000)
        {
            try
            {
                _notifyIcon?.ShowBalloonTip(durationMs, title, text, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "测试通知失败");
            }
        }

        public void ReloadState()
        {
            try
            {
                _state = StorageService.Load();
                RefreshUI();
                UpdateScheduleWindow();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "MainWindow 重新加载状态失败");
            }
        }

        #endregion

        #region 生命周期

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_state.Students == null || !_state.Students.Any())
                    _state = StorageService.Load();
                RefreshUI();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "MainWindow_Loaded");
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 阻止 Alt+F4 / 系统菜单关闭，仅允许托盘菜单"退出"触发
            if (!_allowClose)
            {
                e.Cancel = true;
                return;
            }

            _isShuttingDown = true;

            AppBarHelper.Unregister();

            try { _timer?.Stop(); LogService.Log("业务定时器已停止"); }
            catch (Exception ex) { LogService.Log(ex, "停止定时器失败"); }

            try { _clockTimer?.Stop(); }
            catch (Exception ex) { LogService.Log(ex, "停止时钟定时器失败"); }

            try { _scheduleWindow?.Close(); }
            catch (Exception ex) { LogService.Log(ex, "关闭课程表窗口失败"); }

            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "释放托盘图标失败"); }

            try
            {
                StorageService.Save(_state);
                LogService.Log("退出时数据已保存");
            }
            catch (Exception ex) { LogService.Log(ex, "退出保存数据失败"); }
        }

        #endregion
    }
}
