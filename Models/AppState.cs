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
    }
}
