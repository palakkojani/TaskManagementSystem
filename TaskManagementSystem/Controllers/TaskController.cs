using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TaskManagementSystem.Models;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace TaskManagementSystem.Controllers
{
    public class TaskController : Controller
    {
        static TaskController()
        {
            QuestPDF.Settings.License =
                LicenseType.Community;
        }

        private readonly IConfiguration _configuration;
    public TaskController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =====================
        // DASHBOARD + SEARCH + TASK LIST
        // =====================
        public IActionResult Index(

    string searchText,
    string statusFilter,
    string categoryFilter,
    string sortOrder)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }   
            TaskDashboardViewModel dashboard =
                new TaskDashboardViewModel();

            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                con.Open();

                string role =
                HttpContext.Session.GetString("Role");

                string query;

                if (role == "Admin" || role == "SuperAdmin")
                {
                    query = "SELECT * FROM Tasks WHERE 1=1";
                }
                else
                {
                    query =
                        "SELECT * FROM Tasks WHERE UserEmail=@UserEmail";
                }


                if (!string.IsNullOrEmpty(searchText))
                {
                    query +=
                        " AND (Title LIKE @Search OR Description LIKE @Search)";
                }

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    query +=
                        " AND Status = @Status";
                }
                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    query +=
                        " AND Category = @Category";
                }
                switch (sortOrder)
                {
                    case "newest":
                        query += " ORDER BY Id DESC";
                        break;

                    case "oldest":
                        query += " ORDER BY Id ASC";
                        break;

                    case "dueAsc":
                        query += " ORDER BY DueDate ASC";
                        break;

                    case "dueDesc":
                        query += " ORDER BY DueDate DESC";
                        break;

                    case "highPriority":
                        query +=
                            " ORDER BY CASE " +
                            "WHEN Priority='High' THEN 1 " +
                            "WHEN Priority='Medium' THEN 2 " +
                            "ELSE 3 END";
                        break;

                    default:
                        query += " ORDER BY Id DESC";
                        break;
                }
                SqlCommand cmd =
                    new SqlCommand(query, con);

                if (role != "Admin" && role != "SuperAdmin")
                {
                    cmd.Parameters.AddWithValue(
                        "@UserEmail",
                        HttpContext.Session.GetString(
                            "UserEmail"));
                }
              
                if (!string.IsNullOrEmpty(searchText))
                {
                    cmd.Parameters.AddWithValue(
                        "@Search",
                        "%" + searchText + "%");
                }
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    cmd.Parameters.AddWithValue(
                        "@Status",
                        statusFilter);
                }
                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    cmd.Parameters.AddWithValue(
                        "@Category",
                        categoryFilter);
                }

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    TaskItem task = new TaskItem
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        Title = dr["Title"].ToString() ?? "",
                        Description = dr["Description"].ToString() ?? "",
                        Status = dr["Status"].ToString() ?? "",
                        Priority = dr["Priority"].ToString() ?? "",
                        Category = dr["Category"].ToString() ?? "",
                        DueDate = dr["DueDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(dr["DueDate"])
                    };

                    dashboard.Tasks.Add(task);

                    dashboard.TotalTasks++;

                    if (task.Status == "Pending")
                        dashboard.PendingTasks++;

                    else if (task.Status == "In Progress")
                        dashboard.InProgressTasks++;

                    else if (task.Status == "Completed")
                        dashboard.CompletedTasks++;

                    if (task.Status != "Completed"
                    && task.DueDate.HasValue
                    && task.DueDate.Value.Date < DateTime.Today)
                    {
                        dashboard.OverdueTasks++;
                    }

                }
            }
            ViewBag.SearchText = searchText;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CategoryFilter = categoryFilter;
            ViewBag.SortOrder = sortOrder;
            return View(dashboard);
        }

        // =====================
        // TASK DETAILS
        // =====================
        public IActionResult Details(int id)
        {
            TaskItem task = new TaskItem();

            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                con.Open();

                string role =
    HttpContext.Session.GetString("Role");

                string query;

                if (role == "Admin" || role == "SuperAdmin")
                {
                    query =
                        "SELECT * FROM Tasks WHERE Id=@Id";
                }
                else
                {
                    query =
                        @"SELECT * FROM Tasks
          WHERE Id=@Id
          AND UserEmail=@UserEmail";
                }

                SqlCommand cmd =
                    new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Id", id);

                if (role != "Admin" && role != "SuperAdmin")
                {
                    cmd.Parameters.AddWithValue(
                        "@UserEmail",
                        HttpContext.Session.GetString(
                            "UserEmail"));
                }
                

            

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    task.Id = Convert.ToInt32(dr["Id"]);
                    task.Title = dr["Title"].ToString() ?? "";
                    task.Description = dr["Description"].ToString() ?? "";
                    task.Status = dr["Status"].ToString() ?? "";
                    task.Priority = dr["Priority"].ToString() ?? "";
                    task.Category = dr["Category"].ToString() ?? "";

                    task.DueDate =
                        dr["DueDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["DueDate"]);
                }
            }

            return View(task);
        }

        // =====================
        // CREATE PAGE
        // =====================
        public IActionResult Create()
        {
            return View();
        }

        // =====================
        // SAVE TASK
        // =====================
        [HttpPost]
        public IActionResult Create(TaskItem task)
        {
            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Tasks
                (Title, Description, Status, Priority, Category, DueDate,UserEmail)
                VALUES
                (@Title, @Description, @Status, @Priority, @Category, @DueDate, @UserEmail)",
                    con);
     

                cmd.Parameters.AddWithValue("@Title", task.Title);
                cmd.Parameters.AddWithValue("@Description", task.Description);
                cmd.Parameters.AddWithValue("@Status", task.Status);
                cmd.Parameters.AddWithValue("@Priority", task.Priority);
                cmd.Parameters.AddWithValue(
                        "@Category",
                             task.Category);

                cmd.Parameters.AddWithValue(
                    "@DueDate",
                    task.DueDate ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                     "@UserEmail",
                 task.UserEmail);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        // =====================
        // EDIT PAGE
        // =====================
        public IActionResult Edit(int id)
        {
            TaskItem task = new TaskItem();

            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                con.Open();

                string role =
                 HttpContext.Session.GetString("Role");

                string query;

                if (role == "Admin" || role == "SuperAdmin")
                {
                    query =
                        "SELECT * FROM Tasks WHERE Id=@Id";
                }
                else
                {
                    query =
                        @"SELECT * FROM Tasks
                         WHERE Id=@Id
                         AND UserEmail=@UserEmail";
                }

                SqlCommand cmd =
                    new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Id", id);

                if (role != "Admin" && role != "SuperAdmin")
                {
                    cmd.Parameters.AddWithValue(
                        "@UserEmail",
                        HttpContext.Session.GetString(
                            "UserEmail"));
                }
               
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    task.Id = Convert.ToInt32(dr["Id"]);
                    task.Title = dr["Title"].ToString() ?? "";
                    task.Description = dr["Description"].ToString() ?? "";
                    task.Status = dr["Status"].ToString() ?? "";
                    task.Priority = dr["Priority"].ToString() ?? "";
                    task.Category = dr["Category"].ToString() ?? "";

                    task.DueDate =
                        dr["DueDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dr["DueDate"]);
                }
            }

            return View(task);
        }

        // =====================
        // UPDATE TASK
        // =====================
        [HttpPost]
        public IActionResult Edit(TaskItem task)
        {
            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                con.Open();

                string role =
    HttpContext.Session.GetString("Role");

                string query;

                if (role == "Admin" || role == "SuperAdmin")
                {
                    query = @"UPDATE Tasks
              SET Title=@Title,
                  Description=@Description,
                  Status=@Status,
                  Priority=@Priority,
                  Category=@Category,
                  DueDate=@DueDate
              WHERE Id=@Id";
                }
                else
                {
                    query = @"UPDATE Tasks
              SET Title=@Title,
                  Description=@Description,
                  Status=@Status,
                  Priority=@Priority,
                  Category=@Category,
                  DueDate=@DueDate
              WHERE Id=@Id
              AND UserEmail=@UserEmail";
                }

                SqlCommand cmd =
                    new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Id", task.Id);
                if (role != "Admin" && role != "SuperAdmin")
                {
                    cmd.Parameters.AddWithValue(
                        "@UserEmail",
                        HttpContext.Session.GetString(
                            "UserEmail"));
                }
                
                cmd.Parameters.AddWithValue("@Title", task.Title);
                cmd.Parameters.AddWithValue("@Description", task.Description);
                cmd.Parameters.AddWithValue("@Status", task.Status);
                cmd.Parameters.AddWithValue("@Priority", task.Priority);
                cmd.Parameters.AddWithValue("@Category", task.Category);

                cmd.Parameters.AddWithValue(
                    "@DueDate",
                    task.DueDate ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
        // =====================
        // DELETE TASK
        // =====================
        public IActionResult Delete(int id)
        {
            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                con.Open();

                string role =
    HttpContext.Session.GetString("Role");

                string query;

                if (role == "Admin" || role == "SuperAdmin")
                {
                    query =
                        "DELETE FROM Tasks WHERE Id=@Id";
                }
                else
                {
                    query =
                        @"DELETE FROM Tasks
          WHERE Id=@Id
          AND UserEmail=@UserEmail";
                }

                SqlCommand cmd =
                    new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Id", id);
                if (role != "Admin" && role != "SuperAdmin")
                {
                    cmd.Parameters.AddWithValue(
                        "@UserEmail",
                        HttpContext.Session.GetString(
                            "UserEmail"));
                }
               
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
            // =====================
            // EXCEL FILE
            // =====================
            public IActionResult ExportToExcel()
        {
            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Tasks");

            worksheet.Cell(1, 1).Value = "Id";
            worksheet.Cell(1, 2).Value = "Title";
            worksheet.Cell(1, 3).Value = "Description";
            worksheet.Cell(1, 4).Value = "Status";
            worksheet.Cell(1, 5).Value = "Priority";
            worksheet.Cell(1, 6).Value = "Due Date";

            int row = 2;

            using (SqlConnection con = new SqlConnection(conStr))
            {
                con.Open();

                string role =
      HttpContext.Session.GetString("Role");

                string query;
                if (role == "Admin" || role == "SuperAdmin")
                {
                    query = "SELECT * FROM Tasks";
                }
                else
                {
                    query =
                        "SELECT * FROM Tasks WHERE UserEmail=@UserEmail";
                }

                SqlCommand cmd =
                    new SqlCommand(query, con);

                if (role != "Admin" && role != "SuperAdmin")
                {
                    cmd.Parameters.AddWithValue(
                        "@UserEmail",
                        HttpContext.Session.GetString(
                            "UserEmail"));
                }
               
                SqlDataReader dr =
                    cmd.ExecuteReader();

                while (dr.Read())
                {
                    worksheet.Cell(row, 1).Value =
                        Convert.ToInt32(dr["Id"]);

                    worksheet.Cell(row, 2).Value =
                        dr["Title"]?.ToString();

                    worksheet.Cell(row, 3).Value =
                        dr["Description"]?.ToString();

                    worksheet.Cell(row, 4).Value =
                        dr["Status"]?.ToString();

                    worksheet.Cell(row, 5).Value =
                        dr["Priority"]?.ToString();

                    worksheet.Cell(row, 6).Value =
                        dr["DueDate"] == DBNull.Value
                        ? ""
                        : Convert.ToDateTime(dr["DueDate"])
                            .ToString("dd-MMM-yyyy");

                    row++;
                }
            }

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            var content =
                stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Tasks.xlsx");
        }

        public IActionResult ExportToPdf()
        {
            string conStr =
                _configuration.GetConnectionString("DefaultConnection");

            List<TaskItem> tasks = new();

            using (SqlConnection con =
                new SqlConnection(conStr))
            {
                con.Open();
                string role =
                    HttpContext.Session.GetString("Role");

                string query;

                if (role == "Admin" || role == "SuperAdmin")
                {
                    query = "SELECT * FROM Tasks";
                }
                else
                {
                    query =
                        "SELECT * FROM Tasks WHERE UserEmail=@UserEmail";
                }

                SqlCommand cmd =
                    new SqlCommand(query, con);

                if (role != "Admin" && role != "SuperAdmin")
                {
                    cmd.Parameters.AddWithValue(
                        "@UserEmail",
                        HttpContext.Session.GetString(
                            "UserEmail"));
                }
               
                SqlDataReader dr =
                    cmd.ExecuteReader();

                while (dr.Read())
                {
                    tasks.Add(new TaskItem
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        Title = dr["Title"].ToString() ?? "",
                        Description = dr["Description"].ToString() ?? "",
                        Status = dr["Status"].ToString() ?? "",
                        Priority = dr["Priority"].ToString() ?? "",
                        Category = dr["Category"].ToString() ?? "",
                        DueDate = dr["DueDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(dr["DueDate"])
                    });
                }
            }

            byte[] pdf =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);

                        page.Header()
                            .Text("Task Report")
                            .FontSize(20);

                        page.Content()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Title");
                                    header.Cell().Text("Status");
                                    header.Cell().Text("Priority");
                                    header.Cell().Text("Category");
                                });

                                foreach (var task in tasks)
                                {
                                    table.Cell().Text(task.Title);
                                    table.Cell().Text(task.Status);
                                    table.Cell().Text(task.Priority);
                                    table.Cell().Text(task.Category);
                                }
                            });
                    });
                }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                "Tasks.pdf");
        }

    }
    }

