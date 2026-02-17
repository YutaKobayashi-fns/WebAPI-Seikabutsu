using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

/*
WebAPI 基本構成のメモ
①builder の DIコンテナへサービス登録(Addxxx)
②登録したDI(サービス)を使って動作するミドルウェア(Usexxx群)を構成・実行
③app.Mapxxxでルーティングの定義（エンドポイントや処理の定義）をする。
*/

/* DI準備   */
var builder = WebApplication.CreateBuilder(args); // DIを含む、ビルダーオブジェクトの作成
var connectionString = builder.Configuration.GetConnectionString("Tasks") ?? "Data Source=Tasks.db"; // SQLiteに接続するための接続文字列を取得

builder.Services.AddEndpointsApiExplorer();  // エンドポイントに関するのDIコンテナ登録

builder.Services.AddSqlite<TasksDb>(connectionString);
builder.Services.AddSwaggerGen(c => // Swaggerのドキュメント生成関連DIコンテナ登録
{
   c.SwaggerDoc("v1", new OpenApiInfo
   {
      Title = "TaskManager API",
      Description = "Making the your tasks!",
      Version = "v1"
   });
});

/* ミドルウェア構築  */
var app = builder.Build(); // WebApplicationのインスタンス生成
if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI(c =>
   {
      c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManager API V1");
   });
}

/* ルーティング   */
// Root
app.MapGet("/", () => "Welcome to TaskManager! (^o^)b\n" + "Please access to [ http://localhost:(portNo)/swagger ]").WithTags("00-Root");

// Create(MapPost系)=================================================
// タスク追加機能
app.MapPost("/tasks", async (TasksDb db, InputTaskDto taskDto) =>
{
   if (string.IsNullOrWhiteSpace(taskDto.Name) || taskDto.Name.StartsWith(" ") || taskDto.Name.StartsWith("　"))
   {
      return Results.BadRequest("タスク名には、スペース以外で1文字以上を指定してください。");
   }
// TODO:要確認
   if (string.IsNullOrWhiteSpace(taskDto.Details) ||
      ((taskDto.Details.StartsWith(" ") || taskDto.Details.StartsWith("　")) && (taskDto.Details.Length > 1)))
   {
      return Results.BadRequest("タスク詳細には、先頭文字がスペース以外の文字を1文字以上入力してください。");
   }

   DateTime dt = DateTime.Now;

   Tasks task = new Tasks
   {
      Id = 0,
      Name = taskDto.Name,
      Details = taskDto.Details,
      CreateDate = dt.ToString("yyyy/MM/dd HH:mm:ss"),
      UpdateDate = "0000/00/00 00:00:00"
   };

   await db.Tasks.AddAsync(task);
   await db.SaveChangesAsync();

   return Results.Created($"/tasks/{task.Id}", task);
}
).WithTags("01-Create[MapPost]");
// Create(MapPost系)=================================================

// Read(MapGet系)==================================================
// タスク一覧表示機能
app.MapGet("/tasks", async (TasksDb db) =>
{
   await db.Tasks.ToListAsync();
}).WithTags("02-Read[MapGet]");


// タスク表示機能(ID指定)
app.MapGet("/tasks/{id}", async (TasksDb db, int id) =>
{
   Tasks? task = await db.Tasks.FindAsync(id);

   return task is not null ? Results.Ok(task) : Results.NotFound();
}).WithTags("02-Read[MapGet]");


// タスク表示機能(キーワード指定)
app.MapGet("/tasks/search", async (TasksDb db, string keyword) =>
{
   if (string.IsNullOrWhiteSpace(keyword))
   {
      return Results.BadRequest("検索キーワードを確認してください。");
   }

   var task = await db.Tasks.Where(c => c.Name.Contains(keyword)).ToListAsync();

   return Results.Ok(task);
}).WithTags("02-Read[MapGet]");
// Read(MapGet系)==================================================

// Upadte(MapPut系)==================================================
// タスク更新
app.MapPut("/tasks/{id}", async (TasksDb db, InputTaskDto taskDto, int id) =>
{
   Tasks? tasks = await db.Tasks.FindAsync(id);
   if (tasks is null)
   {
      return Results.NotFound();
   }

// TODO:要確認
   if ((string.IsNullOrWhiteSpace(taskDto.Name) || taskDto.Name.StartsWith(" ") || taskDto.Name.StartsWith("　")))
   {
      return Results.BadRequest("タスク名には、スペース以外で1文字以上を指定してください。");
   }

   Tasks updateTasks = tasks;
   DateTime dt = DateTime.Now;

   updateTasks.Name = taskDto.Name;

// TODO:要確認
   updateTasks.Details = taskDto.Details;
   if (string.IsNullOrWhiteSpace(updateTasks.Details) || updateTasks.Details.StartsWith(" ") || updateTasks.Details.StartsWith("　"))
   {
      updateTasks.Details = "Non input details...";
   }

// TODO:要確認
   if (string.IsNullOrWhiteSpace(updateTasks.CreateDate))
   {
      updateTasks.CreateDate = "0000/00/00 00:00:00";
   }

   updateTasks.UpdateDate = dt.ToString("yyyy/MM/dd HH:mm:ss");

   await db.SaveChangesAsync();

   return Results.NoContent();
}).WithTags("03-Update[MapPut]");
// Upadte(MapPut系)==================================================

// Delete(MapDelete系)===============================================
// タスク削除
app.MapDelete("/tasks/{id}", async (TasksDb db, int id) =>
{
   Tasks? task = await db.Tasks.FindAsync(id);
   if (task is null)
   {
      return Results.NotFound();
   }

   Tasks deleteTask = task;

   db.Tasks.Remove(deleteTask);
   await db.SaveChangesAsync();

   return Results.Ok();
}).WithTags("04-Delete[MapDelete]");
// Delete(MapDelete系)===============================================

app.Run();