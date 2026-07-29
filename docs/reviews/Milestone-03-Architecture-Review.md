---
title: CBOS Architecture Review Package — Milestone 3 (Platform Foundation)
prepared_for: Solution Architect (ChatGPT)
prepared_by: Senior Software Engineer (Claude Code)
date: 2026-07-27
scope_note: >
  This package covers exactly one commit — f91b1590edd4b6b312941125d2432c75b309b299,
  "Milestone 3: Platform Foundation" — the implementation work completed since the
  prior "Stabilization adjustments" commit (cb03681). If a broader review window was
  intended (e.g. including Repository Stabilization), that would require re-scoping.
verification_note: >
  Every build/test number below was reproduced via a clean rebuild (bin/obj deleted,
  dotnet build/dotnet test re-run) before this document was written, not pulled from
  memory of earlier runs. This document itself was exported without modifying,
  building, or committing any code.
---

# 1. Executive Summary

- **Milestone name:** Milestone 3 — Platform Foundation
- **Objective:** Create the runtime infrastructure every future CBOS module depends on: Configuration, Dependency Injection conventions, Application Bootstrap, Execution Context, Module Registration, Infrastructure Registration. Explicitly excluded from scope: Authentication, Login, Identity logic, Password hashing, JWT, Refresh Tokens, Claims, Permissions, Roles, Users, Organizations, database seeding, business modules, and UI.
- **Status:** Complete, as scoped. No Authentication/Identity/UI/business logic was implemented. No existing scaffolding, documentation, or architecture was modified.
- **Build status:** `Tools/Clovent.CLI/Clovent.CLI.slnx` — **FAILS overall** (3 errors), but the 3 errors are entirely inside `Clovent.CBOS.Desktop` (missing DevExpress package reference), a **pre-existing, unrelated issue** carried forward from Repository Stabilization, not introduced by this milestone. Every other project in the solution, including the two new projects, builds with **0 errors, 0 warnings**. `Tools/Clovent.CLI/Clovent.PackageManager.slnx` (unrelated solution) — 0 errors, 0 warnings, unaffected.
- **Test status:** 24 tests executed across the repository (`Clovent.Core.Tests`: 2, `Clovent.Generator.Tests`: 4, `Clovent.Platform.Tests`: 17, `Clovent.PackageManager.Tests`: 1 — the last in a separate solution). **All 24 passed, 0 failed, 0 skipped.** 17 of the 24 are new this milestone.

---

# 2. Files Changed

Scope: commit `f91b159` only (see scope note above). 28 files changed: 1 modified, 27 new, 0 deleted. 1065 lines added, 0 lines removed (confirmed via `git show --numstat HEAD`).

| Full Path | Type | Reason |
|---|---|---|
| `Tools/Clovent.CLI/Clovent.CLI.slnx` | Modified | Added `Clovent.Platform` and `Clovent.Platform.Tests` as solution members (2 lines added, 0 removed) so both are covered by solution-level build/test. |
| `src/Clovent.Platform/Clovent.Platform.csproj` | New | Project file for the new Platform Foundation library. |
| `src/Clovent.Platform/Configuration/PlatformConfiguration.cs` | New | Centralizes the required configuration source precedence (json → json.{env} → env vars → command line). |
| `src/Clovent.Platform/Configuration/OptionsRegistrationExtensions.cs` | New | `AddValidatedOptions<T>` — generic strongly-typed Options binding + DataAnnotations validation + `ValidateOnStart()`. |
| `src/Clovent.Platform/Configuration/PlatformOptions.cs` | New | Sample strongly-typed Options class demonstrating the required pattern; no hardcoded fallback values. |
| `src/Clovent.Platform/Execution/IExecutionContext.cs` | New | Read-only contract for the ambient execution context (User/Tenant/Organization/Company/Branch as identifiers; Language/Currency/TimeZone/Culture; CorrelationId/RequestId/ExecutionTimestamp). |
| `src/Clovent.Platform/Execution/PlatformExecutionContext.cs` | New | Immutable `record` default implementation of `IExecutionContext`. |
| `src/Clovent.Platform/Execution/IExecutionContextAccessor.cs` | New | Ambient accessor contract, shaped like `IHttpContextAccessor` but with no ASP.NET Core dependency. |
| `src/Clovent.Platform/Execution/ExecutionContextAccessor.cs` | New | `AsyncLocal`-backed implementation using the holder-object pattern to avoid cross-flow leakage. |
| `src/Clovent.Platform/Execution/ExecutionContextScope.cs` | New | `IDisposable` push/pop scope + `BeginScope` extension method. |
| `src/Clovent.Platform/Execution/ExecutionContextServiceCollectionExtensions.cs` | New | DI registration (`AddExecutionContextAccessor`). |
| `src/Clovent.Platform/Modules/IModule.cs` | New | Contract every future module implements to self-register. |
| `src/Clovent.Platform/Modules/ModuleRegistry.cs` | New | Read-only view over all registered `IModule` instances, resolved from the container; validates no duplicate module names. |
| `src/Clovent.Platform/Modules/ModuleServiceCollectionExtensions.cs` | New | `AddModule<TModule>(configuration)` — module self-registration entry point. |
| `src/Clovent.Platform/DependencyInjection/ApplicationServiceCollectionExtensions.cs` | New | `AddApplication()` — Platform's own Application-layer registration; establishes the naming convention. |
| `src/Clovent.Platform/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | New | `AddInfrastructure()` — Platform's own Infrastructure-layer registration. |
| `src/Clovent.Platform/DependencyInjection/PersistenceServiceCollectionExtensions.cs` | New | `AddPersistence()` — extensibility point only; Platform Foundation has no persistence of its own. |
| `src/Clovent.Platform/DependencyInjection/PlatformServiceCollectionExtensions.cs` | New | `AddPlatform()` — composes the three above into one call. |
| `src/Clovent.Platform/Bootstrap/IPersistenceInitializer.cs` | New | Extension point for a module's persistence initialization, run during bootstrap. |
| `src/Clovent.Platform/Bootstrap/IStartupTask.cs` | New | Extension point for arbitrary future startup work, run during bootstrap. |
| `src/Clovent.Platform/Bootstrap/ApplicationBootstrapper.cs` | New | The single bootstrap entry point: loads configuration, builds DI (via `HostApplicationBuilder`), registers logging/modules, runs persistence/startup-task pipelines. |
| `src/Clovent.Platform.Tests/Clovent.Platform.Tests.csproj` | New | Test project file. |
| `src/Clovent.Platform.Tests/TestSupport/TempDirectory.cs` | New | Test helper: isolated temp directory for appsettings.json fixtures. |
| `src/Clovent.Platform.Tests/TestSupport/FakeModules.cs` | New | Test-only fake `IModule`/`IPersistenceInitializer`/`IStartupTask` implementations. |
| `src/Clovent.Platform.Tests/Configuration/PlatformConfigurationTests.cs` | New | 4 tests verifying configuration source precedence. |
| `src/Clovent.Platform.Tests/Execution/ExecutionContextAccessorTests.cs` | New | 4 tests verifying ambient-context scoping, including async-flow isolation. |
| `src/Clovent.Platform.Tests/Modules/ModuleRegistryTests.cs` | New | 4 tests verifying module registration and duplicate-name detection. |
| `src/Clovent.Platform.Tests/Bootstrap/ApplicationBootstrapperTests.cs` | New | 5 tests verifying end-to-end bootstrap, startup validation, and the persistence/startup-task pipeline. |

No file outside `src/Clovent.Platform`, `src/Clovent.Platform.Tests`, and the one line in `Clovent.CLI.slnx` was touched. No file was deleted in this milestone.

---

# 3. New Projects

## Clovent.Platform

- **Project name:** Clovent.Platform
- **Purpose:** Runtime infrastructure every future module depends on — Configuration, DI conventions, Application Bootstrap, Execution Context, Module Registration.
- **Target Framework:** `net10.0`
- **Dependencies:**
  - Package: `Microsoft.Extensions.Hosting` 10.0.10
  - Package: `Microsoft.Extensions.Options.DataAnnotations` 10.0.10
  - Project references: none
- **References (referenced by):** `Clovent.Platform.Tests` only. **No other project in the repository references it yet** — it is not consumed by `Clovent.Authentication`, `Clovent.Modules.Identity`, `Clovent.CBOS.Desktop`, or `Clovent.CLI`. This is expected given the milestone's scope (nothing downstream was allowed to be touched), but it means the API has not been exercised by a real consumer, only by its own tests.

## Clovent.Platform.Tests

- **Project name:** Clovent.Platform.Tests
- **Purpose:** Unit/integration tests for all six Platform Foundation capabilities.
- **Target Framework:** `net10.0`
- **Dependencies:**
  - Project reference: `Clovent.Platform`
  - Packages: `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `coverlet.collector` 6.0.4 (versions matched to the existing `Clovent.Core.Tests`/`Clovent.PackageManager.Tests` projects for consistency)
- **References (referenced by):** none.

Both projects were added to `Tools/Clovent.CLI/Clovent.CLI.slnx` (the solution that already contained `Clovent.Authentication` and `Clovent.Modules.Identity`) as top-level entries, referenced by relative path (`../../src/Clovent.Platform/...`), matching how `Clovent.Authentication` is already referenced from that solution file.

---

# 4. Dependency Graph

## New package dependencies introduced

| Package | Version | Project | Why it exists |
|---|---|---|---|
| `Microsoft.Extensions.Hosting` | 10.0.10 | Clovent.Platform | Provides the generic host abstraction (`HostApplicationBuilder`, `IHost`, `IHostEnvironment`) used by `ApplicationBootstrapper`. Chosen specifically because it is **not** part of `Microsoft.AspNetCore.*` — it's the same host abstraction used for console apps, Worker Services, and (already, elsewhere in this repo) `Clovent.CLI` and `Clovent.CBOS.Desktop`. Also transitively brings in `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Options`, and `Microsoft.Extensions.Logging` (+ console provider), so no separate references to those were needed. |
| `Microsoft.Extensions.Options.DataAnnotations` | 10.0.10 | Clovent.Platform | Supplies `.ValidateDataAnnotations()`, used by `AddValidatedOptions<T>` for the required "validation on startup" behavior. Not included transitively by `Microsoft.Extensions.Hosting`. |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Clovent.Platform.Tests | Standard test-host SDK, version matched to existing test projects in the repo. |
| `xunit` | 2.9.3 | Clovent.Platform.Tests | Test framework, matched to existing test projects. |
| `xunit.runner.visualstudio` | 3.1.4 | Clovent.Platform.Tests | Test discovery/runner, matched to existing test projects. |
| `coverlet.collector` | 6.0.4 | Clovent.Platform.Tests | Code-coverage collector, matched to existing test projects; used to produce the coverage figures in Section 10. |

## New project-reference dependencies introduced

| From | To | Why |
|---|---|---|
| `Clovent.Platform.Tests` | `Clovent.Platform` | Test project references the code under test. |

No other project reference was added or changed anywhere in the repository this milestone. `Clovent.Platform` itself has **zero** project references — it depends only on the two NuGet packages above.

## Circular dependencies

**None identified.** `Clovent.Platform` has no outgoing project references (nothing to cycle through), and it is referenced only by its own leaf test project. Confirmed by direct inspection of every `<ProjectReference>` element in both new `.csproj` files — the graph is a single one-directional edge (`Clovent.Platform.Tests` → `Clovent.Platform`).

---

# 5. Public API Inventory

## Every public class

- `Clovent.Platform.Execution.ExecutionContextAccessor` — `AsyncLocal`-backed implementation of `IExecutionContextAccessor`.
- `Clovent.Platform.Execution.ExecutionContextScope` — `IDisposable` scope that pushes an `IExecutionContext` ambient and restores the previous value on dispose.
- `Clovent.Platform.Modules.ModuleRegistry` — read-only view over every module registered via `AddModule<TModule>()`; validates no duplicate module names at construction.
- `Clovent.Platform.Bootstrap.ApplicationBootstrapper` — the single bootstrap entry point (configuration, DI, logging, modules, persistence/startup-task pipelines).

## Every public interface

- `Clovent.Platform.Execution.IExecutionContext` — read-only ambient-context contract (User/Tenant/Organization/Company/Branch identifiers; Language/Currency/TimeZone/Culture; CorrelationId/RequestId/ExecutionTimestamp).
- `Clovent.Platform.Execution.IExecutionContextAccessor` — settable ambient accessor contract (`IExecutionContext? Current { get; set; }`).
- `Clovent.Platform.Modules.IModule` — contract every future module implements to self-register (`Name`, `RegisterServices`).
- `Clovent.Platform.Bootstrap.IPersistenceInitializer` — extension point for a module's persistence initialization, run during bootstrap.
- `Clovent.Platform.Bootstrap.IStartupTask` — extension point for arbitrary future startup work, run during bootstrap.

## Every public record

- `Clovent.Platform.Execution.PlatformExecutionContext` — immutable, `sealed record` default implementation of `IExecutionContext`; exposes `static PlatformExecutionContext Empty { get; }`.

## Every public enum

**None.** This milestone introduced zero enums (confirmed via a repository-wide search of `src/Clovent.Platform` for the `enum` keyword — no matches).

## Every public extension method

All are `public static` methods on `IServiceCollection` (or `IExecutionContextAccessor`), grouped by their containing static class:

- `Clovent.Platform.Configuration.OptionsRegistrationExtensions`
  - `AddValidatedOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string sectionName) where TOptions : class`
- `Clovent.Platform.Modules.ModuleServiceCollectionExtensions`
  - `AddModuleRegistry(this IServiceCollection services)`
  - `AddModule<TModule>(this IServiceCollection services, IConfiguration configuration) where TModule : IModule, new()`
- `Clovent.Platform.DependencyInjection.ApplicationServiceCollectionExtensions`
  - `AddApplication(this IServiceCollection services, IConfiguration configuration)`
- `Clovent.Platform.DependencyInjection.InfrastructureServiceCollectionExtensions`
  - `AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`
- `Clovent.Platform.DependencyInjection.PersistenceServiceCollectionExtensions`
  - `AddPersistence(this IServiceCollection services, IConfiguration configuration)` (currently a no-op pass-through; extensibility point only)
- `Clovent.Platform.DependencyInjection.PlatformServiceCollectionExtensions`
  - `AddPlatform(this IServiceCollection services, IConfiguration configuration)` (composes the three above)
- `Clovent.Platform.Execution.ExecutionContextServiceCollectionExtensions`
  - `AddExecutionContextAccessor(this IServiceCollection services)`
- `Clovent.Platform.Execution.ExecutionContextAccessorExtensions`
  - `BeginScope(this IExecutionContextAccessor accessor, IExecutionContext context)`

## Non-extension public static members (for completeness)

- `Clovent.Platform.Configuration.PlatformConfiguration` (static class)
  - `static IConfiguration Build(string basePath, string? environmentName = null, string[]? commandLineArgs = null)`
  - `static void Configure(IConfigurationBuilder builder, string basePath, string? environmentName = null, string[]? commandLineArgs = null)`
- `Clovent.Platform.Configuration.PlatformOptions` (sealed class, not static) — sample Options class: `EnvironmentName`, `DefaultCulture`, `DefaultTimeZone`, `DefaultCurrency` (all `required` + `[Required]`); `const string SectionName = "Platform"`.
- `Clovent.Platform.Bootstrap.ApplicationBootstrapper` non-extension members:
  - `static ApplicationBootstrapper Create(string[]? args = null, string? basePath = null, string? environmentName = null)`
  - `IConfiguration Configuration { get; }`, `IServiceCollection Services { get; }`, `IHostEnvironment Environment { get; }`
  - `ApplicationBootstrapper WithLogging(Action<ILoggingBuilder>? configureLogging = null)`
  - `ApplicationBootstrapper WithPlatform()`
  - `ApplicationBootstrapper WithModule<TModule>() where TModule : IModule, new()`
  - `IHost Build()`
  - `Task<IHost> BuildAndInitializeAsync(CancellationToken cancellationToken = default)`

**Totals:** 4 public classes, 5 public interfaces, 1 public record, 0 public enums, 8 extension-method-bearing static classes (11 extension methods total), plus 2 additional non-extension static/instance API surfaces (`PlatformConfiguration`, `PlatformOptions`) already itemized above.

*(Test-project types — `FakeModuleA`/`FakeModuleB`/`FakeModuleMarker`/etc. in `Clovent.Platform.Tests` — are test-only and intentionally excluded from this inventory; they are not part of the product API.)*

---

# 6. Architecture Decisions

Every decision below was made without further architect sign-off at the time it was made (except Decision 1, which was put to the repository owner directly before any code was written). Each is disclosed here for retroactive review.

**Decision 1 — Project location: `src/Clovent.Platform`.**
- *Reasoning:* No existing project or document designated a home for cross-cutting platform code; `Clovent.Shared` (in the internal `Tools/Clovent.CLI` tooling solution) only holds DDD primitives and lives outside the product `src/` tree.
- *Alternatives considered:* Extend `Tools/Clovent.CLI/src/Clovent.Shared` instead of creating a new project; let the repository owner specify a different name/location.
- *Documentation support:* None beforehand — this is exactly why it was put to the repository owner directly rather than assumed. (Not a unilateral decision.)

**Decision 2 — Namespace `Clovent.Platform.Execution`, not `Clovent.Platform.ExecutionContext`; concrete class `PlatformExecutionContext`, not `ExecutionContext`.**
- *Reasoning:* Avoids a same-name collision with `System.Threading.ExecutionContext`, a BCL type. A class named `ExecutionContext` inside a namespace also named `ExecutionContext` would be doubly ambiguous.
- *Alternatives considered:* Keep the literal specification vocabulary ("ExecutionContext") and accept the collision risk.
- *Documentation support:* None — the specification uses "Execution Context" as prose, not as a literal required type/namespace name. Required interpretation.

**Decision 3 — `IExecutionContextAccessor.Current` has a public setter**, deliberately shaped like ASP.NET Core's `IHttpContextAccessor`.
- *Reasoning:* Familiarity for future maintainers; matches a well-known .NET pattern.
- *Alternatives considered:* A read-only `Current { get; }` with mutation possible only through `ExecutionContextScope` (stricter encapsulation) — not chosen.
- *Documentation support:* None. Required interpretation; a real, disclosed trade-off (see Risks §8 and Questions §13.3).

**Decision 4 — `AsyncLocal<T>` + holder-object indirection** for ambient storage, rather than a bare `AsyncLocal<IExecutionContext?>`.
- *Reasoning:* Mirrors a documented, well-known pattern from ASP.NET Core's own internal `HttpContextAccessor`, avoiding a specific class of cross-async-flow leakage.
- *Alternatives considered:* Plain `AsyncLocal<IExecutionContext?>` without indirection — rejected as a known .NET leakage pitfall.
- *Documentation support:* None in CBOS docs — general .NET platform knowledge applied by interpretation, not a documented CBOS pattern.

**Decision 5 — `IExecutionContext` holds bare `Guid?` identifiers**, not domain entities or richer value objects, for User/Tenant/Organization/Company/Branch.
- *Reasoning:* User/Organization/Company/Branch domain types are explicitly out of scope this milestone; the specification asked for these fields without specifying representation.
- *Alternatives considered:* A generic `object?`-typed placeholder — rejected as strictly worse (no type safety, no clearer intent).
- *Documentation support:* None directly; consistent with "do not implement Users/Organizations," but the specific representation was interpretation.

**Decision 6 — `AddModule<TModule>()` requires an `IConfiguration` parameter**, deviating from the specification's zero-argument illustration (`services.AddModule<TModule>();`).
- *Reasoning:* `IModule.RegisterServices` needs `IConfiguration` for realistic module needs (e.g. a connection string) at registration time.
- *Alternatives considered:* Resolve `IConfiguration` from a temporary `BuildServiceProvider()` call during registration — rejected as an unreliable, discouraged pattern (a provider built before the real container is finalized can silently diverge from the real one).
- *Documentation support:* The specification explicitly shows zero arguments; this is a **direct, acknowledged deviation**, not merely an interpretation. See also Section 7.

**Decision 7 — `ModuleRegistry` validates duplicate names lazily**, at first resolution from DI, rather than eagerly at each `AddModule<T>()` call.
- *Reasoning:* Eager validation would require the same mid-registration provider-building problem as Decision 6.
- *Alternatives considered:* A static/shared mutable name-tracking set updated at `AddModule<T>()` call time — rejected because static shared state would leak across unrelated `IServiceCollection` instances (e.g. across parallel test runs in the same process), a real correctness bug.
- *Documentation support:* None; required interpretation.

**Decision 8 — `ApplicationBootstrapper` is built on `Microsoft.Extensions.Hosting.HostApplicationBuilder`** rather than a fully bespoke, dependency-free bootstrapper.
- *Reasoning:* Already used elsewhere in this repo (`Clovent.CLI`, `Clovent.CBOS.Desktop`); provides host lifecycle/graceful-shutdown semantics for free that a future web host would likely want too.
- *Alternatives considered:* Hand-roll `IServiceCollection` + `IConfigurationBuilder` + manual `BuildServiceProvider()` with zero `Microsoft.Extensions.Hosting` dependency — rejected as reinventing already-adopted functionality.
- *Documentation support:* The specification's instruction is "do not couple it to ASP.NET" — interpreted narrowly as "no `Microsoft.AspNetCore.*` package," not "no `Microsoft.Extensions.Hosting` package." **This interpretation is not confirmed by any document and should be explicitly ratified or corrected** (see Questions §13.4).

**Decision 9 — Test project at `src/Clovent.Platform.Tests`**, not `Tools/Clovent.CLI/tests/Clovent.Platform.Tests` (the existing convention for `Clovent.Core.Tests`/`Clovent.Generator.Tests`) and not a new repo-root `tests/` folder.
- *Reasoning:* `Clovent.Platform` itself lives in `src/`, not inside the internal CLI tooling; tests placed alongside the code they test.
- *Alternatives considered:* Follow the `Tools/Clovent.CLI/tests/` convention exactly — rejected because that convention is specifically for the CLI tooling's own tests, and `Clovent.Platform` is product code.
- *Documentation support:* None; required interpretation, and a genuinely different structural choice than what already exists elsewhere in the repo (see Questions §13.5).

**Decision 10 — `IPersistenceInitializer` and `IStartupTask` as two separate, structurally-identical interfaces**, mirroring the specification's two separate bullet points ("Initializing persistence" / "Future startup tasks") literally rather than merging them into one.
- *Reasoning:* Literal fidelity to the specification's own itemization.
- *Alternatives considered:* A single unified `IStartupTask` covering both — not chosen.
- *Documentation support:* Direct — the specification lists them as two distinct bootstrap responsibilities.

**Decision 11 — `PlatformOptions` uses `required` properties + `[Required]`, with no C# literal default values**, interpreting "no hardcoded values" as strictly as possible.
- *Reasoning:* Any host missing the "Platform" configuration section fails to start rather than silently running with a fallback value.
- *Alternatives considered:* Give sensible defaults (e.g. `"UTC"`) — rejected in favor of the strict reading.
- *Documentation support:* Direct ("No hardcoded values" is explicit in the specification), but the strict all-or-nothing consequence (hard startup failure with zero graceful degradation) is interpretation of how far that instruction extends — flagged in Risks and Questions.

---

# 7. Deviations from Specification

1. **`services.AddModule<TModule>();` (specification's illustrated zero-argument call) vs. implemented `services.AddModule<TModule>(configuration)`.** Reason: `IModule.RegisterServices` needs `IConfiguration` to do anything realistic (e.g. a connection string at registration time); there is no reliable way to supply it later without either an anti-pattern (temporary provider mid-registration) or a static configuration locator (judged worse). This is the most significant deviation in this milestone and needs an explicit ruling (Questions §13.1).

2. **Namespace/type naming: "Execution Context" (specification prose) → `Clovent.Platform.Execution` namespace / `PlatformExecutionContext` class**, not `ExecutionContext`. Reason: collision avoidance with `System.Threading.ExecutionContext`. Naming-only, no behavioral difference.

3. **Bootstrap responsibility ordering not specified** — "Initializing persistence" and "Future startup tasks" are listed as two separate bullets with no stated order. Implemented as sequential, persistence-first, inside `BuildAndInitializeAsync()`. This is an assumption (schema-before-other-work is the conventional order) rather than a confirmed requirement.

4. **"Every project should expose `AddApplication()`/`AddInfrastructure()`/`AddPersistence()`"** — only `Clovent.Platform` exposes these this milestone. `Clovent.Authentication`, `Clovent.Modules.Identity`, and `Clovent.CBOS.Desktop` were **not** retrofitted with these methods, because doing so would mean editing Authentication/Identity/UI code, explicitly forbidden this milestone. This is a deliberate, scope-driven partial fulfillment of the literal instruction ("every project"), not an oversight.

5. **Command-line configuration format** — implemented via `Microsoft.Extensions.Configuration.CommandLine`'s standard `--Key=Value` / `--Key value` convention. The specification says "Command Line" as a source without specifying the argument format; this is the .NET-conventional default, not something explicitly written down for CBOS.

6. **No real host calls `ApplicationBootstrapper`.** The specification's stated success criterion is "the runtime can bootstrap itself" — this is verified only through the test suite (`ApplicationBootstrapperTests`, 5 tests including a full `Create → WithLogging → WithPlatform → WithModule → Build/StartAsync` path), not through an actual running process, since wiring it into `Clovent.CBOS.Desktop` (the only existing host) is UI work and explicitly out of scope.

---

# 8. Risks

## Technical risks

- `IExecutionContextAccessor.Current`'s public setter means any code — not just `ExecutionContextScope` — can mutate ambient state directly and forget to restore the previous value, leaking context across a logical operation. Nothing in the current design prevents this.
- Every host that boots via `ApplicationBootstrapper.WithPlatform()` **will crash at `host.StartAsync()`** (`OptionsValidationException`) unless its configuration supplies a complete "Platform" section (`EnvironmentName`, `DefaultCulture`, `DefaultTimeZone`, `DefaultCurrency`). This is correct per the "validate on startup" requirement but is a hard, unconditional failure mode with no fallback — confirmed by a passing test (`StartAsync_WithIncompletePlatformConfiguration_FailsValidationOnStart`), so this is proven behavior, not a hypothetical.
- `ApplicationBootstrapper.Create()` clears `HostApplicationBuilder`'s default configuration sources and replaces them with `PlatformConfiguration.Configure`. This means CBOS's bootstrap behaves differently from a "vanilla" generic host in ways a .NET-experienced maintainer might not expect (e.g. no automatic user-secrets in Development) unless they read the source.
- The whole toolchain runs on a **preview** .NET SDK: `dotnet --version` reports `10.0.400-preview.0.26322.102`. Every build in the repository (not just this milestone) emits `NETSDK1057` for this reason. This predates this milestone but applies directly to the new project too.

## Maintainability risks

- `IPersistenceInitializer` and `IStartupTask` are structurally identical (`Task X(CancellationToken)`), differing only in name and intended timing. A future contributor may be unsure which to implement for a given task since nothing enforces the distinction beyond the two sequential loops in `BuildAndInitializeAsync`.
- Platform Foundation has **zero real consumers** — `Clovent.Authentication` and `Clovent.Modules.Identity` do not reference it. Its design has been validated only against its own test suite, not against a real module's actual needs. Problems that would only surface under real usage have not been found yet.
- Six separate `*ServiceCollectionExtensions` static classes exist for a currently very small amount of logic — `AddPersistence()` in particular does nothing at all yet. This ceremony is a direct, deliberate consequence of matching the specification's literal naming convention, but it's worth naming as overhead relative to current functionality.

## Performance risks

None identified specific to this milestone. No hot-path or high-throughput code was introduced; DI registration and ambient-context access are the only runtime logic, and both use patterns (`AsyncLocal`, container resolution) that .NET's own frameworks use at scale without issue.

## Future scalability risks

- Whether `IExecutionContext`'s flat `Guid?`-identifier shape (no richer tenant/permission/organization resolution) remains adequate once real multi-tenant business rules exist is genuinely uncertain — there is no basis to assess this further without knowing the intended shape of the future Organization/Company/Branch modules. Flagged as a question, not asserted as a problem.

---

# 9. Build Verification

Reproduced via a clean rebuild (deleted `bin`/`obj` for both new projects, then `dotnet build`) immediately before this document was prepared.

**`dotnet --version`:** `10.0.400-preview.0.26322.102`

**Solutions built:**

| Solution | Result | Errors | Warnings |
|---|---|---|---|
| `Tools/Clovent.CLI/Clovent.CLI.slnx` | **Build FAILED** | 3 | 0 |
| `Tools/Clovent.CLI/Clovent.PackageManager.slnx` | Build succeeded | 0 | 0 |

**Exact error output (`Clovent.CBOS.Desktop.csproj`, the only failing project):**

```
D:\Clovent Business Operating System\Tools\Clovent.CLI\Clovent.CBOS.Desktop\Program.cs(3,7): error CS0246: The type or namespace name 'DevExpress' could not be found (are you missing a using directive or an assembly reference?) [D:\Clovent Business Operating System\Tools\Clovent.CLI\Clovent.CBOS.Desktop\Clovent.CBOS.Desktop.csproj]
D:\Clovent Business Operating System\Tools\Clovent.CLI\Clovent.CBOS.Desktop\Program.cs(4,7): error CS0246: The type or namespace name 'DevExpress' could not be found (are you missing a using directive or an assembly reference?) [D:\Clovent Business Operating System\Tools\Clovent.CLI\Clovent.CBOS.Desktop\Clovent.CBOS.Desktop.csproj]
D:\Clovent Business Operating System\Tools\Clovent.CLI\Clovent.CBOS.Desktop\Program.cs(5,7): error CS0246: The type or namespace name 'DevExpress' could not be found (are you missing a using directive or an assembly reference?) [D:\Clovent Business Operating System\Tools\Clovent.CLI\Clovent.CBOS.Desktop\Clovent.CBOS.Desktop.csproj]
    0 Warning(s)
    3 Error(s)
```

This is **identical** to the error recorded at the original Repository Stabilization baseline and after the "re-add Clovent.CBOS.Desktop" adjustment — it is not new, and Platform Foundation code has no relationship to `Clovent.CBOS.Desktop` (nothing in this milestone touches or references it). Root cause remains: no DevExpress package feed is configured in this repository.

**Projects built successfully (13 of 14 in `Clovent.CLI.slnx`):**
`Clovent.Configuration`, `Clovent.Core`, `Clovent.Shared`, `Clovent.Authentication`, **`Clovent.Platform`**, `Clovent.Documents`, `Clovent.Templates`, `Clovent.Modules.Identity`, `Clovent.Core.Tests`, `Clovent.Generator`, **`Clovent.Platform.Tests`**, `Clovent.Generator.Tests`, `Clovent.CLI` — all 0 errors, 0 warnings each.

**Warnings:** 0 (excluding informational `NETSDK1057` "preview SDK" messages, which are not warnings and appear for every project in the solution, unrelated to this milestone — e.g. `message NETSDK1057: You are using a preview version of .NET.`).

---

# 10. Test Verification

**Tests added this milestone:** 17, all in `src/Clovent.Platform.Tests`:

| Test class | Count | Verifies |
|---|---|---|
| `PlatformConfigurationTests` | 4 | Config source precedence: base json only; env-specific json overrides base; env var overrides json; command line overrides everything. |
| `ExecutionContextAccessorTests` | 4 | Default null state; scope set/restore; nested scope restore; ambient isolation across two parallel async flows. |
| `ModuleRegistryTests` | 4 | Module registration discoverable via registry; module's own services registered; multiple modules coexist; duplicate name throws. |
| `ApplicationBootstrapperTests` | 5 | Complete config resolves platform services; incomplete config fails `StartAsync` with `OptionsValidationException`; complete config succeeds `StartAsync`; `WithModule<T>()` registers correctly; `BuildAndInitializeAsync` runs both persistence-initializer and startup-task pipelines. |

**Tests executed (full repository, exact run performed for this document):**

```
Passed!  - Failed: 0, Passed: 2,  Skipped: 0, Total: 2,  Duration: 78 ms  - Clovent.Core.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed: 4,  Skipped: 0, Total: 4,  Duration: 335 ms - Clovent.Generator.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed: 17, Skipped: 0, Total: 17, Duration: 3 s   - Clovent.Platform.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed: 1,  Skipped: 0, Total: 1,  Duration: 7 ms  - Clovent.PackageManager.Tests.dll (net10.0)  [separate solution]
```

| Project | Solution | Passed | Failed | Skipped |
|---|---|---|---|---|
| `Clovent.Core.Tests` | Clovent.CLI.slnx | 2 | 0 | 0 |
| `Clovent.Generator.Tests` | Clovent.CLI.slnx | 4 | 0 | 0 |
| `Clovent.Platform.Tests` | Clovent.CLI.slnx | 17 | 0 | 0 |
| `Clovent.PackageManager.Tests` | Clovent.PackageManager.slnx | 1 | 0 | 0 |
| **Total** | | **24** | **0** | **0** |

**Coverage achieved** (via `dotnet test --collect:"XPlat Code Coverage"` against `Clovent.Platform.Tests`, cobertura report):

Overall for `Clovent.Platform`: **91.9% line coverage** (159/173 lines), **90.9% branch coverage** (20/22 branches).

Per-class, classes below 100%:

| Class | Line rate | Branch rate | Why |
|---|---|---|---|
| `PlatformExecutionContext` | 30.8% | 100% | Almost entirely compiler-generated `record` members (`Equals`, `GetHashCode`, `ToString`, `PrintMembers`, `Deconstruct`, `<Clone>$`) that no test exercises — not untested application logic. Every property is exercised by usage in other tests. |
| `ExecutionContextScope` | 84.6% | 50% | The double-dispose guard (`if (_disposed) return;`) is not exercised — no test calls `Dispose()` twice on the same scope. |
| `ApplicationBootstrapper` | 92.3% | 50% | The optional `configureLogging` callback parameter on `WithLogging(...)` is never exercised with a non-null delegate by any test — only the parameterless path is covered. |

All other classes (`ModuleRegistry`, `ModuleServiceCollectionExtensions`, `ExecutionContextAccessor`, `ExecutionContextAccessorExtensions`, `ExecutionContextServiceCollectionExtensions`, `ApplicationServiceCollectionExtensions`, `InfrastructureServiceCollectionExtensions`, `PersistenceServiceCollectionExtensions`, `PlatformServiceCollectionExtensions`, `OptionsRegistrationExtensions`, `PlatformConfiguration`, `PlatformOptions`): 100% line and branch coverage.

**Failures:** none, in any run performed for this document.

---

# 11. Known Issues

Nothing below is hidden or minimized:

1. **`Clovent.CBOS.Desktop` does not build** (3× `CS0246: DevExpress`). Pre-existing, unrelated to this milestone, root cause is a missing DevExpress package feed — unresolved since Repository Stabilization.
2. **`Clovent.Platform` has no real consumers yet.** Not referenced by `Clovent.Authentication`, `Clovent.Modules.Identity`, or `Clovent.CBOS.Desktop`. Expected given scope, but means the design is unvalidated against real usage.
3. **`IExecutionContextAccessor.Current` is publicly settable**, allowing ambient-state mutation outside the `ExecutionContextScope` discipline (see Risks §8).
4. **Hard startup failure on incomplete configuration** — any host using `AddPlatform()`/`AddApplication()` will throw `OptionsValidationException` at `StartAsync()` if the "Platform" configuration section is missing or incomplete. This is intended behavior per the "validate on startup" requirement, but it is unconditional (no partial/degraded startup path).
5. **`PlatformExecutionContext` shows 30.8% line coverage** in the raw coverage report — clarified in Section 10 as almost entirely compiler-generated record boilerplate, not untested logic, but flagged here so the raw number isn't misread later out of that context.
6. **`ExecutionContextScope`'s double-dispose guard is untested.**
7. **`ApplicationBootstrapper.WithLogging`'s optional callback parameter is untested** with a non-null argument.
8. **No running host currently calls `ApplicationBootstrapper`.** "The runtime can bootstrap itself" is proven only via the test suite, not via a real running process.
9. **The whole solution runs on a preview .NET SDK** (`10.0.400-preview.0.26322.102`), predating this milestone but applicable to every new file added.
10. **`IPersistenceInitializer` and `IStartupTask` are structurally duplicate interfaces** — flagged as a design question in Sections 6/13, not a defect, but real duplication.

---

# 12. Recommendations

*(Not implemented — recommendations only.)*

1. Restrict `IExecutionContextAccessor.Current`'s setter (e.g. an internal-only setter, mutation exclusively via `ExecutionContextScope`) to close the ambient-state-leak gap in Known Issue 3.
2. Document, near `PlatformConfiguration`/`ApplicationBootstrapper.Create`, that the generic host's default configuration sources are intentionally cleared and replaced — so a maintainer familiar with vanilla `HostApplicationBuilder` behavior isn't surprised.
3. Once a real module (starting with Identity) exists, revisit whether `IPersistenceInitializer` and `IStartupTask` should stay as two interfaces or be unified — real usage will clarify this better than speculation can.
4. Add a test (or a small non-UI console sample host) that actually calls `ApplicationBootstrapper` end-to-end in a running process, to move "the runtime can bootstrap itself" from "proven by unit test" to "proven by running the thing."
5. Resolve the DevExpress package feed configuration for `Clovent.CBOS.Desktop` — unrelated to Platform Foundation, but it's the one remaining build blocker for a fully green solution build.
6. When Organization/Company/Branch/User are eventually designed, explicitly revisit `IExecutionContext`'s bare-`Guid?` shape before other modules start depending on it, since changing it later will be a breaking change for every consumer by then.

---

# 13. Questions for the Solution Architect

1. Is `services.AddModule<TModule>(configuration)` (requiring `IConfiguration`) an acceptable permanent deviation from the specification's illustrated `services.AddModule<TModule>();`, or is a genuinely zero-argument form required — and if so, how should a module obtain configuration during `RegisterServices`?
2. Is renaming away from the literal "ExecutionContext" (to `Clovent.Platform.Execution` namespace / `PlatformExecutionContext` class, avoiding the `System.Threading.ExecutionContext` collision) acceptable, or should the literal name be preserved some other way?
3. Should `IExecutionContextAccessor.Current` remain publicly settable (current implementation, familiar `IHttpContextAccessor` shape) or be restricted to mutation only via `ExecutionContextScope`?
4. Is `Microsoft.Extensions.Hosting.HostApplicationBuilder` an acceptable interpretation of "support future desktop and web hosts, do not couple to ASP.NET," or was a dependency-free custom bootstrapper intended?
5. Is `src/Clovent.Platform.Tests` the correct convention for this and future `src/`-level test projects, versus following the existing `Tools/Clovent.CLI/tests/` pattern or a new repo-root `tests/` folder?
6. Is the identifiers-only shape of `IExecutionContext` (bare `Guid?` for User/Tenant/Organization/Company/Branch) the intended long-term design, or should it be expected to change once Identity/Organization modules exist?
7. Should `IPersistenceInitializer` and `IStartupTask` remain two separate interfaces, or be unified now before any real module implements either?
8. Does "no hardcoded values" extend to disallowing default values on every Options property (as implemented — hard startup failure if any value is missing), or was a default-with-override model intended for some settings?
9. Is verification via the test suite sufficient to claim "the runtime can bootstrap itself" for this milestone's success criteria, or is wiring `ApplicationBootstrapper` into an actual running host (even non-UI) required before this milestone is considered fully proven?

---

*End of document. No code was written or modified while preparing this export (confirmed via `git status`/`git diff --stat` immediately before and after). No commit was made.*
