$ErrorActionPreference='Stop'
$r='D:\project\ProjectWait'
$d="$r\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~"
function Call([string]$name,[string[]]$arguments) {
    $start=New-Object Diagnostics.ProcessStartInfo
    $start.FileName='C:\Users\MBC\AppData\Local\Unity\bin\unity.exe'
    $start.Arguments="--format json command --project-path $r --timeout 120 "+($arguments -join ' ')
    $start.UseShellExecute=$false;$start.RedirectStandardOutput=$true;$start.StandardOutputEncoding=[Text.Encoding]::UTF8
    $p=New-Object Diagnostics.Process;$p.StartInfo=$start;$null=$p.Start();$raw=$p.StandardOutput.ReadToEnd();$p.WaitForExit()
    [IO.File]::WriteAllText("$d\$name.json",$raw,[Text.Encoding]::UTF8)
    $q=$raw|ConvertFrom-Json;if(!$q.success){throw "$name failed"};return $q
}
try {
    Call 'BeforePlay' @('editor_play')|Out-Null
    for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 800;try{$q=Call 'ReproduceStatus' @('editor_status');if($q.data.result.playMode -eq 'playing'){break}}catch{}}
    Start-Sleep -Seconds 2
    $q=Call 'Reproduction' @('run_script','--file',"$d\Reproduce.cs")
    if(!$q.data.result.success){throw 'Reproduction failed'}
    Call 'AfterReproduceStop' @('editor_stop')|Out-Null
    [IO.File]::WriteAllText("$d\ReproduceDone.txt",'SUCCESS',[Text.Encoding]::UTF8)
}catch{[IO.File]::WriteAllText("$d\ReproduceDone.txt",($_|Out-String),[Text.Encoding]::UTF8)}