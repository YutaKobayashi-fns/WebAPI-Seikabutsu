using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

/*
WebAPI 基本構成のメモ
①builder の DIコンテナへサービス登録(Addxxx)
②登録したDI(サービス)を使って動作するミドルウェア(Usexxx群)を構成・実行
③app.Mapxxxでルーティングを定義（エンドポイントや処理を定義）する。
*/

/* DI準備   */
var builder = WebApplication.CreateBuilder(args); // DIを含む、ビルダーオブジェクトの作成
var connectionString = builder.Configuration.GetConnectionString("Tasks") ?? "Data Source=Tasks.db"; // SQLiteに接続するための接続文字列を取得

builder.Services.AddEndpointsApiExplorer();  // エンドポイントに関するのDIコンテナ登録

builder.Services.AddSqlite<TasksDb>(connectionString);
builder.Services.AddSwaggerGen(c => // Swaggerのドキュメント生成関連DIコンテナ登録
{
   // Swagger表示情報設定
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
   // ミドルウェア(Swagger)設定
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
app.MapPost("/tasks/Post", async (TasksDb db, InputTaskDto taskDto) =>
{
   // 有効値が入力されたか？
   if (string.IsNullOrWhiteSpace(taskDto.Name) || taskDto.Name.StartsWith(" ") || taskDto.Name.StartsWith("　"))
   {
      // 処理結果(異常)
      return Results.BadRequest("先頭文字がスペース以外の文字列を1文字以上入力してください。また、スペースのみの入力も無効です。");
   }

   DateTime dt = DateTime.Now;   // 現在時刻取得

   // タスク構成基本情報設定
   Tasks task = new Tasks
   {
      Id = 0,
      Name = taskDto.Name,
      Details = taskDto.Details,
      CreateDate = dt.ToString("yyyy/MM/dd HH:mm:ss"),
      UpdateDate = "0000/00/00 00:00:00"
   };

   // タスク詳細の設定なしか？
   if (string.IsNullOrWhiteSpace(task.Details))
   {
      task.Details = "Non input details..."; // 固定値(未入力)設定
   }

   await db.Tasks.AddAsync(task);   // コンテキスト追加
   await db.SaveChangesAsync();  // 変更をDBに反映

   return Results.Created($"/tasks/{task.Id}", task); // 処理結果(正常)
}
).WithTags("01-Create[MapPost]");
// Create(MapPost系)=================================================

// Read(MapGet系)==================================================
// タスク一覧表示機能
app.MapGet("/tasks/Get", async (TasksDb db) =>
{
   return await db.Tasks.ToListAsync();   // 処理結果(正常)

}).WithTags("02-Read[MapGet]");


// タスク表示機能(ID指定)
app.MapGet("/tasks/Get/{id}", async (TasksDb db, int id) =>
{
   Tasks? task = await db.Tasks.FindAsync(id);  // DBのレコードを取得(ID指定)

   // 処理結果(正常/異常)
   return task is not null ? Results.Ok(task) : Results.NotFound("指定されたIDのタスク情報が見つかりませんでした。");
}).WithTags("02-Read[MapGet]");

// タスク表示機能(キーワード指定)
app.MapGet("/tasks/Get/search/Keyword", async (TasksDb db, string keyword) =>
{
   // 検索キーワードは有効値か？
   if (string.IsNullOrWhiteSpace(keyword))
   {
      // 処理結果(異常)
      return Results.BadRequest("検索キーワードを確認してください。");
   }

   List<Tasks> dispTaskList = new List<Tasks>();   // 表示対象タスク情報格納リスト
   List<Tasks>? getTaskLst;   // タスク情報取得リスト

   getTaskLst = await db.Tasks.ToListAsync();   // DBの保有タスク情報を取得

   // DBの保有タスク情報分ループ
   foreach (Tasks task in getTaskLst)
   {
      // 当該タスク情報は有効値か？
      if ((string.IsNullOrWhiteSpace(task.Name) == false) && (string.IsNullOrWhiteSpace(task.Details) == false))
      {
         // 当該タスク情報の名称、または詳細にキーワードが含まれているか？
         if (task.Name.Contains(keyword) || task.Details.Contains(keyword))
         {
            dispTaskList.Add(task); // リストに表示対象タスク情報を追加
         }
      }
   }

   // 表示対象タスク情報なしか？
   if (dispTaskList.Count == 0)
   {
      // 処理結果(異常)
      return Results.NotFound("指定されたキーワードを含むタスクが見つかりませんでした。");
   }

   return Results.Ok(dispTaskList); // 処理結果(正常)
}).WithTags("02-Read[MapGet]");


// タスク表示機能(年月日指定)
app.MapGet("/tasks/Get/search/date", async (TasksDb db, string date) =>
{
   string dateFormat = "yyyy/MM/dd";   // 年月日フォーマット

   // 有効値が入力されたか？ 
   if ((string.IsNullOrWhiteSpace(date) == true) ||
      (DateTime.TryParseExact(date, dateFormat, System.Globalization.CultureInfo.InvariantCulture,
                                                System.Globalization.DateTimeStyles.None,
                                                out DateTime parsedDate) == false))
   {
      // 処理結果(異常)
      return Results.BadRequest($"入力した年月日を確認してください。入力フォーマットは、[{dateFormat}]です。");
   }

   List<Tasks> dispTaskList = new List<Tasks>();   // 表示対象タスク情報格納リスト
   List<Tasks>? getTaskLst;   // タスク情報取得リスト

   getTaskLst = await db.Tasks.ToListAsync();   // DBの保有タスク情報を取得

   // DBの保有タスク情報分ループ
   foreach (Tasks task in getTaskLst)
   {
      // 当該タスク情報は有効値か？
      if ((string.IsNullOrWhiteSpace(task.CreateDate) == false) && (string.IsNullOrWhiteSpace(task.UpdateDate) == false))
      {
         // 当該タスク情報の作成年月日、または更新年月日に入力年月日が含まれているか？
         if (task.CreateDate.Contains(date) || task.UpdateDate.Contains(date))
         {
            dispTaskList.Add(task); // リストに表示対象タスク情報を追加
         }
      }
   }

   // 表示対象タスク情報なしか？
   if (dispTaskList.Count == 0)
   {
      // 処理結果(異常)
      return Results.NotFound("指定された年月日を含むタスクが見つかりませんでした。");
   }

   return Results.Ok(dispTaskList); // 処理結果(正常)
}).WithTags("02-Read[MapGet]");
// Read(MapGet系)==================================================

// Upadte(MapPut系)==================================================
// タスク更新機能(ID指定)
app.MapPut("/tasks/Put/{id}", async (TasksDb db, InputTaskDto taskDto, int id) =>
{
   Tasks? tasks = await db.Tasks.FindAsync(id); // DBのレコードを取得(ID指定)

   // タスク情報はnullか？
   if (tasks is null)
   {
      // 処理結果(異常)
      return Results.NotFound("指定されたIDのタスク情報が見つかりませんでした。");
   }

   // 有効値が入力されたか？
   if (string.IsNullOrWhiteSpace(taskDto.Name) || taskDto.Name.StartsWith(" ") || taskDto.Name.StartsWith("　"))
   {
      // 処理結果(異常)
      return Results.BadRequest("先頭文字がスペース以外の文字列を1文字以上入力してください。また、スペースのみの入力も無効です。");
   }

   Tasks updateTasks = tasks; // 更新対象のタスク情報コピー
   updateTasks.Name = taskDto.Name; // タスク名称格納

   bool nonInput = false;  // 未入力判定

   // タスク詳細の設定なしか？
   if (string.IsNullOrWhiteSpace(taskDto.Details))
   {
      nonInput = true;  // タスク詳細の未入力検知
   }

   // タスク詳細は入力されていたか？
   if (nonInput == false)
   {
      updateTasks.Details = taskDto.Details; // タスク詳細に入力値を格納
   }
   else
   {
      updateTasks.Details = "Non input details...";   // タスク詳細に固定値(未入力)格納
   }

   DateTime dt = DateTime.Now;   // 現在時刻取得

   // 更新対象のタスク情報作成日時は未設定か？
   if (string.IsNullOrWhiteSpace(updateTasks.CreateDate))
   {
      updateTasks.CreateDate = "0000/00/00 00:00:00"; // 作成日時(初期値)格納
   }

   updateTasks.UpdateDate = dt.ToString("yyyy/MM/dd HH:mm:ss");   // 更新日時格納

   await db.SaveChangesAsync();  // 変更をDBに反映

   return Results.NoContent();   // 処理結果(正常)
}).WithTags("03-Update[MapPut]");


// タスク作成年月日更新機能(ID指定)
app.MapPut("/tasks/Put/{id}/date", async (TasksDb db, InputCreateDate date, int id) =>
{
   Tasks? tasks = await db.Tasks.FindAsync(id); // DBのレコードを取得(ID指定)

   // タスク情報はnullか？
   if (tasks is null)
   {
      // 処理結果(異常)
      return Results.NotFound("指定されたIDのタスク情報が見つかりませんでした。");
   }

   string dateFormat = "yyyy/MM/dd HH:mm:ss";   // 作成年月日フォーマット

   // 更新対象の作成年月日は初期値以外か？ 
   if ((tasks.CreateDate != "0000/00/00 00:00:00") &&
      (DateTime.TryParseExact(tasks.CreateDate, dateFormat, System.Globalization.CultureInfo.InvariantCulture,
                                                System.Globalization.DateTimeStyles.None,
                                                out DateTime oldDate) == true))
   {
      // 処理結果(異常)
      return Results.BadRequest($"指定されたタスク(ID[{id}])は、作成年月日の変更対象外です。作成年月日が初期値(0000/00/00 00:00:00) または 空欄のタスクのみ変更が可能です。");
   }

   // 有効値が入力されたか？ 
   if ((string.IsNullOrWhiteSpace(date.CreateDate) == true) ||
      (DateTime.TryParseExact(date.CreateDate, dateFormat, System.Globalization.CultureInfo.InvariantCulture,
                                                System.Globalization.DateTimeStyles.None,
                                                out DateTime newDate) == false))
   {
      // 処理結果(異常)
      return Results.BadRequest($"入力した年月日を確認してください。入力フォーマットは、[{dateFormat}]です。");
   }

   tasks.CreateDate = date.CreateDate; // 作成年月日に入力値格納
   DateTime dt = DateTime.Now;   // 現在時刻取得
   tasks.UpdateDate = dt.ToString("yyyy/MM/dd HH:mm:ss");   // 更新年月日格納

   await db.SaveChangesAsync();  // 変更をDBに反映

   return Results.NoContent();   // 処理結果(正常)
}).WithTags("03-Update[MapPut]");
// Upadte(MapPut系)==================================================

// Delete(MapDelete系)===============================================
// タスク削除機能(ID指定)
app.MapDelete("/tasks/Delete/{id}", async (TasksDb db, int id) =>
{
   Tasks? task = await db.Tasks.FindAsync(id);  // DBのレコードを取得(ID指定)

   // タスク情報はnullか？
   if (task is null)
   {
      return Results.NotFound("指定されたIDのタスク情報が見つかりませんでした。"); // 処理結果(異常)
   }

   Tasks deleteTask = task;   // 削除対象のタスク情報コピー

   db.Tasks.Remove(deleteTask);  // 削除対象をコンテキストに記録
   await db.SaveChangesAsync();  // 変更をDBに反映

   return Results.Ok(); // 処理結果(正常)
}).WithTags("04-Delete[MapDelete]");


// タスク削除機能(年月日指定)
app.MapDelete("/tasks/Delete/date)", async (TasksDb db, string date) =>
{
   string dateFormat = "yyyy/MM/dd";   // 年月日フォーマット

   // 有効値が入力されたか？
   if ((string.IsNullOrWhiteSpace(date) == true) ||
      ((date != "0000/00/00") &&
      (DateTime.TryParseExact(date, dateFormat, System.Globalization.CultureInfo.InvariantCulture,
                                                System.Globalization.DateTimeStyles.None,
                                                out DateTime parsedDate) == false)))
   {
      return Results.BadRequest($"入力した年月日を確認してください。入力フォーマットは、[{dateFormat}]です。");
   }

   List<Tasks>? getTaskLst;   // タスク情報格納リスト

   getTaskLst = await db.Tasks.ToListAsync();   // DBの保有タスク情報を取得
   int delCount = 0; // 削除タスク情報数

   // DBの保有タスク情報分ループ
   foreach (Tasks delTask in getTaskLst)
   {
      // 当該タスク情報は有効値か？
      if (string.IsNullOrWhiteSpace(delTask.CreateDate) == false)
      {
         // 当該タスク情報の作成年月日と入力値の年月日は一致するか？
         if (delTask.CreateDate.Contains(date))
         {
            db.Tasks.Remove(delTask);  // 削除対象をコンテキストに記録
            delCount++; // 削除タスク情報数更新
         }
      }
   }

   // 削除対象のタスク情報なしか？
   if (delCount == 0)
   {
      // 処理結果(異常)
      return Results.NotFound("指定された年月日を含むタスクが見つかりませんでした。");
   }

   await db.SaveChangesAsync();  // 変更をDBに反映

   return Results.Ok(); // 処理結果(正常)
}).WithTags("04-Delete[MapDelete]");
// Delete(MapDelete系)===============================================

app.Run();