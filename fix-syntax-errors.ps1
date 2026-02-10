# PowerShell script to fix orphaned lines from logger removal

$filePath = "MeshCore.Net.SDK\MeshCoreClient.cs"
Write-Host "Processing file: $filePath"

$content = Get-Content $filePath -Raw

# Fix specific orphaned lines that are causing syntax errors
# These are leftover from multi-line _logger calls

# Line 304: hex, timestamp, deviceTime);
$content = $content -replace '(?m)^\s+hex, timestamp, deviceTime\);\s*$', ''

# Lines 559-562: Multi-line string parameters
$content = $content -replace '(?m)^\s+"Starting operation: \{OperationName\} for device: \{DeviceId\}, publicKey=\{PublicKey\}",\s*$', ''
$content = $content -replace '(?m)^\s+operationName,\s*$', ''  
$content = $content -replace '(?m)^\s+deviceId,\s*$', ''
$content = $content -replace '(?m)^\s+publicKey\);\s*$', ''

# Lines 579-581
$content = $content -replace '(?m)^\s+\(byte\)MeshCoreCommand\.CMD_GET_CONTACT_BY_KEY,\s*$', ''
$content = $content -replace '(?m)^\s+firstPayloadByte,\s*$', ''

# Lines 609-614
$content = $content -replace '(?m)^\s+"Operation completed: \{OperationName\} for device: \{DeviceId\} in \{Duration\}ms\. Contact=\{ContactName\} \(\{ContactKey\}\)",\s*$', ''
$content = $content -replace '(?m)^\s+contact\.Name,\s*$', ''
$content = $content -replace '(?m)^\s+contact\.PublicKey\);\s*$', ''

# Lines 628-630
$content = $content -replace '(?m)^\s+"Contact with publicKey=\{PublicKey\} not found on device \{DeviceId\}",\s*$', ''

# Line 1044
$content = $content -replace '(?m)^\s+operationName, deviceId, channelConfig\.Name, channelConfig\.Index\);\s*$', ''

# Save the file
[System.IO.File]::WriteAllText((Resolve-Path $filePath).Path, $content, [System.Text.Encoding]::UTF8)

Write-Host "Fixed syntax errors in $filePath"
