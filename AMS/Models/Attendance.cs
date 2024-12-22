namespace AMS.Models
{
    namespace AMS.Models
    {
        public class Attendance
        {
            public int Id { get; set; }
            public int TeacherId { get; set; }
            public int CourseId { get; set; }
            public int SectionId { get; set; }
            public string TemporaryId { get; set; }
            public DateTime DateCreated { get; set; }
        }
    }

}
