using AMS.Models;
using Microsoft.AspNetCore.Mvc;
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

        // Create Teacher
        public IActionResult Create()
        {
            ViewBag.Courses = GetCourses();
            ViewBag.Sections = GetSections();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Teacher teacher, int[] SectionIds)
        {
            try
            {
                SaveTeacher(teacher, SectionIds);
                TempData["SuccessMessage"] = "Teacher created successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating teacher: {ex.Message}";
            }

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

                string teacherQuery = "INSERT INTO Teacher (Name, Password, CourseId) VALUES (@Name, @Password, @CourseId); SELECT SCOPE_IDENTITY();";
                SqlCommand teacherCommand = new SqlCommand(teacherQuery, connection);
                teacherCommand.Parameters.AddWithValue("@Name", teacher.Name);
                teacherCommand.Parameters.AddWithValue("@Password", teacher.Password);
                teacherCommand.Parameters.AddWithValue("@CourseId", teacher.CourseId);

                int teacherId = Convert.ToInt32(teacherCommand.ExecuteScalar());

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

        // Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
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
                        int teacherId = (int)reader["Id"];
                        HttpContext.Session.SetInt32("TeacherId", teacherId); // Save TeacherId in session
                        TempData["WelcomeMessage"] = $"Welcome, {reader["Name"]}!";
                        return RedirectToAction("TeacherDashboard");
                    }
                    else
                    {
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


        public IActionResult TeacherDashboard()
        {
            ViewBag.Message = TempData["WelcomeMessage"];
            return View();
        }

        // Generate Code
        [HttpGet]
        [HttpGet]
        public IActionResult GenerateCode()
        {
            if (HttpContext.Session.GetInt32("TeacherId") is int teacherId)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Fetch teacher details
                    string teacherQuery = "SELECT t.Id, t.Name, c.Name AS CourseName " +
                                          "FROM Teacher t " +
                                          "INNER JOIN Course c ON t.CourseId = c.Id " +
                                          "WHERE t.Id = @Id";
                    SqlCommand teacherCommand = new SqlCommand(teacherQuery, connection);
                    teacherCommand.Parameters.AddWithValue("@Id", teacherId);

                    SqlDataReader teacherReader = teacherCommand.ExecuteReader();
                    if (teacherReader.Read())
                    {
                        ViewBag.Teacher = new
                        {
                            Id = (int)teacherReader["Id"],
                            Name = teacherReader["Name"].ToString(),
                            CourseName = teacherReader["CourseName"].ToString()
                        };
                    }
                    teacherReader.Close();

                    // Fetch sections
                    string sectionsQuery = "SELECT s.Id, s.Name FROM Section s " +
                                           "INNER JOIN TeacherSection ts ON s.Id = ts.SectionId " +
                                           "WHERE ts.TeacherId = @TeacherId";
                    SqlCommand sectionsCommand = new SqlCommand(sectionsQuery, connection);
                    sectionsCommand.Parameters.AddWithValue("@TeacherId", teacherId);

                    SqlDataReader sectionsReader = sectionsCommand.ExecuteReader();
                    var sections = new List<Section>();
                    while (sectionsReader.Read())
                    {
                        sections.Add(new Section
                        {
                            Id = (int)sectionsReader["Id"],
                            Name = sectionsReader["Name"].ToString()
                        });
                    }
                    ViewBag.Sections = sections;
                }

                return View();
            }

            TempData["ErrorMessage"] = "Session expired. Please log in again.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult GenerateCode(int sectionId)
        {
            if (HttpContext.Session.GetInt32("TeacherId") is int teacherId)
            {
                // Generate a unique code
                string generatedCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                // Return the generated code to the view
                TempData["GeneratedCode"] = generatedCode;

                return RedirectToAction("GenerateCode");
            }

            TempData["ErrorMessage"] = "Session expired. Please log in again.";
            return RedirectToAction("Login");
        }

    }
}
