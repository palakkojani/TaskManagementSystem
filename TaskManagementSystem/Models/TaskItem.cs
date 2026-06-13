namespace TaskManagementSystem.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string Status { get; set; } = "";

        public string Priority { get; set; } = "";

        public string Category { get; set; } = "";

        public DateTime? DueDate { get; set; }
        public string UserEmail { get; set; } = "";
    }
}