using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Настройка логирования
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/adservice-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1),
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}");
});

// Добавляем необходимые сервисы
builder.Services.AddControllers();
builder.Services.AddSingleton<AdvertisingStorageTrie>();

var app = builder.Build();

// Логирование HTTP запросов
app.UseSerilogRequestLogging();
// Middleware для обработки исключений и логирования
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (exception != null)
        {
            // Логирование ошибки
            Log.Error(exception.Error, "Unhandled exception occurred");
            
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal Server Error");
        }
    });
});


app.UseRouting();
app.MapControllers();

app.Run();

