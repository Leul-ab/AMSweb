using Microsoft.AspNetCore.Mvc;
using AMS.Models;
using System.Data.SqlClient;

namespace AMS.Controllers
{
    public class TeacherController : Controller
    {
        private readonly string _connectionString;

        public TeacherController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Create()
        {
            // Populate courses
            ViewBag.Courses = GetCourses();

            // Populate sections
            ViewBag.Sections = GetSections();

            return View();
        }

        [HttpPost]
        public IActionResult Create(Teacher teacher, int[] SectionIds)
        {
            try
            {
                // Save teacher and assigned sections
                SaveTeacher(teacher, SectionIds);

                // Set success message
                TempData["SuccessMessage"] = "Teacher created successfully!";
            }
            catch (Exception ex)
            {
                // Set error message
                TempData["ErrorMessage"] = $"Error creating teacher: {ex.Message}";
            }

            // Redirect to Create page to show the message
            return RedirectToAction("Create");
        }

        private List<Course> GetCourses()
        {
            var courses = new List<Course>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, Name FROM Course";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    courses.Add(new Course
                    {
                        Id = (int)reader["Id"],
                        Name = (string)reader["Name"]
                    });
                }
            }

            return courses;
        }

        private List<Section> GetSections()
        {
            var sections = new List<Section>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, Name FROM Section";
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    sections.Add(new Section
                    {
                        Id = (int)reader["Id"],
                        Name = (string)reader["Name"]
                    });
                }
            }

            return sections;
        }

        private void SaveTeacher(Teacher teacher, int[] SectionIds)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Insert teacher
                string teacherQuery = "INSERT INTO Teacher (Name, Password, CourseId) VALUES (@Name, @Password, @CourseId); SELECT SCOPE_IDENTITY();";
                SqlCommand teacherCommand = new SqlCommand(teacherQuery, connection);
                teacherCommand.Parameters.AddWithValue("@Name", teacher.Name);
                teacherCommand.Parameters.AddWithValue("@Password", teacher.Password);
                teacherCommand.Parameters.AddWithValue("@CourseId", teacher.CourseId);

                int teacherId = Convert.ToInt32(teacherCommand.ExecuteScalar());

                // Insert teacher sections
                foreach (var sectionId in SectionIds)
                {
                    string sectionQuery = "INSERT INTO TeacherSection (TeacherId, SectionId) VALUES (@TeacherId, @SectionId)";
                    SqlCommand sectionCommand = new SqlCommand(sectionQuery, connection);
                    sectionCommand.Parameters.AddWithValue("@TeacherId", teacherId);
                    sectionCommand.Parameters.AddWithValue("@SectionId", sectionId);

                    sectionCommand.ExecuteNonQuery();
                }
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(Teacher teacher)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT Id, Name FROM Teacher WHERE Name = @Name AND Password = @Password";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", teacher.Name);
                    command.Parameters.AddWithValue("@Password", teacher.Password);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        // Login successful
                        TempData["WelcomeMessage"] = $"Welcome, {reader["Name"]}!";
                        return RedirectToAction("Welcome");
                    }
                    else
                    {
                        // Login failed
                        TempData["ErrorMessage"] = "Invalid Name or Password!";
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        public IActionResult Welcome()
        {
            ViewBag.Message = TempData["WelcomeMessage"];
            return View();
        }

    }
}
