#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceTimeoutException(BinanceError errorDetails) : BinanceException(" request was valid, the server went to execute but then timed out. This doesn't mean it failed, and should be treated as UNKNOWN.", errorDetails)
{
}

