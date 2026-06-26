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
        private Course _currentSelectedCourse; // 追踪当前选中的课程
        private int _aboutClickCount = 0;      // 关于页面标题点击计数器

        public SettingsWindow()
        {
            InitializeComponent();

            try
            {
                _state = StorageService.Load();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "SettingsWindow 加载配置失败");
                _state = new AppState();
            }

            RefreshLists();

            // 初始化自动关机定时器
            _shutdownTimer = new DispatcherTimer();
            _shutdownTimer.Tick += ShutdownTimer_Tick;

            // 绑定导航按钮事件
            NavGeneralBtn.Click += (s, e) => { NavGeneralBtn_Click(s, e); };
            NavStudentsBtn.Click += (s, e) => { NavStudentsBtn_Click(s, e); };
            NavCoursesBtn.Click += (s, e) => { NavCoursesBtn_Click(s, e); };
            NavGroupsBtn.Click += (s, e) => { NavGroupsBtn_Click(s, e); };
            NavDutyBtn.Click += (s, e) => { NavDutyBtn_Click(s, e); };
            NavAboutBtn.Click += (s, e) => { NavAboutBtn_Click(s, e); };

            AddStudentBtn.Click += (s, e) => { AddStudentBtn_Click(s, e); };
            RemoveStudentBtn.Click += (s, e) => { RemoveStudentBtn_Click(s, e); };
            AddCourseBtn.Click += (s, e) => { AddCourseBtn_Click(s, e); };
            ImportConfigBtn.Click += (s, e) => { ImportConfigBtn_Click(s, e); };
            ExportConfigBtn.Click += (s, e) => { ExportConfigBtn_Click(s, e); };
            AddGroupBtn.Click += (s, e) => { AddGroupBtn_Click(s, e); };
            RemoveGroupBtn.Click += (s, e) => { RemoveGroupBtn_Click(s, e); };
            AddMemberBtn.Click += (s, e) => { AddMemberBtn_Click(s, e); };
            RemoveMemberBtn.Click += (s, e) => { RemoveMemberBtn_Click(s, e); };
            ApplyStudentBtn.Click += (s, e) => { ApplyStudentBtn_Click(s, e); };
            ResetStudentBtn.Click += (s, e) => { ResetStudentBtn_Click(s, e); };
            ApplyCourseBtn.Click += (s, e) => { ApplyCourseBtn_Click(s, e); };
            ResetCourseBtn.Click += (s, e) => { ResetCourseBtn_Click(s, e); };
            ApplyGroupBtn.Click += (s, e) => { ApplyGroupBtn_Click(s, e); };
            ResetGroupBtn.Click += (s, e) => { ResetGroupBtn_Click(s, e); };
            AssignDutyBtn.Click += (s, e) => { AssignDutyBtn_Click(s, e); };
            RemoveAssignedDutyBtn.Click += (s, e) => { RemoveAssignedDutyBtn_Click(s, e); };
            CloseBtn.Click += (s, e) => { CloseBtn_Click(s, e); };

            // 字体设置
            FontSizeBox.Text = _state.FontSize.ToString();

            // 初始化常规设置控件
            RunOnStartupChk.IsChecked = IsRunOnStartup();
            EnableShutdownChk.IsChecked = _state.EnableAutoShutdown;
            AutoShutdownTimeBox.Text = _state.AutoShutdownTime;
            EnableSpeechChk.IsChecked = _state.EnableSpeech;
            SemesterEndPicker.SelectedDate = _state.SemesterEndDate;

            // 当字体框失去焦点时自动保存字体设置
            FontSizeBox.LostFocus += (s, e) => { FontSizeBox_LostFocus(s, e); };
            FontSizeBox.KeyDown += (s, e) => { FontSizeBox_KeyDown(s, e); };

            // 开机自启动变更即时生效
            RunOnStartupChk.Checked += (s, e) => { RunOnStartupChk_Changed(s, e); };
            RunOnStartupChk.Unchecked += (s, e) => { RunOnStartupChk_Changed(s, e); };

            // 自动关机设置事件
            EnableShutdownChk.Checked += (s, e) => { EnableShutdownChk_Changed(s, e); };
            EnableShutdownChk.Unchecked += (s, e) => { EnableShutdownChk_Changed(s, e); };
            AutoShutdownTimeBox.LostFocus += (s, e) => { AutoShutdownTimeBox_LostFocus(s, e); };
            AutoShutdownTimeBox.KeyDown += (s, e) => { AutoShutdownTimeBox_KeyDown(s, e); };
            EnableSpeechChk.Checked += (s, e) => { EnableSpeechChk_Changed(s, e); };
            EnableSpeechChk.Unchecked += (s, e) => { EnableSpeechChk_Changed(s, e); };
            SemesterEndPicker.SelectedDateChanged += (s, e) => { SemesterEndPicker_Changed(); };

            // 关于页面标题点击计数器（7次触发开发者选项）
            AboutTitle.MouseLeftButtonDown += (s, e) => { AboutTitle_Click(); };

            // 开发者选项按钮
            DevTestNotifyBtn.Click += (s, e) => { DevTestNotifyBtn_Click(s, e); };
            DevTestDialogBtn.Click += (s, e) => { DevTestDialogBtn_Click(s, e); };
            DevTestSpeechBtn.Click += (s, e) => { DevTestSpeechBtn_Click(s, e); };
            DevViewLogBtn.Click += (s, e) => { DevViewLogBtn_Click(s, e); };

            // 列表选择事件
            StudentsList.SelectionChanged += (s, e) => { StudentsList_SelectionChanged(s, e); };
            DutyGroupsList.SelectionChanged += (s, e) => { DutyGroupsList_SelectionChanged(s, e); };
            DutyCalendar.SelectedDatesChanged += (s, e) => { DutyCalendar_SelectedDatesChanged(s, e); };

            // Set initial selected button (常规)
            NavGeneralBtn.Style = (Style)FindResource("NavButtonSelectedStyle");

            // 启动自动关机定时器
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

        private void NavAboutBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowPage("About");
            UpdateNavigationButtonStyles(NavAboutBtn);
        }

        private void ShowPage(string pageName)
        {
            GeneralPage.Visibility = Visibility.Collapsed;
            StudentsPage.Visibility = Visibility.Collapsed;
            CoursesPage.Visibility = Visibility.Collapsed;
            GroupsPage.Visibility = Visibility.Collapsed;
            DutyPage.Visibility = Visibility.Collapsed;
            AboutPage.Visibility = Visibility.Collapsed;

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
                case "About":
                    AboutPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateNavigationButtonStyles(System.Windows.Controls.Button selectedButton)
        {
            var navButtons = new[] { NavGeneralBtn, NavStudentsBtn, NavCoursesBtn, NavGroupsBtn, NavDutyBtn, NavAboutBtn };
            var normalStyle = (Style)FindResource("NavButtonStyle");
            var selectedStyle = (Style)FindResource("NavButtonSelectedStyle");

            foreach (var btn in navButtons)
            {
                btn.Style = btn == selectedButton ? selectedStyle : normalStyle;
            }
        }

        private void RefreshLists()
        {
            try
            {
                StudentsList.ItemsSource = null;
                StudentsList.ItemsSource = _state.Students;

                // 按天分组课程
                var coursesByDay = new Dictionary<string, List<Course>>
                {
                    { "周一", _state.Courses.Where(c => c.DayOfWeek == "周一").ToList() },
                    { "周二", _state.Courses.Where(c => c.DayOfWeek == "周二").ToList() },
                    { "周三", _state.Courses.Where(c => c.DayOfWeek == "周三").ToList() },
                    { "周四", _state.Courses.Where(c => c.DayOfWeek == "周四").ToList() },
                    { "周五", _state.Courses.Where(c => c.DayOfWeek == "周五").ToList() },
                    { "周六", _state.Courses.Where(c => c.DayOfWeek == "周六").ToList() },
                    { "周日", _state.Courses.Where(c => c.DayOfWeek == "周日").ToList() }
                };

                MondayList.ItemsSource = coursesByDay["周一"];
                TuesdayList.ItemsSource = coursesByDay["周二"];
                WednesdayList.ItemsSource = coursesByDay["周三"];
                ThursdayList.ItemsSource = coursesByDay["周四"];
                FridayList.ItemsSource = coursesByDay["周五"];
                SaturdayList.ItemsSource = coursesByDay["周六"];
                SundayList.ItemsSource = coursesByDay["周日"];

                DutyGroupsList.ItemsSource = null;
                DutyGroupsList.ItemsSource = _state.DutyGroups;

                DutyGroupAssignBox.ItemsSource = null;
                DutyGroupAssignBox.ItemsSource = _state.DutyGroups;

                AssignedDutiesList.ItemsSource = null;
                AssignedDutiesList.ItemsSource = _state.DailyDuties.OrderBy(d => d.Date).ToList();

                AddMemberStudentBox.ItemsSource = null;
                AddMemberStudentBox.ItemsSource = _state.Students;

                // 保持选中项同步
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

                if (DutyGroupsList.SelectedItem is DutyGroup selGroup)
                {
                    var g = _state.DutyGroups.FirstOrDefault(x => x.Id == selGroup.Id);
                    if (g != null)
                    {
                        GroupNameBox.Text = g.Name;
                        GroupMembersList.ItemsSource = g.Members.Select(m =>
                            new
                            {
                                StudentName = _state.Students.FirstOrDefault(st => st.Id == m.StudentId)?.Name ?? "",
                                m.Role,
                                m.StudentId
                            }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "RefreshLists 失败");
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
                        // 验证导入数据的完整性
                        state.Students ??= new List<Student>();
                        state.Courses ??= new List<Course>();
                        state.DutyGroups ??= new List<DutyGroup>();
                        state.DailyDuties ??= new List<DailyDuty>();

                        _state = state;
                        RefreshLists();
                        AutoSave();
                        LogService.Log($"成功导入配置: {dlg.FileName}");
                        System.Windows.MessageBox.Show(
                            $"导入成功！\n学生: {state.Students.Count}\n课程: {state.Courses.Count}\n值日组: {state.DutyGroups.Count}",
                            "导入成功",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("导入失败：JSON 文件格式不正确。", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    LogService.Log(ex, "导入配置 JSON 解析失败");
                    System.Windows.MessageBox.Show($"导入失败：JSON 格式错误。\n{ex.Message}", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "导入配置失败");
                    System.Windows.MessageBox.Show($"导入失败：{ex.Message}", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var json = System.Text.Json.JsonSerializer.Serialize(_state,
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(dlg.FileName, json);
                    LogService.Log($"成功导出配置: {dlg.FileName}");
                    System.Windows.MessageBox.Show("配置导出成功！", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogService.Log(ex, "导出配置失败");
                    System.Windows.MessageBox.Show($"导出失败：{ex.Message}", "导出错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var id = Guid.NewGuid().ToString();
                var name = "学生" + (_state.Students.Count + 1);
                _state.Students.Add(new Student { Id = id, Name = name });
                RefreshLists();
                AutoSave();
            }
            catch (Exception ex) { LogService.Log(ex, "添加学生失败"); }
        }

        private void RemoveStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StudentsList.SelectedItem is Student s)
                {
                    _state.Students.Remove(s);
                    RefreshLists();
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "移除学生失败"); }
        }

        private void StudentsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (StudentsList.SelectedItem is Student s)
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
            catch (Exception ex) { LogService.Log(ex, "学生列表选择变更"); }
        }

        private void ApplyStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StudentsList.SelectedItem is Student s)
                {
                    if (string.IsNullOrWhiteSpace(StudentNameBox.Text))
                    {
                        System.Windows.MessageBox.Show("学生姓名不能为空。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    s.Name = StudentNameBox.Text;
                    if (int.TryParse(StudentCreditsBox.Text, out var c))
                        s.SocialCredits = c;
                    else
                        System.Windows.MessageBox.Show("社评值必须为整数。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RefreshLists();
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "应用学生更改失败"); }
        }

        private void ResetStudentBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StudentsList.SelectedItem is Student s)
                {
                    StudentNameBox.Text = s.Name;
                    StudentCreditsBox.Text = s.SocialCredits.ToString();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "重置学生表单失败"); }
        }

        private void AddCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var id = Guid.NewGuid().ToString();
                _state.Courses.Add(new Course
                {
                    Id = id,
                    Subject = "科目" + (_state.Courses.Count + 1),
                    DayOfWeek = "周一",
                    StartTime = "08:00",
                    EndTime = "08:45"
                });
                RefreshLists();
                AutoSave();
            }
            catch (Exception ex) { LogService.Log(ex, "添加课程失败"); }
        }

        private void RemoveCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.Tag is Course c)
                {
                    _state.Courses.Remove(c);
                    RefreshLists();
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "移除课程失败"); }
        }

        private void CoursesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is Course c)
                {
                    _currentSelectedCourse = c;
                    CourseSubjectBox.Text = c.Subject;
                    CourseDayBox.SelectedItem = CourseDayBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(it => (string)it.Content == c.DayOfWeek);
                    CourseStartBox.Text = c.StartTime;
                    CourseEndBox.Text = c.EndTime;
                }
                else
                {
                    _currentSelectedCourse = null;
                    CourseSubjectBox.Text = string.Empty;
                    CourseStartBox.Text = string.Empty;
                    CourseEndBox.Text = string.Empty;
                    CourseDayBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex) { LogService.Log(ex, "课程列表选择变更"); }
        }

        private void ApplyCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSelectedCourse != null)
                {
                    // 验证时间格式
                    if (!string.IsNullOrWhiteSpace(CourseStartBox.Text) && !IsValidTime(CourseStartBox.Text))
                    {
                        System.Windows.MessageBox.Show("开始时间格式无效，请使用 HH:mm 格式。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (!string.IsNullOrWhiteSpace(CourseEndBox.Text) && !IsValidTime(CourseEndBox.Text))
                    {
                        System.Windows.MessageBox.Show("结束时间格式无效，请使用 HH:mm 格式。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _currentSelectedCourse.Subject = CourseSubjectBox.Text;
                    if (CourseDayBox.SelectedItem is ComboBoxItem it)
                        _currentSelectedCourse.DayOfWeek = (string)it.Content;
                    _currentSelectedCourse.StartTime = CourseStartBox.Text;
                    _currentSelectedCourse.EndTime = CourseEndBox.Text;
                    RefreshLists();
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "应用课程更改失败"); }
        }

        private void ResetCourseBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSelectedCourse != null)
                {
                    CourseSubjectBox.Text = _currentSelectedCourse.Subject;
                    CourseStartBox.Text = _currentSelectedCourse.StartTime;
                    CourseEndBox.Text = _currentSelectedCourse.EndTime;
                    CourseDayBox.SelectedItem = CourseDayBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(it => (string)it.Content == _currentSelectedCourse.DayOfWeek);
                }
            }
            catch (Exception ex) { LogService.Log(ex, "重置课程表单失败"); }
        }

        private void AddGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var id = Guid.NewGuid().ToString();
                _state.DutyGroups.Add(new DutyGroup { Id = id, Name = "组" + (_state.DutyGroups.Count + 1) });
                RefreshLists();
                AutoSave();
            }
            catch (Exception ex) { LogService.Log(ex, "添加值日组失败"); }
        }

        private void RemoveGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DutyGroupsList.SelectedItem is DutyGroup g)
                {
                    _state.DutyGroups.Remove(g);
                    RefreshLists();
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "移除值日组失败"); }
        }

        private void DutyGroupsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (DutyGroupsList.SelectedItem is DutyGroup g)
                {
                    GroupNameBox.Text = g.Name;
                    GroupMembersList.ItemsSource = g.Members.Select(m =>
                        new
                        {
                            StudentName = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "",
                            m.Role,
                            m.StudentId
                        }).ToList();
                }
                else
                {
                    GroupNameBox.Text = string.Empty;
                    GroupMembersList.ItemsSource = null;
                }
            }
            catch (Exception ex) { LogService.Log(ex, "值日组列表选择变更"); }
        }

        private void AddMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(DutyGroupsList.SelectedItem is DutyGroup g)) return;
                if (AddMemberStudentBox.SelectedItem == null) return;

                string sid = null;

                if (AddMemberStudentBox.SelectedItem is Student s)
                {
                    sid = s.Id;
                }
                else
                {
                    // 尝试通过反射获取 Id（兼容匿名类型）
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

                // 检查是否已在组中
                if (g.Members.Any(m => m.StudentId == sid))
                {
                    System.Windows.MessageBox.Show("该学生已在值日组中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var role = AddMemberRoleBox.Text ?? string.Empty;
                g.Members.Add(new DutyMember { StudentId = sid, Role = role });
                // 刷新成员列表（触发 DutyGroupsList_SelectionChanged 绑定的显示）
                DutyGroupsList_SelectionChanged(null, null);
                AutoSave();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "添加值日组成员失败");
                System.Windows.MessageBox.Show($"添加成员失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DutyGroupsList.SelectedItem is DutyGroup g && GroupMembersList.SelectedItem != null)
                {
                    // 使用反射获取 StudentId（兼容匿名类型）
                    var sel = GroupMembersList.SelectedItem;
                    var prop = sel.GetType().GetProperty("StudentId");
                    if (prop == null)
                    {
                        LogService.LogError("RemoveMemberBtn", "无法获取选中项的 StudentId 属性");
                        return;
                    }
                    string sid = prop.GetValue(sel)?.ToString();
                    if (string.IsNullOrEmpty(sid)) return;

                    var member = g.Members.FirstOrDefault(m => m.StudentId == sid);
                    if (member != null)
                    {
                        g.Members.Remove(member);
                        DutyGroupsList_SelectionChanged(null, null);
                        AutoSave();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "移除值日组成员失败");
            }
        }

        private void ApplyGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DutyGroupsList.SelectedItem is DutyGroup g)
                {
                    if (string.IsNullOrWhiteSpace(GroupNameBox.Text))
                    {
                        System.Windows.MessageBox.Show("值日组名不能为空。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    g.Name = GroupNameBox.Text;
                    RefreshLists();
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "应用值日组更改失败"); }
        }

        private void ResetGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DutyGroupsList.SelectedItem is DutyGroup g)
                {
                    GroupNameBox.Text = g.Name;
                    GroupMembersList.ItemsSource = g.Members.Select(m =>
                        new
                        {
                            StudentName = _state.Students.FirstOrDefault(s => s.Id == m.StudentId)?.Name ?? "",
                            m.Role,
                            m.StudentId
                        }).ToList();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "重置值日组表单失败"); }
        }

        private void FontSizeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (double.TryParse(FontSizeBox.Text, out var fs))
                {
                    _state.FontSize = Math.Max(8, Math.Min(48, fs));
                    FontSizeBox.Text = _state.FontSize.ToString(); // 回写合法值
                    AutoSave();
                    NotifyMainWindow();
                }
                else
                {
                    // 恢复原值
                    FontSizeBox.Text = _state.FontSize.ToString();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "字体大小设置失败"); }
        }

        private void FontSizeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FontSizeBox_LostFocus(sender, e);
            }
        }

        private void RunOnStartupChk_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RunOnStartupChk.IsChecked == true)
                {
                    SetRunOnStartup();
                }
                else
                {
                    RemoveRunOnStartup();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "开机自启动设置失败"); }
        }

        private void EnableShutdownChk_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                _state.EnableAutoShutdown = EnableShutdownChk.IsChecked == true;
                AutoSave();
                SetupShutdownTimer();
            }
            catch (Exception ex) { LogService.Log(ex, "自动关机设置变更失败"); }
        }

        private void EnableSpeechChk_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                _state.EnableSpeech = EnableSpeechChk.IsChecked == true;
                AutoSave();
            }
            catch (Exception ex) { LogService.Log(ex, "语音播报设置变更失败"); }
        }

        private void SemesterEndPicker_Changed()
        {
            try
            {
                if (SemesterEndPicker.SelectedDate.HasValue)
                {
                    _state.SemesterEndDate = SemesterEndPicker.SelectedDate.Value;
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "学期结束日期设置失败"); }
        }

        private void AutoShutdownTimeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsValidTime(AutoShutdownTimeBox.Text))
                {
                    _state.AutoShutdownTime = AutoShutdownTimeBox.Text;
                    AutoSave();
                    SetupShutdownTimer();
                }
                else
                {
                    // 恢复原值
                    AutoShutdownTimeBox.Text = _state.AutoShutdownTime;
                    System.Windows.MessageBox.Show("时间格式无效，请使用 HH:mm 格式（如 23:00）。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex) { LogService.Log(ex, "关机时间设置失败"); }
        }

        private void AutoShutdownTimeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AutoShutdownTimeBox_LostFocus(sender, e);
            }
        }

        private void DutyCalendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // 选中日期时更新界面（目前无需操作）
        }

        private void AssignDutyBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DutyCalendar.SelectedDate == null)
                {
                    System.Windows.MessageBox.Show("请先选择一个日期。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (DutyGroupAssignBox.SelectedItem == null)
                {
                    System.Windows.MessageBox.Show("请先选择一个值日组。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string gid;
                if (DutyGroupAssignBox.SelectedItem is DutyGroup g)
                {
                    gid = g.Id;
                }
                else
                {
                    var prop = DutyGroupAssignBox.SelectedItem.GetType().GetProperty("Id");
                    if (prop == null)
                    {
                        LogService.LogError("AssignDuty", "无法获取选中值日组的 Id");
                        return;
                    }
                    gid = prop.GetValue(DutyGroupAssignBox.SelectedItem)?.ToString();
                    if (string.IsNullOrEmpty(gid)) return;
                }

                var date = DutyCalendar.SelectedDate.Value.Date;

                // 检查是否已存在同日的安排
                if (_state.DailyDuties.Any(d => d.Date.Date == date))
                {
                    System.Windows.MessageBox.Show("该日期已有值日安排，请先移除旧安排。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _state.DailyDuties.Add(new DailyDuty { Date = date, DutyGroupId = gid });
                RefreshLists();
                AutoSave();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "指派值日失败");
                System.Windows.MessageBox.Show($"指派值日失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveAssignedDutyBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AssignedDutiesList.SelectedItem is DailyDuty d)
                {
                    _state.DailyDuties.Remove(d);
                    RefreshLists();
                    AutoSave();
                }
            }
            catch (Exception ex) { LogService.Log(ex, "移除值日安排失败"); }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AutoSave()
        {
            try
            {
                StorageService.Save(_state);
                NotifyMainWindow();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "自动保存失败");
            }
        }

        /// <summary>
        /// 安全地通知主窗口更新课程表窗口
        /// </summary>
        private void NotifyMainWindow()
        {
            try
            {
                var main = System.Windows.Application.Current?.MainWindow as MainWindow;
                main?.ReloadState();
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "通知主窗口更新失败");
            }
        }

        private void SetupShutdownTimer()
        {
            try
            {
                _shutdownTimer.Stop();

                if (!_state.EnableAutoShutdown)
                    return;

                if (!IsValidTime(_state.AutoShutdownTime))
                {
                    LogService.LogError("SetupShutdownTimer", $"无效的关机时间: {_state.AutoShutdownTime}");
                    return;
                }

                var parts = _state.AutoShutdownTime.Split(':');
                if (!int.TryParse(parts[0], out int hh) || !int.TryParse(parts[1], out int mm))
                {
                    LogService.LogError("SetupShutdownTimer", $"关机时间解析失败: {_state.AutoShutdownTime}");
                    return;
                }

                var now = DateTime.Now;
                var next = new DateTime(now.Year, now.Month, now.Day, hh, mm, 0);
                if (next <= now)
                    next = next.AddDays(1);

                var span = next - now;
                if (span.TotalMilliseconds <= 0)
                    span = TimeSpan.FromDays(1);

                _shutdownTimer.Interval = span;
                _shutdownTimer.Start();
                LogService.Log($"自动关机定时器已设置: {next:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "设置关机定时器失败");
            }
        }

        private void ShutdownTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _shutdownTimer.Stop();
                LogService.Log("执行定时关机");

                var psi = new ProcessStartInfo("shutdown", "/s /t 60")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "执行关机命令失败");
            }
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
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    if (key == null) return false;
                    var val = key.GetValue("smartClass") as string;
                    return !string.IsNullOrEmpty(val);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "读取开机自启动注册表失败");
                return false;
            }
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
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key == null)
                    {
                        LogService.LogError("SetRunOnStartup", "无法打开注册表 Run 键");
                        return;
                    }
                    var path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrEmpty(path))
                    {
                        LogService.LogError("SetRunOnStartup", "无法获取 exe 路径");
                        return;
                    }
                    key.SetValue("smartClass", $"\"{path}\" --minimized");
                    LogService.Log($"已设置开机自启动: {path}");
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "设置开机自启动失败");
                System.Windows.MessageBox.Show("设置开机自启动失败，可能需要管理员权限。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveRunOnStartup()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key == null) return;
                    key.DeleteValue("smartClass", false);
                    LogService.Log("已移除开机自启动");
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "移除开机自启动失败");
            }
        }

        #region 开发者选项

        /// <summary>
        /// 关于页面标题点击计数器：7次后显示开发者选项面板
        /// </summary>
        private void AboutTitle_Click()
        {
            try
            {
                _aboutClickCount++;
                if (_aboutClickCount >= 7)
                {
                    DevPanel.Visibility = Visibility.Visible;
                    LogService.Log("开发者选项面板已激活");
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "关于页点击计数");
            }
        }

        private void DevTestNotifyBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var main = System.Windows.Application.Current?.MainWindow as MainWindow;
                if (main != null)
                {
                    main.ShowTestNotification("🧪 测试通知", "这是一条来自开发者选项的测试通知！", 5000);
                    System.Windows.MessageBox.Show(
                        "测试通知已发送。\n\n请查看系统托盘区域的气泡提示。",
                        "测试通知",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("无法获取主窗口，请重启程序后重试。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "开发者-测试通知");
                System.Windows.MessageBox.Show($"测试通知失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DevTestDialogBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new AutoCloseDialog(
                    "🔧 开发者测试对话框\n\n这是一个测试对话框，将在30秒后自动关闭。\n点击 [是] 或 [否] 测试按钮响应。",
                    TimeSpan.FromSeconds(30));
                dlg.Owner = this;
                var result = dlg.ShowDialog();

                string msg;
                if (result == true)
                    msg = "你点击了 [是]";
                else if (result == false)
                    msg = "你点击了 [否]";
                else
                    msg = "对话框超时自动关闭";

                System.Windows.MessageBox.Show(msg, "对话框测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "开发者-测试对话框");
                System.Windows.MessageBox.Show($"测试对话框失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DevTestSpeechBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SpeechService.SpeakAsync("这是一条语音播报测试。如果你能听到这段话，说明语音功能正常工作。");
                System.Windows.MessageBox.Show(
                    "语音播报测试已触发。\n\n如果你听到「这是一条语音播报测试...」的语音，说明语音功能正常。\n\n请检查扬声器是否开启。",
                    "测试语音播报",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "开发者-测试语音播报");
                System.Windows.MessageBox.Show($"语音播报测试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DevViewLogBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                if (System.IO.File.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", logPath);
                }
                else
                {
                    // 尝试旧日志
                    var oldLogPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.old.log");
                    if (System.IO.File.Exists(oldLogPath))
                    {
                        System.Diagnostics.Process.Start("notepad.exe", oldLogPath);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "暂无日志文件。\n\n日志文件路径:\n" + logPath,
                            "查看日志",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "开发者-查看日志");
                System.Windows.MessageBox.Show($"打开日志失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
