using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.MasterData.Branches;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Branches.Commands;
using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Application.Branches.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public sealed class BranchDesignerCompositionTests
{
    private const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;

    [Fact]
    public void BranchEdit_OwnedTableRetainsEveryCellAndInheritedButton()
    {
        Sta(() =>
        {
            using var form = new BranchEditForm("Edit Branch", "A", "B", "C", "D", "E", "F");
            var table = (TableLayoutPanel)typeof(MasterDataEditFormBase).GetField("_contentPanel", Fields)!.GetValue(form)!;
            Assert.Same(table, typeof(BranchEditForm).GetField("_branchLayout", Fields)!.GetValue(form));
            Assert.Equal(2, form.Controls.Count);
            Assert.Equal(6, table.RowCount);
            Assert.Equal(2, table.ColumnCount);
            Assert.Equal(12, table.Controls.Count);
            var fields = new[] { "_nameEdit", "_streetEdit", "_cityEdit", "_stateEdit", "_postalCodeEdit", "_countryEdit" };
            var captions = new[] { "Name:", "Street:", "City:", "State:", "Postal Code:", "Country:" };
            for (var row = 0; row < fields.Length; row++)
            {
                var editor = (Control)typeof(BranchEditForm).GetField(fields[row], Fields)!.GetValue(form)!;
                Assert.Same(table, editor.Parent);
                Assert.Same(editor, table.GetControlFromPosition(1, row));
                Assert.Equal(captions[row], table.GetControlFromPosition(0, row)!.Text);
                Assert.Equal(1, table.GetColumnSpan(editor));
                Assert.Equal(1, table.GetRowSpan(editor));
            }
            Assert.Equal(new[] { "A", "B", "C", "D", "E", "F" },
                new[] { form.BranchNameValue, form.Street, form.City, form.State, form.PostalCode, form.Country });
            var ok = Assert.IsAssignableFrom<Control>(form.AcceptButton);
            var cancel = Assert.IsAssignableFrom<Control>(form.CancelButton);
            Assert.Same(ok.Parent, cancel.Parent);
            Assert.Equal(2, ok.Parent!.Controls.Count);
            var events = (EventHandlerList)typeof(Component).GetProperty("Events", Fields)!.GetValue(ok)!;
            var handlers = typeof(Control).GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(f => f.FieldType == typeof(object) && f.Name.Contains("click", StringComparison.OrdinalIgnoreCase))
                .SelectMany(f => events[f.GetValue(null)!]?.GetInvocationList() ?? []);
            Assert.Single(handlers, d => d.Method.Name == "OkButton_Click");
            var children = table.Controls.Cast<Control>().ToArray();
            form.Dispose();
            Assert.True(table.IsDisposed);
            Assert.All(children, control => Assert.True(control.IsDisposed));
        });
    }

    [Fact]
    public void BranchManagement_DesignerCtorBuildsRealControlsWithoutServices()
    {
        Sta(() =>
        {
            using var view = (BranchManagementView)Activator.CreateInstance(typeof(BranchManagementView))!;
            var list = Assert.Single(view.Controls.OfType<BranchListView>());
            Assert.Equal(5, list.GridView.Columns.Count);
            Assert.Equal(new[] { "Name", "City", "Country", "Status", "CreatedAtUtc" },
                list.GridView.Columns.Cast<DevExpress.XtraGrid.Columns.GridColumn>().Select(c => c.FieldName));
            Assert.Single(view.Controls.OfType<OrganizationHierarchySelector>());
            Assert.NotNull(list.Controls.OfType<DevExpress.XtraGrid.GridControl>().SingleOrDefault()
                ?? Descendants(list).OfType<DevExpress.XtraGrid.GridControl>().Single());
            view.Dispose();
            view.Dispose();
            Assert.True(list.IsDisposed);
        });
    }

    [Fact]
    public void BranchManagement_BindsScopedServicesQueriesAndAuthorization_ThenDisposesOnce()
    {
        Sta(() =>
        {
            var mediator = new SpyMediator();
            var policy = new SpyPolicy();
            using var provider = new ServiceCollection().AddScoped<IMediator>(_ => mediator)
                .AddScoped<IFeatureAuthorizationPolicy>(_ => policy).BuildServiceProvider(true);
            var session = new Session();
            var view = new BranchManagementView(provider.GetRequiredService<IServiceScopeFactory>(), session);
            var list = view.Controls.OfType<BranchListView>().Single();
            var selector = view.Controls.OfType<OrganizationHierarchySelector>().Single();
            Assert.Empty(mediator.Requests);
            Assert.False(list.CanUseFeatureAsync!("create").GetAwaiter().GetResult());
            Assert.Equal("branches.create", Assert.Single(policy.Features));
            session.UserId = null;
            Assert.False(list.CanUseFeatureAsync!("create").GetAwaiter().GetResult());
            Assert.Single(policy.Features);
            session.UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Assert.Empty(list.LoadItemsAsync!(CancellationToken.None).GetAwaiter().GetResult());
            Assert.Empty(mediator.Requests); // no company selected
            var company = Guid.NewGuid();
            typeof(OrganizationHierarchySelector).GetProperty("SelectedCompanyId")!.SetValue(selector, company);
            Assert.Empty(list.LoadItemsAsync!(CancellationToken.None).GetAwaiter().GetResult());
            Assert.Equal(company, Assert.IsType<ListBranchesByCompanyQuery>(Assert.Single(mediator.Requests)).CompanyId);
            list.RefreshAsync().GetAwaiter().GetResult();
            Assert.Equal(2, mediator.Requests.OfType<ListBranchesByCompanyQuery>().Count());
            var dto = new BranchDto(Guid.NewGuid(), company, "Branch", null, "City", null, null, "Country", "Active", DateTimeOffset.UtcNow);
            Assert.Equal("Branch", list.SearchTextSelector!(dto));
            Assert.Equal("Active", list.StatusSelector!(dto));
            list.OnActivate!(dto).GetAwaiter().GetResult();
            list.OnDeactivate!(dto).GetAwaiter().GetResult();
            Assert.Single(mediator.Requests.OfType<ActivateBranchCommand>());
            Assert.Single(mediator.Requests.OfType<DeactivateBranchCommand>());
            var disposedBeforeServices = false;
            list.Disposed += (_, _) => disposedBeforeServices = mediator.DisposeCount == 0;
            view.Dispose(); view.Dispose();
            Assert.True(disposedBeforeServices);
            Assert.Equal(1, mediator.DisposeCount);
            Assert.Equal(1, policy.DisposeCount);
            Assert.False(list.CanUseFeatureAsync!("create").GetAwaiter().GetResult());
        });
    }

    [Fact]
    public void BranchManagement_FailedResolutionReleasesTheCreatedScope()
    {
        Sta(() =>
        {
            var mediator = new SpyMediator();
            using var provider = new ServiceCollection().AddScoped<IMediator>(_ => mediator).BuildServiceProvider(true);
            Assert.Throws<InvalidOperationException>(() => new BranchManagementView(
                provider.GetRequiredService<IServiceScopeFactory>(), new Session()));
            Assert.Equal(1, mediator.DisposeCount);
        });
    }

    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>()
        .SelectMany(child => new[] { child }.Concat(Descendants(child)));

    private static void Sta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45)));
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    private sealed class Session : ICurrentSession
    {
        public bool IsAuthenticated => UserId.HasValue;
        public Guid? UserId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public Guid? SessionId => null;
        public string? DisplayName => "Synthetic test";
        public void SignIn(Guid userId, Guid sessionId, string displayName) => throw new NotSupportedException();
        public void SignOut() => throw new NotSupportedException();
        public event EventHandler? Changed { add { } remove { } }
    }

    private sealed class SpyPolicy : IFeatureAuthorizationPolicy, IDisposable
    {
        public List<string> Features { get; } = [];
        public int DisposeCount { get; private set; }
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
        { Features.Add(featureCode); return Task.FromResult(false); }
        public void Dispose() => DisposeCount++;
    }

    private sealed class SpyMediator : IMediator, IDisposable
    {
        public List<object> Requests { get; } = [];
        public int DisposeCount { get; private set; }
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return request is ListBranchesByCompanyQuery
                ? Task.FromResult((TResponse)(object)Array.Empty<BranchDto>())
                : Task.FromResult(default(TResponse)!);
        }
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        { Requests.Add(request); return Task.CompletedTask; }
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        { Requests.Add(request); return Task.FromResult<object?>(null); }
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Dispose() => DisposeCount++;
    }
}
