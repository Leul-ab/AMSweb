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
        [HttpPost]
        public IActionResult GenerateCode(int sectionId)
        {
            if (HttpContext.Session.GetInt32("TeacherId") is int teacherId)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Get the course ID for the logged-in teacher
                        string courseQuery = "SELECT CourseId FROM Teacher WHERE Id = @TeacherId";
                        int courseId;

                        using (SqlCommand courseCommand = new SqlCommand(courseQuery, connection))
                        {
                            courseCommand.Parameters.AddWithValue("@TeacherId", teacherId);
                            object result = courseCommand.ExecuteScalar();
                            if (result == null)
                            {
                                TempData["ErrorMessage"] = "Course not found for the logged-in teacher.";
                                return RedirectToAction("GenerateCode");
                            }
                            courseId = Convert.ToInt32(result);
                        }

                        // Generate a unique temporary ID
                        string temporaryId = GenerateUniqueTemporaryId(connection);

                        // Insert into the Attendance table
                        string insertQuery = "INSERT INTO Attendance (TeacherId, CourseId, SectionId, TemporaryId) " +
                                             "VALUES (@TeacherId, @CourseId, @SectionId, @TemporaryId)";
                        using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@TeacherId", teacherId);
                            insertCommand.Parameters.AddWithValue("@CourseId", courseId);
                            insertCommand.Parameters.AddWithValue("@SectionId", sectionId);
                            insertCommand.Parameters.AddWithValue("@TemporaryId", temporaryId);

                            insertCommand.ExecuteNonQuery();
                        }

                        // Display the generated code
                        TempData["GeneratedCode"] = temporaryId;
                        TempData["SuccessMessage"] = "Temporary ID generated successfully!";
                    }

                    return RedirectToAction("GenerateCode");
                }
                catch (SqlException ex)
                {
                    TempData["ErrorMessage"] = $"SQL Error: {ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
            }

            return RedirectToAction("Login");
        }

        private string GenerateUniqueTemporaryId(SqlConnection connection)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
            var random = new Random();
            string temporaryId;
            bool isUnique;

            do
            {
                temporaryId = new string(Enumerable.Repeat(chars, 12)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                // Check if TemporaryId is unique
                string checkQuery = "SELECT COUNT(*) FROM Attendance WHERE TemporaryId = @TemporaryId";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@TemporaryId", temporaryId);
                    isUnique = Convert.ToInt32(checkCommand.ExecuteScalar()) == 0;
                }
            } while (!isUnique);

            return temporaryId;
        }

        [HttpGet]
        public IActionResult List()
        {
            List<TeacherViewModel> teachers = new List<TeacherViewModel>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                SELECT t.Id AS TeacherId, t.Name AS TeacherName, 
                       c.Name AS CourseName, 
                       STRING_AGG(sec.Name, ', ') AS Sections
                FROM Teacher t
                LEFT JOIN TeacherSection ts ON t.Id = ts.TeacherId
                LEFT JOIN Section sec ON ts.SectionId = sec.Id
                LEFT JOIN Course c ON t.CourseId = c.Id
                GROUP BY t.Id, t.Name, c.Name
                ORDER BY t.Name";

                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        teachers.Add(new TeacherViewModel
                        {
                            TeacherId = (int)reader["TeacherId"],
                            TeacherName = reader["TeacherName"].ToString(),
                            CourseName = reader["CourseName"] != DBNull.Value ? reader["CourseName"].ToString() : "No Course Assigned",
                            Sections = reader["Sections"] != DBNull.Value ? reader["Sections"].ToString() : "No Sections Assigned"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return View(teachers);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            Teacher teacher = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT Id, Name, Password, CourseId FROM Teacher WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", id);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        teacher = new Teacher
                        {
                            Id = (int)reader["Id"],
                            Name = reader["Name"].ToString(),
                            Password = reader["Password"].ToString(),
                            CourseId = (int)reader["CourseId"]
                        };
                    }
                }

                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction("List");
                }

                // Populate courses for dropdown
                ViewBag.Courses = GetCourses();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("List");
            }

            return View(teacher);
        }

        [HttpPost]
        public IActionResult Edit(Teacher teacher)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "UPDATE Teacher SET Name = @Name, Password = @Password, CourseId = @CourseId WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", teacher.Name);
                    command.Parameters.AddWithValue("@Password", teacher.Password);
                    command.Parameters.AddWithValue("@CourseId", teacher.CourseId);
                    command.Parameters.AddWithValue("@Id", teacher.Id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Teacher updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("List");
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM Teacher WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Teacher deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("List");
        }

    }
}
