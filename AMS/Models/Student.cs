namespace AMS.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }

        // Relationships
        public int SectionId { get; set; } // Foreign Key
        public List<StudentCourse> StudentCourses { get; set; } // Navigation Property
    }


}
