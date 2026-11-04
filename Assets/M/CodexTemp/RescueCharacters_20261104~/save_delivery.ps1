$ErrorActionPreference = 'Stop'
$deliverySource = Join-Path $PSScriptRoot 'Delivery'
$deliveryTarget = [System.IO.Path]::GetFullPath('E:\ProjectWait\Blender\구조요원')
if ($deliveryTarget -ne 'E:\ProjectWait\Blender\구조요원') { throw 'Unexpected destination' }
$validationResult = Get-Content -LiteralPath (Join-Path $deliverySource 'Validation_구조요원.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($validationResult.status -ne 'PASS') { throw 'Model validation has not passed' }
New-Item -ItemType Directory -Path $deliveryTarget -Force | Out-Null
$deliveryFiles = @(Get-ChildItem -LiteralPath $deliverySource -File)
foreach ($deliveryFile in $deliveryFiles) {
    Copy-Item -LiteralPath $deliveryFile.FullName -Destination (Join-Path $deliveryTarget $deliveryFile.Name) -Force
}
$hashErrors = @()
foreach ($deliveryFile in $deliveryFiles) {
    $sourceHash = (Get-FileHash -LiteralPath $deliveryFile.FullName -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath (Join-Path $deliveryTarget $deliveryFile.Name) -Algorithm SHA256).Hash
    if ($sourceHash -ne $targetHash) { $hashErrors += $deliveryFile.Name }
}
if ($hashErrors.Count -gt 0) { throw ('Copy hash mismatch: ' + ($hashErrors -join ', ')) }
$copyResult = [ordered]@{
    destination = $deliveryTarget
    file_count = $deliveryFiles.Count
    total_bytes = ($deliveryFiles | Measure-Object -Property Length -Sum).Sum
    sha256_all_match = $true
    model_validation = $validationResult.status
    files = @($deliveryFiles | ForEach-Object { $_.Name })
}
$copyJson = $copyResult | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'Delivery_Copy_Validation.json'), $copyJson, [System.Text.UTF8Encoding]::new($false))
$copyJson
