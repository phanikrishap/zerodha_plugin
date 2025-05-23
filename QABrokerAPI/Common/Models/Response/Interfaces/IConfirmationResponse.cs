#nullable disable
namespace QABrokerAPI.Common.Models.Response.Interfaces;

public interface IConfirmationResponse : IResponse
{
  bool Success { get; set; }
}

