namespace AMS.Models
{
    public class SubmitAttendanceViewModel
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public string Section { get; set; }
        public List<Course> Courses { get; set; }
    }


}
