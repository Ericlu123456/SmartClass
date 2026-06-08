using System.Linq;
using System.Windows;
using System.Windows.Input;
using SamrtClass.Models;

namespace SamrtClass.Windows
{
    public partial class ScheduleWindow : Window
    {
        private AppState _state;

        public ScheduleWindow(AppState state)
        {
            InitializeComponent();
            _state = state;
            PositionWindowBottom();
            UpdateUI();
            // 支持拖动窗口
            this.MouseLeftButtonDown += ScheduleWindow_MouseLeftButtonDown;
        }

        private void ScheduleWindow_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            }
            catch { }
        }

        private void PositionWindowBottom()
        {
            var screen = System.Windows.SystemParameters.WorkArea;
            Left = screen.Left + 20;
            Top = screen.Bottom - Height - 20;
            // 置于桌面最底层（不是 Topmost）
            Topmost = false;
        }

        public void UpdateState(AppState state)
        {
            _state = state;
            UpdateUI();
        }

        private void UpdateUI()
        {
            var map = new[] { "周日","周一","周二","周三","周四","周五","周六" };
            var today = DateTime.Now.DayOfWeek;
            var todayText = map[(int)today];

            CoursesList.ItemsSource = _state.Courses.Where(c => c.DayOfWeek == todayText).ToList();

            var duty = _state.DailyDuties.FirstOrDefault(d => d.Date.Date == DateTime.Today);
            if (duty == null)
            {
                DutyText.Text = "无";
            }
            else
            {
                var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
                if (group == null) { DutyText.Text = "无"; return; }
                var names = group.Members.Select(m => _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "");
                DutyText.Text = string.Join(",", names);
            }

            // 应用字体大小
            try
            {
                var fs = _state.FontSize;
                CoursesList.FontSize = fs;
                DutyText.FontSize = fs;
            }
            catch { }
        }
    }
}
