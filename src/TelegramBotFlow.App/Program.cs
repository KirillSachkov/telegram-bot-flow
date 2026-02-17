using System.Reflection;
using Serilog;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Hosting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = BotApplication.CreateBuilder(args);

    builder.WebAppBuilder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddBotEndpoints(Assembly.GetExecutingAssembly());

    // Rate limiting (защита от флуда)
    builder.Services.AddThrottling(builder.WebAppBuilder.Configuration);

    // Redis session store (раскомментировать для продакшена):
    // builder.Services.AddRedisSessionStore(builder.WebAppBuilder.Configuration);

    var app = BotApplication.Build(builder);

    app.UseErrorHandling();
    app.UseLogging();
    app.UseThrottling();  // Rate limiting — защита от флуда
    app.UseSession();
    app.UseFlows();

    app.SetMenu(menu => menu
        .Command("start", "Главное меню (InlineKeyboard)")
        .Command("help", "Справка по командам и UI")
        .Command("settings", "Настройки (ReplyKeyboard)")
        .Command("register", "Регистрация (Flow + ReplyKeyboard)"));

    app.MapBotEndpoints();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Expose Program for integration tests
public partial class Program;
