. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
$pos = GetPos
if (-not $pos) { Log "ERROR: no POS window"; exit 1 }
[QaV3.E]::SetForegroundWindow([IntPtr]$pos.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 700

$orderText = Get-OrderPaneText
Log ("order pane: '$orderText'")
if ($orderText -notmatch '^ORD-') { Log "ERROR: no active order"; exit 1 }

# add items
$adds = @(
    @(915, 873,  "Chicken Biryani 450"),
    @(1571, 2034, "Aloo Gobi Full 380"),
    @(1331, 2034, "Aloo Gobi Half 250"),
    @(2107, 1453, "White Daal Mash Full 340"),
    @(2107, 2034, "Koyla Half 350")
)
foreach ($a in $adds) { ClickXY $a[0] $a[1] 1300; Log ("added: " + $a[2]) }
Start-Sleep -Seconds 2
Guard-Modals
Capture-Pos "v3_w3_cart5.png" "cart with 5 items"
Dump-Pos "v3_w3_cart5_uia.txt"
Log "=== PHASE 3 COMPLETE ==="
