using System.Linq;
using System.Windows;
using System.Windows.Input;
using smartClass.Models;

namespace smartClass.Windows
{
    public partial class ScheduleWindow : Window
    {
        private AppState _state;

        public ScheduleWindow(AppState state)
        {
            InitializeComponent();
            _state = state;
            // 使窗口根据内容自适应大小，然后定位
            this.SizeToContent = SizeToContent.WidthAndHeight;
            UpdateUI();
            this.UpdateLayout();
            PositionWindowBottom();
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
            this.UpdateLayout();
            double h = this.ActualHeight;
            if (double.IsNaN(h) || h <= 0) h = this.Height;
            Top = screen.Bottom - h - 20;
            // 置于桌面最底层（不是 Topmost）
            Topmost = false;
        }

        public void UpdateState(AppState state)
        {
            _state = state;
            UpdateUI();
            this.UpdateLayout();
            PositionWindowBottom();
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
                DutyList.ItemsSource = null;
            }
            else
            {
                var group = _state.DutyGroups.FirstOrDefault(g => g.Id == duty.DutyGroupId);
                if (group == null) { DutyList.ItemsSource = null; }
                else
                {
                    var members = group.Members.Select(m => new { Name = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "", Role = m.Role ?? "" }).ToList();
                    DutyList.ItemsSource = members;
                }
            }

            // 应用字体大小
            try
            {
                var fs = _state.FontSize;
                CoursesList.FontSize = fs;
                DutyList.FontSize = fs;
            }
            catch { }
        }
    }
}
