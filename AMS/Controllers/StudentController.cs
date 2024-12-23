using AMS.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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
                var model = new SubmitAttendanceViewModel
                {
                    StudentId = studentId,
                    Name = string.Empty,
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
                                model.Name = reader["Name"].ToString();
                                model.Section = reader["SectionName"].ToString();
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
                                model.Courses.Add(new Course
                                {
                                    Id = (int)courseReader["Id"],
                                    Name = courseReader["Name"].ToString()
                                });
                            }
                        }
                    }
                }

                return View(model);
            }

            TempData["ErrorMessage"] = "Session expired. Please log in again.";
            return RedirectToAction("Login");
        }


        [HttpPost]
        public IActionResult SubmitAttendance(int CourseId, string TemporaryId)
        {
            if (HttpContext.Session.GetInt32("StudentId") is not int studentId)
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // Query to find the relevant Attendance record
                    string query = @"
                        SELECT Id, Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9, Day10,
                               Day11, Day12, Day13, Day14, Day15, Day16, Day17, Day18, Day19, Day20,
                               Day21, Day22, Day23, Day24, Day25, Day26, Day27, Day28, Day29, Day30
                        FROM Attendance
                        WHERE CourseId = @CourseId AND SectionId = 
                        (SELECT SectionId FROM Student WHERE Id = @StudentId) AND TemporaryId = @TemporaryId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@CourseId", CourseId);
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    command.Parameters.AddWithValue("@TemporaryId", TemporaryId);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        int attendanceId = (int)reader["Id"];

                        // Find the first null day (e.g., Day1, Day2, etc.)
                        string dayToUpdate = null;
                        for (int i = 1; i <= 30; i++)
                        {
                            if (reader[$"Day{i}"] == DBNull.Value)
                            {
                                dayToUpdate = $"Day{i}";
                                break;
                            }
                        }

                        if (dayToUpdate != null)
                        {
                            reader.Close();

                            // Update the found day column to TRUE
                            string updateQuery = $@"
                                UPDATE Attendance 
                                SET {dayToUpdate} = 1 
                                WHERE Id = @AttendanceId";

                            SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                            updateCommand.Parameters.AddWithValue("@AttendanceId", attendanceId);
                            updateCommand.ExecuteNonQuery();

                            TempData["SuccessMessage"] = "Attendance submitted successfully!";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Attendance for all days has already been submitted.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Invalid Temporary ID or Course.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("SubmitAttendance");
        }

        [HttpGet]
        public IActionResult Status()
        {
            if (HttpContext.Session.GetInt32("StudentId") is not int studentId)
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login");
            }

            List<AttendanceStatusViewModel> statuses = new List<AttendanceStatusViewModel>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT c.Name AS CourseName, s.Name AS SectionName,
                               a.Day1, a.Day2, a.Day3, a.Day4, a.Day5, 
                               a.Day6, a.Day7, a.Day8, a.Day9, a.Day10
                        FROM Attendance a
                        INNER JOIN Course c ON a.CourseId = c.Id
                        INNER JOIN Section s ON a.SectionId = s.Id
                        INNER JOIN StudentCourse sc ON sc.CourseId = c.Id
                        WHERE sc.StudentId = @StudentId";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        statuses.Add(new AttendanceStatusViewModel
                        {
                            CourseName = reader["CourseName"].ToString(),
                            SectionName = reader["SectionName"].ToString(),
                            Day1 = reader["Day1"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["Day1"]) : null,
                            Day2 = reader["Day2"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["Day2"]) : null,
                            Day3 = reader["Day3"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["Day3"]) : null,
                            // Continue for all days up to Day30
                        });

                    }

                }

                ViewBag.Statuses = statuses;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while fetching attendance: {ex.Message}";
            }

            return View();
        }
    }
}
