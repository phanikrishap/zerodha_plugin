using System.Runtime.Serialization;

#nullable disable
namespace QABrokerAPI.Common.Enums;

public enum NewOrderResponseType
{
  [EnumMember(Value = "RESULT")] Result,
  [EnumMember(Value = "ACK")] Acknowledge,
  [EnumMember(Value = "FULL")] Full,
}

