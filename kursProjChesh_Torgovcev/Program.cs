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

// ── Автоматическое применение миграций при старте ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChessDbContext>();
    var retries = 10;
    while (retries > 0)
    {
        try
        {
            db.Database.Migrate();
            Console.WriteLine("✓ Миграции применены успешно");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"БД недоступна, повтор через 3 сек... ({retries} попыток). {ex.Message}");
            Thread.Sleep(3000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();

// Отдаём index.html по адресу /
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

// маршрут для Hub
app.MapHub<GameHub>("/gameHub");

app.MapControllers();

app.Run();
