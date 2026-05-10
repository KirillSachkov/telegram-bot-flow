using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Screens;

/// <summary>
/// Регрессии на семантику <c>BackButton</c>, поставленный на action-view,
/// показанной через <see cref="NavigationService.ShowViewAsync"/>:
/// «Назад» обязан закрыть overlay и вернуть пользователя на <c>CurrentScreen</c>,
/// поверх которого overlay был показан, а не сделать pop из NavigationStack.
/// </summary>
public sealed class ActionViewBackButtonTests
{
    [Fact]
    public async Task NavigateBack_OnActionView_ReturnsToCurrentScreen_WithoutPoppingStack()
    {
        var harness = new NavigationHarness();
        UpdateContext ctx = harness.CreateContext();

        // Setup: main → profile (current).
        await harness.Navigation.NavigateToAsync(ctx, "main");
        await harness.Navigation.NavigateToAsync(ctx, "profile");

        ctx.Session!.Navigation.CurrentScreen.Should().Be("profile");
        ctx.Session.Navigation.NavigationStack.Should().Equal("main");

        // Action: show overlay (e.g. result/error) с поставленным руками BackButton.
        await harness.Navigation.ShowViewAsync(ctx, new ScreenView("ok").BackButton());

        ctx.Session.Navigation.CurrentScreen.Should().Be("profile",
            "ShowView не меняет CurrentScreen — overlay показан поверх профиля");
        ctx.Session.Navigation.NavigationStack.Should().Equal("main");

        // User taps Back on the overlay.
        await harness.Navigation.NavigateBackAsync(ctx);

        // Expected: overlay closed, user is back on profile, stack untouched.
        ctx.Session.Navigation.CurrentScreen.Should().Be("profile",
            "Back на action-view закрывает overlay и возвращает на CurrentScreen");
        ctx.Session.Navigation.NavigationStack.Should().Equal("main");
        ctx.Session.Navigation.IsActionViewActive.Should().BeFalse(
            "флаг overlay'я обязан сбрасываться после Back");
    }

    [Fact]
    public async Task NavigateBack_OnRegularScreen_StillPopsStack()
    {
        // Sanity check: обычный Back на экране (без активного overlay) работает по-старому.
        var harness = new NavigationHarness();
        UpdateContext ctx = harness.CreateContext();

        await harness.Navigation.NavigateToAsync(ctx, "main");
        await harness.Navigation.NavigateToAsync(ctx, "profile");

        await harness.Navigation.NavigateBackAsync(ctx);

        ctx.Session!.Navigation.CurrentScreen.Should().Be("main");
        ctx.Session.Navigation.NavigationStack.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowView_ThenNavigateTo_DoesNotTreatNextNavAsActionViewBack()
    {
        // Регрессия: после ShowView'я флаг overlay'я обязан сбрасываться, иначе следующий
        // обычный NavigateTo — а потом Back с него — будет вести себя как Back на overlay'е.
        var harness = new NavigationHarness();
        UpdateContext ctx = harness.CreateContext();

        await harness.Navigation.NavigateToAsync(ctx, "main");
        await harness.Navigation.NavigateToAsync(ctx, "profile");
        await harness.Navigation.ShowViewAsync(ctx, new ScreenView("toast"));

        // Обычный переход — должен сбросить overlay-флаг.
        await harness.Navigation.NavigateToAsync(ctx, "settings");

        await harness.Navigation.NavigateBackAsync(ctx);

        ctx.Session!.Navigation.CurrentScreen.Should().Be("profile",
            "после обычной навигации Back должен попать стек, не возвращаться на settings");
        ctx.Session.Navigation.NavigationStack.Should().Equal("main");
        ctx.Session.Navigation.IsActionViewActive.Should().BeFalse(
            "обычная навигация после ShowView должна сбросить overlay-флаг");
    }

    /// <summary>
    /// Минимальный гарнес для прямого тестирования <see cref="NavigationService"/> +
    /// <see cref="ScreenManager"/>: stub renderer, два тестовых экрана с предсказуемыми ID.
    /// </summary>
    private sealed class NavigationHarness
    {
        public INavigationService Navigation { get; }
        private readonly IServiceProvider _services;

        public NavigationHarness()
        {
            ServiceCollection services = new();
            services.AddTransient<MainTestScreen>();
            services.AddTransient<ProfileTestScreen>();
            services.AddTransient<SettingsTestScreen>();
            _services = services.BuildServiceProvider();

            ScreenRegistry registry = new();
            registry.RegisterWithId("main", typeof(MainTestScreen));
            registry.RegisterWithId("profile", typeof(ProfileTestScreen));
            registry.RegisterWithId("settings", typeof(SettingsTestScreen));

            ITelegramBotClient bot = Substitute.For<ITelegramBotClient>();
            ScreenManager manager = new(registry, new StubRenderer(), bot, NullLogger<ScreenManager>.Instance);
            Navigation = new NavigationService(manager);
        }

        public UpdateContext CreateContext()
        {
            UpdateContext ctx = TestHelpers.CreateCallbackContext("noop", services: _services);
            ctx.Session = new UserSession(ctx.UserId);
            return ctx;
        }
    }

    private sealed class StubRenderer : IScreenMessageRenderer
    {
        public Task<Message> RenderAsync(
            UpdateContext context,
            ScreenView view,
            InlineKeyboardMarkup? keyboard,
            int? existingMessageId,
            ScreenMediaType oldMediaType,
            ScreenMediaType newMediaType) =>
            Task.FromResult(new Message { Id = 42, Date = DateTime.UtcNow });
    }

    private sealed class MainTestScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("main"));
    }

    private sealed class ProfileTestScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("profile"));
    }

    private sealed class SettingsTestScreen : IScreen
    {
        public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
            ValueTask.FromResult(new ScreenView("settings"));
    }
}
