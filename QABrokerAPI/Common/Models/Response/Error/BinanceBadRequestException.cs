#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceBadRequestException(BinanceError errorDetails) : BinanceException("Malformed requests are sent to the server. Please review the request object/string", errorDetails)
{
}

