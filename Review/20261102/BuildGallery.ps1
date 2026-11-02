$ErrorActionPreference = 'Stop'
$reviewRoot = $PSScriptRoot
$reviewData = Get-Content -LiteralPath (Join-Path $reviewRoot 'review.json') -Raw | ConvertFrom-Json
$captureRows = @(Get-Content -LiteralPath (Join-Path $reviewRoot 'captures.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
if ($captureRows.Count -ne 20 -or @($captureRows.number | Sort-Object -Unique).Count -ne 20) { throw 'Expected 20 unique captures.' }
function Html([object]$Value) { return [System.Net.WebUtility]::HtmlEncode([string]$Value) }
$sceneRows = ($reviewData.scenes | ForEach-Object { '<tr><td><strong>' + (Html $_.name) + '</strong><small>' + (Html $_.path) + '</small></td><td>' + (Html $_.result) + '</td></tr>' }) -join "`n"
$strengthRows = ($reviewData.strengths | ForEach-Object { '<li>' + (Html $_) + '</li>' }) -join "`n"
$findings = ($reviewData.findings | ForEach-Object {
    $finding = $_
    $evidence = (@($finding.shots) | ForEach-Object { '<a class="evidence" href="#shot' + $_ + '" onclick="showAll()">화면 ' + $_ + '</a>' }) -join ' '
    '<article class="finding"><div class="tags"><span class="priority ' + $finding.priority + '">' + $finding.priority + '</span><span>' + (Html $finding.kind) + '</span></div><h3>' + (Html $finding.title) + '</h3><p>' + (Html $finding.detail) + '</p><p class="action">' + (Html $finding.action) + '</p><small>' + (Html $finding.source) + '</small><div>' + $evidence + '</div></article>'
}) -join "`n"
$imageCards = foreach ($shot in $reviewData.shots) {
    $capture = @($captureRows | Where-Object number -eq $shot.number)
    if ($capture.Count -ne 1) { throw ('Capture missing: ' + $shot.number) }
    $imagePath = Join-Path $reviewRoot ([IO.Path]::GetFileName($capture[0].path))
    $bytes = [IO.File]::ReadAllBytes($imagePath)
    if ($bytes.Length -lt 1000) { throw ('Invalid PNG: ' + $imagePath) }
    $width = [System.Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes,16))
    $height = [System.Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($bytes,20))
    if ($width -ne 1600 -or $height -ne 900) { throw ('Unexpected dimensions: ' + $imagePath) }
    $encoded = [Convert]::ToBase64String($bytes)
    '<figure class="shot" data-group="' + $shot.group + '" id="shot' + $shot.number + '"><button class="image-button" aria-label="' + (Html $shot.title) + ' 확대" onclick="enlarge(this)"><img loading="lazy" width="1600" height="900" src="data:image/png;base64,' + $encoded + '" alt="' + (Html $shot.title) + '"></button><figcaption><h3><span>' + ('{0:00}' -f $shot.number) + '</span> ' + (Html $shot.title) + '</h3><p>' + (Html $shot.caption) + '</p><small>' + (Html $capture[0].scene) + ' · frame ' + $capture[0].frame + ' · 1600×900</small></figcaption></figure>'
}
$imageCards = $imageCards -join "`n"
$html = @'
<!DOCTYPE html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>ProjectDwtD 실행 리뷰 · 20장</title>
<style>
:root{color-scheme:light;--paper:#fff9e9;--ink:#352e27;--teal:#266e67;--muted:#6f7069;--line:#dedacb}*{box-sizing:border-box}body{margin:0;background:#f3f0e7;color:var(--ink);font:16px/1.75 "Segoe UI","Malgun Gothic",sans-serif}main{max-width:1340px;margin:auto;padding:52px 28px 80px}a{color:var(--teal)}header{max-width:1000px;margin-bottom:32px}h1{font-size:42px;letter-spacing:-1.8px;line-height:1.3;margin:12px 0 20px}h2{font-size:28px;margin:46px 0 18px;letter-spacing:-.7px}h3{font-size:18px;line-height:1.5;margin:12px 0 8px}p{margin:10px 0}.eyebrow{letter-spacing:.12em;color:var(--teal);font-size:12px;font-weight:700}.meta{color:var(--muted);font-size:14px}.lead{font-size:20px;line-height:1.8}.stats{display:flex;gap:12px;flex-wrap:wrap;margin:24px 0}.stats span{background:var(--teal);color:white;border-radius:7px;padding:7px 16px}.note{border-left:3px solid var(--teal);padding:10px 20px;background:var(--paper);color:#55574d;font-size:14px}.table-wrap{overflow-x:auto}table{border-collapse:collapse;width:100%;background:var(--paper)}td{padding:18px;border-bottom:1px solid var(--line);vertical-align:top}td:first-child{width:35%}small{display:block;overflow-wrap:anywhere;color:var(--muted);font-size:12px;line-height:1.6}.findings{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.finding{padding:22px 26px;background:var(--paper);border:1px solid var(--line);border-radius:10px}.tags{display:flex;align-items:center;gap:10px;font-size:12px;color:var(--muted)}.priority{padding:2px 9px;font-weight:700;border-radius:4px;background:#d6e7df;color:var(--teal)}.P1{background:#f3d5c1;color:#873c13}.action{color:var(--teal)}.evidence{display:inline-block;font-size:12px;margin:12px 6px 0 0}.filters{display:flex;gap:8px;flex-wrap:wrap;position:sticky;top:0;background:#f3f0e7eb;padding:12px 0;z-index:2;backdrop-filter:blur(10px)}.filters button{border:1px solid #c6cabe;border-radius:30px;background:var(--paper);color:var(--ink);padding:8px 15px;cursor:pointer;font:inherit;font-size:13px}.filters button[aria-pressed=true]{background:var(--teal);border-color:var(--teal);color:white}.gallery{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:22px;margin-top:12px}.shot{margin:0;background:var(--paper);border:1px solid var(--line);border-radius:10px;overflow:hidden;scroll-margin-top:90px}.shot[hidden]{display:none}.image-button{display:block;border:0;background:#ddd;padding:0;width:100%;cursor:zoom-in}.shot img{display:block;width:100%;height:auto;aspect-ratio:16/9}.shot figcaption{padding:16px 20px 22px}.shot h3{margin:0 0 6px}.shot h3 span{color:var(--teal);font-size:13px;font-variant-numeric:tabular-nums}.shot p{font-size:14px}.shot small{font-size:11px}dialog{border:0;padding:12px;background:#181b1a;color:white;max-width:98vw;max-height:98vh;border-radius:8px}dialog::backdrop{background:#000c}dialog img{display:block;max-width:calc(98vw - 24px);max-height:calc(98vh - 90px);width:auto;height:auto}dialog button{float:right;background:#fff;color:#222;border:0;border-radius:4px;padding:8px 16px;margin-bottom:10px;cursor:pointer}details{margin-top:32px;font-size:13px;color:var(--muted)}footer{margin-top:42px;color:var(--muted);font-size:13px}@media(max-width:760px){main{padding:30px 14px}h1{font-size:32px}.lead{font-size:18px}.findings,.gallery{grid-template-columns:1fr}td{padding:12px}.finding{padding:18px}h2{font-size:24px}}@media print{.filters,dialog{display:none}.shot{break-inside:avoid}.findings{display:block}.finding{break-inside:avoid;margin-bottom:10px}}
</style></head><body><main>
<header><div class="eyebrow">PROJECTDWTD / PLAY REVIEW / 2026.11.02</div><h1>실제 화면으로 보는<br>프로젝트 실행 리뷰</h1><p class="lead">{{SUMMARY}}</p><div class="stats"><span>6개 씬 실행</span><span>20장 실제 캡처</span><span>1600×900 원본</span></div><p class="meta">{{ENVIRONMENT}}</p></header>
<div class="note">{{METHOD}}<br><br>{{LIMITS}}</div>
<h2>씬별 실행 결과</h2><div class="table-wrap"><table><tbody>{{SCENES}}</tbody></table></div>
<h2>잘 작동한 점</h2><ul>{{STRENGTHS}}</ul>
<h2>우선 정리할 항목</h2><div class="findings">{{FINDINGS}}</div>
<h2 id="captures">실제 플레이 캡처 20장</h2><p class="meta">이미지를 누르면 확대합니다. 모든 이미지는 해당 시점의 실제 Game 화면입니다.</p>
<nav class="filters" aria-label="씬별 캡처 필터"><button data-filter="all" aria-pressed="true" onclick="filterShots('all',this)">전체 20장</button><button data-filter="original" aria-pressed="false" onclick="filterShots('original',this)">엘리베이터 원본 · 10</button><button data-filter="guided" aria-pressed="false" onclick="filterShots('guided',this)">직접 상호작용 · 3</button><button data-filter="corridor" aria-pressed="false" onclick="filterShots('corridor',this)">복도 · 4</button><button data-filter="friend" aria-pressed="false" onclick="filterShots('friend',this)">새 친구 복도 · 1</button><button data-filter="animation" aria-pressed="false" onclick="filterShots('animation',this)">애니메이션 · 1</button><button data-filter="empty" aria-pressed="false" onclick="filterShots('empty',this)">빈 씬 · 1</button></nav>
<div class="gallery">{{IMAGES}}</div>
<details><summary>실행 검증 방법과 원시 기록에 관한 설명</summary><p>{{VERIFICATION}}</p><p>원시 자료: captures.jsonl, audit.json, 각 실행 단계 JSON, console_final.json. CLI의 대기 시간 초과 뒤에도 실행이 끝까지 이어진 원본 경로는 original2.json에 완료 기록이 남아 있습니다. 게임 기능·씬을 수정하지 않고 리뷰와 캡처만 작성했습니다.</p></details>
<footer>Unity Editor 실행 리뷰 · 2026-11-02 · 캡처 20/20 · 웹 연결 없이 열 수 있는 독립 HTML</footer>
</main><dialog id="viewer"><button onclick="this.closest('dialog').close()">닫기 · Esc</button><img id="large-image" alt=""><p id="large-caption"></p></dialog>
<script>
function filterShots(group,button){document.querySelectorAll('.shot').forEach(el=>el.hidden=group!=='all'&&el.dataset.group!==group);document.querySelectorAll('.filters button').forEach(el=>el.setAttribute('aria-pressed',String(el===button)))}
function showAll(){filterShots('all',document.querySelector('[data-filter="all"]'))}
function enlarge(button){const img=button.querySelector('img');document.getElementById('large-image').src=img.src;document.getElementById('large-image').alt=img.alt;document.getElementById('large-caption').textContent=img.alt;document.getElementById('viewer').showModal()}
document.getElementById('viewer').addEventListener('click',event=>{if(event.target===event.currentTarget)event.currentTarget.close()});
</script></body></html>
'@
$replacements = @{
    '{{SUMMARY}}' = (Html $reviewData.summary)
    '{{ENVIRONMENT}}' = (Html $reviewData.environment)
    '{{METHOD}}' = (Html $reviewData.method)
    '{{LIMITS}}' = (Html $reviewData.limits)
    '{{SCENES}}' = $sceneRows
    '{{STRENGTHS}}' = $strengthRows
    '{{FINDINGS}}' = $findings
    '{{IMAGES}}' = $imageCards
    '{{VERIFICATION}}' = (Html $reviewData.verificationNote)
}
foreach ($key in $replacements.Keys) { $html = $html.Replace($key, $replacements[$key]) }
if ($html.Contains('{{') -or [regex]::Matches($html,'data:image/png;base64,').Count -ne 20) { throw 'Gallery validation failed.' }
[IO.File]::WriteAllText((Join-Path $reviewRoot 'gallery.html'), $html, [Text.UTF8Encoding]::new($false))
$pngPaths = @($captureRows | ForEach-Object { Join-Path $reviewRoot ([IO.Path]::GetFileName($_.path)) })
$packagePaths = $pngPaths + @(Join-Path $reviewRoot 'gallery.html') + @(Join-Path $reviewRoot 'review.json') + @(Join-Path $reviewRoot 'audit.json')
Compress-Archive -LiteralPath $packagePaths -DestinationPath (Join-Path $reviewRoot 'ProjectDwtD_Review_20shots.zip') -CompressionLevel Optimal -Force
[pscustomobject]@{ captures = $captureRows.Count; dimensions = '1600x900'; htmlBytes = (Get-Item -LiteralPath (Join-Path $reviewRoot 'gallery.html')).Length; zipBytes = (Get-Item -LiteralPath (Join-Path $reviewRoot 'ProjectDwtD_Review_20shots.zip')).Length } | ConvertTo-Json
