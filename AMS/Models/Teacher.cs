namespace AMS.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int CourseId { get; set; } // Foreign key to Course
        //public string CourseName { get; set; }
    }
}
