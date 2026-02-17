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
app.MapGet("/", () => "Welcome to TaskManager! (^o^)b\n" + "Please access to [ http://localhost:(portNo)/swagger ]");

// タスク一覧表示
app.MapGet("/tasks", async (TasksDb db) => await db.Tasks.ToListAsync());

// タスク表示(ID指定)
app.MapGet("/tasks{id}", async (TasksDb db, int id) =>
{
   await db.Tasks.ToListAsync();
   var task = await db.Tasks.FindAsync(id);
   return task is not null ? Results.Ok(task) : Results.NotFound();
});

// タスク追加
//TODO: MapPost "/tasks"

// タスク更新
//TODO: MapPut "/tasks{id}"

// タスク削除
//TODO: MapDelete "/tasks{id}"

app.Run();