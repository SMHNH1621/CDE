$lines = Get-Content -Path "Default.aspx.cs"
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match "DocumentVersions") {
        Write-Output ("Line " + ($i + 1) + ": " + $lines[$i])
    }
}
