using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Shared;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Customers;

public class CustomersViewCsvExportTests
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

        #pragma warning disable CS0067
        public event EventHandler? Changed;
        #pragma warning restore CS0067
    }

    private class FakeFeatureAuthorizationPolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private class FakeMediator : IMediator
    {
        public Func<object, object>? QueryHandler { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (QueryHandler != null)
            {
                return Task.FromResult((TResponse)QueryHandler(request));
            }
            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
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
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Tester");

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
    public void ExportToCsv_CreatesFileWithExpectedValuesAndEscaping()
    {
        var customers = new List<CustomerDto>
        {
            new CustomerDto(
                CustomerId: Guid.NewGuid(),
                Code: "C001",
                Name: "Doe, John", // name containing comma
                MobileNumber: "555-1234",
                Address: "123 St",
                Email: "john@doe.com",
                OpeningBalance: 0m,
                CreditLimit: 500m,
                OutstandingBalance: 123.45m,
                IsActive: true,
                Notes: "Says \"Hello\"", // notes containing quotes
                CreatedAtUtc: DateTimeOffset.UtcNow,
                UpdatedAtUtc: DateTimeOffset.UtcNow
            ),
            new CustomerDto(
                CustomerId: Guid.NewGuid(),
                Code: "C002",
                Name: "Plain Jane",
                MobileNumber: "555-5678",
                Address: "456 Rd",
                Email: null, // empty / null value
                OpeningBalance: 0m,
                CreditLimit: 0m,
                OutstandingBalance: 0m,
                IsActive: false,
                Notes: null,
                CreatedAtUtc: DateTimeOffset.UtcNow,
                UpdatedAtUtc: DateTimeOffset.UtcNow
            )
        };

        var (view, _, _, _) = CreateViewContext(customers);
        LoadView(view);

        var tempFile = Path.GetTempFileName();
        try
        {
            var gridView = GetPrivateField<DevExpress.XtraGrid.Views.Grid.GridView>(view, "_gridView");
            gridView.ExportToCsv(tempFile);

            Assert.True(File.Exists(tempFile));

            var lines = File.ReadAllLines(tempFile);
            Assert.NotEmpty(lines);

            var headers = CsvFile.ParseRow(lines[0]);
            var parsedRows = CsvFile.ParseDataRows(lines);

            // Verify Headers
            Assert.Equal(8, headers.Count);
            Assert.Equal("Code", headers[0]);
            Assert.Equal("Customer Name", headers[1]);
            Assert.Equal("Mobile", headers[2]);
            Assert.Equal("Email", headers[3]);
            Assert.Equal("Outstanding", headers[4]);
            Assert.Equal("Credit Limit", headers[5]);
            Assert.Equal("Status", headers[6]);
            Assert.Equal("Last Transaction", headers[7]);

            // Verify Row Count
            Assert.Equal(2, parsedRows.Count);

            // Verify Row 1: escaping of comma in Name and formatting
            var row1 = parsedRows[0];
            Assert.Equal("C001", row1[0]);
            Assert.Equal("Doe, John", row1[1]);
            Assert.Equal("555-1234", row1[2]);
            Assert.Equal("john@doe.com", row1[3]);
            Assert.Equal(CurrencyDisplay.Format(123.45m), row1[4]);
            Assert.Equal(CurrencyDisplay.Format(500m), row1[5]);
            Assert.Equal("Active", row1[6]);
            Assert.Equal("-", row1[7]); // Last transaction date is null

            // Verify Row 2: empty/null handling
            var row2 = parsedRows[1];
            Assert.Equal("C002", row2[0]);
            Assert.Equal("Plain Jane", row2[1]);
            Assert.Equal("555-5678", row2[2]);
            Assert.Equal("-", row2[3]); // Email null translates to "-" in grid row
            Assert.Equal(CurrencyDisplay.Format(0m), row2[4]);
            Assert.Equal(CurrencyDisplay.Format(0m), row2[5]);
            Assert.Equal("Inactive", row2[6]);
            Assert.Equal("-", row2[7]);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
