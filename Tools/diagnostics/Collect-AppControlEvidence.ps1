<#
.SYNOPSIS
    Collects Application Control (WDAC, AppLocker, Smart App Control, Code Integrity) diagnostic evidence
    from the current system (e.g. Windows Sandbox guest or host) in a strictly read-only manner.
.DESCRIPTION
    Extracts complete XML event records from Microsoft-Windows-CodeIntegrity/Operational and
    Microsoft-Windows-AppLocker, inspects system policy state (Smart App Control, WDAC policies),
    verifies target file Authenticode and hash provenance, and distinguishes outer setup binaries
    from child/extracted payloads.
.PARAMETER TargetFilePath
    Optional path to the installer or binary being evaluated.
.PARAMETER OutputDir
    Directory to save the evidence text report, XML event bundle, and JSON provenance dump.
    Defaults to the directory containing this script, or current directory.
#>
[CmdletBinding()]
param(
    [string]$TargetFilePath = "",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Continue"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "CLOVENT APPLICATION CONTROL & CODE INTEGRITY EVIDENCE COLLECTOR" -ForegroundColor Cyan
Write-Host "Mode: Strictly Read-Only Diagnostic Inspection" -ForegroundColor Cyan
Write-Host "Timestamp (UTC): " ((Get-Date).ToUniversalTime().ToString("o")) -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        $OutputDir = $PSScriptRoot
    } else {
        $OutputDir = (Get-Location).Path
    }
}

if (-not (Test-Path $OutputDir)) {
    try {
        New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
    } catch {
        $OutputDir = [System.IO.Path]::GetTempPath()
    }
}

$timestamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
$reportTxtPath = Join-Path $OutputDir "AppControlEvidence_$timestamp.txt"
$reportJsonPath = Join-Path $OutputDir "AppControlEvidence_$timestamp.json"
$eventsXmlPath = Join-Path $OutputDir "AppControlEvents_$timestamp.xml"

$reportLines = [System.Collections.Generic.List[string]]::new()
$reportData = [ordered]@{}

function Add-ReportLine([string]$line = "") {
    Write-Host $line
    $reportLines.Add($line)
}

function Add-ReportHeader([string]$title) {
    Add-ReportLine ""
    Add-ReportLine ("=" * 80)
    Add-ReportLine $title
    Add-ReportLine ("=" * 80)
}

# -----------------------------------------------------------------------------
# 1. Target File Inspection
# -----------------------------------------------------------------------------
Add-ReportHeader "1. TARGET FILE INSPECTION & AUTHENTICODE STATUS"

$targetInfo = [ordered]@{}
if (-not [string]::IsNullOrWhiteSpace($TargetFilePath)) {
    if (Test-Path $TargetFilePath) {
        $fileItem = Get-Item $TargetFilePath
        $targetInfo["Path"] = $fileItem.FullName
        $targetInfo["FileName"] = $fileItem.Name
        $targetInfo["SizeBytes"] = $fileItem.Length
        $targetInfo["CreationTimeUtc"] = $fileItem.CreationTimeUtc.ToString("o")
        $targetInfo["LastWriteTimeUtc"] = $fileItem.LastWriteTimeUtc.ToString("o")

        $sha256 = (Get-FileHash -Path $fileItem.FullName -Algorithm SHA256).Hash
        $sha1 = (Get-FileHash -Path $fileItem.FullName -Algorithm SHA1).Hash
        $targetInfo["Sha256"] = $sha256
        $targetInfo["Sha1"] = $sha1

        Add-ReportLine ("File:        {0}" -f $fileItem.FullName)
        Add-ReportLine ("Size:        {0:N0} bytes" -f $fileItem.Length)
        Add-ReportLine ("SHA-256:     {0}" -f $sha256)
        Add-ReportLine ("SHA-1:       {0}" -f $sha1)

        # Authenticode Signature Status
        $sig = Get-AuthenticodeSignature -FilePath $fileItem.FullName
        $targetInfo["SignatureStatus"] = $sig.Status.ToString()
        $targetInfo["StatusMessage"] = $sig.StatusMessage

        Add-ReportLine ("Signature:   {0}" -f $sig.Status)
        if ($sig.StatusMessage) {
            Add-ReportLine ("StatusMsg:   {0}" -f $sig.StatusMessage)
        }

        if ($sig.SignerCertificate -ne $null) {
            $signer = [ordered]@{
                "Subject" = $sig.SignerCertificate.Subject
                "Issuer" = $sig.SignerCertificate.Issuer
                "Thumbprint" = $sig.SignerCertificate.Thumbprint
                "NotBefore" = $sig.SignerCertificate.NotBefore.ToString("o")
                "NotAfter" = $sig.SignerCertificate.NotAfter.ToString("o")
                "SerialNumber" = $sig.SignerCertificate.SerialNumber
            }
            $targetInfo["SignerCertificate"] = $signer
            Add-ReportLine ("Signer:      {0}" -f $sig.SignerCertificate.Subject)
            Add-ReportLine ("Issuer:      {0}" -f $sig.SignerCertificate.Issuer)
            Add-ReportLine ("Thumbprint:  {0}" -f $sig.SignerCertificate.Thumbprint)
        } else {
            $targetInfo["SignerCertificate"] = $null
            Add-ReportLine "Signer:      [None - Unsigned Executable]"
        }

        if ($sig.TimeStamperCertificate -ne $null) {
            $timestamper = [ordered]@{
                "Subject" = $sig.TimeStamperCertificate.Subject
                "Issuer" = $sig.TimeStamperCertificate.Issuer
                "Thumbprint" = $sig.TimeStamperCertificate.Thumbprint
            }
            $targetInfo["TimeStamperCertificate"] = $timestamper
            Add-ReportLine ("Timestamper: {0}" -f $sig.TimeStamperCertificate.Subject)
        } else {
            $targetInfo["TimeStamperCertificate"] = $null
        }

        # Check Alternate Data Stream (Zone.Identifier / Mark of the Web)
        $zonePath = $fileItem.FullName + ":Zone.Identifier"
        if (Test-Path $zonePath) {
            $zoneContent = Get-Content -Path $zonePath -Raw
            $targetInfo["ZoneIdentifier"] = $zoneContent.Trim()
            Add-ReportLine ("Mark of the Web (Zone.Identifier):`n{0}" -f $zoneContent.Trim())
        } else {
            $targetInfo["ZoneIdentifier"] = "None"
            Add-ReportLine "Mark of the Web: None (Local Origin)"
        }

        # Classification / Layer distinction
        if ($fileItem.Name -match "-Setup\.exe$") {
            $targetInfo["BinaryRole"] = "OuterSetupExecutable"
            Add-ReportLine "Binary Role: Outer Setup Executable (Inno Setup 6 Bootstrapper)"
        } elseif ($fileItem.Name -match "^is-[A-Za-z0-9_]+\.tmp$") {
            $targetInfo["BinaryRole"] = "InnoSetupExtractedTempBinary"
            Add-ReportLine "Binary Role: Inno Setup Extracted Temporary Binary"
        } elseif ($fileItem.Name -match "Clovent\.(Desktop|Installer\.Provisioner)\.exe") {
            $targetInfo["BinaryRole"] = "FirstPartyOwnedPayload"
            Add-ReportLine "Binary Role: First-Party Application Payload Executable"
        } elseif ($fileItem.Name -match "(SQLEXPR|SSMS|VC_redist|msoledbsql)") {
            $targetInfo["BinaryRole"] = "ThirdPartyPrerequisiteMedia"
            Add-ReportLine "Binary Role: Third-Party Microsoft Prerequisite Media"
        } else {
            $targetInfo["BinaryRole"] = "GeneralBinary"
            Add-ReportLine "Binary Role: General Binary"
        }
    } else {
        Add-ReportLine ("Specified target file not found: {0}" -f $TargetFilePath)
        $targetInfo["Error"] = "NotFound"
    }
} else {
    Add-ReportLine "No TargetFilePath specified. Proceeding with system policy and event inspection."
    $targetInfo["Status"] = "NotSpecified"
}
$reportData["TargetFile"] = $targetInfo

# -----------------------------------------------------------------------------
# 2. System Policy & Security State
# -----------------------------------------------------------------------------
Add-ReportHeader "2. SYSTEM APPLICATION CONTROL & CODE INTEGRITY STATE"

$sysInfo = [ordered]@{
    "OSVersion" = [Environment]::OSVersion.ToString()
    "OSArchitecture" = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    "MachineName" = [Environment]::MachineName
    "IsElevated" = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Add-ReportLine ("OS Version:      {0}" -f $sysInfo["OSVersion"])
Add-ReportLine ("Architecture:    {0}" -f $sysInfo["OSArchitecture"])
Add-ReportLine ("Machine Name:    {0}" -f $sysInfo["MachineName"])
Add-ReportLine ("Running Elevated:{0}" -f $sysInfo["IsElevated"])

# Smart App Control (SAC)
$sacRegPath = "HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy"
$sacState = "Unknown"
$sacRawVal = $null
if (Test-Path $sacRegPath) {
    try {
        $sacRawVal = (Get-ItemProperty -Path $sacRegPath -Name "VerifiedAndReputablePolicyState" -ErrorAction SilentlyContinue).VerifiedAndReputablePolicyState
        switch ($sacRawVal) {
            0 { $sacState = "Off (0)" }
            1 { $sacState = "Enforce (1) - Blocks untrusted or unverified files" }
            2 { $sacState = "Evaluation (2) - Audits untrusted files without blocking" }
            default { $sacState = "Configured ($sacRawVal)" }
        }
    } catch {
        $sacState = "ErrorReadingKey: $($_.Exception.Message)"
    }
} else {
    $sacState = "RegistryPathMissing"
}
$sysInfo["SmartAppControl"] = [ordered]@{
    "RawValue" = $sacRawVal
    "State" = $sacState
}
Add-ReportLine ("Smart App Control State: {0}" -f $sacState)

# WDAC Active Policies (via CiTool if available, or Registry)
$activeWdacPolicies = [System.Collections.Generic.List[object]]::new()
$ciTool = Get-Command "CiTool.exe" -ErrorAction SilentlyContinue
if ($ciTool) {
    try {
        $ciOutput = & $ciTool.Source -lp -json 2>$null | ConvertFrom-Json
        if ($ciOutput.Policies) {
            foreach ($p in $ciOutput.Policies) {
                $pObj = [ordered]@{
                    "PolicyID" = $p.PolicyID
                    "BasePolicyID" = $p.BasePolicyID
                    "FriendlyName" = $p.FriendlyName
                    "Version" = $p.Version
                    "IsEnforced" = $p.IsEnforced
                    "IsAuthorized" = $p.IsAuthorized
                }
                $activeWdacPolicies.Add($pObj)
                Add-ReportLine ("WDAC Policy: [{0}] '{1}' Version {2} (Enforced: {3})" -f $p.PolicyID, $p.FriendlyName, $p.Version, $p.IsEnforced)
            }
        }
    } catch {
        Add-ReportLine ("CiTool execution note: {0}" -f $_.Exception.Message)
    }
} else {
    # Check ActivePolicies registry
    $activePolReg = "HKLM:\SYSTEM\CurrentControlSet\Control\CI\ActivePolicies"
    if (Test-Path $activePolReg) {
        $polKeys = Get-ChildItem -Path $activePolReg -ErrorAction SilentlyContinue
        foreach ($pk in $polKeys) {
            $polId = $pk.PSChildName
            $polProps = Get-ItemProperty -Path $pk.PSPath
            $pObj = [ordered]@{
                "PolicyID" = $polId
                "FriendlyName" = $polProps.PolicyName
                "Options" = $polProps.Options
            }
            $activeWdacPolicies.Add($pObj)
            Add-ReportLine ("WDAC Active Policy Key: [{0}] '{1}' Options: {2}" -f $polId, $polProps.PolicyName, $polProps.Options)
        }
    } else {
        Add-ReportLine "CiTool not present and HKLM:\SYSTEM\CurrentControlSet\Control\CI\ActivePolicies not populated."
    }
}
$sysInfo["ActiveWdacPolicies"] = $activeWdacPolicies

# AppLocker Service Status
$appIdSvc = Get-Service -Name "AppIDSvc" -ErrorAction SilentlyContinue
if ($appIdSvc) {
    $sysInfo["AppIDSvcStatus"] = $appIdSvc.Status.ToString()
    $sysInfo["AppIDSvcStartType"] = $appIdSvc.StartType.ToString()
    Add-ReportLine ("AppLocker Service (AppIDSvc): Status={0}, StartType={1}" -f $appIdSvc.Status, $appIdSvc.StartType)
} else {
    $sysInfo["AppIDSvcStatus"] = "NotInstalled"
    Add-ReportLine "AppLocker Service (AppIDSvc): Not Installed"
}

$reportData["SystemSecurityState"] = $sysInfo

# -----------------------------------------------------------------------------
# 3. Code Integrity & AppLocker Event Extraction
# -----------------------------------------------------------------------------
Add-ReportHeader "3. CODE INTEGRITY & APPLOCKER EVENT RECORDS"

$collectedEvents = [System.Collections.Generic.List[object]]::new()
$allXmlRecords = [System.Collections.Generic.List[string]]::new()

# Query CodeIntegrity/Operational
$ciLogName = "Microsoft-Windows-CodeIntegrity/Operational"
Add-ReportLine ("Querying event log: {0}..." -f $ciLogName)

try {
    # Event IDs: 3076 (Audit block), 3077 (Enforce block), 3089 (Signature info), 3099 (Policy load), 3033, 3034
    $ciEvents = Get-WinEvent -FilterHashtable @{LogName = $ciLogName} -MaxEvents 500 -ErrorAction SilentlyContinue
    if ($ciEvents) {
        $filterEvents = $ciEvents | Where-Object { $_.Id -in @(3076, 3077, 3089, 3099, 3033, 3034, 3086, 3087) }
        Add-ReportLine ("Found {0} relevant CodeIntegrity security events (out of {1} total)." -f ($filterEvents | Measure-Object).Count, $ciEvents.Count)

        foreach ($evt in $filterEvents) {
            $xml = $evt.ToXml()
            $allXmlRecords.Add($xml)

            $xmlDoc = [xml]$xml
            $eventData = @{}
            if ($xmlDoc.Event.EventData.Data) {
                foreach ($d in $xmlDoc.Event.EventData.Data) {
                    if ($d.Name) {
                        $eventData[$d.Name] = $d.'#text'
                    }
                }
            }

            $evtRecord = [ordered]@{
                "Log" = $ciLogName
                "Id" = $evt.Id
                "TimeCreatedUtc" = $evt.TimeCreated.ToUniversalTime().ToString("o")
                "Level" = $evt.LevelDisplayName
                "ProcessName" = $eventData["ProcessName"]
                "FileName" = $eventData["FileName"]
                "PolicyID" = $eventData["PolicyID"]
                "PolicyName" = $eventData["PolicyName"]
                "PolicyFriendlyName" = $eventData["PolicyFriendlyName"]
                "PolicyVersion" = $eventData["PolicyVersion"]
                "RuleId" = $eventData["RuleId"]
                "RuleName" = $eventData["RuleName"]
                "Sha256FlatHash" = $eventData["Sha256FlatHash"]
                "Sha1FlatHash" = $eventData["Sha1FlatHash"]
                "PageHash" = $eventData["PageHash"]
                "StatusCode" = $eventData["StatusCode"]
                "SignerName" = $eventData["SignerName"]
                "IssuerName" = $eventData["IssuerName"]
                "RawXml" = $xml
            }
            $collectedEvents.Add($evtRecord)

            Add-ReportLine ("`n--- [CodeIntegrity Event ID {0}] Time: {1} ---" -f $evt.Id, $evt.TimeCreated.ToUniversalTime().ToString("o"))
            Add-ReportLine ("  Process:      {0}" -f $eventData["ProcessName"])
            Add-ReportLine ("  File Path:    {0}" -f $eventData["FileName"])
            Add-ReportLine ("  Policy:       {0} (ID: {1}, Ver: {2})" -f $eventData["PolicyFriendlyName"], $eventData["PolicyID"], $eventData["PolicyVersion"])
            Add-ReportLine ("  Rule ID/Name: {0} / {1}" -f $eventData["RuleId"], $eventData["RuleName"])
            Add-ReportLine ("  SHA256 Hash:  {0}" -f $eventData["Sha256FlatHash"])
            Add-ReportLine ("  Status Code:  {0}" -f $eventData["StatusCode"])
            if ($eventData["SignerName"]) {
                Add-ReportLine ("  Signer/Issuer:{0} / {1}" -f $eventData["SignerName"], $eventData["IssuerName"])
            }
        }
    } else {
        Add-ReportLine "No events found in Microsoft-Windows-CodeIntegrity/Operational."
    }
} catch {
    Add-ReportLine ("CodeIntegrity log access note: {0}" -f $_.Exception.Message)
}

# Query AppLocker Event Logs
$appLockerLogs = @(
    "Microsoft-Windows-AppLocker/EXE and DLL",
    "Microsoft-Windows-AppLocker/MSI and Script",
    "Microsoft-Windows-AppLocker/Packaged app-Deployment"
)

foreach ($alLog in $appLockerLogs) {
    try {
        $alEvents = Get-WinEvent -FilterHashtable @{LogName = $alLog} -MaxEvents 200 -ErrorAction SilentlyContinue
        if ($alEvents) {
            $alFiltered = $alEvents | Where-Object { $_.Id -in @(8003, 8004, 8028, 8020, 8021) }
            Add-ReportLine ("`nQuerying {0}: found {1} relevant AppLocker events." -f $alLog, ($alFiltered | Measure-Object).Count)
            foreach ($evt in $alFiltered) {
                $xml = $evt.ToXml()
                $allXmlRecords.Add($xml)
                $xmlDoc = [xml]$xml
                $eventData = @{}
                if ($xmlDoc.Event.UserData) {
                    $ruleAndEvt = $xmlDoc.Event.UserData.FirstChild
                    if ($ruleAndEvt) {
                        foreach ($node in $ruleAndEvt.ChildNodes) {
                            $eventData[$node.LocalName] = $node.InnerText
                        }
                    }
                }

                $evtRecord = [ordered]@{
                    "Log" = $alLog
                    "Id" = $evt.Id
                    "TimeCreatedUtc" = $evt.TimeCreated.ToUniversalTime().ToString("o")
                    "FilePath" = $eventData["FilePath"]
                    "FileHash" = $eventData["FileHash"]
                    "RuleId" = $eventData["RuleId"]
                    "RuleName" = $eventData["RuleName"]
                    "PolicyName" = $eventData["PolicyName"]
                    "RawXml" = $xml
                }
                $collectedEvents.Add($evtRecord)

                Add-ReportLine ("  [AppLocker ID {0}] {1} File: {2} (Rule: {3})" -f $evt.Id, $evt.TimeCreated.ToUniversalTime().ToString("o"), $eventData["FilePath"], $eventData["RuleName"])
            }
        }
    } catch {
        # AppLocker log might not be enabled or populated
    }
}

$reportData["Events"] = $collectedEvents

# -----------------------------------------------------------------------------
# 4. Analysis & Diagnostic Classification
# -----------------------------------------------------------------------------
Add-ReportHeader "4. APPLICATION CONTROL DIAGNOSTIC FINDINGS"

$blockedItems = $collectedEvents | Where-Object { $_.Id -in @(3077, 8004, 8028) }
$auditItems = $collectedEvents | Where-Object { $_.Id -in @(3076, 8003) }

Add-ReportLine ("Total Enforced Block Events Detected: {0}" -f ($blockedItems | Measure-Object).Count)
Add-ReportLine ("Total Audit Block Events Detected:    {0}" -f ($auditItems | Measure-Object).Count)

if ($blockedItems) {
    Add-ReportLine "`nBLOCKED BINARY DETAILS:"
    foreach ($b in $blockedItems) {
        $p = if ($b.FileName) { $b.FileName } else { $b.FilePath }
        $h = if ($b.Sha256FlatHash) { $b.Sha256FlatHash } else { $b.FileHash }
        Add-ReportLine (" - Blocked File: {0}" -f $p)
        Add-ReportLine ("   Hash:         {0}" -f $h)
        Add-ReportLine ("   Policy ID:    {0}" -f $b.PolicyID)
        Add-ReportLine ("   Status Code:  {0}" -f $b.StatusCode)

        # Distinguish outer setup EXE vs child/extracted payloads
        if ($p -match "-Setup\.exe$") {
            Add-ReportLine "   Classification: OUTER SETUP EXECUTABLE was blocked prior to process creation or payload extraction."
        } elseif ($p -match "\\Temp\\is-[A-Za-z0-9_]+\.tmp") {
            Add-ReportLine "   Classification: INNO SETUP TEMPORARY ENGINE was blocked during execution."
        } elseif ($p -match "Clovent\.(Desktop|Installer\.Provisioner)\.exe") {
            Add-ReportLine "   Classification: FIRST-PARTY APPLICATION PAYLOAD was blocked."
        } else {
            Add-ReportLine "   Classification: CHILD/PRE-REQUISITE OR SYSTEM BINARY was blocked."
        }
    }
} else {
    Add-ReportLine "`nNo explicit 3077/8004 block events recorded in current event log scope."
    Add-ReportLine "If Windows Sandbox blocked execution with 'An Application Control policy has blocked this file':"
    Add-ReportLine " - Verify whether Smart App Control (SAC) or Default Windows Sandbox WDAC Policy is active."
    Add-ReportLine " - Check if the event log was cleared upon guest boot or if the block was raised via Smart App Control."
}

# -----------------------------------------------------------------------------
# 5. Export Evidence Files
# -----------------------------------------------------------------------------
Add-ReportHeader "5. EVIDENCE BUNDLE EXPORT"

try {
    # 1. Text Summary Report
    Set-Content -Path $reportTxtPath -Value $reportLines -Encoding UTF8
    Add-ReportLine ("Saved summary report to: {0}" -f $reportTxtPath)

    # 2. JSON Structured Data
    $reportData | ConvertTo-Json -Depth 6 | Set-Content -Path $reportJsonPath -Encoding UTF8
    Add-ReportLine ("Saved JSON evidence to:   {0}" -f $reportJsonPath)

    # 3. Raw XML Event Stream
    $xmlBundle = "<AppControlEvents GeneratedUtc='$([DateTime]::UtcNow.ToString('o'))'>`n"
    foreach ($x in $allXmlRecords) {
        $xmlBundle += $x + "`n"
    }
    $xmlBundle += "</AppControlEvents>"
    Set-Content -Path $eventsXmlPath -Value $xmlBundle -Encoding UTF8
    Add-ReportLine ("Saved raw XML events to:  {0}" -f $eventsXmlPath)
} catch {
    Add-ReportLine ("Error saving evidence files: {0}" -f $_.Exception.Message)
}

Write-Host "`nDiagnostic evidence collection complete." -ForegroundColor Green
