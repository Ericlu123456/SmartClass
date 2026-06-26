using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using smartClass.Models;
using smartClass.Services;
using smartClass.Windows;

namespace smartClass
{
    public partial class MainWindow : Window
    {
        private AppState _state = new AppState();
        private DispatcherTimer _timer;
        private NotifyIcon _notifyIcon;
        private ScheduleWindow? _scheduleWindow;
        private bool _isShuttingDown = false; // 防止退出时重复保存

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            SettingsBtn.Click += SettingsBtn_Click;
            ExitBtn.Click += ExitBtn_Click;
        }

        // 在 App.OnStartup 中调用此方法以初始化但不显示主窗口
        public void InitializeHiddenMode()
        {
            try
            {
                _state = StorageService.Load();
                LogService.Log($"数据加载完成: {_state.Students.Count} 学生, {_state.Courses.Count} 课程, {_state.DutyGroups.Count} 值日组, {_state.DailyDuties.Count} 值日安排");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "InitializeHiddenMode 加载数据失败");
                _state = new AppState();
            }

            try
            {
                SetupTrayIcon();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "托盘图标初始化失败");
                // 托盘图标失败不应阻止程序运行，但用户需要知道
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "托盘图标初始化失败，请查看 error.log";
                });
            }

            try
            {
                StartTimer();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "定时器启动失败");
            }

            // 延迟执行开机昨日检查（等 UI 完全就绪）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    CheckYesterdayDuties();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "昨日值日检查失败");
                }
            }), DispatcherPriority.ApplicationIdle);

            // 延迟创建并显示桌面底部课程窗口
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    ShowScheduleWindow();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "课程表窗口创建失败");
                }
            }), DispatcherPriority.ApplicationIdle);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 如果以正常方式显示窗口，也需要初始化
                if (_state.Students == null || !_state.Students.Any())
                {
                    _state = StorageService.Load();
                }

                StatusText.Text = "已加载数据, 学生: " + _state.Students.Count;

                if (_notifyIcon == null)
                {
                    SetupTrayIcon();
                }

                if (_timer == null)
                {
                    StartTimer();
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "MainWindow_Loaded");
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = true;

            // 安全提取图标：优先使用 exe 图标，失败则使用默认图标
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "提取exe图标失败");
            }

            // 如果图标提取失败，_notifyIcon.Icon 为 null，系统会使用默认图标
            if (_notifyIcon.Icon == null)
            {
                LogService.Log("托盘图标为 null，将使用系统默认图标");
            }

            _notifyIcon.Text = "SmartClass";
            _notifyIcon.DoubleClick += (s, e) =>
            {
                try { ShowMainWindow(); }
                catch (Exception ex) { LogService.Log(ex, "托盘双击"); }
            };

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
            exitItem.Click += (s, e) => Close();
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

        private void ShowMainWindow()
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
        }

        private void OpenSettings()
        {
            Dispatcher.Invoke(() =>
            {
                var win = new SettingsWindow();
                win.Owner = this;
                win.ShowDialog();
                // reload
                _state = StorageService.Load();
                StatusText.Text = "已加载数据, 学生: " + _state.Students.Count;
                UpdateScheduleWindow();
            });
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            try { OpenSettings(); }
            catch (Exception ex) { LogService.Log(ex, "设置按钮"); }
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            LogService.Log("主定时器已启动 (间隔60秒)");
        }

        /// <summary>
        /// 定时器回调：检查课程和值日提醒。
        /// 关键：任何未处理异常都会导致 DispatcherTimer 永久停止，
        /// 因此必须捕获所有异常。
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_isShuttingDown) return;

                var now = DateTime.Now;

                // 检查每节课的上课与下课时间
                foreach (var c in _state.Courses)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(c.StartTime) ||
                            string.IsNullOrWhiteSpace(c.EndTime) ||
                            string.IsNullOrWhiteSpace(c.DayOfWeek))
                            continue;

                        if (!MatchesToday(c.DayOfWeek))
                            continue;

                        if (TimeMatches(now, c.StartTime))
                        {
                            AskBoardCleaned(c);
                        }

                        if (TimeMatches(now, c.EndTime))
                        {
                            NotifyAfterClass(c);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(ex, $"处理课程 '{c.Subject}' 时出错");
                    }
                }

                // 放学提醒
                if (now.Hour == 17 && now.Minute == 30)
                {
                    try
                    {
                        NotifyDutyAtEndOfDay();
                    }
                    catch (Exception ex)
                    {
                        LogService.Log(ex, "放学提醒失败");
                    }
                }
            }
            catch (Exception ex)
            {
                // 最外层兜底：确保定时器永远不会因异常停止
                LogService.Log(ex, "Timer_Tick 顶层异常(定时器继续运行)");
            }
        }

        private bool MatchesToday(string dayOfWeek)
        {
            var map = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            var today = DateTime.Now.DayOfWeek;
            var todayText = map[(int)today];
            return dayOfWeek == todayText;
        }

        private bool TimeMatches(DateTime now, string time)
        {
            if (!TimeSpan.TryParse(time, out var t))
                return false;
            return now.Hour == t.Hours && now.Minute == t.Minutes;
        }

        private void NotifyAfterClass(Models.Course course)
        {
            try
            {
                var text = $"课程 {course.Subject} 下课，请老师确认并提醒值日生擦黑板。";
                _notifyIcon?.ShowBalloonTip(5000, "下课提醒", text, ToolTipIcon.Info);
                // 语音播报下课提醒
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
            // 语音播报上课提醒
            if (_state.EnableSpeech)
                SpeechService.SpeakAsync($"上课了。{course.Subject}课程开始，请检查黑板是否已擦干净。");

            // 弹出可自动关闭对话框询问老师，10分钟后自动关闭
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var dlg = new AutoCloseDialog($"课程 {course.Subject} 上课：黑板是否已擦？", TimeSpan.FromMinutes(10));
                    var res = dlg.ShowDialog();
                    if (res == true)
                    {
                        ApplyBoardResult(course, true);
                    }
                    else if (res == false)
                    {
                        ApplyBoardResult(course, false);
                    }
                    // res == null 表示超时，忽略
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
                // 语音播报放学值日提醒
                if (_state.EnableSpeech)
                    SpeechService.SpeakAsync("放学时间到。请今天的值日生" + string.Join("、", names) + "执行值日任务。");
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
                    {
                        sw.WriteLine($"{s.Id},{s.Name},{s.SocialCredits}");
                    }
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
                {
                    System.Diagnostics.Process.Start(exePath);
                }

                _isShuttingDown = true;
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "重启失败");
                System.Windows.MessageBox.Show(
                    "重启失败：\n" + ex.Message,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ToggleScheduleWindow()
        {
            if (_scheduleWindow == null)
            {
                ShowScheduleWindow();
            }
            else
            {
                try
                {
                    _scheduleWindow.Close();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "关闭课程表窗口");
                }
                _scheduleWindow = null;
            }
        }

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

        /// <summary>
        /// 供 SettingsWindow 开发者选项调用的测试通知方法
        /// </summary>
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

        /// <summary>
        /// 从存储重新加载状态（供 ScheduleWindow 设置变更后同步）
        /// </summary>
        public void ReloadState()
        {
            try
            {
                _state = StorageService.Load();
                StatusText.Text = "已加载数据, 学生: " + _state.Students.Count;
                UpdateScheduleWindow();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "MainWindow 重新加载状态失败");
            }
        }

        public void UpdateScheduleWindow()
        {
            try
            {
                if (_scheduleWindow != null)
                {
                    _scheduleWindow.UpdateState(_state);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "更新课程表窗口失败");
                // 如果更新失败，尝试重新创建
                _scheduleWindow = null;
                ShowScheduleWindow();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _isShuttingDown = true;

            try
            {
                _timer?.Stop();
                LogService.Log("主定时器已停止");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "停止定时器失败");
            }

            try
            {
                _scheduleWindow?.Close();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "关闭课程表窗口失败");
            }

            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "释放托盘图标失败");
            }

            try
            {
                StorageService.Save(_state);
                LogService.Log("退出时数据已保存");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "退出保存数据失败");
            }
        }
    }
}
