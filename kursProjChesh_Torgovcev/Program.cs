using ChessPlatform.Data;
using kursProjChesh_Torgovcev.services;
using Microsoft.EntityFrameworkCore;
using kursProjChesh_Torgovcev.Hubs; 

var builder = WebApplication.CreateBuilder(args);

// контекст базы данных
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// сервисы
builder.Services.AddScoped<ChessService>();

// SignalR
builder.Services.AddSignalR();

// контроллеры
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Отдаём index.html по адресу /
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

//  маршрут для Hub
app.MapHub<GameHub>("/gameHub");

app.MapControllers();

app.Run();