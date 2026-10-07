# ADR-0002: Clean Architecture & Autonomous Bounded Contexts

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-09-15 |
| **Deciders** | Architecture Working Group |
| **Area** | Solution Architecture & Domain-Driven Design |

---

## 1. Context & Problem Statement

As enterprise software systems grow, business logic frequently leaks into UI event handlers, database queries, and third-party API adapters. In desktop applications, WinForms code-behind files often accumulate thousands of lines of direct SQL queries, transaction coordination, and UI formatting, creating an unmaintainable codebase that cannot be automated or safely upgraded.

---

## 2. Decision

CBOS strictly implements **Clean Architecture** across six domain-bounded contexts:
1. **Layer Hierarchy per Bounded Context:**
   - `Clovent.<Context>`: Domain model (aggregates, value objects, domain events, repository interfaces). Zero dependencies on external libraries or frameworks.
   - `Clovent.<Context>.Application`: CQRS application layer with MediatR commands, queries, validators, DTOs, and pipeline behaviors. References Domain only.
   - `Clovent.<Context>.Infrastructure`: EF Core `DbContext`, entity type configurations, repository implementations, and data access. References Domain and Application.
2. **Platform & Presentation Layers:**
   - `Clovent.Platform`: Shared cross-cutting abstractions (circuit breakers, logging primitives, base interfaces).
   - `Clovent.Desktop`: Presentation shell, WinForms views, DevExpress ribbon, view models, and startup composition root.
3. **Cross-Context Communication:**
   - Zero direct assembly references between domain layers.
   - Cross-context workflows are triggered via MediatR queries/commands or integration events published to the Transactional Outbox.

---

## 3. Consequences

### Positive
- **High Testability:** Domain logic can be tested 100% in memory with zero database or UI dependencies.
- **Maintainability:** Clear code placement rules prevent sprawl and spaghetti dependencies.
- **Architectural Longevity:** Infrastructure or UI frameworks (e.g. migrating views or changing persistence providers) can be upgraded without modifying core domain models.

### Negative / Trade-offs
- Higher number of C# projects in the solution (`.slnx`).
- Requires mapping between domain entities and application DTOs.
