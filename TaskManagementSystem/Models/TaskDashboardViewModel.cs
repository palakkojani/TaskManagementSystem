namespace TaskManagementSystem.Models
{
    public class TaskDashboardViewModel
    {
        public int TotalTasks { get; set; }

    public int PendingTasks { get; set; }

        public int InProgressTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int OverdueTasks { get; set; }

        public List<TaskItem> Tasks { get; set; } = new();
    }


}
