using System;

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Interfaces;

public interface IBalanceResponse
{
  string Asset { get; set; }

  Decimal Free { get; set; }

  Decimal Locked { get; set; }
}

