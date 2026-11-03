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
    $q=$raw|ConvertFrom-Json;if(!$q.success){throw "$name CLI failed"};return $q
}
function WaitMode([string]$mode) {
    for($i=0;$i -lt 30;$i++){Start-Sleep -Milliseconds 800;try{$q=Call 'RunnerStatus' @('editor_status');if($q.data.result.playMode -eq $mode -and !$q.data.result.compiling){return}}catch{}}
    throw "Editor did not reach $mode"
}
$tests=@(
    @('Header',"$d\Verify.cs"),
    @('Fade',"$d\FadeRegression.cs"),
    @('Readability',"$d\ReadabilityRegression.cs"),
    @('Polish',"$d\PolishRegression.cs"),
    @('Camera',"$r\Assets\M\CodexTemp\InteractionPolish_20261103~\CameraRegression.cs"),
    @('Interaction',"$r\Assets\M\CodexTemp\InteractionFix_20261103~\Verify.cs")
)
try {
    WaitMode 'stopped'
    # Deliberately do not open, save, clear dirtiness, or replace the current scene.
    $inspect=Call 'InspectionAtTestStart' @('run_script','--file',"$d\Inspect.cs")
    Call 'PlayCurrent' @('editor_play')|Out-Null
    WaitMode 'playing';Start-Sleep -Seconds 2
    foreach($test in $tests){$q=Call $test[0] @('run_script','--file',$test[1]);if(!$q.data.result.success -or !$q.data.result.result.success){throw "$($test[0]) failed"}}
    Call 'StopCurrent' @('editor_stop')|Out-Null;WaitMode 'stopped'
    Call 'InspectionAfterTests' @('run_script','--file',"$d\Inspect.cs")|Out-Null
    Call 'CompilationFinal' @('recompile_status')|Out-Null
    Call 'ConsoleFinal' @('console','--level','error','--tail','15')|Out-Null
    Call 'EditorFinal' @('editor_status')|Out-Null
    $lines=@();$total=0
    foreach($test in $tests){$q=([IO.File]::ReadAllText("$d\$($test[0]).json")|ConvertFrom-Json).data.result.result;$pass=@($q.checks|Where-Object{$_.passed}).Count;$total+=$pass;$lines+="$($test[0]) success=$($q.success) passed=$pass/$($q.checks.Count)"}
    $lines+="Total passed=$total";[IO.File]::WriteAllLines("$d\FinalSummary.txt",$lines,[Text.Encoding]::UTF8)
    [IO.File]::WriteAllText("$d\RunnerDone.txt",'SUCCESS',[Text.Encoding]::UTF8)
}catch {
    [IO.File]::WriteAllText("$d\RunnerDone.txt",($_|Out-String),[Text.Encoding]::UTF8)
    try{Call 'StopAfterFailure' @('editor_stop')|Out-Null}catch{}
}