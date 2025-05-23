#nullable disable
namespace QANinjaAdapter.Classes.Binance.Symbols;

public class RateLimit
{
  public string RateLimitType { get; set; }

  public string Interval { get; set; }

  public int IntervalNum { get; set; }

  public int Limit { get; set; }
}

