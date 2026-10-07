# CBOS Developer Environment Setup Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Developer Onboarding & Workstation Tooling |
| **Audience** | Software Developers, QA Engineers, DevOps |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Prerequisites & Required Workstation Tooling

To develop, compile, debug, and test Clovent Business Operating System (CBOS), ensure your workstation meets the following tooling specifications:

### 1.1 Operating System & Hardware
- **OS:** Windows 11 (64-bit, Version 22H2 or newer) or Windows 10 (64-bit, Version 21H2 or newer).
- **RAM:** Minimum 16 GB (32 GB strongly recommended for running Visual Studio, SQL Server, and multiple test runners concurrently).
- **Storage:** Solid-state drive (NVMe SSD recommended) with at least 25 GB free disk space.

### 1.2 Software Tooling & SDKs
- **.NET 10.0 SDK:** Required to compile C# 13 and target `.NET 10.0` and `.NET 10.0-windows`. Verify via `dotnet --version`.
- **Integrated Development Environment (IDE):**
  - **Visual Studio 2026** (or Visual Studio 2022 Version 17.12+ with preview features enabled).
  - Required Workloads:
    - **.NET Desktop Development** (includes Windows Forms tools, CodeDom designer support, and ClickOnce/packaging).
- **DevExpress WinForms 26.1:**
  - `DevExpress.Win`, `DevExpress.Reporting.Core`, `DevExpress.Images` (Version `26.1.4-pre-26179` or active NuGet feed).
  - *Notice:* Configure your corporate or licensed DevExpress NuGet feed in `nuget.config`. Never commit personal feed authorization keys to version control.
- **Database Engine:**
  - Microsoft SQL Server 2022 (Developer Edition, Express, or LocalDB).
  - SQL Server Management Studio (SSMS 20+) or Azure Data Studio.
- **Version Control:** Git for Windows (2.40+).
- **Packaging (Optional for installer authoring):** Inno Setup 6.3+ (installed in `Tools\InnoSetup\` or host PATH).

---

## 2. Repository Cloning & NuGet Package Configuration

### Step 1: Clone Repository
```powershell
git clone <RepositoryUrl> "Clovent-Business-Operating-System"
cd "Clovent-Business-Operating-System"
```

### Step 2: Configure NuGet Package Sources
Ensure DevExpress packages are resolvable. Add your licensed NuGet feed (or local folder package source):
```powershell
dotnet nuget add source "https://nuget.devexpress.com/<YOUR_API_KEY>/api" -n "DevExpress"
```

### Step 3: Restore Solution Dependencies
```powershell
dotnet restore Clovent.BusinessOperatingSystem.slnx
```

---

## 3. Initial Build & Verification

### Step 1: Compile Debug Solution
```powershell
dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug
```
*Expected Result:* `Build succeeded. 0 Warning(s). 0 Error(s).`

### Step 2: Run Targeted Test Suite
```powershell
dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~BusinessFormatters"
```
*Expected Result:* All tests pass with 100% workstation test isolation.

---

## 4. Local Database Setup for Development

1. Open SSMS and connect to `(localdb)\MSSQLLocalDB` or `localhost\SQLEXPRESS`.
2. Create development database:
   ```sql
   CREATE DATABASE [Clovent_BusinessOperatingSystem] COLLATE SQL_Latin1_General_CP1_CI_AS;
   ```
3. Update connection string in `src\Clovent.Desktop\appsettings.Development.json` (git-ignored):
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=localhost\\SQLEXPRESS;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```
4. Run migrations using `dotnet ef` (see [Migrations Guide](../database/migrations.md)).

---

## 5. Cross References
- [Build Guide](build.md)
- [Project Structure](project-structure.md)
- [Coding Guidelines](coding-guidelines.md)
- [WinForms Designer Safety](winforms-designer-safety.md)
