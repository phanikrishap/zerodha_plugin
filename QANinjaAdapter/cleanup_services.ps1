# List of files to remove
$filesToRemove = @(
    "Services\AppLogger.cs",
    "Services\ConfigurationService.cs",
    "Services\IConfigurationService.cs",
    "Services\IInstrumentDefinitionService.cs",
    "Services\INinjaTraderInstrumentManager.cs",
    "Services\InstrumentDefinitionService.cs",
    "Services\ISymbolMappingService.cs",
    "Services\ITickDataService.cs",
    "Services\IZerodhaHttpApiService.cs",
    "Services\IZerodhaWebSocketService.cs",
    "Services\NinjaTraderInstrumentManager.cs",
    "Services\SymbolMappingService.cs",
    "Services\TickDataService.cs",
    "Services\ZerodhaHttpApiService.cs",
    "Services\ZerodhaWebSocketService.cs"
)

# Remove each file
foreach ($file in $filesToRemove) {
    $fullPath = Join-Path -Path $PSScriptRoot -ChildPath $file
    if (Test-Path $fullPath) {
        Write-Host "Removing $fullPath"
        Remove-Item $fullPath
    } else {
        Write-Host "File not found: $fullPath"
    }
}

Write-Host "Cleanup complete. The following files are part of our architecture and were kept:"
Write-Host "- Services\Configuration\ConfigurationManager.cs"
Write-Host "- Services\Zerodha\ZerodhaClient.cs"
Write-Host "- Services\Instruments\InstrumentManager.cs"
Write-Host "- Services\MarketData\HistoricalDataService.cs"
Write-Host "- Services\MarketData\MarketDataService.cs"
Write-Host "- Services\WebSocket\WebSocketManager.cs"
