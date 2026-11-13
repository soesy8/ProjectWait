$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
function Need([bool]$condition,[string]$message){if(!$condition){throw $message}}
function Documents([string]$text){
    $result=@{}
    foreach($m in [regex]::Matches($text,'(?ms)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?\n(.*?)(?=^--- !u!|\z)')){$result[$m.Groups[2].Value]=@{Type=[int]$m.Groups[1].Value;Body=$m.Groups[3].Value}}
    return $result
}
$guids=@{}
Get-ChildItem (Join-Path $root 'Assets') -Filter '*.meta' -File -Recurse|Where-Object{$_.FullName -notmatch '[\\/][^\\/]*~[\\/]'}|ForEach-Object{
    $m=[regex]::Match([IO.File]::ReadAllText($_.FullName),'(?m)^guid: ([a-f0-9]{32})\r?$')
    if($m.Success){$id=$m.Groups[1].Value;Need (!$guids.ContainsKey($id)) ('Duplicate live asset GUID: '+$id);$guids[$id]=$_.FullName}
}
$report=[Collections.Generic.List[string]]::new()
$scene=[IO.File]::ReadAllText((Join-Path $root 'Assets/M/Scene/Wait_Elevator.unity'))
$docs=Documents $scene
Push-Location $root
try{
    $baseline=(git show HEAD:Assets/M/Scene/Wait_Elevator.unity) -join "`n"
    $expected=$baseline.Replace('m_LocalPosition: {x: 0, y: -0.2549641, z: 0}','m_LocalPosition: {x: 0, y: 1.1064756, z: 0}').Replace('m_LocalPosition: {x: 0, y: 1.36144, z: 0}','m_LocalPosition: {x: 0, y: 0, z: 0}')
    Need (($scene -replace "`r",'').TrimEnd() -ceq $expected.TrimEnd()) 'Elevator scene changed beyond the two approved camera-offset transforms'
    $report.Add('PASS: Elevator scene differs only in saved Camera Offset Y and initial eye Y; gym spawn, floor coordinates and deferred test objects are preserved.')
    foreach($file in @('Assets/M/VRPreview','Assets/M/Scripts/TitleScene/LobbyDissolveTest.cs','Assets/M/Scripts/DialoguePreview/ElevatorView.cs','Assets/M/Scripts/DialoguePreview/ElevatorFlow.cs','Assets/M/Scripts/DialoguePreview/ElevatorDialogue.cs')){
        Need (!(git diff --numstat HEAD -- $file)) ('Deferred/story implementation changed: '+$file)
    }
    $report.Add('PASS: Deferred preview/test/developer binding and story-flow/dialogue implementations are unchanged.')
}finally{Pop-Location}
$spatial=[IO.File]::ReadAllText((Join-Path $root 'Assets/M/Scripts/DialoguePreview/ElevatorSpatialUI.cs'))
Need ($spatial.Contains('bool show=!view.lesson.Flow.IsCinematic&&status&&!complete&&(screenOnly||!DialogueOnScreen());')) 'Goal visibility layout policy changed'
Need ($spatial.Contains('bool wantsStatus=!view.lesson.Flow.IsCinematic&&DescribeStatus(out _,out _)&&!complete&&(screenOnly||!DialogueOnScreen())||KeepOutgoingHudStatus();')) 'Goal visibility tick policy changed'
$report.Add('PASS: Both goal-card gaze/visibility policy expressions remain unchanged.')
foreach($hand in @(@{Name='Left';Root='758605768'},@{Name='Right';Root='1131101901'})){
    $path=Join-Path $root ('Assets/M/Prefabs/Controllers/'+$hand.Name+'AimController.prefab')
    $prefab=[IO.File]::ReadAllText($path);$prefabDocs=Documents $prefab
    Need ($prefabDocs.Count -eq 42) 'Controller visual hierarchy is incomplete'
    foreach($id in $prefabDocs.Keys){
        $doc=$prefabDocs[$id];$original=$docs[$id].Body
        Need ($doc.Type -in @(1,4,23,33)) 'Controller prefab contains a behavior/collider'
        if($doc.Type -eq 4){
            $expectedTransform=if($id -eq $hand.Root){[regex]::Replace($original,'(?m)^  m_Father: .*','  m_Father: {fileID: 0}')}else{$original}
            Need ($doc.Body.TrimEnd() -ceq $expectedTransform.TrimEnd()) ('Controller axis/transform differs from elevator: '+$id)
        }
        if($doc.Type -in @(23,33)){Need ($doc.Body.TrimEnd() -ceq $original.TrimEnd()) ('Controller mesh/material differs from elevator: '+$id)}
        foreach($reference in [regex]::Matches($doc.Body,'\{fileID: (-?\d+)([^}]*)\}')){
            $target=$reference.Groups[1].Value;if($target -eq '0'){continue}
            $guid=[regex]::Match($reference.Groups[2].Value,'guid: ([a-f0-9]{32})')
            if($guid.Success){Need ($guids.ContainsKey($guid.Groups[1].Value)) ('Missing asset GUID '+$guid.Groups[1].Value)}
            else{Need ($prefabDocs.ContainsKey($target)) ('Missing prefab object '+$target)}
        }
    }
    $report.Add('PASS: '+$hand.Name+' prefab: all 12 objects / 9 meshes, materials, local transforms and references match elevator; no behavior/collision dependencies.')
}
$lobby=Documents ([IO.File]::ReadAllText((Join-Path $root 'Assets/M/Scene/Wait_Robby.unity')))
foreach($doc in $lobby.Values){if($doc.Body -match 'ProjectWait.M.TitleScene.TitlePlazaPlayer'){
    Need ($doc.Body -match 'leftControllerVisual: \{fileID: 758605767, guid: b69a5ed3925b47dca383ef95924da5bd') 'Left controller scene reference missing'
    Need ($doc.Body -match 'rightControllerVisual: \{fileID: 1131101900, guid: ef654e43cc8e4d328cc2f5e40eae6651') 'Right controller scene reference missing'
}}
$report.Add('PASS: Lobby references the two M-owned controller prefabs with fresh unique GUIDs.')
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'scene-contracts.txt'),$report,[Text.UTF8Encoding]::new($false))
$report
