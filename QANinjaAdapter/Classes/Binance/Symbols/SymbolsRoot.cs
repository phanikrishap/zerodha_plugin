#nullable disable
namespace QANinjaAdapter.Classes.Binance.Symbols;

public class SymbolsRoot
{
  public string TimeZone { get; set; }

  public string ServerTime { get; set; }

  public RateLimit[] RateLimits { get; set; }

  public SymbolObject[] Symbols { get; set; }
}

