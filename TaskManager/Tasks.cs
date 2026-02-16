namespace TaskManager.Models
{
    public class Tasks
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? Update { get; set; }
    }
}