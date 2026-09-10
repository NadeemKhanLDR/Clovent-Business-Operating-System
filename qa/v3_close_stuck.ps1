. "d:\Clovent Business Operating System\qa\v3_common.ps1"
[QaV3.E]::PostMessage([IntPtr]11999318, 0x0010, [IntPtr]0, [IntPtr]0) | Out-Null
Start-Sleep -Milliseconds 800
[QaV3.E]::PostMessage([IntPtr]9241508, 0x0010, [IntPtr]0, [IntPtr]0) | Out-Null
Start-Sleep -Seconds 2
[QaV3.E]::FindMain($procId)
Log ("owned count: " + @([QaV3.E]::Owned($procId)).Count)
Log ("POS enabled: " + [QaV3.E]::IsWindowEnabled([QaV3.E]::Main))
