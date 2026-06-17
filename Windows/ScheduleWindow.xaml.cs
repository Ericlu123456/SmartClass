using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
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

        ApplyTheme();

        this.SizeToContent = SizeToContent.WidthAndHeight;

        UpdateUI();

        this.UpdateLayout();
        PositionWindowBottom();

        this.MouseLeftButtonDown += ScheduleWindow_MouseLeftButtonDown;
    }

    private bool IsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            return (int?)key?.GetValue("AppsUseLightTheme") == 0;
        }
        catch
        {
            return false;
        }
    }

    /*
     * v1.1
     * 增加自动深浅模式支持
     */
    private void ApplyTheme()
    {
        if (IsDarkTheme())
        {
            MainBorder.Background =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(230, 25, 25, 25));

            MainBorder.BorderBrush =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(70, 70, 70));

            this.Foreground =
                System.Windows.Media.Brushes.White;
        }
        else
        {
            MainBorder.Background =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(220, 255, 255, 255));

            MainBorder.BorderBrush =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(210, 210, 210));

            this.Foreground =
                System.Windows.Media.Brushes.Black;
        }
    }

    private void ScheduleWindow_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        catch
        {
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

        Top = screen.Bottom - h - 20;

        Topmost = false;
    }

    public void UpdateState(AppState state)
    {
        _state = state;

        UpdateUI();

        UpdateLayout();

        PositionWindowBottom();
    }

    private void UpdateUI()
    {
        var map = new[]
        {
            "周日",
            "周一",
            "周二",
            "周三",
            "周四",
            "周五",
            "周六"
        };

        var today = DateTime.Now.DayOfWeek;
        var todayText = map[(int)today];

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

            CoursesList.FontSize = fs;
            DutyList.FontSize = fs;
        }
        catch
        {
        }
    }
}


}
