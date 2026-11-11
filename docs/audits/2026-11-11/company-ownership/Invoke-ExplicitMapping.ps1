[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$MappingCsv,
    [Parameter(Mandatory)][string]$ExpectedDatabase,
    [Parameter(Mandatory)][string]$ConnectionString,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
# Windows PowerShell's built-in provider; no module/package installation.
$target = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString
if (-not $target.IntegratedSecurity -or $target.UserID -or $target.Password) {
    throw 'This reviewed action requires Windows integrated authentication without SQL credentials.'
}
if (-not $ExpectedDatabase -or $target.InitialCatalog -ne $ExpectedDatabase -or
    $ExpectedDatabase -in @('master','model','msdb','tempdb')) {
    throw 'Explicit database target does not match, or is a system database.'
}
$mappingBytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $MappingCsv).Path)
$rows = @(ConvertFrom-Csv -InputObject ([Text.Encoding]::UTF8.GetString($mappingBytes).TrimStart([char]0xFEFF)))
$sha = [Security.Cryptography.SHA256]::Create()
try { $mappingHash = [BitConverter]::ToString($sha.ComputeHash($mappingBytes)).Replace('-', '') }
finally { $sha.Dispose() }
if ($rows.Count -eq 0) { throw 'A complete explicitly approved mapping is required; the template is not a mapping.' }
$seen = @{}
$validated = foreach ($row in $rows) {
    $customer = [Guid]::Parse($row.CustomerId)
    $company = [Guid]::Parse($row.CompanyId)
    $approvedAt = [DateTimeOffset]::Parse($row.ApprovedAtUtc, [Globalization.CultureInfo]::InvariantCulture)
    if ($customer -eq [Guid]::Empty -or $company -eq [Guid]::Empty -or $seen.ContainsKey($customer) -or
        [string]::IsNullOrWhiteSpace($row.ApprovedBy) -or [string]::IsNullOrWhiteSpace($row.EvidenceReference) -or
        $row.ApprovedBy.Length -gt 200 -or $row.EvidenceReference.Length -gt 500 -or $approvedAt.Offset -ne [TimeSpan]::Zero) {
        throw 'Mapping contains an empty/duplicate identity or missing/invalid approval metadata.'
    }
    $seen[$customer] = $true
    [pscustomobject]@{ CustomerId=$customer; CompanyId=$company; ApprovedBy=$row.ApprovedBy
        ApprovedAtUtc=$approvedAt; EvidenceReference=$row.EvidenceReference }
}
if (-not $Apply) {
    [pscustomobject]@{ Status='REVIEW ONLY'; Rows=$validated.Count; Database=$ExpectedDatabase; ConnectionOpened=$false }
    return
}
$connection = New-Object System.Data.SqlClient.SqlConnection $target.ConnectionString
try {
    $connection.Open()
    $check = $connection.CreateCommand()
    $check.CommandText = 'SELECT DB_NAME();'
    if ($check.ExecuteScalar() -ne $ExpectedDatabase) { throw 'Connected database does not match the reviewed target.' }
    $transaction = $connection.BeginTransaction()
    try {
        foreach ($row in $validated) {
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = @'
IF EXISTS (SELECT 1 FROM Restaurant.CustomerCompanyOwnershipMap WHERE CustomerId=@customer AND
    (CompanyId<>@company OR ApprovedBy<>@reviewer OR ApprovedAtUtc<>@at OR EvidenceReference<>@evidence))
    THROW 51008, 'An existing approved mapping differs; no mapping was overwritten.', 1;
IF NOT EXISTS (SELECT 1 FROM Restaurant.CustomerCompanyOwnershipMap WHERE CustomerId=@customer)
    INSERT Restaurant.CustomerCompanyOwnershipMap (CustomerId,CompanyId,ApprovedBy,ApprovedAtUtc,EvidenceReference)
    VALUES (@customer,@company,@reviewer,@at,@evidence);
'@
            [void]$command.Parameters.AddWithValue('@customer', $row.CustomerId)
            [void]$command.Parameters.AddWithValue('@company', $row.CompanyId)
            [void]$command.Parameters.AddWithValue('@reviewer', $row.ApprovedBy)
            $time = $command.Parameters.Add('@at', [System.Data.SqlDbType]::DateTimeOffset)
            $time.Value = $row.ApprovedAtUtc
            [void]$command.Parameters.AddWithValue('@evidence', $row.EvidenceReference)
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
        }
        $transaction.Commit()
    } catch { $transaction.Rollback(); throw }
    finally { $transaction.Dispose() }
    $command = $connection.CreateCommand()
    $command.CommandText = "EXEC sys.sp_set_session_context @key=N'CBOS.Ownership.ExpectedDatabase', @value=@database;"
    [void]$command.Parameters.AddWithValue('@database', $ExpectedDatabase)
    [void]$command.ExecuteNonQuery()
    $command.CommandTimeout = 120
    $command.Parameters.Clear()
    $command.CommandText = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'ApplyExplicitMapping.sql'))
    [void]$command.ExecuteNonQuery()
    $command.Dispose()
    [pscustomobject]@{ Status='MAPPED'; Rows=$validated.Count; Database=$ExpectedDatabase
        MappingSHA256=$mappingHash }
} finally { $connection.Dispose() }
