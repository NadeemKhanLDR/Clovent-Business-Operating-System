using Clovent.Platform.Execution;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Platform.Tests.Execution;

public sealed class ScopeEnforcementBehaviorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IExecutionContextAccessor _accessor;
    private readonly FakeInteractiveScopeProvider _interactiveProvider;
    private readonly IMediator _mediator;

    public ScopeEnforcementBehaviorTests()
    {
        var services = new ServiceCollection();
        _accessor = new ExecutionContextAccessor();
        _interactiveProvider = new FakeInteractiveScopeProvider();

        services.AddSingleton(_accessor);
        services.AddSingleton<IInteractiveScopeProvider>(_interactiveProvider);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ScopeEnforcementBehaviorTests).Assembly);
            cfg.AddOpenBehavior(typeof(ScopeEnforcementBehavior<,>));
        });

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task BranchScopedRequest_WithoutScope_ThrowsScopeUnresolvedException()
    {
        var command = new SampleBranchCommand("Item1");

        await Assert.ThrowsAsync<ScopeUnresolvedException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task BranchScopedRequest_WithValidBranchScope_ExecutesSuccessfully()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var context = new PlatformExecutionContext
        {
            OrganizationId = orgId,
            CompanyId = compId,
            BranchId = branchId
        };

        using (_accessor.BeginScope(context))
        {
            var result = await _mediator.Send(new SampleBranchCommand("Item1"));
            Assert.Equal("Processed Item1 at branch " + branchId, result);
        }
    }

    [Fact]
    public async Task TargetBranchScoped_WithMismatchedBranch_ThrowsScopeMismatchException()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();

        var context = new PlatformExecutionContext
        {
            OrganizationId = orgId,
            CompanyId = compId,
            BranchId = branchA
        };

        using (_accessor.BeginScope(context))
        {
            // Command targets branchB while caller is at branchA
            var command = new SampleTargetBranchCommand(branchB);

            await Assert.ThrowsAsync<ScopeMismatchException>(() => _mediator.Send(command));
        }
    }

    [Fact]
    public async Task CompanyScopedRequest_WithValidCompanyScope_SucceedsEvenWithoutBranch()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var context = new PlatformExecutionContext
        {
            OrganizationId = orgId,
            CompanyId = compId,
            BranchId = null // No branch scope
        };

        using (_accessor.BeginScope(context))
        {
            var result = await _mediator.Send(new SampleCompanyReportQuery());
            Assert.Equal("Report for company " + compId, result);
        }
    }

    [Fact]
    public async Task CompanyScopedRequest_WithoutCompanyScope_ThrowsScopeUnresolvedException()
    {
        var orgId = Guid.NewGuid();
        var context = new PlatformExecutionContext
        {
            OrganizationId = orgId,
            CompanyId = null // Missing company
        };

        using (_accessor.BeginScope(context))
        {
            await Assert.ThrowsAsync<ScopeUnresolvedException>(() => _mediator.Send(new SampleCompanyReportQuery()));
        }
    }

    [Fact]
    public async Task TargetCompanyScoped_WithMismatchedCompany_ThrowsScopeMismatchException()
    {
        var orgId = Guid.NewGuid();
        var compA = Guid.NewGuid();
        var compB = Guid.NewGuid();

        var context = new PlatformExecutionContext
        {
            OrganizationId = orgId,
            CompanyId = compA
        };

        using (_accessor.BeginScope(context))
        {
            var command = new SampleTargetCompanyCommand(compB);
            await Assert.ThrowsAsync<ScopeMismatchException>(() => _mediator.Send(command));
        }
    }

    [Fact]
    public async Task OrganizationScopedRequest_WithValidOrgScope_SucceedsEvenWithoutCompanyOrBranch()
    {
        var orgId = Guid.NewGuid();
        var context = new PlatformExecutionContext
        {
            OrganizationId = orgId,
            CompanyId = null,
            BranchId = null
        };

        using (_accessor.BeginScope(context))
        {
            var result = await _mediator.Send(new SampleOrgQuery());
            Assert.Equal("Org result for " + orgId, result);
        }
    }

    [Fact]
    public async Task OrganizationScopedRequest_WithoutOrgScope_ThrowsScopeUnresolvedException()
    {
        var context = new PlatformExecutionContext
        {
            OrganizationId = null
        };

        using (_accessor.BeginScope(context))
        {
            await Assert.ThrowsAsync<ScopeUnresolvedException>(() => _mediator.Send(new SampleOrgQuery()));
        }
    }

    [Fact]
    public async Task AnonymousScopedRequest_WithoutAnyScope_ExecutesSuccessfully()
    {
        Assert.Null(_accessor.Current);

        var result = await _mediator.Send(new SampleAnonymousCommand());

        Assert.Equal("Public success", result);
    }

    [Fact]
    public async Task InteractiveScopeProvider_BindsAtDispatchBoundary_AndDisposesAfterExecution()
    {
        var orgId = Guid.NewGuid();
        var compId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        _interactiveProvider.SetCurrentContext(new PlatformExecutionContext
        {
            OrganizationId = orgId,
            CompanyId = compId,
            BranchId = branchId
        });

        // Ambient accessor is currently null
        Assert.Null(_accessor.Current);

        var result = await _mediator.Send(new SampleBranchCommand("BoundItem"));

        Assert.Equal("Processed BoundItem at branch " + branchId, result);
        // After dispatch boundary, ambient context is reliably restored to null
        Assert.Null(_accessor.Current);
    }

    [Fact]
    public async Task InteractiveScopeProvider_DisposesOnHandlerException()
    {
        _interactiveProvider.SetCurrentContext(new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            BranchId = Guid.NewGuid()
        });

        Assert.Null(_accessor.Current);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.Send(new SampleFailingCommand()));

        // Ambient context is reliably cleaned up despite the exception
        Assert.Null(_accessor.Current);
    }

    private sealed class FakeInteractiveScopeProvider : IInteractiveScopeProvider
    {
        private IExecutionContext? _context;

        public IExecutionContext? GetCurrentContext() => _context;
        public void SetCurrentContext(IExecutionContext context) => _context = context;
        public void Clear() => _context = null;
    }
}

// Sample Requests & Handlers for testing pipeline enforcement
public sealed record SampleBranchCommand(string Name) : IRequest<string>, IRequireBranchScope;

public sealed class SampleBranchCommandHandler(IExecutionContextAccessor accessor) : IRequestHandler<SampleBranchCommand, string>
{
    public Task<string> Handle(SampleBranchCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Processed {request.Name} at branch {accessor.Current?.BranchId}");
    }
}

public sealed record SampleTargetBranchCommand(Guid TargetBranchId) : IRequest<string>, ITargetBranchScoped;

public sealed class SampleTargetBranchCommandHandler : IRequestHandler<SampleTargetBranchCommand, string>
{
    public Task<string> Handle(SampleTargetBranchCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Target branch verified");
    }
}

public sealed record SampleCompanyReportQuery : IRequest<string>, IRequireCompanyScope;

public sealed class SampleCompanyReportQueryHandler(IExecutionContextAccessor accessor) : IRequestHandler<SampleCompanyReportQuery, string>
{
    public Task<string> Handle(SampleCompanyReportQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Report for company {accessor.Current?.CompanyId}");
    }
}

public sealed record SampleTargetCompanyCommand(Guid TargetCompanyId) : IRequest<string>, ITargetCompanyScoped;

public sealed class SampleTargetCompanyCommandHandler : IRequestHandler<SampleTargetCompanyCommand, string>
{
    public Task<string> Handle(SampleTargetCompanyCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Target company verified");
    }
}

public sealed record SampleOrgQuery : IRequest<string>, IRequireOrganizationScope;

public sealed class SampleOrgQueryHandler(IExecutionContextAccessor accessor) : IRequestHandler<SampleOrgQuery, string>
{
    public Task<string> Handle(SampleOrgQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Org result for {accessor.Current?.OrganizationId}");
    }
}

public sealed record SampleAnonymousCommand : IRequest<string>, IAllowAnonymousScope;

public sealed class SampleAnonymousCommandHandler : IRequestHandler<SampleAnonymousCommand, string>
{
    public Task<string> Handle(SampleAnonymousCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Public success");
    }
}

public sealed record SampleFailingCommand : IRequest<string>, IRequireBranchScope;

public sealed class SampleFailingCommandHandler : IRequestHandler<SampleFailingCommand, string>
{
    public Task<string> Handle(SampleFailingCommand request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Simulated business error inside handler.");
    }
}
