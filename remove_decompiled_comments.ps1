# PowerShell script to remove decompilation comments from .cs files
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$files = Get-ChildItem -Path $projectRoot -Include *.cs -Recurse -File

$pattern = @'
// Decompiled with JetBrains decompiler
// Type: .+
// Assembly: .+
// MVID: [0-9A-F-]+
// Assembly location: .+

'@

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    $newContent = $content -replace $pattern, ""
    
    if ($newContent -ne $content) {
        Write-Host "Updating $($file.FullName)"
        Set-Content -Path $file.FullName -Value $newContent.TrimStart()
    }
}

Write-Host "Done removing decompilation comments."
