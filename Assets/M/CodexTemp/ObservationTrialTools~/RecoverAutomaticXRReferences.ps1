# Unity rebuilds the list of three Android XR feature references on reload.
# Recover pre-existing user bytes ONLY when the candidate has the exact captured
# SHA-256. This never writes to the original; it produces a verified M backup.
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$path = 'Assets/XR/Settings/OpenXRPackageSettings.asset'
$baseline = Get-Content (Join-Path $project 'Assets/M/Documentation/ObservationTrial_20261106/OriginalHashes.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$text = [IO.File]::ReadAllText((Join-Path $project $path))
$blocks = [regex]::Matches($text, '(?ms)^--- !u!114 &(-?\d+)\s.*?(?=^--- !u!|\z)')
$ids = @('5201027382187981087','7402433293575341334','7912580273555643784')
$sets = @()
foreach ($id in $ids) {
    $block = $blocks | Where-Object { $_.Groups[1].Value -eq $id }
    $guid = [regex]::Match($block.Value, 'm_Script: \{fileID: \d+, guid: ([a-f0-9]+)').Groups[1].Value
    $sets += ,@($blocks | Where-Object { $_.Value.Contains($guid) } | ForEach-Object { $_.Groups[1].Value })
}
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
public static class RecoverTrialXR {
 public static string Run(string text, string target, string[] ids, string[] first, string[] second, string[] third, string output) {
  using(var sha=SHA256.Create()) {
   foreach(var newline in new[]{"\r\n","\n"}) {
    var source=Regex.Replace(text,"\r?\n",newline);
    foreach(var a in first) foreach(var b in second) foreach(var c in third) {
     var candidate=source.Replace("  - {fileID: "+ids[0]+"}","  - {fileID: "+a+"}").Replace("  - {fileID: "+ids[1]+"}","  - {fileID: "+b+"}").Replace("  - {fileID: "+ids[2]+"}","  - {fileID: "+c+"}");
     var raw=Encoding.UTF8.GetBytes(candidate);
     foreach(bool bom in new[]{false,true}) {
      byte[] bytes=raw;
      if(bom){ bytes=new byte[raw.Length+3]; bytes[0]=239;bytes[1]=187;bytes[2]=191;Buffer.BlockCopy(raw,0,bytes,3,raw.Length); }
      var hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
      if(hash==target){File.WriteAllBytes(output,bytes);return "Exact original recovered; references="+a+","+b+","+c+"; CRLF="+(newline.Length==2)+"; BOM="+bom;}
     }
    }
   }
  }
  throw new Exception("No exact SHA-256 match. Original was not written.");
 }
}
'@
[RecoverTrialXR]::Run($text, $baseline.$path, $ids, $sets[0], $sets[1], $sets[2], (Join-Path $PSScriptRoot 'OriginalOpenXR.bytes'))
