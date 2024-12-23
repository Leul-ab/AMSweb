namespace AMS.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; } // Optional: For convenience if you want to display section name
    }
}
