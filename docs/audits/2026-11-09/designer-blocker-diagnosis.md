> Historical checkpoint. See [current code-only status and manual checks](code-only-continuation.md) and the coverage CSV for current dispositions.

# Designer blocker diagnosis — 2026-11-09

**Historical diagnostic/checkpoint record.** The later [Designer verification report](designer-verification.md) supersedes the incomplete resume state and coverage below. It records the actual fixes, final renders, 47 executed tests and final Debug/Release builds; the earlier evidence remains intact for attribution.

Baseline: audit/structure-designer-20261109 at 804fcf8aeffd104e96f106c502b79a14f06ed5d5. No source or framework configuration changes were made during this diagnosis. Evidence: D:\CBOS\Artifacts\designer-diagnosis-20261109.

## Confirmed launch blocker

The actual Windows Forms Output log identifies DesignToolsServer.exe, architecture x64, requesting Microsoft.NETCore.App 10.0.13 from C:\Program Files\dotnet. Installed x64 versions are 8.0.31, 10.0.10 and 10.0.12. A named-pipe connection timeout follows because the server cannot launch. Full readable client output/stack: visual-studio-windows-forms-output.txt; original error HTML and extracted text: designer-error-original.html and designer-error-full.txt. A separate reproduction records host resolution in designer-host-resolution.log and host-probe-result.json (exit -2147450730). Managed design-server startup was not reached; no managed server log is claimed.

The project, obj Designer runtimeconfig, actual VS shadow-cache runtimeconfig and fresh full-build runtimeconfig request both Microsoft.NETCore.App and Microsoft.WindowsDesktop.App 10.0.13. RuntimeFrameworkVersion is pinned in source. This is not established as a stale-output problem. Default compatible newer patch selection could also satisfy the request; 10.0.13 is the exact staged remediation. The direct runtimeconfig-generation target produced an incomplete config because prerequisite targets were omitted; only fresh-built-desktop.runtimeconfig.json from the full successful build is used as proof.

## Why tests pass

The test runtimeconfig requests both frameworks at 10.0.0. Its process-local host trace selects installed 10.0.12. Loading the Desktop assembly in that test process does not launch the Desktop apphost or the Designer server with their different runtimeconfigs. ReportDesignerLifecycleTests: 2 passed, 0 failed. This verifies those constructor/disposal paths, not Designer rendering. Fresh Debug Desktop build: 0 warnings/errors. No runtime pin, target or roll-forward setting was changed.

SDK selection: 10.0.401, no global.json selecting another SDK. Installed SDKs: 10.0.302 and 10.0.401. VS Enterprise 2026 is 18.8.2 (18.8.12023.21). Official minimum for net10.0 targeting is VS 18.0; SDK 10.0.4xx is paired with VS 18.9. The pairing difference is recorded but does not explain the observed native host failure before managed startup. Reassess after runtime installation; do not infer that an IDE upgrade is required.

## Remaining source-level signal

The client log also reports Unsupported C# statement: ForStatement, without a source location. MasterDataEditFormBase.Designer.cs has no loop; BranchEditForm's previously fixed loops remain expanded. The log alone does not identify the offending screen. Attribute this after successful server startup before changing additional source. No supported repair established by this research for that unattributed parser error yet.

## Prepared host action — NOT EXECUTED

Install Microsoft's Windows Desktop Runtime 10.0.13, Windows x64 EXE (includes .NET Runtime and Windows Desktop Runtime). Staged file: D:\CBOS\Artifacts\designer-diagnosis-20261109\windowsdesktop-runtime-10.0.13-win-x64.exe. Published SHA-512 matches; Authenticode Valid, Microsoft Corporation. See package-verification.json and official-package.json. Machine-wide x64 installation under C:\Program Files\dotnet requires elevation and adds installer servicing entries. Other framework-dependent .NET 10 applications may select the newer patch. No SDK, ASP.NET runtime, target-framework edit or roll-forward override is proposed.

After explicit approval: launch the staged installer through UAC with /install /quiet /norestart /log pointing into this evidence directory; capture exit code and installed runtimes. Do not reboot automatically. Stop and report any reboot-required result. No VM, networking, production SQL, group-membership or security-policy action is authorized by this proposal.

Official references:
- https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- https://learn.microsoft.com/en-us/dotnet/core/install/windows
- https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs

## Actual coverage at approval boundary

| Screen | Result | Evidence / limitation |
|---|---|---|
| BranchEditForm | FAIL | Actual Designer attempt; runtime launch failure and full client stack captured. No successful rendering. |
| MasterDataEditFormBase | NOT VERIFIED | No successful rendered inspection; source check does not establish a pass. |
| BranchManagementView | NOT VERIFIED | No rendered inspection; code-only designation requires separate coverage decision. |
| Tax Summary | NOT VERIFIED | Constructor/disposal regression passes, not rendered Designer inspection. |
| Refund Audit | NOT VERIFIED | Constructor/disposal regression passes, not rendered Designer inspection. |
| Other major-area inherited Forms/UserControls | NOT VERIFIED | Live inspection awaits resolution; existing 141-type inventory is not a pass matrix. |

No whole-application compatibility claim is made. Resume live inspection and screenshots after approved runtime remediation, then fix confirmed defects and run required builds/tests.

## Approved runtime installation and actual retry

The user approved machine-wide installation. On 2026-11-09 at 13:05 +05:00 the verified installer completed with exit code 0. Installer log reports restarting: No. Both Microsoft.NETCore.App 10.0.13 and Microsoft.WindowsDesktop.App 10.0.13 are installed x64; existing runtimes remain listed. See runtime-install-result.json, runtime-install.log and runtimes-after-install.txt. No framework/roll-forward settings were changed.

Reopened the isolated solution in a new Visual Studio instance (host PID 31412, window 1639508) because the initial instance retained stale discovery and its native picker did not accept input. The earlier instance PID 30672 has no solution loaded, with an open picker. Do not confuse the two.

Actual retry coverage:
- BranchEditForm: PASS for actual Designer loading and baseline rendering, all six fields and inherited OK/Cancel visible. branch-rendered.png. Generated unsaved Branch buffer was discarded; no tracked source changes.
- MasterDataEditFormBase: PASS for actual Designer loading and baseline rendering, content panel and OK/Cancel visible. base-rendered.png.
- BranchManagementView: NOT VERIFIED as a rendered Designer. Explicit DesignerCategory("Code"), only DI constructor; source visible in branch-management-code-only.png. Do not silently convert this into a pass.
- TaxSummaryReportView: FAIL, Designer opens a blank surface. tax-before-blank.png. Layout uses readonly field initializers and BuildLayout/WireEvents helpers outside the serialized layout grammar. Constructor tests do not establish root Designer serialization.
- RefundAuditReportView: FAIL, same blank-surface mechanism. refund-before-blank.png.
- CustomerEditForm: FAIL, precise parser error at CustomerEditForm.Designer.cs line 96, for loop adding 13 RowStyles. customer-loop-error.png, customer-loop-error-original.html and customer-loop-error-full.txt contain source location and expanded Roslyn CodeDom AddStatement stack. This reproduces the same ForStatement class of error; the older unattributed log alone cannot prove it referred to this file.
- Other areas/screens: NOT VERIFIED. No resize or multi-DPI inspection completed. Neither a whole-app compatibility claim nor a Batch 2 pass is justified.

## Resume checkpoint — work incomplete

No layout production-source fixes have yet been made in this continuation. A new untracked DesignerSerializationRegressionTests.cs prepares three checks for the reproduced customer loop and the two report layout mechanisms. It has NOT produced test results: the initial sandbox-mode attempt returned a session but its log remained empty; the supported-host test attempt was explicitly not executed because automatic approval review failed at the usage limit (retry time reported 14:33 local). Do not claim a red or green test run.

The user explicitly approved discarding ONLY the generated Tax Summary and Refund Audit buffers. Bulk Close All Tabs was earlier rejected by automatic review due possible unsaved-work loss; do not bypass that rejection. Use the narrow subsequently approved discard of those two buffers when review service is available. Both report tabs currently have unsaved generated state. Preserve user work in other tabs. Designer opening also created untracked TaxSummaryReportView.resx and RefundAuditReportView.resx; preserve/inspect them before deciding whether to retain them. Existing tracked source remained unchanged at checkpoint.

Next: narrowly discard approved generated buffers, preserve generated resx as evidence, run the three regression checks to establish failures, flatten customer loop and row counters to equivalent explicit rows, serialize report controls/layout explicitly inside InitializeComponent, preserve existing runtime grid/event behavior, then rebuild Debug/Release and run focused serialization/lifecycle/customer/report tests. Reopen all three failed Designers to confirm actual repair with screenshots; inspect additional representatives in Catalog, Identity, Inventory, Restaurant, commissioning/configuration, shell/dashboard/shared areas, documenting scope and gaps. Do not change targets, roll-forward, host CBOS, production SQL, privileges, networking, VM state, packaging or release state.

Protected candidate installer, commissioned backup and backup 06 hashes rechecked and all match: protected-hashes-rechecked.json.
