$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$records = @()
$bytes = [IO.File]::ReadAllBytes((Join-Path $PSScriptRoot '../VRUIComfort_20261113~/player-api/Assembly-CSharp.dll'))
$release = [Text.Encoding]::ASCII.GetString($bytes)
$editor = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes((Join-Path $PSScriptRoot '../VRUIComfort_20261113~/editor-api/Assembly-CSharp.dll')))
foreach ($binding in @('<Keyboard>/f8','ElevatorChoice')) {
    $pattern = [Text.Encoding]::ASCII.GetString([Text.Encoding]::Unicode.GetBytes($binding))
    if (!$editor.Contains($pattern)) { throw "Editor positive control missing: $binding" }
    if ($release.Contains($pattern)) { throw "Release still contains development binding: $binding" }
    $records += "PASS: Editor contains / non-Editor non-development DLL omits $binding"
}
$restart = [Text.Encoding]::ASCII.GetString([Text.Encoding]::Unicode.GetBytes('<Keyboard>/r'))
if (!$release.Contains($restart)) { throw 'Public PC restart binding was removed' }
$records += 'PASS: Release API DLL retains public R restart binding'
foreach ($getter in @('get_kKey','get_f8Key','get_f9Key')) {
    if (!$editor.Contains($getter) -or $release.Contains($getter)) { throw "Preview key preprocessing check failed: $getter" }
    $records += "PASS: Editor contains / release DLL omits preview key reference $getter"
}
$records += 'LIMIT: API/preprocessor check only; not a platform build or real device input test'
$records | Set-Content -Encoding utf8 (Join-Path $PSScriptRoot 'release-shortcuts.txt')

$inventory = Get-ChildItem (Join-Path $root 'Assets/M/CodexTemp') -Directory -Force | ForEach-Object {
    $files = @(Get-ChildItem $_.FullName -Recurse -File -Force)
    [ordered]@{folder=$_.Name;files=$files.Count;bytes=($files | Measure-Object Length -Sum).Sum}
}
$inventory | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $PSScriptRoot 'temp-inventory.json')
$hashes = Get-ChildItem (Join-Path $root 'Assets/M/Scripts') -Recurse -Filter '*.cs' -File | ForEach-Object {
    [ordered]@{path=$_.FullName.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}
}
$hashes | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $PSScriptRoot 'source-hashes.json')

$outside = @(& git -C $root diff --name-only -- | Where-Object { $_ -and $_ -notlike 'Assets/M/*' })
if ($LASTEXITCODE -ne 0 -or $outside.Count) { throw ('Unexpected outside-M content differences: '+($outside -join ', ')) }
$protected = @('Assets/M/Scene/Wait_Elevator.unity','Assets/M/Scene/Wait_Robby.unity','Packages/manifest.json','Packages/packages-lock.json','Assets/XR/Settings/OpenXRPackageSettings.asset','ProjectSettings/EditorBuildSettings.asset','ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json')
$preservation = foreach ($path in $protected) {
    $expected = & git -C $root rev-parse "HEAD:$path"
    $actual = & git -C $root hash-object -- $path
    if ($expected -ne $actual) { throw "Protected content changed: $path" }
    [ordered]@{path=$path;gitContentHash=$actual;matchesHead=$true}
}
$preservation | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $PSScriptRoot 'preservation.json')
Write-Output ($records -join "`n")
Write-Output 'PASS: Scene content/GUID references, packages and protected project/XR settings match HEAD; no outside-M content diff.'
