using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using smartClass.Models;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class SettingsWindow : Window
    {
        private AppState _state;

        public SettingsWindow()
        {
            InitializeComponent();
            _state = StorageService.Load();
            RefreshLists();

            // Navigation buttons
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
            SaveBtn.Click += SaveBtn_Click;
            CloseBtn.Click += CloseBtn_Click;
            // 字体设置
            FontSizeBox.Text = _state.FontSize.ToString();

            // 初始化开机自启动状态
            RunOnStartupChk.IsChecked = IsRunOnStartup();

            // 列表选择事件
            StudentsList.SelectionChanged += StudentsList_SelectionChanged;
            CoursesList.SelectionChanged += CoursesList_SelectionChanged;
            DutyGroupsList.SelectionChanged += DutyGroupsList_SelectionChanged;
            DutyCalendar.SelectedDatesChanged += DutyCalendar_SelectedDatesChanged;

            // Set initial selected button
            NavStudentsBtn.Style = (Style)FindResource("NavButtonSelectedStyle");
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
            StudentsPage.Visibility = Visibility.Collapsed;
            CoursesPage.Visibility = Visibility.Collapsed;
            GroupsPage.Visibility = Visibility.Collapsed;
            DutyPage.Visibility = Visibility.Collapsed;

            // Show selected page
            switch (pageName)
            {
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
            var navButtons = new[] { NavStudentsBtn, NavCoursesBtn, NavGroupsBtn, NavDutyBtn };
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
        }

        private void RemoveStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StudentsList.SelectedItem is Student s)
            {
                _state.Students.Remove(s);
                RefreshLists();
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
        }

        private void RemoveCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CoursesList.SelectedItem is Course c)
            {
                _state.Courses.Remove(c);
                RefreshLists();
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
        }

        private void RemoveGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DutyGroupsList.SelectedItem is DutyGroup g)
            {
                _state.DutyGroups.Remove(g);
                RefreshLists();
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
            }
        }

        private void ApplyGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DutyGroupsList.SelectedItem is smartClass.Models.DutyGroup g)
            {
                g.Name = GroupNameBox.Text;
                RefreshLists();
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

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // 保存字体设置
            if (double.TryParse(FontSizeBox.Text, out var fs))
            {
                _state.FontSize = Math.Max(8, Math.Min(48, fs));
            }

            StorageService.Save(_state);

            // 处理开机自启动
            if (RunOnStartupChk.IsChecked == true)
            {
                SetRunOnStartup();
            }
            else
            {
                RemoveRunOnStartup();
            }

            System.Windows.MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
        }

        private void RemoveAssignedDutyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AssignedDutiesList.SelectedItem is DailyDuty d)
            {
                _state.DailyDuties.Remove(d);
                RefreshLists();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
