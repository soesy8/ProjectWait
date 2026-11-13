$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$guids = @{}
Get-ChildItem (Join-Path $root 'Assets') -Recurse -Filter '*.meta' -File | Where-Object { $_.FullName -notlike '*VRImprovement_20261113~*' } | ForEach-Object {
    $m = [regex]::Match([IO.File]::ReadAllText($_.FullName), '(?m)^guid: ([a-f0-9]{32})\r?$')
    if ($m.Success) { $guids[$m.Groups[1].Value] = $_.FullName }
}
$checks = @()
$scenes = @{ Wait_Robby = @('TitlePlazaPlayer', 'TitlePortal'); Wait_Elevator = @('ElevatorView','ElevatorSpatialUI','ElevatorCinematicOverlay','ElevatorTutorialPreview') }
foreach ($name in $scenes.Keys) {
    $text = [IO.File]::ReadAllText((Join-Path $root "Assets/M/Scene/$name.unity"))
    $documents = @{}
    [regex]::Matches($text, '(?ms)^--- !u!\d+ &(\d+)[^\n]*\n(.*?)(?=^--- |\z)') | ForEach-Object { $documents[$_.Groups[1].Value] = $_.Groups[2].Value }
    foreach ($kind in $scenes[$name]) {
        $found = 0
        foreach ($id in $documents.Keys) {
            $doc = $documents[$id]
            if ($doc -notmatch "(?m)^  m_EditorClassIdentifier: .*\.$kind`r?`$") { continue }
            $found++
            foreach ($reference in [regex]::Matches($doc, '\{fileID: (-?\d+)([^}]*)\}')) {
                $target = $reference.Groups[1].Value
                if ($target -eq '0') { continue }
                $guid = [regex]::Match($reference.Groups[2].Value, 'guid: ([a-f0-9]{32})')
                if ($guid.Success) {
                    if (!$guids.ContainsKey($guid.Groups[1].Value)) { throw "$name $kind missing GUID $($guid.Groups[1].Value)" }
                } elseif (!$documents.ContainsKey($target)) { throw "$name $kind missing scene object $target" }
            }
            if ($kind -eq 'TitlePlazaPlayer' -and $doc -notmatch '(?m)^  interactionDistance: 8\r?$') { throw 'Lobby scene is not 8m' }
            if ($kind -eq 'ElevatorSpatialUI' -and $doc -notmatch '(?m)^  readingDistance: 1.4\r?$') { throw 'Reading distance changed' }
            $checks += @{ scene=$name; component=$kind; fileID=$id; references='resolved' }
        }
        if (!$found) { throw "Missing $kind in $name" }
    }
}
Push-Location $root
try {
    foreach ($path in @('Assets/M/Scripts/DialoguePreview/ElevatorTypography.cs','Assets/M/Scripts/DialoguePreview/ElevatorFlow.cs','Assets/M/Scripts/DialoguePreview/ElevatorDialogue.cs','Assets/M/Scene/Wait_Elevator.unity')) {
        if (git diff --numstat HEAD -- $path) { throw "Unexpected change to $path" }
    }
} finally { Pop-Location }
$result = @{ checks=$checks; globalTypographyDialogueFlowAndElevatorScene='unchanged'; note='Static YAML/GUID checks only; not a runtime scene or headset test.' } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'scene-reference-check.json'), $result)
$result
