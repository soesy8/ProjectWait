$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$harness = Join-Path $PSScriptRoot 'Harness'
$result = Join-Path $harness 'Assets/M/CodexTemp/VRImprovement_20261113~/validation.txt'
if (Test-Path $result) { [IO.File]::Delete($result) }
[IO.Directory]::CreateDirectory((Join-Path $harness 'Assets/Editor')) | Out-Null
Copy-Item (Join-Path $root 'Assets/M/Scripts/DialoguePreview/StereoUISafeArea.cs') (Join-Path $harness 'Assets/StereoUISafeArea.cs')
Copy-Item (Join-Path $root 'Assets/M/Scripts/TitleScene/TitlePlazaPlayer.cs') (Join-Path $harness 'Assets/TitlePlazaPlayer.cs')
Copy-Item (Join-Path $root 'Assets/M/Scripts/TitleScene/TitleLobbyControllerVisuals.cs') (Join-Path $harness 'Assets/TitleLobbyControllerVisuals.cs')
Copy-Item (Join-Path $root 'Assets/M/Scripts/TitleScene/TitleControllerPose.cs') (Join-Path $harness 'Assets/TitleControllerPose.cs')
Copy-Item (Join-Path $root 'Assets/M/Scripts/TitleScene/TitlePortal.cs') (Join-Path $harness 'Assets/TitlePortal.cs')
Copy-Item (Join-Path $root 'Assets/M/Scripts/Editor/VRImprovementValidation.cs') (Join-Path $harness 'Assets/Editor/VRImprovementValidation.cs')
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $harness + '"'), '-executeMethod', 'ProjectWait.M.EditorTools.VRImprovementValidation.RunHarnessInPlayMode', '-logFile', ('"' + (Join-Path $PSScriptRoot 'harness-editor.log') + '"'))
$process = Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Editor/Unity.exe' -ArgumentList $arguments -Wait -PassThru
$code = $process.ExitCode
if (Test-Path $result) { Copy-Item $result (Join-Path $PSScriptRoot 'harness-validation.txt'); Get-Content $result }
if ($code -ne 0) { throw "Unity exited with code $code. See harness-editor.log." }
if (!(Test-Path $result) -or (Select-String -Path $result -Pattern '^FAIL:')) { throw 'Validation missing or failed. See harness-validation.txt.' }
