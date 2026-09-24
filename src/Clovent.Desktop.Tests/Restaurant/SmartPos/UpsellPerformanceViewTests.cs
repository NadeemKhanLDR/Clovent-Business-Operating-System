using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Sessions;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class UpsellPerformanceViewTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Manager User";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class FakeMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(TResponse)!);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];
        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;
        public object? GetService(Type serviceType) => _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private sealed class FakeServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public void Dispose() { }
    }

    private sealed class FakeServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope(serviceProvider);
    }

    private static UpsellPerformanceView CreateView()
    {
        var session = new FakeCurrentSession();
        var mediator = new FakeMediator();
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<ILogger<UpsellPerformanceView>>(NullLogger<UpsellPerformanceView>.Instance);

        return new UpsellPerformanceView(new FakeServiceScopeFactory(provider), session);
    }

    [Fact]
    public void UpsellPerformanceView_HasEnterpriseHeader_AndSubtitle()
    {
        using var view = CreateView();

        var headerField = typeof(UpsellPerformanceView).GetField("_headerPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(headerField);
        var headerPanel = (PanelControl)headerField.GetValue(view)!;
        Assert.NotNull(headerPanel);

        var labels = GetAllControls(headerPanel).OfType<LabelControl>().ToList();
        var titleLabel = labels.FirstOrDefault(l => l.Text == "UPSELL PERFORMANCE");
        var subLabel = labels.FirstOrDefault(l => l.Text.Contains("Review recommendation offers"));

        Assert.NotNull(titleLabel);
        Assert.NotNull(subLabel);
    }

    private static List<Control> GetAllControls(Control root)
    {
        var list = new List<Control>();
        foreach (Control c in root.Controls)
        {
            list.Add(c);
            list.AddRange(GetAllControls(c));
        }
        return list;
    }

    [Fact]
    public void UpsellPerformanceView_PeriodCombo_DefaultsToLast30Days_AndHasAllPresets()
    {
        using var view = CreateView();

        var periodField = typeof(UpsellPerformanceView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(periodField);
        var periodCombo = (ComboBoxEdit)periodField.GetValue(view)!;
        Assert.NotNull(periodCombo);

        Assert.Equal("Last 30 Days", periodCombo.SelectedItem?.ToString());

        var items = periodCombo.Properties.Items.Cast<object>().Select(o => o.ToString()).ToList();
        Assert.Contains("Today", items);
        Assert.Contains("Yesterday", items);
        Assert.Contains("This Week", items);
        Assert.Contains("Last Week", items);
        Assert.Contains("This Month", items);
        Assert.Contains("Last Month", items);
        Assert.Contains("Last 7 Days", items);
        Assert.Contains("Last 30 Days", items);
        Assert.Contains("Custom", items);
    }

    [Fact]
    public void UpsellPerformanceView_PeriodChange_UpdatesDateEdits()
    {
        using var view = CreateView();

        var periodField = typeof(UpsellPerformanceView).GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic);
        var fromField = typeof(UpsellPerformanceView).GetField("_fromEdit", BindingFlags.Instance | BindingFlags.NonPublic);
        var toField = typeof(UpsellPerformanceView).GetField("_toEdit", BindingFlags.Instance | BindingFlags.NonPublic);

        var periodCombo = (ComboBoxEdit)periodField!.GetValue(view)!;
        var fromEdit = (DateEdit)fromField!.GetValue(view)!;
        var toEdit = (DateEdit)toField!.GetValue(view)!;

        // Change to Today
        periodCombo.SelectedItem = "Today";
        Assert.Equal(DateTime.Today, fromEdit.DateTime.Date);
        Assert.Equal(DateTime.Today, toEdit.DateTime.Date);

        // Change to Yesterday
        periodCombo.SelectedItem = "Yesterday";
        Assert.Equal(DateTime.Today.AddDays(-1), fromEdit.DateTime.Date);
        Assert.Equal(DateTime.Today.AddDays(-1), toEdit.DateTime.Date);
    }
}
