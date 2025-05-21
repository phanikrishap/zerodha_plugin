namespace QANinjaAdapter.Classes
{
    // Renamed to avoid ambiguity with QABrokerAPI.Common.Enums.MarketType
    public enum ZerodhaMarketType
    {
        Spot,       // Typically NSE/BSE for stocks
        UsdM,       // Typically NFO for F&O
        CoinM,      // Typically MCX for commodities
        Futures,    // Could be NFO or MCX
        Options     // Could be NFO or other options markets
    }
}
