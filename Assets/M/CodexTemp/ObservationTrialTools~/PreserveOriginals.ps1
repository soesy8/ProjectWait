# Restore only verified Unity-generated writes to the exact captured user state.
# Never use a Git checkout: OpenXR was already modified before this task started.
param([string]$Evidence = 'Assets/M/Documentation/ObservationTrial_20261106/')
$ErrorActionPreference = 'Stop'
if (!$Evidence.StartsWith('Assets/M/')) { throw 'Evidence must remain under Assets/M.' }
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$baseline = Get-Content (Join-Path $project 'Assets/M/Documentation/ObservationTrial_20261106/OriginalHashes.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Hash([byte[]]$bytes) { [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($bytes)).Replace('-', '') }
$restored = @()
$xr = 'Assets/XR/Settings/OpenXRPackageSettings.asset'
$backup = Join-Path $PSScriptRoot 'OriginalOpenXR.bytes'
if ((Get-FileHash (Join-Path $project $xr) -Algorithm SHA256).Hash -ne $baseline.$xr) {
    [byte[]]$original = [IO.File]::ReadAllBytes($backup)
    if ((Hash $original) -ne $baseline.$xr) { throw 'OpenXR backup is not the exact original.' }
    $oldText = [Text.Encoding]::UTF8.GetString($original)
    $newText = [IO.File]::ReadAllText((Join-Path $project $xr))
    $before = $oldText -split '\r?\n'
    $after = $newText -split '\r?\n'
    if ($before.Count -ne $after.Count) { throw 'OpenXR structure changed, refusing restoration.' }
    $changed = 0
    for ($i = 0; $i -lt $before.Count; $i++) {
        if ($before[$i] -eq $after[$i]) { continue }
        if ($before[$i] -notmatch '^  - \{fileID: (-?\d+)\}$') { throw "Unexpected OpenXR change at line $($i+1)." }
        $oldId = $Matches[1]
        if ($after[$i] -notmatch '^  - \{fileID: (-?\d+)\}$') { throw 'Unexpected OpenXR reference format.' }
        $newId = $Matches[1]
        $oldBlock = [regex]::Match($oldText, '(?ms)^--- !u!114 &' + [regex]::Escape($oldId) + '\s.*?(?=^--- !u!|\z)').Value
        $newBlock = [regex]::Match($newText, '(?ms)^--- !u!114 &' + [regex]::Escape($newId) + '\s.*?(?=^--- !u!|\z)').Value
        $oldClass = [regex]::Match($oldBlock, 'm_Script: \{fileID: \d+, guid: ([a-f0-9]+)').Groups[1].Value
        $newClass = [regex]::Match($newBlock, 'm_Script: \{fileID: \d+, guid: ([a-f0-9]+)').Groups[1].Value
        if ($oldClass -ne $newClass -or $oldClass -notin @('12d3bf61b014a48a2b7564c61da9d7ad','90ae1e8c3c7a542bba339faf2e771c5a','6c36843bac48b29469f79baf3c98f9b6')) {
            throw 'OpenXR change is not the known automatic Android XR feature rebinding.'
        }
        $changed++
    }
    if ($changed -gt 3) { throw 'More than three automatic references changed.' }
    [IO.File]::WriteAllBytes((Join-Path $PSScriptRoot 'AutomaticOpenXR.after-run.bytes'), [IO.File]::ReadAllBytes((Join-Path $project $xr)))
    [IO.File]::WriteAllBytes((Join-Path $project $xr), $original)
    $restored += $xr
}
$iet = 'ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json'
if ((Get-FileHash (Join-Path $project $iet) -Algorithm SHA256).Hash -ne $baseline.$iet) {
    $text = [IO.File]::ReadAllText((Join-Path $project $iet))
    $candidate = [Text.UTF8Encoding]::new($false).GetBytes(($text -replace '\r?\n', "`r`n"))
    if ((Hash $candidate) -ne $baseline.$iet) { throw 'IET change was not just automatic line-ending normalization.' }
    [IO.File]::WriteAllBytes((Join-Path $project $iet), $candidate)
    $restored += $iet
}
$changedOriginals = @()
foreach ($entry in $baseline.PSObject.Properties) {
    $full = Join-Path $project $entry.Name
    if (!(Test-Path -LiteralPath $full) -or (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash -ne $entry.Value) { $changedOriginals += $entry.Name }
}
$fontBaseline = Get-Content (Join-Path $project 'Assets/M/Documentation/ObservationTrial_20261106/OriginalFontBaseline.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$fontPreserved = (Get-FileHash (Join-Path $project $fontBaseline.path) -Algorithm SHA256).Hash -eq $fontBaseline.sha256
if (!$fontPreserved) { $changedOriginals += $fontBaseline.path }
$report = @{ checked = @($baseline.PSObject.Properties).Count + 1; changedOriginals = $changedOriginals; restoredOnlyKnownAutomaticWrites = $restored; sharedFontMatchesCleanBaseline = $fontPreserved; preExistingUserChangesPreserved = $changedOriginals.Count -eq 0 }
$path = Join-Path $project (Join-Path $Evidence 'OriginalPreservation.json')
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
[IO.File]::WriteAllText($path, ($report | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
$report | ConvertTo-Json -Depth 5
if ($changedOriginals.Count -ne 0) { throw 'Original preservation check failed.' }
