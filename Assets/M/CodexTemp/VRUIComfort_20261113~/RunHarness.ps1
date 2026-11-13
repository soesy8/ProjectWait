$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$harness=Join-Path $PSScriptRoot 'Harness'
$existing=Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'"|Where-Object{$_.CommandLine -like ('*'+$harness+'*') -and $_.CommandLine -notlike '*AssetImportWorker*'}
if($existing){Wait-Process -Id $existing.ProcessId -ErrorAction SilentlyContinue}
$result=Join-Path $harness 'Assets/M/CodexTemp/VRUIComfort_20261113~/validation.txt'
if(Test-Path $result){[IO.File]::Delete($result)}
[IO.Directory]::CreateDirectory((Join-Path $harness 'Assets/Editor'))|Out-Null
[IO.Directory]::CreateDirectory((Join-Path $harness 'Assets/Plugins'))|Out-Null
[IO.Directory]::CreateDirectory((Join-Path $harness 'Assets/Resources'))|Out-Null
Copy-Item (Join-Path $PSScriptRoot 'plugin/VRComfortRuntime.dll') (Join-Path $harness 'Assets/Plugins/VRComfortRuntime.dll')
Copy-Item (Join-Path $root 'Assets/M/Scripts/Editor/VRUIComfortValidation.cs') (Join-Path $harness 'Assets/Editor/VRUIComfortValidation.cs')
# No original meta/GUID is copied; the isolated project's assets get their own identities.
if(!(Test-Path (Join-Path $harness 'Assets/ValidationFont.otf'))){Copy-Item (Join-Path $root 'Assets/M/Arts/Fonts/NotoSansCJKkr-Medium.otf') (Join-Path $harness 'Assets/ValidationFont.otf')}
foreach($name in @('TMP Settings.asset','LineBreaking Leading Characters.txt','LineBreaking Following Characters.txt')){
    Copy-Item (Join-Path $root ('Assets/TextMesh Pro/Resources/'+$name)) (Join-Path $harness ('Assets/Resources/'+$name))
}
$shaders=Join-Path $root 'Assets/TextMesh Pro/Shaders'
Get-ChildItem $shaders -Recurse -File|Where-Object{$_.Extension -ne '.meta'}|ForEach-Object{
    $relative=$_.FullName.Substring($shaders.Length+1)
    $destination=Join-Path $harness ('Assets/TMPShaders/'+$relative)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))|Out-Null
    Copy-Item $_.FullName $destination
}
$arguments=@('-batchmode','-nographics','-disable-assembly-updater','-projectPath',('"'+$harness+'"'),'-executeMethod','ProjectWait.M.EditorTools.VRUIComfortValidation.Run','-quit','-logFile',('"'+(Join-Path $PSScriptRoot 'harness-editor.log')+'"'))
$process=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Editor/Unity.exe' -ArgumentList $arguments -Wait -PassThru
if(Test-Path $result){Copy-Item $result (Join-Path $PSScriptRoot 'validation.txt');Get-Content $result}
if($process.ExitCode -ne 0){throw "Unity exited with code $($process.ExitCode). See harness-editor.log."}
if(!(Test-Path $result) -or (Select-String $result -Pattern '^FAIL:')){throw 'Validation failed or missing.'}
