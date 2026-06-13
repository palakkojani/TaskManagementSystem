using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TaskManagementSystem.Models;

namespace TaskManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Users()
        {
            if (HttpContext.Session.GetString("Role")
      != "SuperAdmin")
            {
                return RedirectToAction(
                    "Index",
                    "Task");
            }

            List<User> users = new();

            string conStr =
                _configuration.GetConnectionString(
                    "DefaultConnection");

            using (SqlConnection con =
                new SqlConnection(conStr))
            {
                con.Open();

                SqlCommand cmd =
                    new SqlCommand(
                        "SELECT * FROM Users",
                        con);

                SqlDataReader dr =
                    cmd.ExecuteReader();

                while (dr.Read())
                {
                    users.Add(new User
                    {
                        Id = Convert.ToInt32(
                            dr["Id"]),

                        Name = dr["Name"]
                            .ToString() ?? "",

                        Email = dr["Email"]
                            .ToString() ?? "",

                        Password = dr["Password"]
                            .ToString() ?? "",

                        Role = dr["Role"]
                            .ToString() ?? ""
                    });
                }
            }

            return View(users);
        }
            public IActionResult DeleteUser(int id)
        {
            if (HttpContext.Session.GetString("Role")
    != "SuperAdmin")
            {
                return RedirectToAction(
                    "Index",
                    "Task");
            }

            string conStr =
                _configuration.GetConnectionString(
                    "DefaultConnection");

            using (SqlConnection con =
                new SqlConnection(conStr))
            {
                con.Open();

                // Prevent deleting Admin
                SqlCommand checkCmd =
                    new SqlCommand(
                        "SELECT Role FROM Users WHERE Id=@Id",
                        con);

                checkCmd.Parameters.AddWithValue(
                    "@Id",
                    id);

                string role =
                    checkCmd.ExecuteScalar()?.ToString();

                if (role == "Admin"
                || role == "SuperAdmin")
                {
                    return RedirectToAction(
                        "Users");
                }
                
                // Delete user's tasks first
                SqlCommand taskCmd =
                    new SqlCommand(
                        "DELETE FROM Tasks WHERE UserEmail=(SELECT Email FROM Users WHERE Id=@Id)",
                        con);

                taskCmd.Parameters.AddWithValue(
                    "@Id",
                    id);

                taskCmd.ExecuteNonQuery();

                // Delete user
                SqlCommand userCmd =
                    new SqlCommand(
                        "DELETE FROM Users WHERE Id=@Id",
                        con);

                userCmd.Parameters.AddWithValue(
                    "@Id",
                    id);

                userCmd.ExecuteNonQuery();
            }

            return RedirectToAction(
              "Users");
        }

        public IActionResult ChangeRole(
            int id,
            string role)
        {
            if (HttpContext.Session.GetString("Role")
                != "SuperAdmin")
            {
                return RedirectToAction(
                    "Index",
                    "Task");
            }

            string conStr =
                _configuration.GetConnectionString(
                    "DefaultConnection");

            using (SqlConnection con =
                new SqlConnection(conStr))
            {
                con.Open();

                SqlCommand checkCmd =
                    new SqlCommand(
                        "SELECT Role FROM Users WHERE Id=@Id",
                        con);

                checkCmd.Parameters.AddWithValue(
                    "@Id",
                    id);

                string currentRole =
                    checkCmd.ExecuteScalar()?.ToString();

                if (currentRole == "SuperAdmin")
                {
                    return RedirectToAction(
                        "Users");
                }

                SqlCommand cmd =
                    new SqlCommand(
                        "UPDATE Users SET Role=@Role WHERE Id=@Id",
                        con);

                cmd.Parameters.AddWithValue(
                    "@Role",
                    role);

                cmd.Parameters.AddWithValue(
                    "@Id",
                    id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction(
                "Users");
        }

    }
}