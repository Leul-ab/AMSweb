using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace AMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        
        public IActionResult Index()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    ViewBag.Message = "Database connection successful!";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Database connection failed: {ex.Message}";
            }

            return View();
        }
    }
}
