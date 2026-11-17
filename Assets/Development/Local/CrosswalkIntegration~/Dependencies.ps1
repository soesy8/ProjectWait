param([ValidateSet('Inventory','Apply')] [string]$Mode = 'Inventory')
$ErrorActionPreference = 'Stop'
Set-Location (Resolve-Path (Join-Path $PSScriptRoot '../../../..'))
$manifest = Join-Path $PSScriptRoot 'dependencies.json'
if ($Mode -eq 'Apply') {
    $data = Get-Content $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
    if ((git branch --show-current) -ne 'main') { throw 'Import must run on main' }
    if ($data.collisions.Count) { throw 'Review path collisions before importing' }
    foreach ($path in $data.files) { if (Test-Path -LiteralPath $path) { throw "Refusing to overwrite $path" } }
    for ($i = 0; $i -lt $data.files.Count; $i += 40) {
        $chunk = @($data.files[$i..([Math]::Min($i + 39, $data.files.Count - 1))])
        git restore --source=$($data.source) --worktree -- @chunk
        if ($LASTEXITCODE) { throw 'Dependency import failed' }
    }
    Write-Output "Imported $($data.files.Count) files without overwriting main assets."
    exit
}
function Guid-Map([string]$Ref) {
    $map = @{}
    git -c core.quotepath=false grep -I -E '^guid: [0-9a-f]{32}' $Ref -- '*.meta' | ForEach-Object {
        if ($_ -match ('^' + [regex]::Escape($Ref) + ':(.*)\.meta:guid: ([0-9a-f]{32})')) { $map[$Matches[2]] = $Matches[1] }
    }
    return $map
}
$source = (git rev-parse HEAD).Trim()
$sourceMap = Guid-Map $source
$targetMap = Guid-Map main
$sourceFiles = [System.Collections.Generic.HashSet[string]]::new([string[]]@(git -c core.quotepath=false ls-tree -r --name-only $source))
$targetFiles = [System.Collections.Generic.HashSet[string]]::new([string[]]@(git -c core.quotepath=false ls-tree -r --name-only main))
$pending = [System.Collections.Generic.Stack[string]]::new()
$pending.Push('Assets/Scenes/CrosswalkScene.unity')
foreach ($path in $sourceFiles) { if ($path -like 'Assets/Scripts/*.cs') { $pending.Push($path) } }
$imported = [System.Collections.Generic.HashSet[string]]::new()
$retained = [System.Collections.Generic.HashSet[string]]::new()
$external = [System.Collections.Generic.HashSet[string]]::new()
$reasons = @{}
while ($pending.Count) {
    $path = $pending.Pop()
    if (!$imported.Add($path)) { continue }
    $contents = ''
    if ($path -match '\.(unity|prefab|asset|mat|anim|controller|overrideController|shader|hlsl|cginc|shadergraph|inputactions)$') {
        $contents = git show "${source}:$path" | Out-String
    }
    if ($sourceFiles.Contains($path + '.meta')) { $contents += git show "${source}:$path.meta" | Out-String }
    foreach ($match in [regex]::Matches($contents, 'guid:\s*([0-9a-f]{32})')) {
        $guid = $match.Groups[1].Value
        if ($targetMap.ContainsKey($guid)) { [void]$retained.Add($targetMap[$guid]); continue }
        if ($sourceMap.ContainsKey($guid)) {
            $dep = $sourceMap[$guid]
            $reasons[$dep] = $path
            $pending.Push($dep)
        } elseif ($guid -notlike '0000000000000000*') { [void]$external.Add($guid) }
    }
    if ($path -match '\.(shader|hlsl|cginc)$') {
        foreach ($match in [regex]::Matches($contents, '#include\s+"([^"]+)"')) {
            $dep = ((Split-Path $path -Parent) + '/' + $match.Groups[1].Value).Replace('\','/')
            if ($sourceFiles.Contains($dep) -and !$targetFiles.Contains($dep)) { $pending.Push($dep) }
        }
    }
}
$files = [System.Collections.Generic.HashSet[string]]::new([string[]]@($imported))
foreach ($path in $imported) {
    if ($sourceFiles.Contains($path + '.meta')) { [void]$files.Add($path + '.meta') }
    $parent = Split-Path $path -Parent
    while ($parent -and $parent -ne 'Assets') {
        $meta = $parent.Replace('\','/') + '.meta'
        if ($sourceFiles.Contains($meta) -and !$targetFiles.Contains($meta)) { [void]$files.Add($meta) }
        $parent = Split-Path $parent -Parent
    }
}
$collisions = @($files | Where-Object { $targetFiles.Contains($_) } | Sort-Object)
[ordered]@{ source=$source; target=(git rev-parse main).Trim(); files=@($files | Sort-Object); retained=@($retained | Sort-Object); externalGuids=@($external | Sort-Object); collisions=$collisions; reasons=$reasons } |
    ConvertTo-Json -Depth 6 | Set-Content $manifest -Encoding UTF8
[pscustomobject]@{Source=$source; ImportedFiles=$files.Count; RetainedAssets=$retained.Count; ExternalGuids=$external.Count; Collisions=$collisions} | Format-List
$files | Group-Object { ($_ -split '/')[0..1] -join '/' } | Select-Object Count,Name | Format-Table -AutoSize
