using ChessPlatform.Data;
using kursProjChesh_Torgovcev.services;
using Microsoft.EntityFrameworkCore;
using kursProjChesh_Torgovcev.Hubs; 

var builder = WebApplication.CreateBuilder(args);

// Добавляем контекст базы данных
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем сервисы
builder.Services.AddScoped<ChessService>();

// ДОБАВЛЯЕМ SignalR
builder.Services.AddSignalR();

// Добавляем контроллеры
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();

// ДОБАВЛЯЕМ маршрут для Hub
app.MapHub<GameHub>("/gameHub");

app.UseStaticFiles();
app.MapControllers();

app.Run();