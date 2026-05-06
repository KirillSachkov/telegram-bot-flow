using Serilog;
using TelegramBotFlow.App.Features.MainMenu.Screens;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Hosting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    BotApplicationBuilder builder = BotApplication.CreateBuilder(args);

    builder.WebAppBuilder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddBotCoreData(builder.Configuration);

    builder.Services.AddBotEndpoints(typeof(Program).Assembly);
    builder.Services.AddScreens(typeof(Program).Assembly);
    builder.Services.AddWizards(typeof(Program).Assembly);

    builder.UseErrorHandling();
    builder.UseLogging();
    builder.UsePrivateChatOnly();
    builder.UseSession();
    builder.UseAccessPolicy();
    builder.UseWizards();
    builder.Use<UserTrackingMiddleware<BotUser>>();
    builder.UsePendingInput();

    BotApplication app = builder.Build();

    app.SetMenu(menu => menu
        .Command("start", "Главное меню"));

    app.UseNavigation<MainMenuScreen>();
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

public partial class Program;