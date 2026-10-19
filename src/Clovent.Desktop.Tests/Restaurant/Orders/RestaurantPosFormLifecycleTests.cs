using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public class RestaurantPosFormLifecycleTests
{
    private sealed class LifecycleTestMediator : IMediator
    {
        public bool SimulateFailure { get; set; }
        public bool SimulateDelay { get; set; }
        public int SendCount { get; private set; }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SendCount++;

            if (SimulateDelay)
            {
                await Task.Delay(50, cancellationToken);
            }

            if (SimulateFailure)
            {
                throw new InvalidOperationException("GENUINE_DB_FAILURE: Simulated database connection loss.");
            }

            var type = typeof(TResponse);
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IReadOnlyList<>) ||
                    genericDef == typeof(IReadOnlyCollection<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(IEnumerable<>))
                {
                    var itemType = type.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(itemType);
                    return (TResponse)Activator.CreateInstance(listType)!;
                }
            }

            return default!;
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(null);

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private static void PumpUntilComplete(Task task, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!task.IsCompleted && sw.ElapsedMilliseconds < timeoutMs)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        if (!task.IsCompleted)
        {
            throw new TimeoutException($"Task did not complete within {timeoutMs}ms.");
        }
    }

    [Fact]
    public void NavigationOrCloseDuringInitialization_CancelsCleanly_WithoutCrashing()
    {
        // 1. Navigation/close during initialization
        var mediator = new LifecycleTestMediator { SimulateDelay = true };
        var form = new RestaurantPosForm();
        form.StartupFailureDialogHandler = (msg, ex) => DialogResult.Cancel;

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(RestaurantPosForm).GetField("_mediator", flags)!.SetValue(form, mediator);
        typeof(RestaurantPosForm).GetField("_currentSession", flags)!.SetValue(form, new CurrentSession());

        // Kick off LoadAsync
        var loadTask = (Task)typeof(RestaurantPosForm).GetMethod("LoadAsync", flags)!.Invoke(form, null)!;

        // Immediately simulate user navigation or closing the form
        form.Close();
        form.Dispose();

        PumpUntilComplete(loadTask);

        Assert.True(form.IsDisposed);
        Assert.False(form.IsInitialLoadComplete);
        Assert.True(form.InitializationTask.IsCanceled || form.InitializationTask.IsCompleted);
    }

    [Fact]
    public void SuccessfulNormalInitialization_CompletesInitializationTask_AndSetsIsInitialLoadComplete()
    {
        // 2. Successful normal initialization and navigation
        var mediator = new LifecycleTestMediator { SimulateDelay = false, SimulateFailure = false };
        using var form = new RestaurantPosForm();
        string? failureDialogShown = null;
        Exception? failureEx = null;
        form.StartupFailureDialogHandler = (msg, ex) =>
        {
            failureDialogShown = msg;
            failureEx = ex;
            return DialogResult.Cancel;
        };

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(RestaurantPosForm).GetField("_mediator", flags)!.SetValue(form, mediator);
        typeof(RestaurantPosForm).GetField("_currentSession", flags)!.SetValue(form, new CurrentSession());

        var loadTask = (Task)typeof(RestaurantPosForm).GetMethod("LoadAsync", flags)!.Invoke(form, null)!;
        PumpUntilComplete(loadTask);

        Assert.True(failureEx == null, $"Unexpected initialization failure: {failureEx}");
        Assert.Null(failureDialogShown);
        Assert.True(form.IsInitialLoadComplete);
        Assert.True(form.InitializationTask.IsCompletedSuccessfully);
    }

    [Fact]
    public void GenuineInitializationFailure_WhenNotDisposed_LeavesIsInitialLoadCompleteFalse_AndFaultsTask()
    {
        // 3. Genuine initialization failure remaining visible
        var mediator = new LifecycleTestMediator { SimulateDelay = false, SimulateFailure = true };
        using var form = new RestaurantPosForm();
        string? failureDialogMessage = null;
        Exception? failureEx = null;
        form.StartupFailureDialogHandler = (msg, ex) =>
        {
            failureDialogMessage = msg;
            failureEx = ex;
            return DialogResult.Cancel;
        };

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(RestaurantPosForm).GetField("_mediator", flags)!.SetValue(form, mediator);
        typeof(RestaurantPosForm).GetField("_currentSession", flags)!.SetValue(form, new CurrentSession());

        var loadTask = (Task)typeof(RestaurantPosForm).GetMethod("LoadAsync", flags)!.Invoke(form, null)!;
        PumpUntilComplete(loadTask);

        // Genuine failure remains visible via dialog handler
        Assert.NotNull(failureDialogMessage);
        Assert.Contains("Unable to load the Restaurant POS screen", failureDialogMessage);
        Assert.NotNull(failureEx);
        Assert.Contains("GENUINE_DB_FAILURE", failureEx.Message);
        Assert.False(form.IsInitialLoadComplete);
        Assert.True(form.InitializationTask.IsFaulted);
    }
}
