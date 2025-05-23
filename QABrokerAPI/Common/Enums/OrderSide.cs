using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Enums;

public enum OrderSide
{
  [EnumMember(Value = "BUY")] Buy,
  [EnumMember(Value = "SELL")] Sell,
}

