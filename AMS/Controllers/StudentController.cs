using AMS.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace AMS.Controllers
{
    public class StudentController : Controller
    {
        private readonly string _connectionString;

        public StudentController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Fetch sections for the dropdown
            ViewBag.Sections = GetSections();
            ViewBag.Courses = GetCourses();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Student student, int[] CourseIds)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Insert student into Student table
                    string studentQuery = "INSERT INTO Student (Name, Password, SectionId) VALUES (@Name, @Password, @SectionId); SELECT SCOPE_IDENTITY();";
                    SqlCommand studentCommand = new SqlCommand(studentQuery, connection);
                    studentCommand.Parameters.AddWithValue("@Name", student.Name);
                    studentCommand.Parameters.AddWithValue("@Password", student.Password);
                    studentCommand.Parameters.AddWithValue("@SectionId", student.SectionId);

                    int studentId = Convert.ToInt32(studentCommand.ExecuteScalar());

                    // Insert student courses into StudentCourse table
                    foreach (var courseId in CourseIds)
                    {
                        string courseQuery = "INSERT INTO StudentCourse (StudentId, CourseId) VALUES (@StudentId, @CourseId)";
                        SqlCommand courseCommand = new SqlCommand(courseQuery, connection);
                        courseCommand.Parameters.AddWithValue("@StudentId", studentId);
                        courseCommand.Parameters.AddWithValue("@CourseId", courseId);

                        courseCommand.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Student registered successfully with selected courses!";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error registering student: {ex.Message}";
                return RedirectToAction("Create");
            }
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
                        Name = reader["Name"].ToString()
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
                        Name = reader["Name"].ToString()
                    });
                }
            }

            return sections;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(Student student)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT Id, Name, SectionId FROM Student WHERE Name = @Name AND Password = @Password";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", student.Name);
                    command.Parameters.AddWithValue("@Password", student.Password);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        // Login successful
                        HttpContext.Session.SetInt32("StudentId", (int)reader["Id"]);
                        HttpContext.Session.SetString("StudentName", reader["Name"].ToString());
                        HttpContext.Session.SetInt32("SectionId", (int)reader["SectionId"]);

                        return RedirectToAction("StudentDashboard");
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

        public IActionResult StudentDashboard()
        {
            if (HttpContext.Session.GetInt32("StudentId") is int studentId)
            {
                ViewBag.StudentName = HttpContext.Session.GetString("StudentName");
                return View();
            }

            TempData["ErrorMessage"] = "Session expired. Please log in again.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult SubmitAttendance()
        {
            if (HttpContext.Session.GetInt32("StudentId") is int studentId)
            {
                var studentDetails = new
                {
                    Name = string.Empty,
                    Id = studentId,
                    Section = string.Empty,
                    Courses = new List<Course>()
                };

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Fetch student details
                    string studentQuery = "SELECT s.Name, sec.Name AS SectionName " +
                                          "FROM Student s " +
                                          "INNER JOIN Section sec ON s.SectionId = sec.Id " +
                                          "WHERE s.Id = @StudentId";
                    using (SqlCommand studentCommand = new SqlCommand(studentQuery, connection))
                    {
                        studentCommand.Parameters.AddWithValue("@StudentId", studentId);

                        using (SqlDataReader reader = studentCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                studentDetails = new
                                {
                                    Name = reader["Name"].ToString(),
                                    Id = studentId,
                                    Section = reader["SectionName"].ToString(),
                                    Courses = new List<Course>()
                                };
                            }
                        }
                    }

                    // Fetch courses for the student
                    string courseQuery = "SELECT c.Id, c.Name " +
                                         "FROM StudentCourse sc " +
                                         "INNER JOIN Course c ON sc.CourseId = c.Id " +
                                         "WHERE sc.StudentId = @StudentId";
                    using (SqlCommand courseCommand = new SqlCommand(courseQuery, connection))
                    {
                        courseCommand.Parameters.AddWithValue("@StudentId", studentId);

                        using (SqlDataReader courseReader = courseCommand.ExecuteReader())
                        {
                            while (courseReader.Read())
                            {
                                studentDetails.Courses.Add(new Course
                                {
                                    Id = (int)courseReader["Id"],
                                    Name = courseReader["Name"].ToString()
                                });
                            }
                        }
                    }
                }

                ViewBag.StudentDetails = studentDetails;
                return View();
            }

            TempData["ErrorMessage"] = "Session expired. Please log in again.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult SubmitAttendance(int courseId, string password)
        {
            if (HttpContext.Session.GetInt32("StudentId") is int studentId)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Verify student password
                    string passwordQuery = "SELECT Password FROM Student WHERE Id = @StudentId";
                    using (SqlCommand passwordCommand = new SqlCommand(passwordQuery, connection))
                    {
                        passwordCommand.Parameters.AddWithValue("@StudentId", studentId);
                        string storedPassword = passwordCommand.ExecuteScalar()?.ToString();

                        if (storedPassword != password)
                        {
                            TempData["ErrorMessage"] = "Invalid password!";
                            return RedirectToAction("SubmitAttendance");
                        }
                    }

                    // Mark attendance
                    string attendanceQuery = "INSERT INTO Attendance (TeacherId, CourseId, SectionId, TemporaryId) " +
                                             "SELECT t.Id, @CourseId, s.SectionId, NEWID() " +
                                             "FROM Student s " +
                                             "INNER JOIN Course c ON c.Id = @CourseId " +
                                             "INNER JOIN Teacher t ON t.CourseId = c.Id " +
                                             "WHERE s.Id = @StudentId";
                    using (SqlCommand attendanceCommand = new SqlCommand(attendanceQuery, connection))
                    {
                        attendanceCommand.Parameters.AddWithValue("@StudentId", studentId);
                        attendanceCommand.Parameters.AddWithValue("@CourseId", courseId);

                        attendanceCommand.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Attendance submitted successfully!";
                return RedirectToAction("SubmitAttendance");
            }

            TempData["ErrorMessage"] = "Session expired. Please log in again.";
            return RedirectToAction("Login");
        }


        [HttpGet]
        public IActionResult Status()
        {
            if (HttpContext.Session.GetInt32("StudentId") is int studentId)
            {
                var attendanceList = new List<dynamic>();

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                SELECT a.Id, c.Name AS CourseName, s.Name AS SectionName, a.Day1, a.Day2, a.Day3, a.Day4, a.Day5, a.Day6, a.Day7, 
                       a.Day8, a.Day9, a.Day10, a.Day11, a.Day12, a.Day13, a.Day14, a.Day15, a.Day16, a.Day17, a.Day18, a.Day19, 
                       a.Day20, a.Day21, a.Day22, a.Day23, a.Day24, a.Day25, a.Day26, a.Day27, a.Day28, a.Day29, a.Day30
                FROM Attendance a
                INNER JOIN Course c ON a.CourseId = c.Id
                INNER JOIN Section s ON a.SectionId = s.Id
                INNER JOIN StudentCourse sc ON sc.CourseId = c.Id
                WHERE sc.StudentId = @StudentId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@StudentId", studentId);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        attendanceList.Add(new
                        {
                            Id = reader["Id"],
                            CourseName = reader["CourseName"].ToString(),
                            SectionName = reader["SectionName"].ToString(),
                            Days = Enumerable.Range(1, 30)
                                             .Select(day => reader[$"Day{day}"] is DBNull ? "N/A" : (bool)reader[$"Day{day}"] ? "✔️" : "❌")
                                             .ToList()
                        });
                    }
                }

                return View(attendanceList);
            }

            TempData["ErrorMessage"] = "Session expired. Please log in again.";
            return RedirectToAction("Login");
        }


    }
}
