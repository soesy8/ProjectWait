param([switch]$Player, [switch]$EditorTests)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$source = Join-Path $root 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp'
$mode = if ($Player) { 'player-api' } else { 'editor-api' }
if ($EditorTests) { $source = Join-Path $root 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp'; $mode = 'editor-tests' }
$output = Join-Path $PSScriptRoot $mode
[IO.Directory]::CreateDirectory($output) | Out-Null
$lines = Get-Content $source | Where-Object { $_ -notmatch '^-(out|refout|analyzer|additionalfile):' }
if ($Player) { $lines = $lines | Where-Object { $_ -notmatch '^-define:UNITY_EDITOR' } }
if ($EditorTests) {
    $lines = $lines | ForEach-Object {
        if ($_ -match '^-r:.*[/\\]Assembly-CSharp\.ref\.dll') { '-r:"' + (Join-Path $PSScriptRoot 'editor-api/Assembly-CSharp.dll') + '"' } else { $_ }
    }
    $lines += '-out:"' + (Join-Path $output 'Assembly-CSharp-Editor.dll') + '"'
    if (-not ($lines -match 'VRImprovementValidation.cs')) { $lines += '"Assets/M/Scripts/Editor/VRImprovementValidation.cs"' }
} else {
    $lines += '-out:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
    if (-not ($lines -match 'StereoUISafeArea.cs')) { $lines += '"Assets/M/Scripts/DialoguePreview/StereoUISafeArea.cs"' }
}
$rsp = Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($rsp, $lines, [Text.UTF8Encoding]::new($false))
$editor = 'C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Editor/Data'
Push-Location $root
try {
    & "$editor/netcorerun/netcorerun.exe" "$editor/DotNetSdkRoslyn/csc.dll" "@$rsp" 2>&1 | Tee-Object (Join-Path $output 'compile.log')
    if ($LASTEXITCODE -ne 0) { throw "Compiler exit code: $LASTEXITCODE" }
} finally { Pop-Location }
