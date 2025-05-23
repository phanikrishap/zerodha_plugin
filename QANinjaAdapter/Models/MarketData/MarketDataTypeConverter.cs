using System;
using NinjaTrader.Data;

namespace QANinjaAdapter.Models.MarketData
{
    /// <summary>
    /// Enum representing different types of market data events
    /// </summary>
    public enum QAMarketDataType
    {
        Ask,
        Bid,
        Last,
        DailyHigh,
        DailyLow,
        DailyVolume,
        LastClose,
        Opening,
        OpenInterest
    }

    /// <summary>
    /// Utility class to convert between NinjaTrader.Data.MarketDataType and our QAMarketDataType
    /// </summary>
    public static class MarketDataTypeConverter
    {
        /// <summary>
        /// Converts NinjaTrader.Data.MarketDataType to QAMarketDataType
        /// </summary>
        public static QAMarketDataType Convert(MarketDataType ninjaType)
        {
            switch (ninjaType)
            {
                case MarketDataType.Ask:
                    return QAMarketDataType.Ask;
                case MarketDataType.Bid:
                    return QAMarketDataType.Bid;
                case MarketDataType.Last:
                    return QAMarketDataType.Last;
                case MarketDataType.DailyHigh:
                    return QAMarketDataType.DailyHigh;
                case MarketDataType.DailyLow:
                    return QAMarketDataType.DailyLow;
                case MarketDataType.DailyVolume:
                    return QAMarketDataType.DailyVolume;
                case MarketDataType.LastClose:
                    return QAMarketDataType.LastClose;
                case MarketDataType.Opening:
                    return QAMarketDataType.Opening;
                case MarketDataType.OpenInterest:
                    return QAMarketDataType.OpenInterest;
                default:
                    // For any unhandled types, default to Last
                    return QAMarketDataType.Last;
            }
        }

        /// <summary>
        /// Converts QAMarketDataType to NinjaTrader.Data.MarketDataType
        /// </summary>
        public static MarketDataType ConvertBack(QAMarketDataType qaType)
        {
            switch (qaType)
            {
                case QAMarketDataType.Ask:
                    return MarketDataType.Ask;
                case QAMarketDataType.Bid:
                    return MarketDataType.Bid;
                case QAMarketDataType.Last:
                    return MarketDataType.Last;
                case QAMarketDataType.DailyHigh:
                    return MarketDataType.DailyHigh;
                case QAMarketDataType.DailyLow:
                    return MarketDataType.DailyLow;
                case QAMarketDataType.DailyVolume:
                    return MarketDataType.DailyVolume;
                case QAMarketDataType.LastClose:
                    return MarketDataType.LastClose;
                case QAMarketDataType.Opening:
                    return MarketDataType.Opening;
                case QAMarketDataType.OpenInterest:
                    return MarketDataType.OpenInterest;
                default:
                    return MarketDataType.Last;
            }
        }
    }
}
