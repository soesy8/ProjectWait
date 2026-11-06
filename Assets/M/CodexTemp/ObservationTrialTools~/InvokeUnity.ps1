param(
    [Parameter(Mandatory = $true)][string]$Command,
    [hashtable]$Parameters = @{},
    [string]$Evidence,
    [int]$TimeoutSeconds = 120
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$descriptor = Get-Content (Join-Path $project 'Library/Pipeline/.unity-pipeline-port') -Raw | ConvertFrom-Json
if ([IO.Path]::GetFullPath($descriptor.projectPath) -ne $project) { throw 'Wrong Unity project.' }
$body = @{ command = $Command; parameters = $Parameters; timeout = $TimeoutSeconds * 1000 } | ConvertTo-Json -Depth 20 -Compress
$response = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:$($descriptor.port)/api/exec" -Headers @{ Authorization = "Bearer $($descriptor.evalToken)" } -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec $TimeoutSeconds
$json = $response | ConvertTo-Json -Depth 40
if ($Evidence) {
    if (!$Evidence.StartsWith('Assets/M/')) { throw 'Evidence must remain under Assets/M.' }
    $path = Join-Path $project $Evidence
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
    [IO.File]::WriteAllText($path, $json, [Text.UTF8Encoding]::new($false))
}
$json
if ($response.success -eq $false) { throw "Unity command failed: $Command" }
if ($response.result -and $response.result.success -eq $false) { throw "Unity script failed: $Command" }
