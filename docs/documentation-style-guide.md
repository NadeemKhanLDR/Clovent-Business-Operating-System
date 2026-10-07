# CBOS Documentation Style Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Engineering Governance & Documentation Standards |
| **Audience** | Technical Writers, AI Agents, Software Engineers, Architects |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **CANONICAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Principles of Technical Documentation

Technical documentation in CBOS must meet the standards of a mature, mission-critical commercial enterprise software platform. Every document must be:
1. **Source-Backed & Factually Verifiable:** Never document imagined or aspirational behavior as implemented fact. Code and verified runtime behavior are the ultimate sources of truth.
2. **Clear & Concise:** Use direct sentences, informative section headings, bulleted lists, and structured comparison tables. Avoid unnecessary prose or marketing puffery.
3. **Structured & Consistent:** Follow uniform metadata headers, standardized terminology, and standardized status markers across all documents.
4. **Resilient to Rot:** Trace architecture to namespaces, projects, and key aggregate classes rather than brittle line numbers that shift between commits.
5. **Safe for Public Distribution:** Never include passwords, connection string credentials, private keys, customer data, internal employee names, or workstation paths.

---

## 2. Standard Metadata Header

Every top-level document in `docs/` must begin with the canonical metadata table:

```markdown
# Document Title

| Attribute | Details |
| :--- | :--- |
| **Area** | <Subsystem or Bounded Context> |
| **Audience** | <Target Roles: Developers, Operations, Architects, QA, Support> |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | <Standard Status Marker> |
| **Classification** | PUBLIC-SAFE |
```

---

## 3. Standardized Status Markers

When describing subsystems, architectural components, or operational features, use the following standardized markers:

| Marker | Formal Meaning |
| :--- | :--- |
| **`IMPLEMENTED`** | The capability exists in the C# source code and builds cleanly in the active release baseline. |
| **`PARTIAL`** | Core aspects exist in code, but key extensions or authoritative boundaries remain incomplete. |
| **`PLANNED`** | Architecture or interface is designed or contemplated, but no operational product code exists in the baseline. |
| **`VALIDATED`** | The capability has been formally proven through automated test suites or verified manual acceptance. |
| **`NOT YET VALIDATED`** | Code exists, but specific clean-environment or end-to-end integration tests have not yet been executed. |
| **`DEPRECATED`** | Legacy pattern, script, or configuration maintained for backward compatibility but scheduled for removal. |

---

## 4. Documentation Structure

Major architectural and subsystem guides should follow this structural progression:
1. **Metadata Header:** Standard attribute table.
2. **Architectural Overview / Executive Summary:** High-level description of purpose and domain scope.
3. **Mermaid Diagrams:** Clear architectural flow, sequence, or state machine where visual clarity aids comprehension.
4. **Current Implementation Details:** Factual review of existing behavior, schemas, and workflows.
5. **Key Classes & Source Traceability:** Bulleted list of relevant projects and primary C# classes.
6. **Known Limitations & Edge Cases:** Honest accounting of constraints and boundaries.
7. **Planned Evolution:** Clear demarcation of upcoming features or architectural goals (if applicable).
8. **Cross References:** Links to related documents in the repository.

---

## 5. Visual Diagrams (Mermaid Guidelines)

- Use Mermaid code blocks (` ```mermaid `) for diagrams.
- Stick to standard diagrams: `flowchart TD` / `flowchart LR`, `sequenceDiagram`, `stateDiagram-v2`, and `erDiagram`.
- Ensure all node labels with special characters (parentheses, brackets, colons) are properly quoted:
  - `id["Order Form (POS)"]`
- Keep diagrams legible and focused on logical boundaries rather than exhaustive class listings.

---

## 6. Code Formatting & Commands

- Always wrap commands, file paths, namespaces, and class names in backticks: `` `dotnet test` ``.
- Format multi-line shell commands in fenced code blocks with language identifiers (`powershell`, `sql`, `json`, `csharp`).
- Only specify build or test commands that have been verified against the repository.

---

## 7. Security & Privacy Rules

- **Zero Shipped Secrets:** Never paste real SQL connection strings with passwords, DPAPI keys, or RSA private keys.
- **Generic Placeholders:** Use `localhost`, `cbos_app`, `[ENCRYPTED_SECRET]`, `sample_company`, or `<ServerName>` in examples.
- **No Personal Attributions:** Attribute documentation ownership to roles (`Release Engineering`, `Architecture Team`, `Field Operations`) rather than personal employee names.

---

## 8. Cross References
- [Enterprise Glossary](glossary.md)
- [Known Limitations](known-limitations.md)
- [Master Documentation Portal](README.md)
