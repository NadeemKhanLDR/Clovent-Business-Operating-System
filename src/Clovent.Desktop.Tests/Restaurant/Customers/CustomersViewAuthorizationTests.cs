using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Customers;

public class CustomersViewAuthorizationTests
{
    private class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; }
        public Guid? SessionId { get; set; }
        public string? DisplayName { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
        
        public void SignIn(Guid userId, Guid sessionId, string displayName)
        {
            UserId = userId;
            SessionId = sessionId;
            DisplayName = displayName;
        }
        
        public void SignOut()
        {
            UserId = null;
            SessionId = null;
            DisplayName = null;
        }

        #pragma warning disable CS0067 // Event is never used in fake
        public event EventHandler? Changed;
        #pragma warning restore CS0067
    }

    private class FakeFeatureAuthorizationPolicy : IFeatureAuthorizationPolicy
    {
        private readonly Dictionary<string, bool> _permissions = new(StringComparer.OrdinalIgnoreCase);
        public List<string> CheckedFeatures { get; } = new();

        public void SetPermission(string featureCode, bool allowed)
        {
            _permissions[featureCode] = allowed;
        }

        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
        {
            CheckedFeatures.Add(featureCode);
            var allowed = _permissions.TryGetValue(featureCode, out var val) && val;
            return Task.FromResult(allowed);
        }
    }

    private class FakeMediator : IMediator
    {
        public List<object> SentCommands { get; } = new();
        public Func<object, object>? QueryHandler { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentCommands.Add(request);
            if (QueryHandler != null)
            {
                return Task.FromResult((TResponse)QueryHandler(request));
            }
            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SentCommands.Add(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentCommands.Add(request);
            if (QueryHandler != null)
            {
                return Task.FromResult<object?>(QueryHandler(request));
            }
            return Task.FromResult<object?>(null);
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }

    private class FakeServiceScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; }
        public FakeServiceScope(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;
        public void Dispose() { }
    }

    private class FakeServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public FakeServiceScopeFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
        public IServiceScope CreateScope() => new FakeServiceScope(_serviceProvider);
    }

    private (CustomersView, FakeCurrentSession, FakeFeatureAuthorizationPolicy, FakeMediator) CreateViewContext(List<CustomerDto> customers)
    {
        var session = new FakeCurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Cashier User");

        var policy = new FakeFeatureAuthorizationPolicy();
        var mediator = new FakeMediator();

        mediator.QueryHandler = req =>
        {
            if (req is ListCustomersQuery)
            {
                return customers;
            }
            return null!;
        };

        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(policy);
        provider.Register<Microsoft.Extensions.Logging.ILogger<CustomersView>>(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomersView>.Instance);

        var scopeFactory = new FakeServiceScopeFactory(provider);
        var view = new CustomersView(scopeFactory, session);

        return (view, session, policy, mediator);
    }

    private void LoadView(CustomersView view)
    {
        var loadMethod = typeof(CustomersView).GetMethod("CustomersView_Load", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        loadMethod.Invoke(view, [null, EventArgs.Empty]);
    }

    private T GetPrivateField<T>(object obj, string name)
    {
        var field = obj.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (T)field!.GetValue(obj)!;
    }

    [Fact]
    public void Cashier_WithPaymentPermission_EnablesReceivePaymentButton()
    {
        var customer = new CustomerDto(
            CustomerId: Guid.NewGuid(),
            Code: "CUST-001",
            Name: "Jane Doe",
            MobileNumber: "555-1234",
            Address: "123 St",
            Email: "jane@doe.com",
            OpeningBalance: 0m,
            CreditLimit: 500m,
            OutstandingBalance: 100m,
            IsActive: true,
            Notes: null,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow
        );

        var (view, _, policy, _) = CreateViewContext([customer]);
        policy.SetPermission("customers.payment", true);
        policy.SetPermission("customers.viewledger", false);

        LoadView(view);

        var receiveBtn = GetPrivateField<SimpleButton>(view, "_btnReceivePayment");
        var ledgerBtn = GetPrivateField<SimpleButton>(view, "_btnLedger");

        Assert.True(receiveBtn.Enabled);
        Assert.False(ledgerBtn.Enabled);
        Assert.Contains("customers.payment", policy.CheckedFeatures);
    }

    [Fact]
    public void Cashier_WithoutPaymentPermission_DisablesReceivePaymentButton()
    {
        var customer = new CustomerDto(
            CustomerId: Guid.NewGuid(),
            Code: "CUST-001",
            Name: "Jane Doe",
            MobileNumber: "555-1234",
            Address: "123 St",
            Email: "jane@doe.com",
            OpeningBalance: 0m,
            CreditLimit: 500m,
            OutstandingBalance: 100m,
            IsActive: true,
            Notes: null,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow
        );

        var (view, _, policy, _) = CreateViewContext([customer]);
        policy.SetPermission("customers.payment", false);
        policy.SetPermission("customers.viewledger", true);

        LoadView(view);

        var receiveBtn = GetPrivateField<SimpleButton>(view, "_btnReceivePayment");
        var ledgerBtn = GetPrivateField<SimpleButton>(view, "_btnLedger");

        Assert.False(receiveBtn.Enabled);
        Assert.True(ledgerBtn.Enabled);
        Assert.Contains("customers.payment", policy.CheckedFeatures);
    }
}
