$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$text=[IO.File]::ReadAllText((Join-Path $root 'Assets/M/Scene/Wait_Elevator.unity'))
$docs=[ordered]@{};$transforms=@{};$objects=@{}
foreach($match in [regex]::Matches($text,'(?ms)^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?\n(.*?)(?=^--- !u!|\z)')){
    $id=$match.Groups[2].Value;$type=[int]$match.Groups[1].Value;$body=$match.Groups[3].Value
    $docs[$id]=@{Type=$type;Body=$body}
    if($type -eq 4){
        $go=[regex]::Match($body,'m_GameObject: \{fileID: (\d+)').Groups[1].Value
        $father=[regex]::Match($body,'m_Father: \{fileID: (\d+)').Groups[1].Value
        $transforms[$id]=@{Object=$go;Father=$father}
    }
}
$folder=Join-Path $root 'Assets/M/Prefabs/Controllers'
[IO.Directory]::CreateDirectory($folder)|Out-Null
foreach($hand in @(
    @{Name='LeftAimController';Transform='758605768';Guid='b69a5ed3925b47dca383ef95924da5bd'},
    @{Name='RightAimController';Transform='1131101901';Guid='ef654e43cc8e4d328cc2f5e40eae6651'})){
    $selected=@{};$selected[$hand.Transform]=$true
    do{
        $added=$false
        foreach($id in $transforms.Keys){if(!$selected.ContainsKey($id) -and $selected.ContainsKey($transforms[$id].Father)){$selected[$id]=$true;$added=$true}}
    }while($added)
    $gos=@{};foreach($id in $selected.Keys){$gos[$transforms[$id].Object]=$true}
    $keep=@{}
    foreach($id in $docs.Keys){
        $doc=$docs[$id]
        $go=if($doc.Type -eq 1){$id}else{[regex]::Match($doc.Body,'m_GameObject: \{fileID: (\d+)').Groups[1].Value}
        if($go -and $gos.ContainsKey($go) -and $doc.Type -in @(1,4,23,33)){$keep[$id]=$true}
    }
    $output=[Text.StringBuilder]::new("%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n")
    foreach($id in $docs.Keys){if($keep.ContainsKey($id)){
        $doc=$docs[$id];$body=$doc.Body
        if($doc.Type -eq 1){
            $body=[regex]::Replace($body,'(?m)^  - component: \{fileID: (\d+)\}\r?\n',[Text.RegularExpressions.MatchEvaluator]{param($m) if($keep.ContainsKey($m.Groups[1].Value)){$m.Value}else{''}})
        }
        if($id -eq $hand.Transform){$body=[regex]::Replace($body,'(?m)^  m_Father: .*','  m_Father: {fileID: 0}')}
        [void]$output.Append("--- !u!$($doc.Type) &$id`n$body")
    }}
    $path=Join-Path $folder ($hand.Name+'.prefab')
    if(Test-Path $path){throw "Refusing to overwrite $path"}
    [IO.File]::WriteAllText($path,$output.ToString(),[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText(($path+'.meta'),"fileFormatVersion: 2`nguid: $($hand.Guid)`nPrefabImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n",[Text.UTF8Encoding]::new($false))
    Write-Output "$($hand.Name): $($gos.Count) objects, $($keep.Count) documents, root=$($transforms[$hand.Transform].Object)"
}
