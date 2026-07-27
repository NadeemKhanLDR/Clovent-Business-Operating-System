---
title: Platform Foundation API Evolution Policy
type: Architecture
status: Documentation only - no implementation
created: 2026-07-27
updated: Milestone 3.1
applies_to: src/Clovent.Platform
---

# Platform Foundation API Evolution Policy

This document defines how the public API of `Clovent.Platform` (Bootstrap, Configuration, Execution, Modules, DependencyInjection) is expected to evolve as more of CBOS is built on top of it. It is policy, not implementation — no code in this milestone enforces anything written here; it exists so that future changes to Platform Foundation are made consistently rather than ad hoc.

**Current state, stated plainly:** `Clovent.Platform.csproj` does not currently set a `<Version>`/`<PackageVersion>`, is not packaged as a NuGet package, and has no CI/release pipeline. This document describes the policy that should govern versioning *once* Platform Foundation has real consumers (starting with the Identity module) and/or is packaged - it does not claim any of that machinery exists yet.

---

## 1. Public API Stability

Platform Foundation's public API is everything documented in [PlatformFoundation.md](PlatformFoundation.md) and inventoried in the Milestone 3 Architecture Review Package: the public classes, interfaces, records, and extension methods under `Clovent.Platform.*`.

- **Stable, once a module depends on it.** As soon as any module (starting with Identity) takes a `ProjectReference` to `Clovent.Platform` and calls into its public API, that surface is considered stable and subject to the breaking-change policy below - not before. Until then (the current state, as of Milestone 3.1), the API may still be adjusted based on real usage without a formal deprecation cycle, since no consumer yet depends on it.
- **`internal` members are never stable.** Anything marked `internal` (e.g. `ExecutionContextAccessor.Current`'s setter) can change at any time without notice; it exists to be used only within `Clovent.Platform` itself.
- **Types not intended for implementation should say so.** Where an interface is meant to have exactly one implementation (e.g. `IExecutionContextAccessor`, which only `ExecutionContextAccessor` can mutate), that constraint should be documented on the interface (as it is today) rather than left implicit.

## 2. Breaking Change Policy

A **breaking change** is any of the following to a public member: removing it, renaming it, changing a method's parameter list or return type, changing an interface member from optional-to-implement to required (or vice versa), or changing documented runtime behavior a consumer could reasonably have depended on (e.g. configuration source precedence, module registration ordering).

- **Breaking changes require a major version bump** (see Section 5) once Platform Foundation is versioned, and should be called out explicitly in whatever review process governs Platform Foundation changes at that time (currently: an Architecture Review Package, as used for Milestones 3 and 3.1).
- **Breaking changes should be avoided, not forbidden.** Platform Foundation is still young and has (as of this writing) zero real consumers. A breaking change that meaningfully improves correctness or safety (the Milestone 3.1 Execution Context encapsulation change is an example - it changed `IExecutionContextAccessor.Current` from settable to read-only) is preferable to preserving a design that's known to be wrong, provided it is disclosed and justified rather than made silently.
- **Additive changes are not breaking.** New interfaces, new optional parameters (with defaults), new extension methods, and new properties on existing `sealed record` types (added as new `init`-only properties) do not require a major version bump.

## 3. Obsolete Policy

When a public member must be replaced but immediate removal would break consumers:

1. Mark the old member `[Obsolete("Reason and replacement, e.g. 'Use X instead.'")]` rather than deleting it outright.
2. The obsolete message must name the replacement, not just say "deprecated."
3. Obsolete members are removed in the next major version, not before - they should remain functional (not throw) for the remainder of the version line they were deprecated in, unless the reason for deprecation is a defect serious enough that continuing to function is itself harmful.
4. Since Platform Foundation has no consumers yet, no member has needed this treatment so far; this section exists so the next breaking change (once Identity or another module depends on the API) has a defined path other than a silent removal.

## 4. Extension Guidelines

For anyone adding to Platform Foundation (including future milestones):

- **New capability, not new pattern.** A new cross-cutting concern belongs in Platform Foundation only if it is genuinely something *every* module needs (as Configuration, DI conventions, Bootstrap, Execution Context, and Module Registration are). Module-specific concerns belong in the module, not here.
- **Follow the existing folder-per-capability structure** (see [PlatformFoundation.md](PlatformFoundation.md) Section 2) - a new capability gets its own folder and namespace segment, not a member bolted onto an unrelated existing class.
- **Follow the `Add*` naming convention** for any new registration extension method (Section 7 of PlatformFoundation.md) - `AddXyz(this IServiceCollection services, IConfiguration configuration)` returning `IServiceCollection`.
- **Document intent, not mechanics**, per the XML documentation standard already applied throughout `Clovent.Platform` - every public member explains *why* it exists and *when* to use it, not merely a restatement of its name.
- **No new external dependency without justification.** Platform Foundation currently depends on exactly two NuGet packages (`Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Options.DataAnnotations`). Any addition should be `Microsoft.Extensions.*` (or similarly foundational and host-agnostic) and should not pull in `Microsoft.AspNetCore.*` or anything that would compromise the "host-agnostic" design principle.
- **Prefer read-only public surfaces with a single, explicit mutation path**, following the pattern established for `IExecutionContext`/`IExecutionContextAccessor`/`ExecutionContextScope` in Milestone 3.1, when a new capability needs both broad read access and controlled mutation.

## 5. Semantic Version Expectations

Once Platform Foundation is versioned (a `<Version>` is set and/or it is packaged), it is expected to follow standard [Semantic Versioning](https://semver.org) (`MAJOR.MINOR.PATCH`):

- **MAJOR** - any breaking change per Section 2.
- **MINOR** - new capabilities, new public members, additive changes only.
- **PATCH** - bug fixes with no public API surface change at all.

Until that machinery exists, Platform Foundation's "version" is effectively the CBOS repository's own commit history and milestone numbering (Milestone 3, Milestone 3.1, ...); this document should be revisited to define an actual version number scheme (and whether Platform Foundation should be an internal `ProjectReference` only, or ever published as a versioned NuGet package to be consumed across repositories) once that decision is needed.

## 6. Compatibility Expectations for Future Modules

- **A module targets one version of Platform Foundation at a time**, referenced via `ProjectReference` (or, later, a specific `PackageReference` version) - there is no expectation of a module working across multiple, divergent Platform Foundation versions simultaneously.
- **A module should depend on interfaces, not concrete Platform Foundation types**, wherever both exist (`IExecutionContext`/`IExecutionContextAccessor` rather than `PlatformExecutionContext`/`ExecutionContextAccessor`; `IModule`/`IPersistenceInitializer`/`IStartupTask` are already interface-only). This limits a module's exposure to internal implementation changes that aren't breaking changes to the interface contract.
- **A module should not rely on undocumented behavior.** If a module needs a guarantee Platform Foundation doesn't document (e.g. a specific ordering beyond what [PlatformFoundation.md](PlatformFoundation.md) Section 6 states), that should be raised as a documentation gap or a feature request against Platform Foundation, not assumed from current implementation details.
- **Platform Foundation will not add a module-specific special case.** Compatibility is provided through the general extension points in [PlatformFoundation.md](PlatformFoundation.md) Section 9 (`IModule`, `IPersistenceInitializer`, `IStartupTask`, the `Add*` convention) - never through Platform Foundation code that knows about a specific module by name.
