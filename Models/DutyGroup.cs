using System;
using System.Collections.Generic;

namespace SamrtClass.Models
{
    public class DutyMember
    {
        public string StudentId { get; set; }
        public string Role { get; set; }
    }

    public class DutyGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<DutyMember> Members { get; set; } = new List<DutyMember>();
    }
}
