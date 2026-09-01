using FamilyChat.Data;
using FamilyChat.Hubs;
using FamilyChat.Interfaces;
using FamilyChat.Middleware;
using FamilyChat.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/chat-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("Запуск DatChaT...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Database=familychat;Username=postgres;Password=postgres";

    builder.Services.AddDbContext<ChatDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddSingleton<PresenceTracker>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IMessageService, MessageService>();
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
        options.MaximumReceiveMessageSize = 64 * 1024;
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    });
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 12L * 1024 * 1024;
        options.ValueLengthLimit = 12 * 1024 * 1024;
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("""ALTER TABLE "PrivateMessages" ADD COLUMN IF NOT EXISTS "FileUrl" text;""");
        db.Database.ExecuteSqlRaw("""ALTER TABLE "PrivateMessages" ADD COLUMN IF NOT EXISTS "FileType" text;""");
        db.Users.ExecuteUpdate(s => s
            .SetProperty(u => u.IsOnline, false)
            .SetProperty(u => u.ConnectionId, (string?)null));
    }

    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var name = ctx.File.Name;
            if (name.EndsWith(".js") || name.EndsWith(".css") || name.EndsWith(".html"))
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store";
        }
    });

    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    Directory.CreateDirectory(uploadsPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });

    app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

    app.MapHub<ChatHub>("/chat");

    var allowedExtensions = builder.Configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>()
        ?? [".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".txt"];
    var maxSizeMb = builder.Configuration.GetValue("FileUpload:MaxSizeMB", 10);

    app.MapPost("/upload", async (HttpRequest request) =>
    {
        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return Results.BadRequest("Нет файла");

        if (file.Length > maxSizeMb * 1024L * 1024L)
            return Results.BadRequest($"Файл слишком большой (макс. {maxSizeMb}MB)");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isImage = (file.ContentType ?? "").StartsWith("image/");
        if (!isImage && (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext)))
            return Results.BadRequest("Этот тип файла не поддерживается");

        var safeName = Path.GetFileName(file.FileName);
        var fileName = $"{Guid.NewGuid()}_{safeName}";
        var filePath = Path.Combine(uploadsPath, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var fileType = (file.ContentType ?? "").StartsWith("image/") ? "image" : "file";
        Log.Information("Загружен файл: {Name} ({Type}, {Size} байт)", file.FileName, fileType, file.Length);
        return Results.Json(new { fileName = safeName, fileUrl = $"/uploads/{fileName}", fileType });
    }).DisableAntiforgery();

    _ = Task.Run(async () =>
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromDays(1));
                var deleted = 0;
                foreach (var file in Directory.GetFiles(uploadsPath))
                {
                    if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-30))
                    {
                        File.Delete(file);
                        deleted++;
                    }
                }
                if (deleted > 0)
                    Log.Information("Удалено старых файлов: {Count}", deleted);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка в фоновой очистке");
            }
        }
    });

    Log.Information("Запуск хоста...");
    app.Lifetime.ApplicationStarted.Register(() =>
        Log.Information("Сервер готов: {Urls}", string.Join(", ", app.Urls)));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Критическая ошибка при запуске");
}
finally
{
    Log.CloseAndFlush();
}
