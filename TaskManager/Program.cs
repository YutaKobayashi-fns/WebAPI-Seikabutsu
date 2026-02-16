using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
     c.SwaggerDoc("v1", new OpenApiInfo {
         Title = "TaskManager API",
         Description = "Making the your tasks!",
         Version = "v1" });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI(c =>
   {
      c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManager API V1");
   });
}

app.MapGet("/", () => "Welcome to TaskManager! (^o^)b\n" + "Please access to [ http://localhost:(portNo)/swagger ]" );

app.Run();