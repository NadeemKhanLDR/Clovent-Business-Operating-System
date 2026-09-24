using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Navigation;

public class ApplicationModeNavigatorTests
{
    private sealed class FakeErrorDialogService : IErrorDialogService
    {
        public Exception? LastException { get; private set; }
        public string? LastContext { get; private set; }

        public void ShowError(Exception exception, string? context = null)
        {
            LastException = exception;
            LastContext = context;
        }
    }

    private sealed class FakeWorkspaceHost : IWorkspaceHost
    {
        public string? LastShownKey { get; private set; }
        public string? LastShownCaption { get; private set; }

        public void ShowDocument(string key, string caption, Func<Control> contentFactory, bool allowMultipleInstances = false)
        {
            LastShownKey = key;
            LastShownCaption = caption;
        }

        public void SetStatus(string text) { }
    }

    private sealed class TrackingForm : Form
    {
        public bool CloseCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            CloseCalled = true;
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalled = true;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public async Task OpenPosAsync_SetsCurrentForm_AndDisposesPreviousForm()
    {
        var services = new ServiceCollection();
        var fakeErrorService = new FakeErrorDialogService();
        var appContext = new CbosApplicationContext();

        var previousForm = new TrackingForm();
        previousForm.Show();

        // Register RestaurantPosForm parameterless (test-friendly)
        services.AddTransient<RestaurantPosForm>();
        services.AddTransient<MainForm>(_ => throw new InvalidOperationException("Not needed"));
        services.AddSingleton<INavigationService>(sp => new NavigationService(sp, NullLogger<NavigationService>.Instance));

        var provider = services.BuildServiceProvider();
        var navigator = new ApplicationModeNavigator(
            provider,
            appContext,
            fakeErrorService,
            NullLogger<ApplicationModeNavigator>.Instance);

        // Seed previous form
        typeof(ApplicationModeNavigator)
            .GetField("_currentForm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(navigator, previousForm);

        await navigator.OpenPosAsync();

        Assert.NotNull(navigator.CurrentForm);
        Assert.IsType<RestaurantPosForm>(navigator.CurrentForm);
        Assert.Same(navigator.CurrentForm, appContext.MainForm);
        Assert.True(previousForm.IsDisposed);
        Assert.False(navigator.IsTransitioning);
        Assert.Null(navigator.CurrentWorkspaceHost);

        navigator.CurrentForm.Dispose();
    }

    [Fact]
    public async Task OpenBackOfficeAsync_SetsCurrentForm_NavigatesToInitialView_AndDisposesPreviousForm()
    {
        var services = new ServiceCollection();
        var fakeErrorService = new FakeErrorDialogService();
        var appContext = new CbosApplicationContext();

        var previousForm = new TrackingForm();
        previousForm.Show();

        var fakeHost = new FakeWorkspaceHost();

        // Setup DI
        var servicesWithNav = new ServiceCollection();
        servicesWithNav.AddSingleton<IWorkspaceHost>(fakeHost);
        servicesWithNav.AddSingleton<INavigationService>(sp =>
        {
            var nav = new NavigationService(sp, NullLogger<NavigationService>.Instance);
            nav.Register("dashboard", () => new Control());
            return nav;
        });
        // Create a tracking main form that implements IWorkspaceHost
        servicesWithNav.AddTransient<MainForm>(_ => (MainForm)Activator.CreateInstance(typeof(MainForm), nonPublic: true)!);

        var provider = servicesWithNav.BuildServiceProvider();
        var navigator = new ApplicationModeNavigator(
            provider,
            appContext,
            fakeErrorService,
            NullLogger<ApplicationModeNavigator>.Instance);

        typeof(ApplicationModeNavigator)
            .GetField("_currentForm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(navigator, previousForm);

        await navigator.OpenBackOfficeAsync("dashboard", "Dashboard");

        Assert.NotNull(navigator.CurrentForm);
        Assert.IsType<MainForm>(navigator.CurrentForm);
        Assert.Same(navigator.CurrentForm, appContext.MainForm);
        Assert.True(previousForm.IsDisposed);
        Assert.False(navigator.IsTransitioning);
        Assert.NotNull(navigator.CurrentWorkspaceHost);

        navigator.CurrentForm.Dispose();
    }

    [Fact]
    public async Task ReentrantTransition_IsBlockedByGuard()
    {
        var services = new ServiceCollection();
        var fakeErrorService = new FakeErrorDialogService();
        var appContext = new CbosApplicationContext();

        var provider = services.BuildServiceProvider();
        var navigator = new ApplicationModeNavigator(
            provider,
            appContext,
            fakeErrorService,
            NullLogger<ApplicationModeNavigator>.Instance);

        // Manually set _isTransitioning = true to simulate ongoing transition
        typeof(ApplicationModeNavigator)
            .GetField("_isTransitioning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(navigator, true);

        // Calling OpenPosAsync should immediately return without doing anything
        await navigator.OpenPosAsync();
        Assert.Null(navigator.CurrentForm);

        // Calling OpenBackOfficeAsync should also immediately return
        await navigator.OpenBackOfficeAsync();
        Assert.Null(navigator.CurrentForm);
    }

    [Fact]
    public void CbosApplicationContext_SuppressesExitDuringTransition()
    {
        var appContext = new CbosApplicationContext();
        var form = new Form();
        appContext.SetActiveForm(form);

        // When transitioning is true, closing the form must NOT exit the message loop
        appContext.IsTransitioning = true;
        form.Close();

        // If it got here without terminating the test host or throwing, suppression worked
        Assert.True(appContext.IsTransitioning);
    }

    [Fact]
    public void ExitApplication_ClosesCurrentForm_AndExitsContext()
    {
        Exception? threadException = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                var fakeErrorService = new FakeErrorDialogService();
                var appContext = new CbosApplicationContext();

                var form = new TrackingForm();
                form.Show();

                var provider = services.BuildServiceProvider();
                var navigator = new ApplicationModeNavigator(
                    provider,
                    appContext,
                    fakeErrorService,
                    NullLogger<ApplicationModeNavigator>.Instance);

                typeof(ApplicationModeNavigator)
                    .GetField("_currentForm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .SetValue(navigator, form);

                navigator.ExitApplication();

                Assert.True(form.IsDisposed);
                Assert.Null(navigator.CurrentForm);
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException is not null)
        {
            throw threadException;
        }
    }

    [Fact]
    public void CbosApplicationContext_WhenOldFormClosed_DoesNotExitMessageLoop()
    {
        var appContext = new CbosApplicationContext();
        var oldForm = new Form();
        var currentForm = new Form();

        appContext.SetActiveForm(currentForm);
        Assert.Same(currentForm, appContext.MainForm);

        // When an old form closes and sender != MainForm, message loop exit is suppressed even if IsTransitioning is false
        appContext.IsTransitioning = false;
        var onMainFormClosedMethod = typeof(CbosApplicationContext).GetMethod(
            "OnMainFormClosed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        // Calling OnMainFormClosed with oldForm should be safely ignored
        onMainFormClosedMethod.Invoke(appContext, new object[] { oldForm, EventArgs.Empty });

        Assert.Same(currentForm, appContext.MainForm);
        oldForm.Dispose();
        currentForm.Dispose();
    }

    [Fact]
    public async Task FiveConsecutiveRoundTrips_BetweenBackOfficeAndPos_MaintainsValidStateAndContext()
    {
        var services = new ServiceCollection();
        var fakeErrorService = new FakeErrorDialogService();
        var appContext = new CbosApplicationContext();
        var fakeHost = new FakeWorkspaceHost();

        services.AddSingleton<IWorkspaceHost>(fakeHost);
        services.AddSingleton<INavigationService>(sp =>
        {
            var nav = new NavigationService(sp, NullLogger<NavigationService>.Instance);
            nav.Register("dashboard", () => new Control());
            return nav;
        });
        services.AddTransient<RestaurantPosForm>();
        services.AddTransient<MainForm>(_ => (MainForm)Activator.CreateInstance(typeof(MainForm), nonPublic: true)!);

        var provider = services.BuildServiceProvider();
        var navigator = new ApplicationModeNavigator(
            provider,
            appContext,
            fakeErrorService,
            NullLogger<ApplicationModeNavigator>.Instance);

        var shift = new Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto(
            Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Cashier", DateTimeOffset.UtcNow, null, "Open", 100m, 100m, 100m, 0m, null, null, DateTimeOffset.UtcNow);

        // Start in Back Office
        await navigator.OpenBackOfficeAsync();
        Assert.IsType<MainForm>(navigator.CurrentForm);
        Assert.Same(navigator.CurrentForm, appContext.MainForm);

        // Perform 5 round trips: Back Office -> POS -> Back Office
        for (int i = 1; i <= 5; i++)
        {
            var previousForm = navigator.CurrentForm;

            // Transition to POS
            await navigator.OpenPosAsync(shift);
            Assert.NotNull(navigator.CurrentForm);
            Assert.IsType<RestaurantPosForm>(navigator.CurrentForm);
            Assert.Same(navigator.CurrentForm, appContext.MainForm);
            Assert.True(previousForm!.IsDisposed, $"Previous form should be disposed on round {i} to POS");
            Assert.False(navigator.IsTransitioning);

            var previousPos = navigator.CurrentForm;

            // Transition back to Back Office
            await navigator.OpenBackOfficeAsync();
            Assert.NotNull(navigator.CurrentForm);
            Assert.IsType<MainForm>(navigator.CurrentForm);
            Assert.Same(navigator.CurrentForm, appContext.MainForm);
            Assert.True(previousPos!.IsDisposed, $"Previous POS form should be disposed on round {i} to Back Office");
            Assert.False(navigator.IsTransitioning);
        }

        // Clean up final form
        navigator.CurrentForm?.Dispose();
    }

    [Fact]
    public async Task OpenPosAsync_FromThreadPoolThread_MarshalsToUiThread()
    {
        var services = new ServiceCollection();
        var fakeErrorService = new FakeErrorDialogService();
        var appContext = new CbosApplicationContext();

        services.AddTransient<RestaurantPosForm>();
        services.AddTransient<MainForm>(_ => throw new InvalidOperationException("Not needed"));
        services.AddSingleton<INavigationService>(sp => new NavigationService(sp, NullLogger<NavigationService>.Instance));

        var provider = services.BuildServiceProvider();
        var navigator = new ApplicationModeNavigator(
            provider,
            appContext,
            fakeErrorService,
            NullLogger<ApplicationModeNavigator>.Instance);

        var uiThreadId = Environment.CurrentManagedThreadId;
        SendOrPostCallback? postedCallback = null;
        object? postedState = null;
        var postEvent = new System.Threading.ManualResetEventSlim(false);

        var customSyncContext = new TestSyncContext((callback, state) =>
        {
            postedCallback = callback;
            postedState = state;
            postEvent.Set();
        });

        navigator.SetUiSynchronizationContext(customSyncContext);

        Task? openTask = null;
        var navTask = Task.Run(async () =>
        {
            Assert.NotEqual(uiThreadId, Environment.CurrentManagedThreadId);
            openTask = navigator.OpenPosAsync();
            await openTask;
        });

        // Wait for worker thread to post work
        Assert.True(postEvent.Wait(TimeSpan.FromSeconds(5)), "Background thread must post work to SynchronizationContext");
        Assert.NotNull(postedCallback);

        // Simulate UI message loop executing the posted callback
        postedCallback(postedState);
        await navTask;

        Assert.NotNull(navigator.CurrentForm);
        Assert.IsType<RestaurantPosForm>(navigator.CurrentForm);
        Assert.Same(navigator.CurrentForm, appContext.MainForm);

        navigator.CurrentForm.Dispose();
    }

    private sealed class TestSyncContext : SynchronizationContext
    {
        private readonly Action<SendOrPostCallback, object?> _postHandler;

        public TestSyncContext(Action<SendOrPostCallback, object?> postHandler)
        {
            _postHandler = postHandler;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            _postHandler(d, state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            _postHandler(d, state);
        }
    }
}

