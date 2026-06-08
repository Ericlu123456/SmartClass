using System;
using System.Collections.Generic;

namespace SamrtClass.Models
{
    public class AppState
    {
        public List<Student> Students { get; set; } = new List<Student>();
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<DutyGroup> DutyGroups { get; set; } = new List<DutyGroup>();
        public List<DailyDuty> DailyDuties { get; set; } = new List<DailyDuty>();
        // UI preferences
        public double FontSize { get; set; } = 14.0;
    }
}
