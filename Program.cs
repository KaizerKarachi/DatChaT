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
    Log.Information("🚀 Запуск DatChaT v{Version}...", typeof(Program).Assembly.GetName().Version);

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Конфигурация
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    
    // База данных с connection pooling
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Database=familychat;Username=postgres;Password=postgres";
    
    builder.Services.AddDbContext<ChatDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            npgsqlOptions.CommandTimeout(30);
        }));

    // Сервисы
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IMessageService, MessageService>();
    
    // SignalR с настройками из конфига
    var signalrConfig = builder.Configuration.GetSection("SignalR");
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = false;
        options.MaximumReceiveMessageSize = signalrConfig.GetValue<int>("MaxMessageSizeKB", 32) * 1024;
        options.KeepAliveInterval = TimeSpan.FromSeconds(signalrConfig.GetValue<int>("KeepAliveIntervalSeconds", 15));
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalrConfig.GetValue<int>("ClientTimeoutSeconds", 30));
    });

    // Rate limiting
    if (builder.Configuration.GetValue<bool>("RateLimiting:Enabled", true))
    {
        builder.Services.AddMemoryCache();
        builder.Services.Configure<IpRateLimitOptions>(options =>
        {
            options.GeneralRules = new List<RateLimitRule>
            {
                new() { Endpoint = "*", Period = $"{builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60)}s", PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 10) }
            };
        });
        builder.Services.AddSingleton<IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();
        builder.Services.AddInMemoryRateLimiting();
    }

    // CORS для SignalR
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    // === MIDDLEWARE ===
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseCors("AllowAll");
    
    if (builder.Configuration.GetValue<bool>("RateLimiting:Enabled", true))
    {
        app.UseIpRateLimiting();
    }
    
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
    app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow, version = typeof(Program).Assembly.GetName().Version?.ToString() }));

    // API info
    app.MapGet("/api/info", () => Results.Json(new 
    { 
        name = "DatChaT", 
        version = typeof(Program).Assembly.GetName().Version?.ToString(),
        features = builder.Configuration.GetSection("Features").GetChildren().ToDictionary(k => k.Key, v => v.Value)
    }));

    // SignalR хаб
    app.MapHub<ChatHub>("/chat");

    // Загрузка файлов с валидацией
    var fileConfig = builder.Configuration.GetSection("FileUpload");
    var maxSizeBytes = fileConfig.GetValue<int>("MaxSizeMB", 10) * 1024 * 1024;
    
    app.MapPost("/upload", async (HttpRequest request) =>
    {
        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return Results.BadRequest("Нет файла");

        if (file.Length > maxSizeBytes)
            return Results.BadRequest($"Файл слишком большой (макс. {fileConfig.GetValue<int>("MaxSizeMB", 10)}MB)");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName.Replace(" ", "_")}";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileType = file.ContentType.StartsWith("image/") ? "image" : "file";
        Log.Information("📎 Загружен файл: {Name} ({Type}, {Size} байт)", file.FileName, fileType, file.Length);
        return Results.Json(new { fileName = file.FileName, fileUrl = $"/uploads/{fileName}", fileType });
    })
    .DisableAntiforgery();

    Log.Information("✅ Сервер запущен на {Url}", builder.WebHost.GetSetting("urls") ?? "http://0.0.0.0:3060");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Критическая ошибка при запуске");
}
finally
{
    Log.CloseAndFlush();
}
