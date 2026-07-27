---
title: Platform Foundation Reference
type: Architecture
status: Permanent Platform reference
created: 2026-07-27
updated: Milestone 3.1
applies_to: src/Clovent.Platform
---

# Platform Foundation Reference

This document describes `src/Clovent.Platform` as it exists today: what it is for, how it is organized, and how every future CBOS module is expected to build on it. It is the permanent reference for Platform Foundation — update it whenever Platform Foundation's public API or structure changes, rather than letting this document drift out of date.

---

## 1. Purpose

Platform Foundation is the runtime infrastructure every future CBOS module depends on. It answers six questions the same way for every module, so no module has to answer them itself:

1. **Configuration** — where do settings come from, and in what order do they override each other?
2. **Dependency Injection** — what method name does a project expose to register its services, and in what layer?
3. **Application Bootstrap** — what is the one sequence every host follows to start up?
4. **Execution Context** — how does code anywhere in the call stack find out who/what is currently executing?
5. **Module Registration** — how does a host add a module without editing a central list?
6. **Infrastructure Registration** — how does infrastructure code register itself without a host reaching in and wiring it up directly?

Platform Foundation deliberately implements **none** of Authentication, Identity, Users, Organizations, UI, or any business module. It has no database of its own. Its only job is to be the thing every one of those things is eventually built on top of.

---

## 2. Folder Structure

```
src/Clovent.Platform/
  Clovent.Platform.csproj
  Bootstrap/
    ApplicationBootstrapper.cs        - the single bootstrap entry point
    IPersistenceInitializer.cs        - extension point: module persistence init
    IStartupTask.cs                   - extension point: arbitrary startup work
  Configuration/
    PlatformConfiguration.cs          - centralizes configuration source precedence
    OptionsRegistrationExtensions.cs  - AddValidatedOptions<T>
    PlatformOptions.cs                - sample strongly-typed Options class
  DependencyInjection/
    ApplicationServiceCollectionExtensions.cs      - AddApplication()
    InfrastructureServiceCollectionExtensions.cs   - AddInfrastructure()
    PersistenceServiceCollectionExtensions.cs      - AddPersistence()
    PlatformServiceCollectionExtensions.cs         - AddPlatform() (composes the three above)
  Execution/
    IExecutionContext.cs                       - read-only ambient-context contract
    PlatformExecutionContext.cs                - default immutable implementation
    IExecutionContextAccessor.cs               - read-only ambient accessor contract
    ExecutionContextAccessor.cs                - AsyncLocal-backed implementation
    ExecutionContextScope.cs                   - the only supported way to mutate the ambient context
    ExecutionContextServiceCollectionExtensions.cs - AddExecutionContextAccessor()
  Modules/
    IModule.cs                          - contract every module implements
    ModuleRegistry.cs                   - read-only view over registered modules
    ModuleServiceCollectionExtensions.cs - AddModule<TModule>()
```

`src/Clovent.Platform.Tests` (sibling project) mirrors this structure one-for-one: `Bootstrap/`, `Configuration/`, `Execution/`, `Modules/`, plus `TestSupport/` for shared test fixtures.

Each folder corresponds to exactly one of the six capabilities in Section 1 — there is no folder that mixes concerns, and no capability is split across folders.

---

## 3. Dependency Direction

`Clovent.Platform` has **zero project references** — it depends only on two NuGet packages:

- `Microsoft.Extensions.Hosting` (brings in Configuration, DI, Options, and Logging transitively)
- `Microsoft.Extensions.Options.DataAnnotations` (for `.ValidateDataAnnotations()`)

Nothing in `Clovent.Platform` references `Microsoft.AspNetCore.*`. This is deliberate: Platform Foundation must work for a desktop host today and a web host later, without assuming either.

**Internal dependency direction** (within `Clovent.Platform` itself, folder → folder):

```
Bootstrap  ──depends on──▶  DependencyInjection, Configuration, Modules
DependencyInjection  ──depends on──▶  Configuration, Execution, Modules
Modules  ──depends on──▶  (nothing else in Clovent.Platform)
Execution  ──depends on──▶  (nothing else in Clovent.Platform)
Configuration  ──depends on──▶  (nothing else in Clovent.Platform)
```

`Modules`, `Execution`, and `Configuration` are the three leaf capabilities — they don't depend on each other or on `Bootstrap`/`DependencyInjection`. `DependencyInjection` composes them. `Bootstrap` sits on top of everything, as the single entry point. There is no cycle anywhere in this graph.

**Expected direction for future consumers:** every future module (starting with Identity) is expected to add a `ProjectReference` to `Clovent.Platform` and depend on its interfaces (`IModule`, `IExecutionContext`, `IExecutionContextAccessor`, `IPersistenceInitializer`, `IStartupTask`) — never the reverse. `Clovent.Platform` must never reference a module, or it stops being foundational.

As of Milestone 3.1, `Clovent.Platform` has exactly one consumer: `Clovent.Platform.Tests`. No product module references it yet.

---

## 4. Configuration

`Clovent.Platform.Configuration.PlatformConfiguration` centralizes the source precedence every host uses, so no two hosts build their configuration differently:

1. `appsettings.json`
2. `appsettings.{environmentName}.json` (only if an environment name is supplied)
3. Environment variables
4. Command-line arguments (only if any are supplied)

Each source overrides the same key from a source earlier in the list — this is `Microsoft.Extensions.Configuration`'s normal last-source-wins behavior, applied in a fixed, documented order rather than left to each host to decide.

Two ways to use it:

- **`PlatformConfiguration.Build(basePath, environmentName, commandLineArgs)`** — builds a standalone `IConfiguration`, for use outside a full bootstrap sequence (e.g. a design-time tool, or a unit test).
- **`PlatformConfiguration.Configure(builder, basePath, environmentName, commandLineArgs)`** — applies the same sources to an existing `IConfigurationBuilder` in place. `ApplicationBootstrapper.Create` uses this form: it clears the generic host's own default configuration sources and replaces them with this, so CBOS's configuration behavior is the same regardless of host type, not whatever `HostApplicationBuilder` happens to default to.

**Strongly-typed Options and startup validation:** `Clovent.Platform.Configuration.OptionsRegistrationExtensions.AddValidatedOptions<TOptions>(services, configuration, sectionName)` is the one way any Options class — Platform's own or a future module's — should be registered:

```csharp
services.AddValidatedOptions<PlatformOptions>(configuration, PlatformOptions.SectionName);
```

This binds the section, validates it against the class's `DataAnnotations` attributes (`[Required]`, etc.), and calls `.ValidateOnStart()` — so a host with missing or invalid configuration fails at `host.StartAsync()`, before it does anything else, rather than the first time some unrelated code happens to read the option.

`PlatformOptions` is the reference example: every property is `required` plus `[Required]`, and there are **no hardcoded fallback values** anywhere in the class. A host that doesn't supply a complete `"Platform"` configuration section will not start.

---

## 5. Module Registration

`Clovent.Platform.Modules.IModule` is the contract every future module implements:

```csharp
public interface IModule
{
    string Name { get; }
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
```

Adding a module to a host is always exactly one call:

```csharp
services.AddModule<MyModule>(configuration);
```

which registers the module instance itself as an `IModule` singleton and immediately invokes `RegisterServices`. There is no switch statement and no manually-maintained list of module types anywhere in Platform Foundation — the DI container's own registration *is* the list.

`ModuleRegistry` is the read-only view over that list: it takes `IEnumerable<IModule>` as a constructor dependency (resolved by the container from everything registered via `AddModule<T>()`) and exposes `RegisteredModules` and `IsRegistered(name)`. Duplicate module names (case-insensitive) throw `InvalidOperationException` the first time `ModuleRegistry` is resolved — not at each `AddModule<T>()` call, since building a temporary container mid-registration to validate eagerly would be unreliable. This means a duplicate-name mistake is only caught once something actually resolves `ModuleRegistry` (which `ApplicationBootstrapper.WithPlatform()` guarantees happens during startup, via `AddApplication()`).

---

## 6. Bootstrap Flow

`Clovent.Platform.Bootstrap.ApplicationBootstrapper` is the single entry point every host is expected to start from. It is a thin, fluent wrapper around `Microsoft.Extensions.Hosting.HostApplicationBuilder` — the generic host, not `Microsoft.AspNetCore.*` — so the same sequence works for a console tool, a desktop app, or (via a thin adapter later) a web host.

```csharp
using var host = ApplicationBootstrapper
    .Create(args)                  // 1. load configuration
    .WithLogging()                 // 2. register logging
    .WithPlatform()                // 3. register Platform Foundation itself
    .WithModule<IdentityModule>()  // 4. register each module
    .WithModule<RestaurantPosModule>()
    .Build();                      // 5. finalize the DI container

await host.StartAsync();
```

or, for a host with modules that need persistence initialization or other startup work:

```csharp
using var host = await ApplicationBootstrapper
    .Create(args)
    .WithLogging()
    .WithPlatform()
    .WithModule<IdentityModule>()
    .BuildAndInitializeAsync();    // finalize, then run startup pipelines
```

Step by step:

1. **`Create(args, basePath, environmentName)`** — loads configuration per Section 4.
2. **`WithLogging(configureLogging)`** — clears the host's default logging providers, applies the `"Logging"` configuration section, adds a console provider, then lets the host add anything else via the optional callback.
3. **`WithPlatform()`** — calls `AddPlatform(configuration)`, which is `AddApplication()` → `AddInfrastructure()` → `AddPersistence()` in that order (see Section 7).
4. **`WithModule<TModule>()`** — one call per module, as described in Section 5.
5. **`Build()`** or **`BuildAndInitializeAsync()`** — finalizes the container. `BuildAndInitializeAsync` additionally resolves every registered `IPersistenceInitializer` and runs each `InitializeAsync` (in registration order), then every registered `IStartupTask` and runs each `ExecuteAsync` (in registration order) — persistence first, so startup tasks can assume schema is ready.

`IPersistenceInitializer` and `IStartupTask` are extension points only — Platform Foundation registers none of either. A future Identity module's migration-based persistence initialization plugs in by registering its own `IPersistenceInitializer` from within its own `AddPersistence()`; no change to `ApplicationBootstrapper` is ever needed for a new module to participate.

---

## 7. Dependency Injection Convention

Every project in CBOS — Platform Foundation and every future module — is expected to expose registration extension methods with these exact names, on `IServiceCollection`:

- **`AddApplication(services, configuration)`** — Application-layer services (Options, orchestration, module registry-type concerns).
- **`AddInfrastructure(services, configuration)`** — Infrastructure-layer services (accessors, external service clients, etc.).
- **`AddPersistence(services, configuration)`** — Persistence-layer services (`DbContext`, repositories, `IPersistenceInitializer` implementations).

No project should ever wire its own services directly from a host's `Program.cs`; a host calls only the project's `Add*` methods. Platform Foundation composes its own three into `AddPlatform(services, configuration)`, which `ApplicationBootstrapper.WithPlatform()` calls — a pattern any module is free to mirror with its own composing method.

As of Milestone 3.1, only `Clovent.Platform` itself follows this convention; no existing project (`Clovent.Authentication`, `Clovent.Modules.Identity`, `Clovent.CBOS.Desktop`) has been retrofitted with it, since doing so would mean editing Authentication/Identity/UI code, which is out of scope for Platform Foundation milestones.

---

## 8. Execution Context

`IExecutionContext` is a read-only snapshot of "who/what is executing right now": `UserId`, `TenantId`, `OrganizationId`, `CompanyId`, `BranchId` (all `Guid?` identifiers — Platform Foundation defines no domain entities), `Language`, `Currency`, `TimeZone`, `Culture`, plus `CorrelationId`, `RequestId`, and `ExecutionTimestamp`. `PlatformExecutionContext` is the default immutable `record` implementation; use `with` expressions to derive a variant of an existing context.

**Reading** the ambient context is available to any code holding `IExecutionContextAccessor`:

```csharp
var userId = executionContextAccessor.Current?.UserId;
```

**Mutating** it is deliberately not part of that interface. `IExecutionContextAccessor.Current` has only a getter. The only supported way to change what it reports is `ExecutionContextScope`, entered via the `BeginScope` extension method:

```csharp
using (executionContextAccessor.BeginScope(new PlatformExecutionContext { UserId = userId }))
{
    // IExecutionContextAccessor.Current is this context for the duration of this block,
    // including everything awaited inside it.
}
// automatically restored to whatever it was before the scope began
```

Internally, `ExecutionContextAccessor` (the built-in, singleton-registered implementation) stores the ambient value in a `System.Threading.AsyncLocal<T>`, wrapped in a holder object — the same pattern ASP.NET Core's own `HttpContextAccessor` uses internally, so that clearing the value in one async flow never affects a value already captured by a sibling flow that branched off earlier. This preserves correct async-flow isolation: two logically concurrent operations (e.g. two requests handled in parallel) each see only their own context, never each other's, even though both ultimately run through the same static `AsyncLocal` storage.

`ExecutionContextScope`'s mutation is only reachable through the concrete `ExecutionContextAccessor` type (an `internal` setter on `Current`), not through the public `IExecutionContextAccessor` interface — see [PlatformVersioning.md](PlatformVersioning.md) and the Milestone 3.1 review package for why this encapsulation exists.

---

## 9. Extension Points

Everything a future module is expected to plug into, without modifying Platform Foundation itself:

| Extension point | Interface | Where it's discovered |
|---|---|---|
| A module | `IModule` | `AddModule<TModule>()`, surfaced via `ModuleRegistry` |
| Persistence startup work | `IPersistenceInitializer` | Registered via DI inside a module's `AddPersistence()`; run by `ApplicationBootstrapper.BuildAndInitializeAsync()` |
| Arbitrary startup work | `IStartupTask` | Registered via DI inside any module's registration; run by `ApplicationBootstrapper.BuildAndInitializeAsync()`, after persistence initializers |
| A module's own Options | any class | Bind via `AddValidatedOptions<TOptions>()` inside the module's `AddApplication()` |
| A module's own DI registration | — | `AddApplication()`/`AddInfrastructure()`/`AddPersistence()` per Section 7 |

In every case, the discovery mechanism is DI container resolution (`GetServices<T>()` / constructor-injected `IEnumerable<T>`) — never a switch statement, a hardcoded list, or a direct reference from Platform Foundation to a module.

---

## 10. Design Principles

These are the principles this codebase actually follows today, not aspirational goals:

1. **Host-agnostic.** No `Microsoft.AspNetCore.*` reference anywhere in `Clovent.Platform`. Everything is built on `Microsoft.Extensions.*` abstractions so a desktop host and a future web host use the same Platform Foundation the same way.
2. **Discovery over registration lists.** Modules, persistence initializers, and startup tasks are all found by asking the DI container "give me everything registered as `T`" - never by a maintained list or a switch statement.
3. **Fail fast, not silently.** Configuration is validated at host startup (`ValidateOnStart()`), not lazily the first time some option is read. There are no hardcoded fallback values for required settings.
4. **One way to do each thing.** There is exactly one bootstrap entry point (`ApplicationBootstrapper`), one way to mutate the ambient execution context (`ExecutionContextScope`), and one naming convention for DI registration (`AddApplication`/`AddInfrastructure`/`AddPersistence`) - not several competing patterns a module author has to choose between.
5. **Read access is open; mutation is controlled.** `IExecutionContext`/`IExecutionContextAccessor` are read-only by design. Anything that needs to observe ambient state can depend on the interface freely; only the one sanctioned mechanism (`ExecutionContextScope`) can change it.
6. **Zero business logic.** Platform Foundation has no Users, no Organizations, no Authentication, no persistence of its own. Every business concept is represented, if at all, as an opaque identifier (`Guid?`) - never a domain entity - so Platform Foundation never has to change when a business module's domain model changes.
