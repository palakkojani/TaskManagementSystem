using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TaskManagementSystem.Models;

namespace TaskManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ======================
        // REGISTER PAGE
        // ======================

        public IActionResult Register()
        {
            return View();
        }

        // ======================
        // REGISTER SAVE
        // ======================

        [HttpPost]
        public IActionResult Register(User user)
        {
            string conStr =
                _configuration.GetConnectionString(
                    "DefaultConnection");

            using (SqlConnection con =
                new SqlConnection(conStr))
            {
                con.Open();

                SqlCommand cmd =
                    new SqlCommand(
                        @"INSERT INTO Users
                        (Name, Email, Password,Role)
                        VALUES
                        (@Name, @Email, @Password, @Role)",
                        con);

                cmd.Parameters.AddWithValue(
                    "@Name",
                    user.Name);

                cmd.Parameters.AddWithValue(
                    "@Email",
                    user.Email);

                cmd.Parameters.AddWithValue(
                    "@Password",
                    user.Password);

                cmd.Parameters.AddWithValue(
                     "@Role",
                     "User");

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Login");
        }

        // ======================
        // LOGIN PAGE
        // ======================

        public IActionResult Login()
        {
            return View();
        }

        // ======================
        // LOGIN CHECK
        // ======================

        [HttpPost]
        public IActionResult Login(
            string email,
            string password)
        {
            string conStr =
                _configuration.GetConnectionString(
                    "DefaultConnection");

            using (SqlConnection con =
                new SqlConnection(conStr))
            {
                con.Open();

                SqlCommand cmd =
                    new SqlCommand(
                        @"SELECT *
                          FROM Users
                          WHERE Email=@Email
                          AND Password=@Password",
                        con);

                cmd.Parameters.AddWithValue(
                    "@Email",
                    email);

                cmd.Parameters.AddWithValue(
                    "@Password",
                    password);

                SqlDataReader dr =
                    cmd.ExecuteReader();

                if (dr.Read())
                {
                    HttpContext.Session.SetString(
                        "UserName",
                        dr["Name"].ToString());

                    HttpContext.Session.SetString(
                    "UserEmail",
                    dr["Email"].ToString());

                    HttpContext.Session.SetString(
                    "Role",
                    dr["Role"].ToString());

                    return RedirectToAction(
                        "Index",
                        "Task");
                }
            }

            ViewBag.Error =
                "Invalid Email or Password";

            return View();
        }

        // ======================
        // LOGOUT
        // ======================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login");
        }
    }
}