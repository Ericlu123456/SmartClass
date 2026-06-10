using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Diagnostics;
using smartClass.Models;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class SettingsWindow : Window
    {
        private AppState _state;
        private DispatcherTimer _shutdownTimer;

        public SettingsWindow()
        {
            InitializeComponent();
            _state = StorageService.Load();
            RefreshLists();

            // 初始化自动关机定时器
            _shutdownTimer = new DispatcherTimer();
            _shutdownTimer.Tick += ShutdownTimer_Tick;

            // Navigation buttons
            NavGeneralBtn.Click += NavGeneralBtn_Click;
            NavStudentsBtn.Click += NavStudentsBtn_Click;
            NavCoursesBtn.Click += NavCoursesBtn_Click;
            NavGroupsBtn.Click += NavGroupsBtn_Click;
            NavDutyBtn.Click += NavDutyBtn_Click;

            AddStudentBtn.Click += AddStudentBtn_Click;
            RemoveStudentBtn.Click += RemoveStudentBtn_Click;
            AddCourseBtn.Click += AddCourseBtn_Click;
            RemoveCourseBtn.Click += RemoveCourseBtn_Click;
            ImportConfigBtn.Click += ImportConfigBtn_Click;
            ExportConfigBtn.Click += ExportConfigBtn_Click;
            AddGroupBtn.Click += AddGroupBtn_Click;
            RemoveGroupBtn.Click += RemoveGroupBtn_Click;
            AddMemberBtn.Click += AddMemberBtn_Click;
            RemoveMemberBtn.Click += RemoveMemberBtn_Click;
            ApplyStudentBtn.Click += ApplyStudentBtn_Click;
            ResetStudentBtn.Click += ResetStudentBtn_Click;
            ApplyCourseBtn.Click += ApplyCourseBtn_Click;
            ResetCourseBtn.Click += ResetCourseBtn_Click;
            ApplyGroupBtn.Click += ApplyGroupBtn_Click;
            ResetGroupBtn.Click += ResetGroupBtn_Click;
            AssignDutyBtn.Click += AssignDutyBtn_Click;
            RemoveAssignedDutyBtn.Click += RemoveAssignedDutyBtn_Click;
            CloseBtn.Click += CloseBtn_Click;
            // 字体设置
            FontSizeBox.Text = _state.FontSize.ToString();

            // 初始化常规设置控件
            RunOnStartupChk.IsChecked = IsRunOnStartup();
            EnableShutdownChk.IsChecked = _state.EnableAutoShutdown;
            AutoShutdownTimeBox.Text = _state.AutoShutdownTime;

            // 当字体框失去焦点时自动保存字体设置
            FontSizeBox.LostFocus += FontSizeBox_LostFocus;
            // 回车也保存
            FontSizeBox.KeyDown += FontSizeBox_KeyDown;

            // 开机自启动变更即时生效
            RunOnStartupChk.Checked += RunOnStartupChk_Changed;
            RunOnStartupChk.Unchecked += RunOnStartupChk_Changed;

            // 自动关机设置事件
            EnableShutdownChk.Checked += EnableShutdownChk_Changed;
            EnableShutdownChk.Unchecked += EnableShutdownChk_Changed;
            AutoShutdownTimeBox.LostFocus += AutoShutdownTimeBox_LostFocus;
            AutoShutdownTimeBox.KeyDown += AutoShutdownTimeBox_KeyDown;

            // 列表选择事件
            StudentsList.SelectionChanged += StudentsList_SelectionChanged;
            CoursesList.SelectionChanged += CoursesList_SelectionChanged;
            DutyGroupsList.SelectionChanged += DutyGroupsList_SelectionChanged;
            DutyCalendar.SelectedDatesChanged += DutyCalendar_SelectedDatesChanged;

            // Set initial selected button (常规)
            NavGeneralBtn.Style = (Style)FindResource("NavButtonSelectedStyle");

            // 启动自动关机定时器（若启用）
            SetupShutdownTimer();
        }

        private void NavGeneralBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("General");
            UpdateNavigationButtonStyles(NavGeneralBtn);
        }

        private void NavStudentsBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Students");
            UpdateNavigationButtonStyles(NavStudentsBtn);
        }

        private void NavCoursesBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Courses");
            UpdateNavigationButtonStyles(NavCoursesBtn);
        }

        private void NavGroupsBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Groups");
            UpdateNavigationButtonStyles(NavGroupsBtn);
        }

        private void NavDutyBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("Duty");
            UpdateNavigationButtonStyles(NavDutyBtn);
        }

        private void ShowPage(string pageName)
        {
            // Hide all pages
            GeneralPage.Visibility = Visibility.Collapsed;
            StudentsPage.Visibility = Visibility.Collapsed;
            CoursesPage.Visibility = Visibility.Collapsed;
            GroupsPage.Visibility = Visibility.Collapsed;
            DutyPage.Visibility = Visibility.Collapsed;

            // Show selected page
            switch (pageName)
            {
                case "General":
                    GeneralPage.Visibility = Visibility.Visible;
                    break;
                case "Students":
                    StudentsPage.Visibility = Visibility.Visible;
                    break;
                case "Courses":
                    CoursesPage.Visibility = Visibility.Visible;
                    break;
                case "Groups":
                    GroupsPage.Visibility = Visibility.Visible;
                    break;
                case "Duty":
                    DutyPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateNavigationButtonStyles(System.Windows.Controls.Button selectedButton)
        {
            var navButtons = new[] { NavGeneralBtn, NavStudentsBtn, NavCoursesBtn, NavGroupsBtn, NavDutyBtn };
            var normalStyle = (Style)FindResource("NavButtonStyle");
            var selectedStyle = (Style)FindResource("NavButtonSelectedStyle");

            foreach (var btn in navButtons)
            {
                btn.Style = btn == selectedButton ? selectedStyle : normalStyle;
            }
        }

        private void RefreshLists()
        {
            StudentsList.ItemsSource = null;
            StudentsList.ItemsSource = _state.Students;

            CoursesList.ItemsSource = null;
            CoursesList.ItemsSource = _state.Courses;

            DutyGroupsList.ItemsSource = null;
            DutyGroupsList.ItemsSource = _state.DutyGroups;

            DutyGroupAssignBox.ItemsSource = null;
            DutyGroupAssignBox.ItemsSource = _state.DutyGroups;

            AssignedDutiesList.ItemsSource = null;
            AssignedDutiesList.ItemsSource = _state.DailyDuties.OrderBy(d => d.Date).ToList();

            AddMemberStudentBox.ItemsSource = null;
            AddMemberStudentBox.ItemsSource = _state.Students;

            // 如果当前选中项不为空，保持界面与数据同步
            if (StudentsList.SelectedItem is Student selStu)
            {
                var s = _state.Students.FirstOrDefault(x => x.Id == selStu.Id);
                if (s != null)
                {
                    StudentIdBox.Text = s.Id;
                    StudentNameBox.Text = s.Name;
                    StudentCreditsBox.Text = s.SocialCredits.ToString();
                }
            }

            if (CoursesList.SelectedItem is Course selCourse)
            {
                var c = _state.Courses.FirstOrDefault(x => x.Id == selCourse.Id);
                if (c != null)
                {
                    CourseSubjectBox.Text = c.Subject;
                    CourseStartBox.Text = c.StartTime;
                    CourseEndBox.Text = c.EndTime;
                    CourseDayBox.SelectedItem = CourseDayBox.Items.Cast<ComboBoxItem>().FirstOrDefault(it => (string)it.Content == c.DayOfWeek);
                }
            }

            if (DutyGroupsList.SelectedItem is DutyGroup selGroup)
            {
                var g = _state.DutyGroups.FirstOrDefault(x => x.Id == selGroup.Id);
                if (g != null)
                {
                    GroupNameBox.Text = g.Name;
                    GroupMembersList.ItemsSource = g.Members.Select(m => new { StudentName = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "", m.Role, m.StudentId }).ToList();
                }
            }
        }

        private void ImportConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "JSON 文件 (*.json)|*.json|所有文件|*.*";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(dlg.FileName);
                    var state = System.Text.Json.JsonSerializer.Deserialize<AppState>(json);
                    if (state != null)
                    {
                        _state = state;
                        RefreshLists();
                        AutoSave();
                    }
                }
                catch
                {
                }
            }
        }

        private void ExportConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "JSON 文件 (*.json)|*.json";
            dlg.FileName = "appstate_export.json";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(_state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(dlg.FileName, json);
                }
                catch
                {
                }
            }
        }

        private void AddStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            var id = Guid.NewGuid().ToString();
            var name = "学生" + (_state.Students.Count + 1);
            _state.Students.Add(new Student { Id = id, Name = name });
            RefreshLists();
            AutoSave();
        }

        private void RemoveStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StudentsList.SelectedItem is Student s)
            {
                _state.Students.Remove(s);
                RefreshLists();
                AutoSave();
            }
        }

        private void StudentsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (StudentsList.SelectedItem is smartClass.Models.Student s)
            {
                StudentIdBox.Text = s.Id;
                StudentNameBox.Text = s.Name;
                StudentCreditsBox.Text = s.SocialCredits.ToString();
            }
            else
            {
                StudentIdBox.Text = string.Empty;
                StudentNameBox.Text = string.Empty;
                StudentCreditsBox.Text = string.Empty;
            }
        }

        private void ApplyStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StudentsList.SelectedItem is smartClass.Models.Student s)
            {
                s.Name = StudentNameBox.Text;
                if (int.TryParse(StudentCreditsBox.Text, out var c)) s.SocialCredits = c;
                RefreshLists();
                AutoSave();
            }
        }

        private void ResetStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StudentsList.SelectedItem is smartClass.Models.Student s)
            {
                StudentNameBox.Text = s.Name;
                StudentCreditsBox.Text = s.SocialCredits.ToString();
            }
        }

        private void AddCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            var id = Guid.NewGuid().ToString();
            _state.Courses.Add(new Course { Id = id, Subject = "科目" + (_state.Courses.Count + 1), DayOfWeek = "周一", StartTime = "08:00", EndTime = "08:45" });
            RefreshLists();
            AutoSave();
        }

        private void RemoveCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CoursesList.SelectedItem is Course c)
            {
                _state.Courses.Remove(c);
                RefreshLists();
                AutoSave();
            }
        }

        private void CoursesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CoursesList.SelectedItem is smartClass.Models.Course c)
            {
                CourseSubjectBox.Text = c.Subject;
                CourseDayBox.SelectedItem = CourseDayBox.Items.Cast<ComboBoxItem>().FirstOrDefault(it => (string)it.Content == c.DayOfWeek);
                CourseStartBox.Text = c.StartTime;
                CourseEndBox.Text = c.EndTime;
            }
            else
            {
                CourseSubjectBox.Text = string.Empty;
                CourseStartBox.Text = string.Empty;
                CourseEndBox.Text = string.Empty;
            }
        }

        private void ApplyCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CoursesList.SelectedItem is smartClass.Models.Course c)
            {
                c.Subject = CourseSubjectBox.Text;
                if (CourseDayBox.SelectedItem is ComboBoxItem it) c.DayOfWeek = (string)it.Content;
                c.StartTime = CourseStartBox.Text;
                c.EndTime = CourseEndBox.Text;
                RefreshLists();
                AutoSave();
            }
        }

        private void ResetCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CoursesList.SelectedItem is smartClass.Models.Course c)
            {
                CourseSubjectBox.Text = c.Subject;
                CourseStartBox.Text = c.StartTime;
                CourseEndBox.Text = c.EndTime;
                CourseDayBox.SelectedItem = CourseDayBox.Items.Cast<ComboBoxItem>().FirstOrDefault(it => (string)it.Content == c.DayOfWeek);
            }
        }

        private void AddGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            var id = Guid.NewGuid().ToString();
            _state.DutyGroups.Add(new DutyGroup { Id = id, Name = "组" + (_state.DutyGroups.Count + 1) });
            RefreshLists();
            AutoSave();
        }

        private void RemoveGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DutyGroupsList.SelectedItem is DutyGroup g)
            {
                _state.DutyGroups.Remove(g);
                RefreshLists();
                AutoSave();
            }
        }

        private void DutyGroupsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DutyGroupsList.SelectedItem is smartClass.Models.DutyGroup g)
            {
                GroupNameBox.Text = g.Name;
                GroupMembersList.ItemsSource = g.Members.Select(m => new { StudentName = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "", m.Role, m.StudentId }).ToList();
            }
            else
            {
                GroupNameBox.Text = string.Empty;
                GroupMembersList.ItemsSource = null;
            }
        }

        private void AddMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(DutyGroupsList.SelectedItem is smartClass.Models.DutyGroup g)) return;
            if (AddMemberStudentBox.SelectedItem == null) return;

            string sid = null;
            // 如果 SelectedItem 是 Student
            if (AddMemberStudentBox.SelectedItem is smartClass.Models.Student s)
            {
                sid = s.Id;
            }
            else
            {
                var sel = AddMemberStudentBox.SelectedItem;
                var prop = sel.GetType().GetProperty("Id");
                if (prop != null)
                {
                    sid = prop.GetValue(sel)?.ToString();
                }
                else if (AddMemberStudentBox.SelectedValue != null)
                {
                    sid = AddMemberStudentBox.SelectedValue.ToString();
                }
            }

            if (string.IsNullOrEmpty(sid)) return;

            var role = AddMemberRoleBox.Text ?? string.Empty;
            g.Members.Add(new DutyMember { StudentId = sid, Role = role });
            GroupMembersList.ItemsSource = g.Members.Select(m => new { StudentName = _state.Students.FirstOrDefault(st => st.Id == m.StudentId)?.Name ?? "", m.Role, m.StudentId }).ToList();
            AutoSave();
        }

        private void RemoveMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DutyGroupsList.SelectedItem is smartClass.Models.DutyGroup g && GroupMembersList.SelectedItem != null)
            {
                dynamic sel = GroupMembersList.SelectedItem;
                string sid = sel.StudentId;
                var member = g.Members.FirstOrDefault(m => m.StudentId == sid);
                if (member != null) g.Members.Remove(member);
                GroupMembersList.ItemsSource = g.Members.Select(m => new { StudentName = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "", m.Role, m.StudentId }).ToList();
                AutoSave();
            }
        }

        private void ApplyGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DutyGroupsList.SelectedItem is smartClass.Models.DutyGroup g)
            {
                g.Name = GroupNameBox.Text;
                RefreshLists();
                AutoSave();
            }
        }

        private void ResetGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DutyGroupsList.SelectedItem is smartClass.Models.DutyGroup g)
            {
                GroupNameBox.Text = g.Name;
                GroupMembersList.ItemsSource = g.Members.Select(m => new { StudentName = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "", m.Role, m.StudentId }).ToList();
            }
        }

        private void FontSizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(FontSizeBox.Text, out var fs))
            {
                _state.FontSize = Math.Max(8, Math.Min(48, fs));
                AutoSave();
                // 通知主窗口更新显示
                ((MainWindow)System.Windows.Application.Current.MainWindow)?.UpdateScheduleWindow();
            }
        }

        private void FontSizeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (double.TryParse(FontSizeBox.Text, out var fs))
                {
                    _state.FontSize = Math.Max(8, Math.Min(48, fs));
                    AutoSave();
                    ((MainWindow)System.Windows.Application.Current.MainWindow)?.UpdateScheduleWindow();
                }
            }
        }

        private void RunOnStartupChk_Changed(object sender, RoutedEventArgs e)
        {
            if (RunOnStartupChk.IsChecked == true)
            {
                SetRunOnStartup();
            }
            else
            {
                RemoveRunOnStartup();
            }
            AutoSave();
        }

        private void EnableShutdownChk_Changed(object sender, RoutedEventArgs e)
        {
            _state.EnableAutoShutdown = EnableShutdownChk.IsChecked == true;
            AutoSave();
            SetupShutdownTimer();
        }

        private void AutoShutdownTimeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsValidTime(AutoShutdownTimeBox.Text))
            {
                _state.AutoShutdownTime = AutoShutdownTimeBox.Text;
                AutoSave();
                SetupShutdownTimer();
            }
        }

        private void AutoShutdownTimeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (IsValidTime(AutoShutdownTimeBox.Text))
                {
                    _state.AutoShutdownTime = AutoShutdownTimeBox.Text;
                    AutoSave();
                    SetupShutdownTimer();
                }
            }
        }

        private void DutyCalendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 可选：选中日期时更新界面
        }

        private void AssignDutyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DutyCalendar.SelectedDate == null) return;
            if (DutyGroupAssignBox.SelectedItem == null) return;
            dynamic sel = DutyGroupAssignBox.SelectedItem;
            string gid = sel.Id;
            var date = DutyCalendar.SelectedDate.Value.Date;
            _state.DailyDuties.Add(new DailyDuty { Date = date, DutyGroupId = gid });
            RefreshLists();
            AutoSave();
        }

        private void RemoveAssignedDutyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AssignedDutiesList.SelectedItem is DailyDuty d)
            {
                _state.DailyDuties.Remove(d);
                RefreshLists();
                AutoSave();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AutoSave()
        {
            // 统一保存当前状态并通知主窗口更新（如果存在）
            try
            {
                StorageService.Save(_state);
                // 通知主窗口更新 ScheduleWindow（使用 Dispatcher 确保在 UI 线程）
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        var main = System.Windows.Application.Current.MainWindow as MainWindow;
                        main?.UpdateScheduleWindow();
                    }
                    catch { }
                });
            }
            catch { }
        }

        private void SetupShutdownTimer()
        {
            try
            {
                _shutdownTimer.Stop();
                if (!_state.EnableAutoShutdown) return;

                if (!IsValidTime(_state.AutoShutdownTime)) return;

                var parts = _state.AutoShutdownTime.Split(':');
                int hh = int.Parse(parts[0]);
                int mm = int.Parse(parts[1]);
                var now = DateTime.Now;
                var next = new DateTime(now.Year, now.Month, now.Day, hh, mm, 0);
                if (next <= now) next = next.AddDays(1);
                var span = next - now;
                _shutdownTimer.Interval = span;
                _shutdownTimer.Start();
            }
            catch { }
        }

        private void ShutdownTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _shutdownTimer.Stop();
                // 执行关机命令
                var psi = new ProcessStartInfo("shutdown", "/s /t 0") { CreateNoWindow = true, UseShellExecute = false };
                Process.Start(psi);
            }
            catch { }
            finally
            {
                // 重新设置定时器为次日
                SetupShutdownTimer();
            }
        }

        private bool IsRunOnStartup()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    if (key == null) return false;
                    var val = key.GetValue("smartClass") as string;
                    return !string.IsNullOrEmpty(val);
                }
            }
            catch { return false; }
        }

        private bool IsValidTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var parts = text.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out var hh)) return false;
            if (!int.TryParse(parts[1], out var mm)) return false;
            return hh >= 0 && hh < 24 && mm >= 0 && mm < 60;
        }

        private void SetRunOnStartup()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key == null) return;
                    var path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    key.SetValue("smartClass", $"\"{path}\" --minimized");
                }
            }
            catch { }
        }

        private void RemoveRunOnStartup()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key == null) return;
                    key.DeleteValue("smartClass", false);
                }
            }
            catch { }
        }
    }
}
