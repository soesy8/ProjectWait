$ErrorActionPreference='Stop'
[Console]::OutputEncoding=New-Object Text.UTF8Encoding($false)
$OutputEncoding=[Console]::OutputEncoding
$r='D:\project\ProjectWait'
$d="$r\Assets\M\CodexTemp\UISmooth_20261103~"
$u='C:\Users\MBC\AppData\Local\Unity\bin\unity.exe'
function CallUnity([string]$name,[string[]]$commandArgs) {
    # Decode the CLI pipe explicitly. Windows PowerShell otherwise corrupts Hangul
    # and can turn valid JSON strings into invalid JSON under a legacy console code page.
    $start=New-Object Diagnostics.ProcessStartInfo
    $start.FileName=$u
    $start.Arguments="--format json command --project-path $r --timeout 120 "+($commandArgs -join ' ')
    $start.UseShellExecute=$false
    $start.RedirectStandardOutput=$true
    $start.RedirectStandardError=$true
    $start.StandardOutputEncoding=[Text.Encoding]::UTF8
    $process=New-Object Diagnostics.Process
    $process.StartInfo=$start
    $null=$process.Start()
    $raw=$process.StandardOutput.ReadToEnd()
    $stderr=$process.StandardError.ReadToEnd()
    $process.WaitForExit()
    [IO.File]::WriteAllText("$d\$name.json",$raw,[Text.Encoding]::UTF8)
    $result=$raw|ConvertFrom-Json
    if(!$result.success){throw "$name CLI failed: $raw"}
    return $result
}
function WaitMode([string]$mode) {
    for($i=0;$i -lt 30;$i++) {
        Start-Sleep -Milliseconds 800
        try {$s=CallUnity 'RunnerStatus' @('editor_status');if($s.data.result.playMode -eq $mode -and !$s.data.result.compiling){return}}catch{}
    }
    throw "Editor did not reach $mode"
}
try {
    foreach($scene in @('Prototype','Test')) {
        CallUnity 'RunnerStop' @('editor_stop')|Out-Null
        WaitMode 'stopped'
        $entry=if($scene -eq 'Prototype'){'OpenInteractionScene.Restore'}else{'OpenInteractionScene.Main'}
        $open=CallUnity ($scene+'Open') @('run_script','--file',"$r\Assets\M\CodexTemp\InteractionFix_20261103~\OpenScene.cs",'--entry',$entry)
        if(!$open.data.result.success){throw "$scene open failed"}
        CallUnity ($scene+'Play') @('editor_play')|Out-Null
        WaitMode 'playing'
        Start-Sleep -Seconds 2
        $tests=@(
            @('Envelope',"$d\Verify.cs"),
            @('Readability',"$d\ReadabilityRegression.cs"),
            @('Polish',"$d\PolishRegression.cs"),
            @('Camera',"$r\Assets\M\CodexTemp\InteractionPolish_20261103~\CameraRegression.cs"),
            @('Interaction',"$r\Assets\M\CodexTemp\InteractionFix_20261103~\Verify.cs")
        )
        foreach($test in $tests) {
            $result=CallUnity ($scene+$test[0]) @('run_script','--file',$test[1])
            if(!$result.data.result.success -or !$result.data.result.result.success){throw "$scene $($test[0]) failed"}
        }
    }
    CallUnity 'RunnerStop' @('editor_stop')|Out-Null
    WaitMode 'stopped'
    CallUnity 'RestorePrototype' @('run_script','--file',"$r\Assets\M\CodexTemp\InteractionFix_20261103~\OpenScene.cs",'--entry','OpenInteractionScene.Restore')|Out-Null
    CallUnity 'SavedValidation' @('run_script','--file',"$r\Assets\M\CodexTemp\DialogueReadability_20261103~\ValidateSaved.cs")|Out-Null
    CallUnity 'CompilationFinal' @('recompile_status')|Out-Null
    CallUnity 'ConsoleFinal' @('console','--level','error','--tail','15')|Out-Null
    CallUnity 'EditorFinal' @('editor_status')|Out-Null
    $lines=@();$total=0
    foreach($scene in @('Prototype','Test')) {foreach($name in @('Envelope','Readability','Polish','Camera','Interaction')) {
        $q=([IO.File]::ReadAllText("$d\$scene$name.json")|ConvertFrom-Json).data.result.result
        $pass=@($q.checks|Where-Object{$_.passed}).Count;$total+=$pass
        $lines+="$scene$name success=$($q.success) passed=$pass/$($q.checks.Count)"
    }}
    $lines+="Total passed=$total"
    [IO.File]::WriteAllLines("$d\FinalSummary.txt",$lines,[Text.Encoding]::UTF8)
    [IO.File]::WriteAllText("$d\RunnerDone.txt",'SUCCESS',[Text.Encoding]::UTF8)
} catch {
    [IO.File]::WriteAllText("$d\RunnerDone.txt",($_|Out-String),[Text.Encoding]::UTF8)
    exit 1
}