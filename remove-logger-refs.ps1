# PowerShell script to remove all _logger references from MeshCoreClient.cs

$filePath = "MeshCore.Net.SDK\MeshCoreClient.cs"
Write-Host "Processing file: $filePath"

$content = Get-Content $filePath -Raw
$lines = $content -split "`r?`n"
$newLines = New-Object System.Collections.ArrayList
$removedCount = 0

foreach ($line in $lines) {
    # Skip lines that contain _logger but NOT MeshCoreSdkEventSource
    if ($line -match '_logger\.' -and $line -notmatch 'MeshCoreSdkEventSource') {
        $removedCount++
        continue
    }
    [void]$newLines.Add($line)
}

# Join lines back together
$newContent = $newLines -join "`r`n"

# Save the file
[System.IO.File]::WriteAllText((Resolve-Path $filePath).Path, $newContent, [System.Text.Encoding]::UTF8)

Write-Host "Removed $removedCount lines containing _logger references"
Write-Host "File updated successfully"
