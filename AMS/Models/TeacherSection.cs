namespace AMS.Models
{
    public class TeacherSection
    {
        public int Id { get; set; }
        public int TeacherId { get; set; } // Foreign key to Teacher
        public int SectionId { get; set; } // Foreign key to Section
    }
}
