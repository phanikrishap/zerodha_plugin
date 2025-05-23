using System;

#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceException : Exception
{
  public BinanceError ErrorDetails { get; set; }

  public BinanceException(string message, BinanceError errorDetails)
    : base(message)
  {
    this.ErrorDetails = errorDetails;
  }
}

