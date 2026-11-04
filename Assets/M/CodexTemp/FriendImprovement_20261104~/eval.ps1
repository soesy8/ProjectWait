param([Parameter(Mandatory=$true)][string]$Source,[string]$Output)
$evalCode = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $Source).Path)
$evalCode = $evalCode.Replace('"','\"')
$result = & unity command eval --project-path 'D:\project\ProjectWait' --caller plugin --skill unity-cli --format json -- --code $evalCode
if ($Output) { [IO.File]::WriteAllText((Join-Path (Get-Location) $Output), ($result -join [Environment]::NewLine), (New-Object Text.UTF8Encoding $false)) }
$result
