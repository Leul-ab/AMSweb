namespace AMS.Models
{
    public class AttendanceStatusViewModel
    {
        public int AttendanceId { get; set; }
        public string CourseName { get; set; }
        public string SectionName { get; set; }
        public bool? Day1 { get; set; } // Attendance for Day 1
        public bool? Day2 { get; set; } // Attendance for Day 2
        public bool? Day3 { get; set; } // Attendance for Day 3
    }

}
