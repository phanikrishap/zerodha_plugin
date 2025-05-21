$output = dotnet build QANinjaAdapter.csproj
$output | Out-File -FilePath "build_results.txt"
Write-Host "Build results saved to build_results.txt"
