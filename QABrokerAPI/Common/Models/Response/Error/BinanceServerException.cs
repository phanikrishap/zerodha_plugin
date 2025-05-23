#nullable disable
namespace QABrokerAPI.Common.Models.Response.Error;

public class BinanceServerException(BinanceError errorDetails) : BinanceException("Request to BinanceAPI is valid but there was an error on the server side", errorDetails)
{
}

