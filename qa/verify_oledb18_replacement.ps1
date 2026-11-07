param([Parameter(Mandatory)][string]$EvidenceFolder)
$ErrorActionPreference='Stop'
$url='https://go.microsoft.com/fwlink/?clcid=0x409&linkid=2383709'
$expectedUrl='https://download.microsoft.com/download/1bf0a51f-dd1e-4cd6-98fb-22f5ea9ce0b2/amd64/1033/msoledbsql.msi'
$path=Join-Path $EvidenceFolder 'media\msoledbsql.msi'
if(Test-Path -LiteralPath $path){throw 'Refusing to overwrite downloaded media'}
$response=Invoke-WebRequest -Uri $url -OutFile $path -PassThru -TimeoutSec 90
$finalUrl=[string]$response.BaseResponse.RequestMessage.RequestUri
if($finalUrl -ne $expectedUrl){throw "Unexpected redirect: $finalUrl"}
$sig=Get-AuthenticodeSignature -LiteralPath $path
$hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
$installer=New-Object -ComObject WindowsInstaller.Installer
$db=$installer.OpenDatabase($path,0)
$properties=@{}
foreach($name in @('ProductVersion','ProductName','Manufacturer','ProductCode','UpgradeCode')) {
    $view=$db.OpenView("SELECT ``Value`` FROM ``Property`` WHERE ``Property``='$name'")
    $view.Execute()
    $record=$view.Fetch()
    $properties[$name]=$record.StringData(1)
    $view.Close()
}
$result=@{source_page='https://learn.microsoft.com/en-us/sql/connect/oledb/release-notes-for-oledb-driver-for-sql-server';source_url=$url;resolved_url=$finalUrl;downloaded_utc=[DateTime]::UtcNow.ToString('o');sha256=$hash;length=(Get-Item $path).Length;signature_status=[string]$sig.Status;signature_message=$sig.StatusMessage;signer=$sig.SignerCertificate.Subject;signer_thumbprint=$sig.SignerCertificate.Thumbprint;timestamp_signer=$sig.TimeStamperCertificate.Subject;msi_properties=$properties;hash_basis='Locally computed SHA-256 of Microsoft HTTPS download; not a separately published Microsoft checksum'}
$result|ConvertTo-Json -Depth 8|Set-Content (Join-Path $EvidenceFolder 'replacement_provenance.json') -Encoding utf8
if($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {throw 'Microsoft signature validation failed'}
if($properties.ProductVersion -notmatch '^18\.7\.6\.' -or $properties.Manufacturer -ne 'Microsoft Corporation'){throw 'Unexpected MSI identity/version'}
foreach($item in @{
    'release_notes'='https://learn.microsoft.com/en-us/sql/connect/oledb/release-notes-for-oledb-driver-for-sql-server';
    'support_matrix'='https://learn.microsoft.com/en-us/sql/connect/oledb/applications/support-policies-for-oledb-driver-for-sql-server';
    'installation'='https://learn.microsoft.com/en-us/sql/connect/oledb/applications/installing-oledb-driver-for-sql-server';
    'sql2022_express_download'='https://www.microsoft.com/en-us/download/details.aspx?id=104781'
}.GetEnumerator()) {
    Invoke-WebRequest -Uri $item.Value -OutFile (Join-Path $EvidenceFolder ($item.Key+'.html')) -TimeoutSec 30
}
$result|ConvertTo-Json -Depth 8
