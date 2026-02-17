using Microsoft.EntityFrameworkCore;
namespace TaskManager.Models
{
    // タスク構成基本情報
    public class Tasks
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Details { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }

    // タスク管理DB
    class TasksDb : DbContext
    {
        public TasksDb(DbContextOptions options) : base(options) { }
        public DbSet<Tasks> Tasks { get; set; } = null!;
    }
}