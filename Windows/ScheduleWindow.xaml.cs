using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using smartClass.Models;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class ScheduleWindow : Window
    {
        private AppState _state;

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
            // 拖拽结束后保存位置
            this.MouseLeftButtonUp += (s, e) => SavePosition();

            try
            {
                UpdateUI();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 初始 UpdateUI 失败");
            }

            this.UpdateLayout();

            // 延迟定位：优先恢复上次位置，否则默认底部
            this.Loaded += (s, e) =>
            {
                try
                {
                    if (!RestorePosition())
                        PositionWindowBottom();
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "ScheduleWindow 定位失败");
                    PositionWindowBottom();
                }
            };
        }

        #region 工具栏

        private void OpenSettings()
        {
            // 在 UI 线程打开设置窗口
            var win = new SettingsWindow();
            win.Owner = this;
            win.ShowDialog();
            // 设置关闭后重新加载数据
            _state = StorageService.Load();
            UpdateUI();
            this.UpdateLayout();
            // 同步主窗口状态
            try
            {
                var main = System.Windows.Application.Current?.MainWindow as MainWindow;
                if (main != null)
                {
                    main.ReloadState();
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ScheduleWindow 同步主窗口状态失败");
            }
        }

        #endregion

        #region 窗口位置管理

        /// <summary>
        /// 从 AppState 恢复上次保存的窗口位置
        /// </summary>
        private bool RestorePosition()
        {
            try
            {
                if (_state.ScheduleWindowLeft >= 0 && _state.ScheduleWindowTop >= 0)
                {
                    var screen = SystemParameters.WorkArea;
                    // 验证保存的位置仍在当前屏幕范围内
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

        /// <summary>
        /// 保存当前窗口位置到 AppState
        /// </summary>
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

            Left = screen.Left + 20;

            UpdateLayout();

            double h = ActualHeight;
            if (double.IsNaN(h) || h <= 0)
                h = Height;
            if (double.IsNaN(h) || h <= 0)
                h = 200;

            Top = screen.Bottom - h - 20;

            if (double.IsNaN(Top) || Top < screen.Top)
                Top = screen.Top + 20;

            Topmost = true;
        }

        #endregion

        #region 拖拽

        private void ScheduleWindow_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
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
            var map = new[]
            {
                "周日", "周一", "周二", "周三", "周四", "周五", "周六"
            };

            var today = DateTime.Now.DayOfWeek;
            var todayText = map[(int)today];

            // ItemsControl (不可选中，触摸屏无高亮残留)
            CoursesList.ItemsSource =
                _state.Courses
                    .Where(c => c.DayOfWeek == todayText)
                    .ToList();

            var duty =
                _state.DailyDuties
                    .FirstOrDefault(d => d.Date.Date == DateTime.Today);

            if (duty == null)
            {
                DutyList.ItemsSource = null;
            }
            else
            {
                var group =
                    _state.DutyGroups
                        .FirstOrDefault(g => g.Id == duty.DutyGroupId);

                if (group == null)
                {
                    DutyList.ItemsSource = null;
                }
                else
                {
                    var members =
                        group.Members
                            .Select(m => new
                            {
                                Name = _state.Students
                                    .FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "",
                                Role = m.Role ?? ""
                            })
                            .ToList();

                    DutyList.ItemsSource = members;
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
