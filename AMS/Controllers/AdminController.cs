using AMS.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace AMS.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _connectionString;

        private const string AdminName = "admin";
        private const string AdminPassword = "admin123";

        public AdminController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == AdminName && password == AdminPassword)
            {
                // Login successful
                TempData["SuccessMessage"] = "Admin logged in successfully!";
                return RedirectToAction("Dashboard");
            }
            else
            {
                // Login failed
                TempData["ErrorMessage"] = "Invalid admin username or password!";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TeachersList()
        {
            List<Teacher> teachers = new List<Teacher>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT t.Id, t.Name, t.Password, c.Name AS CourseName 
                                 FROM Teachers t 
                                 INNER JOIN Courses c ON t.CourseId = c.Id";

                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    teachers.Add(new Teacher
                    {
                        Id = (int)reader["Id"],
                        Name = reader["Name"].ToString(),
                        Password = reader["Password"].ToString(),
                        //CourseName = reader["CourseName"].ToString()
                    });
                }
            }

            return View(teachers);
        }
    }
}
