using Microsoft.EntityFrameworkCore;
namespace TaskManager.Models
{
    // タスク構成基本情報
    public class Tasks
    {
        public int Id { get; set; } // ID
        public string? Name { get; set; }   // タスク名称
        public string? Details { get; set; }    // タスク詳細
        public string? CreateDate { get; set; } // 作成年月日
        public string? UpdateDate { get; set; } // 更新年月日
    }

    // 入力情報(タスク構成情報)
    public class InputTaskDto
    {
        public string? Name { get; set; }   // タスク名称
        public string? Details { get; set; }    // タスク詳細
    }

    // 入力情報(作成年月日)
    public class InputCreateDate
    {
        public string? CreateDate { get; set; } // 作成年月日
    }

    // タスク管理DB
    class TasksDb : DbContext
    {
        public TasksDb(DbContextOptions options) : base(options) { }
        public DbSet<Tasks> Tasks { get; set; } = null!;    // タスク管理DBプロパティ
    }
}