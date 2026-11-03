$ErrorActionPreference = 'Stop'
$root = 'd:\project\ProjectWait'
$dir = Join-Path $root 'Assets\M\CodexTemp\UIReadability_20261103~'

# These four reference captures were clean before this task. The first legacy
# regression run overwrote them; retain that output here, then restore their
# exact original Git bytes. Do not touch any pre-existing user-modified asset.
$captures = @(
    'Assets/M/CodexTemp/DialogueReadability_20261103~/Elevator_Safety_Prototype_01_LobbyReadable.png',
    'Assets/M/CodexTemp/DialogueReadability_20261103~/Elevator_Safety_Prototype_02_ManagerReadable.png',
    'Assets/M/CodexTemp/InteractionFix_20261103~/Elevator_Safety_Prototype_BallHover.png',
    'Assets/M/CodexTemp/InteractionFix_20261103~/Elevator_Safety_Prototype_ThoughtAndHover.png'
)
$archive = Join-Path $dir 'FirstRegressionCaptures'
[System.IO.Directory]::CreateDirectory($archive) | Out-Null
foreach ($relative in $captures) {
    $path = Join-Path $root $relative
    $destination = Join-Path $archive ([System.IO.Path]::GetFileName($path))
    if (-not [System.IO.File]::Exists($destination)) { [System.IO.File]::Copy($path, $destination) }
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command git).Source
    $start.Arguments = 'show HEAD:' + $relative
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $process = [System.Diagnostics.Process]::Start($start)
    $file = [System.IO.File]::Create($path)
    try { $process.StandardOutput.BaseStream.CopyTo($file) }
    finally { $file.Dispose() }
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw ('Failed to restore original capture: ' + $path) }
    $process.Dispose()
}

# Remove only trailing whitespace newly introduced by Unity's scene serializer.
# Retain all pre-existing scene text and line endings.
foreach ($name in 'Elevator_Safety_Prototype', 'Elevator_Safety_ObjectInteraction_Test') {
    $path = Join-Path $root ('Assets\M\Scene\' + $name + '.unity')
    $value = [System.IO.File]::ReadAllText($path)
    $value = [System.Text.RegularExpressions.Regex]::Replace($value, '(?m)^(  m_(?:Name|text):) +(?=\r?$)', '$1')
    [System.IO.File]::WriteAllText($path, $value, [System.Text.UTF8Encoding]::new($false))
}

$results = @()
foreach ($name in 'Elevator_Safety_Prototype', 'Elevator_Safety_ObjectInteraction_Test') {
    $path = Join-Path $dir ($name + '_Verification.json')
    $j = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    $results += [pscustomobject]@{ suite='Readability'; scene=$name; success=$j.success; count=$j.count; failed=$j.failed; error=$j.error }
    foreach ($suite in 'DialogueRegression', 'InteractionRegression', 'PolishRegression') {
        $path = Join-Path $dir ($suite + '\' + $name + '_Verification.json')
        $j = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
        $results += [pscustomobject]@{ suite=$suite; scene=$name; success=$j.success; count=$j.checks.Count; failed=@($j.checks | Where-Object { -not $_.passed }).Count; error=$j.error }
    }
}
$summary = [pscustomobject]@{
    success = @($results | Where-Object { -not $_.success -or $_.failed -gt 0 }).Count -eq 0
    totalChecks = ($results | Measure-Object -Property count -Sum).Sum
    results = $results
    physicalHMDTested = $false
}
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $dir 'FinalSummary.json') -Encoding UTF8
$summary | Select-Object success,totalChecks | Format-List | Out-String
git -C $root diff --check -- Assets/M/Scripts Assets/M/Scene
git -C $root check-ignore -v AGENTS.md
git -C $root status --short