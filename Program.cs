using FamilyChat.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using FamilyChat.Data;
using FamilyChat.Hubs;
using FamilyChat.Services;
using FamilyChat.Middleware;

// === СЕРИЛОГ: структурированное логирование ===
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/chat-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("🚀 Запуск DatChaT...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Регистрируем сервисы
    builder.Services.AddDbContext<ChatDbContext>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IMessageService, MessageService>();
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = false;
        options.MaximumReceiveMessageSize = 32 * 1024;
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    });

    var app = builder.Build();

    // === MIDDLEWARE ===
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseStaticFiles();

    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    if (!Directory.Exists(uploadsPath))
        Directory.CreateDirectory(uploadsPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });

    // Главная страница
    app.MapGet("/", async context =>
    {
        var indexPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html");
        if (File.Exists(indexPath))
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(indexPath);
        }
        else
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("index.html not found");
        }
    });

    // Health check
    app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

    // SignalR хаб
    app.MapHub<ChatHub>("/chat");

    // Загрузка файлов
    app.MapPost("/upload", async (HttpRequest request) =>
    {
        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return Results.BadRequest("Нет файла");

        if (file.Length > 10 * 1024 * 1024)
            return Results.BadRequest("Файл слишком большой (макс. 10MB)");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileType = file.ContentType.StartsWith("image/") ? "image" : "file";
        Log.Information("📎 Загружен файл: {Name} ({Type}, {Size} байт)", file.FileName, fileType, file.Length);
        return Results.Json(new { fileName = file.FileName, fileUrl = $"/uploads/{fileName}", fileType });
    });

    Log.Information("✅ Сервер запущен на {Url}", builder.WebHost.GetSetting("urls") ?? "http://0.0.0.0:3060");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " Критическая ошибка при запуске");
}
finally
{
    Log.CloseAndFlush();
}
