param([string]$Project = 'D:\project\ProjectWait')
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$utf8 = [System.Text.UTF8Encoding]::new($false)
$source = 'Assets/M/Scene/Elevator_Safety_Prototype.unity'
$scene = 'Assets/M/Scenes/Elevator_Observation_Choice_Test.unity'
$runtime = 'Assets/M/Scripts/ObservationTrial'
$evidence = 'Assets/M/Documentation/ObservationTrial_20261106'
$ns = 'ProjectWait.M.Elevator.ObservationTrial'

function Read-Project([string]$path) { [IO.File]::ReadAllText((Join-Path $Project $path)) }
function Write-M([string]$path, [string]$text) {
    if (!$path.StartsWith('Assets/M/')) { throw "Output outside M: $path" }
    $full = Join-Path $Project $path
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($full)) | Out-Null
    [IO.File]::WriteAllText($full, $text, $utf8)
}
function Guid-Of([string]$path) {
    if ((Read-Project ($path + '.meta')) -match '(?m)^guid: ([a-f0-9]+)') { return $Matches[1] }
    throw "Missing GUID: $path"
}
function New-Meta([string]$path) {
    $guid = [Guid]::NewGuid().ToString('N')
    Write-M ($path + '.meta') "fileFormatVersion: 2`nguid: $guid`n"
    return $guid
}

if (Test-Path (Join-Path $Project $runtime)) { throw 'Trial already exists. Never overwrite an edited snapshot.' }
if (Test-Path (Join-Path $Project $scene)) { throw 'Test scene already exists.' }

# Snapshot only. Subsequent changes belong to the isolated trial, never its source.
$originals = @{}
$tracked = & git -c core.quotepath=false -C $Project ls-files
foreach ($path in $tracked) {
    $full = Join-Path $Project $path
    if ((Test-Path -LiteralPath $full -PathType Leaf) -and
        ($path -match '^Assets/M/(Scripts|Scene|Prefabs)/|^Assets/Shared Assets/|^Packages/|^ProjectSettings/|^Assets/XR/')) {
        $originals[$path] = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
    }
}
Write-M "$evidence/OriginalHashes.json" ($originals | ConvertTo-Json -Depth 4)

$map = @{}
$copies = @()
$files = Get-ChildItem (Join-Path $Project 'Assets/M/Scripts/Elevator') -Filter '*.cs' |
    Where-Object { $_.BaseName -notin @('ElevatorSettings', 'ElevatorOptimizedOutlineFeature') }
foreach ($file in $files) {
    $from = 'Assets/M/Scripts/Elevator/' + $file.Name
    $to = "$runtime/" + $file.Name
    $text = (Read-Project $from).Replace('namespace ProjectWait.M.Elevator', "namespace $ns")
    $text = $text.Replace('ProjectWait.M.FriendImprovedExpressions', "$ns.FriendImprovedExpressions")
    Write-M $to ("// Isolated test snapshot of $from. Original is not modified.`n" + $text)
    $map[(Guid-Of $from)] = New-Meta $to
    $copies += @{ source = $from; destination = $to }
}
$from = 'Assets/M/Scripts/FriendImprovedExpressions.cs'
$to = "$runtime/FriendImprovedExpressions.cs"
$text = (Read-Project $from).Replace('namespace ProjectWait.M', "namespace $ns").Replace('using ProjectWait.M.Elevator;', "using $ns;")
Write-M $to ("// Isolated test snapshot of $from. Original is not modified.`n" + $text)
$map[(Guid-Of $from)] = New-Meta $to
$copies += @{ source = $from; destination = $to }

# Prefabs carrying copied behaviours must also be independent (not prefab overrides
# applied to the original). Meshes, animation, materials and shaders stay read-only.
$prefabs = @()
$prefabSources = @(
    'Assets/M/UI/Storybook/Prefabs/ThoughtCloudOptimized.prefab',
    'Assets/M/Arts/Models/Rescuers/Rescuer_A_Firefighter.prefab',
    'Assets/M/Arts/Models/Rescuers/Rescuer_B_LiftSpecialist.prefab',
    'Assets/M/Prefabs/ElevatorUI/ReadableSignage.prefab',
    'Assets/M/Prefabs/SportsCentre3F/SportsCentreMap.prefab',
    'Assets/M/Prefabs/FriendImproved/Wait_Friend_Improved.prefab'
)
foreach ($prefabSource in $prefabSources) {
    foreach ($file in Get-Item (Join-Path $Project $prefabSource)) {
        $text = [IO.File]::ReadAllText($file.FullName)
        $matchesCopiedType = $false
        foreach ($guid in @($map.Keys)) { if ($text.Contains($guid)) { $matchesCopiedType = $true; break } }
        if (!$matchesCopiedType) { continue }
        $from = $file.FullName.Substring($Project.Length + 1).Replace('\', '/')
        $to = 'Assets/M/Prefabs/ObservationTrial/' + $file.BaseName + '_' + (Guid-Of $from).Substring(0, 8) + '.prefab'
        $map[(Guid-Of $from)] = New-Meta $to
        $prefabs += @{ source = $from; destination = $to; text = $text }
    }
}
foreach ($copy in $prefabs) {
    $text = $copy.text
    foreach ($guid in @($map.Keys)) { $text = $text.Replace($guid, $map[$guid]) }
    $text = $text.Replace('ProjectWait.M.Elevator.', "$ns.").Replace('ProjectWait.M.FriendImprovedExpressions', "$ns.FriendImprovedExpressions")
    Write-M $copy.destination $text
    $copies += @{ source = $copy.source; destination = $copy.destination }
}
$text = Read-Project $source
foreach ($guid in @($map.Keys)) { $text = $text.Replace($guid, $map[$guid]) }
$text = $text.Replace('ProjectWait.M.Elevator.', "$ns.").Replace('ProjectWait.M.FriendImprovedExpressions', "$ns.FriendImprovedExpressions")
Write-M $scene $text
$sceneGuid = New-Meta $scene
Write-M "$evidence/IsolationManifest.json" (@{ sourceScene = $source; testScene = $scene; sceneGuid = $sceneGuid; runtimeNamespace = $ns; copies = $copies } | ConvertTo-Json -Depth 6)
Write-Output "Created $scene with fresh GUID $sceneGuid and $($copies.Count) isolated script/prefab snapshots."
