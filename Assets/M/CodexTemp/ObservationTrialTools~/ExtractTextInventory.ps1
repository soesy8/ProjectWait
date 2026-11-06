# Read-only source inspection; output is restricted to the inventory directory.
$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$out=Join-Path $root 'Assets/M/Documentation/ObservationTrial_TextInventory_20261106'
function Cell($value) {
    return ([string]$value).Replace('&','&amp;').Replace('<','&lt;').Replace('>','&gt;').Replace('|','&#124;').Replace("`r",'').Replace("`n",'<br>')
}
$snapshot=(Get-Content (Join-Path $out 'LiveTextSnapshot.json') -Raw -Encoding UTF8 | ConvertFrom-Json).result.result
$manifest=Get-Content (Join-Path $root 'Assets/M/Arts/Models/SportsCentre3F/SportsCentreManifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$parts=@($manifest.objects | Where-Object {$_.body})
$lines=[Collections.Generic.List[string]]::new()
$lines.AddRange([string[]]@('# 씬 텍스트 원본 부록','','주요 문구의 노출 시점·사용 여부는 `TextInventory.md`를 기준으로 읽습니다. 아래는 원문 누락을 막기 위한 원본 목록입니다.','','## 1. 실행 중 씬의 TMP 전체 목록','',"총 $($snapshot.texts.Count)개. Lobby 단계의 단일 시점 스냅샷입니다. active=true여도 상위 알파가 0이거나 시야 밖이면 보이지 않습니다. 일시정지 UI가 열린 시점이므로 현재 보임 여부를 정상 진행의 사용 여부로 해석하면 안 됩니다.",'','| ID | Hierarchy 경로 | 원문 | active / enabled | 상위 CanvasGroup 알파 |','|---|---|---|---|---|'))
$i=0
foreach($t in $snapshot.texts) {
    $i++;$groups=($t.canvasGroups | ForEach-Object {"$($_.name)=$($_.alpha)"}) -join '; '
    $text=Cell $t.text;if(!$text){$text='（빈 문자열）'}
    $lines.Add(('| TMP-{0:000} | {1} | {2} | {3} / {4} | {5} |' -f $i,(Cell $t.path),$text,$t.active,$t.enabled,(Cell $groups)))
}
$lines.AddRange([string[]]@('','## 2. 모델 원본에 포함된 문자 메시','','TMP가 아니라 모델 형상에 포함된 글자입니다. 일부는 가독성용 TMP 표지와 겹치거나 복제되어 있습니다. 원본의 전체 이름·부모·문구를 보존합니다. 모델 원문이라는 이유만으로 현재 카메라에서 보인다고 단정하지 않습니다.','','| ID | 모델 오브젝트 | 부모 / 컬렉션 | 원문 |','|---|---|---|---|'))
$i=0
foreach($p in $parts) {
    $i++;$where="$($p.parent) / $($p.collections -join ', ')"
    $lines.Add(('| MODEL-{0:000} | {1} | {2} | {3} |' -f $i,(Cell $p.name),(Cell $where),(Cell $p.body)))
}
$lines.AddRange([string[]]@('','## 3. 런타임 코드의 한글 문자열 원문','','표시용 외에 내부 이력·판별 문자열·사용하지 않는 폴백도 포함합니다. 문자열 리터럴 단위이므로 동적 조합은 주요 목록에서 완성형을 확인합니다. 주석만 있는 줄은 제외합니다.','','| ID | 소스 위치 | 문자열 리터럴 원문 |','|---|---|---|'))
$number=0
foreach($path in (Get-ChildItem (Join-Path $root 'Assets/M/Scripts/ObservationTrial') -Filter '*.cs' | Sort-Object Name)) {
    $n=0
    foreach($line in (Get-Content $path.FullName -Encoding UTF8)) {
        $n++;if($line.TrimStart().StartsWith('//')){continue}
        foreach($match in [regex]::Matches($line,'"(?:\\.|[^"\\])*"')) {
            $token=$match.Value;if($token -notmatch '[가-힣]'){continue}
            $number++;$value=$token.Substring(1,$token.Length-2).Replace('\n',"`n").Replace('\"','"')
            $lines.Add(('| SRC-{0:000} | {1}:{2} | {3} |' -f $number,$path.Name,$n,(Cell $value)))
        }
    }
}
$utf8=[Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllLines((Join-Path $out 'RawTextAppendix.md'),$lines,$utf8)
[IO.File]::WriteAllText((Join-Path $out 'ModelTextSnapshot.json'),($parts | ConvertTo-Json -Depth 30),$utf8)
[pscustomobject]@{tmp=$snapshot.texts.Count;modelTextBodies=$parts.Count;koreanSourceLiterals=$number;output=(Join-Path $out 'RawTextAppendix.md')} | ConvertTo-Json
