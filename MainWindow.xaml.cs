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
            _state = StorageService.Load();

            SetupTrayIcon();
            StartTimer();

            // 立即执行开机昨日检查
            CheckYesterdayDuties();

            // 创建并显示桌面底部课程窗口
            ShowScheduleWindow();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
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

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = true;
            /*
             * v1.1
             * 更改托盘图标：
             * 使用程序自身的 exe 图标作为系统托盘图标（Assets/smartclass.ico）
             */
            _notifyIcon.Icon =
                System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Diagnostics.Process
                        .GetCurrentProcess()
                        .MainModule!
                        .FileName!);
            _notifyIcon.Text = "SmartClass";
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            var menu = new ContextMenuStrip();
            var settingsItem = new ToolStripMenuItem("设置");
            settingsItem.Click += (s, e) => OpenSettings();
            var showSchedItem = new ToolStripMenuItem("显示/隐藏课程表");
            showSchedItem.Click += (s, e) => ToggleScheduleWindow();
            var exportItem = new ToolStripMenuItem("导出报表");
            exportItem.Click += (s, e) => ExportReports();
            var exitItem = new ToolStripMenuItem("退出程序");
            exitItem.Click += (s, e) => Close();
            var restartItem = new ToolStripMenuItem("重启程序");
            restartItem.Click += (s, e) => RestartApplication();

            menu.Items.Add(settingsItem);
            menu.Items.Add(showSchedItem);
            menu.Items.Add(exportItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);
            menu.Items.Add(restartItem);

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
            OpenSettings();
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30); // 更短用于测试，实际可调整为分钟
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            // 检查每节课的上课与下课时间
            foreach (var c in _state.Courses)
            {
                if (string.IsNullOrWhiteSpace(c.StartTime) || string.IsNullOrWhiteSpace(c.EndTime) || string.IsNullOrWhiteSpace(c.DayOfWeek)) continue;

                if (!MatchesToday(c.DayOfWeek)) continue;

                if (TimeMatches(now, c.StartTime))
                {
                    // 上课询问黑板是否已擦
                    AskBoardCleaned(c);
                }

                if (TimeMatches(now, c.EndTime))
                {
                    // 下课后提醒老师下课并提示值日生擦黑板
                    NotifyAfterClass(c);
                }
            }

            // 放学提醒：简单示例，若当前时间为 17:30 则提醒值日生做值日
            if (now.Hour == 17 && now.Minute == 30)
            {
                NotifyDutyAtEndOfDay();
            }
        }

        private bool MatchesToday(string dayOfWeek)
        {
            var map = new[] { "周日","周一","周二","周三","周四","周五","周六" };
            var today = DateTime.Now.DayOfWeek;
            var todayText = map[(int)today];
            return dayOfWeek == todayText;
        }

        private bool TimeMatches(DateTime now, string time)
        {
            if (!TimeSpan.TryParse(time, out var t)) return false;
            return now.Hour == t.Hours && now.Minute == t.Minutes;
        }

        private void NotifyAfterClass(Models.Course course)
        {
            var text = $"课程 {course.Subject} 下课，请老师确认并提醒值日生擦黑板。";
            _notifyIcon?.ShowBalloonTip(5000, "下课提醒", text, ToolTipIcon.Info);
        }

        private void AskBoardCleaned(Models.Course course)
        {
            // 弹出可自动关闭对话框询问老师，10分钟后自动关闭
            Dispatcher.Invoke(() =>
            {
                var dlg = new AutoCloseDialog($"课程 {course.Subject} 上课：黑板是否已擦？", TimeSpan.FromMinutes(10));
                var res = dlg.ShowDialog();
                if (res == true)
                {
                    // 已擦黑板 -> 值日生 social credits +1
                    ApplyBoardResult(course, true);
                }
                else if (res == false)
                {
                    ApplyBoardResult(course, false);
                }
                else
                {
                    // 超时或未作答，忽略
                }
            });
        }

        private void ApplyBoardResult(Models.Course course, bool cleaned)
        {
            // 简化：如果找到某个值日组并更新其成员 social credits
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

        private void NotifyDutyAtEndOfDay()
        {
            var today = DateTime.Today;
            var duty = _state.DailyDuties.FirstOrDefault(d => d.Date.Date == today);
            if (duty == null) return;
            var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
            if (group == null) return;

            var names = group.Members.Select(m => _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "");
            var text = "请值日生执行任务: " + string.Join(",", names);
            _notifyIcon?.ShowBalloonTip(7000, "放学提醒", text, ToolTipIcon.Info);
        }

        private void CheckYesterdayDuties()
        {
            var yesterday = DateTime.Today.AddDays(-1);
            var duty = _state.DailyDuties.FirstOrDefault(d => d.Date.Date == yesterday);
            if (duty == null) return; // 无记录

            // 弹出询问，若完成+5，未完成-5（简化为 MessageBox 确认）
            Dispatcher.Invoke(() =>
            {
                var res = System.Windows.MessageBox.Show("昨日值日是否已完成？\n点击 是=完成，否=未完成", "昨日值日", MessageBoxButton.YesNo, MessageBoxImage.Question);
                var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
                if (group == null) return;

                foreach (var m in group.Members)
                {
                    var student = _state.Students.FirstOrDefault(s => s.Id == m.StudentId);
                    if (student == null) continue;
                    student.SocialCredits += (res == MessageBoxResult.Yes) ? 5 : -5;
                }

                StorageService.Save(_state);
            });
        }

        private void ExportReports()
        {
            // 简单导出 CSV: students.csv
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
            }
            catch
            {
                // 忽略
            }
        }
        
        /*
         * v1.1
         * 增加重启程序支持及托盘选项：
         * 适应系统深浅模式
         */
        private void RestartApplication()
        {
            try
            {
                var exePath = System.Diagnostics.Process
                        .GetCurrentProcess()
                        .MainModule?
                        .FileName;
                
                if (!string.IsNullOrEmpty(exePath))
                {
                    System.Diagnostics.Process.Start(exePath);
                }

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
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
                _scheduleWindow.Close();
                _scheduleWindow = null;
            }
        }

        private void ShowScheduleWindow()
        {
            if (_scheduleWindow != null) return;
            Dispatcher.Invoke(() =>
            {
                _scheduleWindow = new ScheduleWindow(_state);
                _scheduleWindow.Show();
            });
        }

        public void UpdateScheduleWindow()
        {
            if (_scheduleWindow != null)
            {
                _scheduleWindow.UpdateState(_state);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _notifyIcon?.Dispose();
            _timer?.Stop();
            _scheduleWindow?.Close();
            StorageService.Save(_state);
        }
    }
}
