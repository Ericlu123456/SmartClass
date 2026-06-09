using System;

namespace smartClass.Models
{
    public class Course
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public string DayOfWeek { get; set; }
        // 上课开始时间（HH:mm）
        public string StartTime { get; set; }
        // 下课结束时间（HH:mm）
        public string EndTime { get; set; }
    }
}
