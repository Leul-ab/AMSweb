using AMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
            if (HttpContext.Session.GetInt32("StudentId") is int studentId)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // Validate the TemporaryId and retrieve the Day, SectionId, and TeacherId
                        string query = @"
SELECT TeacherId, SectionId, 
        CASE 
            WHEN [Day1] = 0 THEN 1
            WHEN [Day2] = 0 THEN 2
            WHEN [Day3] = 0 THEN 3
            WHEN [Day4] = 0 THEN 4
            WHEN [Day5] = 0 THEN 5
            -- Repeat for all Day columns (up to Day30)
            WHEN [Day30] = 0 THEN 30
            ELSE 0
        END AS DayNumber
FROM Attendance
WHERE TemporaryId = @TemporaryId AND CourseId = @CourseId AND SectionId = (SELECT SectionId FROM Student WHERE Id = @StudentId)";

                        int dayNumber = 0;
                        int sectionId = 0;
                        int teacherId = 0;

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@TemporaryId", TemporaryId);
                            command.Parameters.AddWithValue("@CourseId", CourseId);
                            command.Parameters.AddWithValue("@StudentId", studentId); // Use studentId to get the section for the student

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    dayNumber = reader.GetInt32(reader.GetOrdinal("DayNumber"));
                                    sectionId = reader.GetInt32(reader.GetOrdinal("SectionId"));
                                    teacherId = reader.GetInt32(reader.GetOrdinal("TeacherId"));
                                }
                            }
                        }

                        if (dayNumber == 0)
                        {
                            TempData["ErrorMessage"] = "Invalid or expired Temporary ID.";
                            return RedirectToAction("SubmitAttendance");
                        }

                        // Check if the student belongs to the section
                        string sectionValidationQuery = @"
SELECT COUNT(1)
FROM Student
WHERE Id = @StudentId AND SectionId = @SectionId";

                        using (SqlCommand sectionValidationCommand = new SqlCommand(sectionValidationQuery, connection))
                        {
                            sectionValidationCommand.Parameters.AddWithValue("@StudentId", studentId);
                            sectionValidationCommand.Parameters.AddWithValue("@SectionId", sectionId);

                            int isValid = (int)sectionValidationCommand.ExecuteScalar();
                            if (isValid == 0)
                            {
                                TempData["ErrorMessage"] = "You are not part of the selected section.";
                                return RedirectToAction("SubmitAttendance");
                            }
                        }

                        // Update the Attendance table for the specific student and day
                        string dayColumn = $"Day{dayNumber}";
                        string updateQuery = $@"
UPDATE Attendance
SET {dayColumn} = 1
WHERE TemporaryId = @TemporaryId AND CourseId = @CourseId AND SectionId = @SectionId";

                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@TemporaryId", TemporaryId);
                            updateCommand.Parameters.AddWithValue("@CourseId", CourseId);
                            updateCommand.Parameters.AddWithValue("@SectionId", sectionId); // Use the sectionId from the student

                            int rowsAffected = updateCommand.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                TempData["SuccessMessage"] = "Attendance successfully submitted.";
                                TempData["Day"] = dayNumber; // Send the day number to the view
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Failed to submit attendance. Please try again.";
                            }
                        }
                    }
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

        [HttpGet]
        public IActionResult List()
        {
            List<StudentViewModel> students = new List<StudentViewModel>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                SELECT s.Id AS StudentId, s.Name AS StudentName, 
                       sec.Name AS SectionName, 
                       STRING_AGG(c.Name, ', ') AS Courses
                FROM Student s
                INNER JOIN Section sec ON s.SectionId = sec.Id
                LEFT JOIN StudentCourse sc ON s.Id = sc.StudentId
                LEFT JOIN Course c ON sc.CourseId = c.Id
                GROUP BY s.Id, s.Name, sec.Name
                ORDER BY s.Name";

                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        students.Add(new StudentViewModel
                        {
                            StudentId = (int)reader["StudentId"],
                            StudentName = reader["StudentName"].ToString(),
                            SectionName = reader["SectionName"].ToString(),
                            Courses = reader["Courses"] != DBNull.Value ? reader["Courses"].ToString() : "No Courses Assigned"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return View(students);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            Student student = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "SELECT Id, Name, Password, SectionId FROM Student WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", id);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        student = new Student
                        {
                            Id = (int)reader["Id"],
                            Name = reader["Name"].ToString(),
                            Password = reader["Password"].ToString(),
                            SectionId = (int)reader["SectionId"]
                        };
                    }
                }

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return RedirectToAction("List");
                }

                // Populate sections for dropdown
                ViewBag.Sections = GetSections();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("List");
            }

            return View(student);
        }

        [HttpPost]
        public IActionResult Edit(Student student)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "UPDATE Student SET Name = @Name, Password = @Password, SectionId = @SectionId WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Name", student.Name);
                    command.Parameters.AddWithValue("@Password", student.Password);
                    command.Parameters.AddWithValue("@SectionId", student.SectionId);
                    command.Parameters.AddWithValue("@Id", student.Id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Student updated successfully.";
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
                    string query = "DELETE FROM Student WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] = "Student deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("List");
        }

    }
}