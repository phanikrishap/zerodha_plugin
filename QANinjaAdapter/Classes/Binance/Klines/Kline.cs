#nullable disable
namespace QANinjaAdapter.Classes.Binance.Klines;

public class Kline
{
  public long OpenTime { get; set; }

  public double Open { get; set; }

  public double High { get; set; }

  public double Low { get; set; }

  public double Close { get; set; }

  public double Volume { get; set; }

  public long CloseTime { get; set; }

  public double QuoteAssetVolume { get; set; }

  public int NumberOfTrades { get; set; }

  public double TakerBuyBase { get; set; }

  public double TakerBuyQuote { get; set; }

  public double Ignore { get; set; }
}

