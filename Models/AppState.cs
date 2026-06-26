using System;
using System.Collections.Generic;

namespace smartClass.Models
{
    public class AppState
    {
        public List<Student> Students { get; set; } = new List<Student>();
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<DutyGroup> DutyGroups { get; set; } = new List<DutyGroup>();
        public List<DailyDuty> DailyDuties { get; set; } = new List<DailyDuty>();
        // UI preferences
        public double FontSize { get; set; } = 14.0;
        // 常规设置：是否启用每天自动关机，以及时间（HH:mm）
        public bool EnableAutoShutdown { get; set; } = false;
        public string AutoShutdownTime { get; set; } = "23:00";
        // 是否启用语音播报上下课提醒
        public bool EnableSpeech { get; set; } = true;
        // 课程表窗口上次位置（-1 表示使用默认底部定位）
        public double ScheduleWindowLeft { get; set; } = -1;
        public double ScheduleWindowTop { get; set; } = -1;
        // 学期结束日期（用于顶栏倒计时）
        public DateTime SemesterEndDate { get; set; } = new DateTime(2026, 7, 15);
    }
}
