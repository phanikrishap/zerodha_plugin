using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Request;
using QABrokerAPI.Common.Models.Response;
using QABrokerAPI.Common.Models.Response.Abstract;



namespace QABrokerAPI.Common.Interfaces
{
    public interface IBrokerClient
    {
        Task<UserDataStreamResponse> StartUserDataStream();

        TimeSpan TimestampOffset { get; set; }

        Task<UserDataStreamResponse> KeepAliveUserDataStream(string userDataListenKey);

        Task<UserDataStreamResponse> CloseUserDataStream(string userDataListenKey);

        Task<EmptyResponse> TestConnectivity();

        Task<ServerTimeResponse> GetServerTime();

        Task<OrderBookResponse> GetOrderBook(string symbol, bool useCache = false, int limit = 100);

        Task<List<CompressedAggregateTradeResponse>> GetCompressedAggregateTrades(
          GetCompressedAggregateTradesRequest request,
          MarketType mt);

        Task<List<KlineCandleStickResponse>> GetKlinesCandlesticks(
          GetKlinesCandlesticksRequest request,
          MarketType mt);

        Task<SymbolPriceChangeTickerResponse> GetDailyTicker(string symbol);

        Task<List<SymbolPriceResponse>> GetSymbolsPriceTicker();

        Task<List<SymbolOrderBookResponse>> GetSymbolOrderBookTicker();

        Task<SymbolOrderBookResponse> GetSymbolOrderBookTicker(string symbol);

        Task<SymbolPriceResponse> GetPrice(string symbol);

        Task<List<SymbolPriceResponse>> GetAllPrices();

        Task<BaseCreateOrderResponse> CreateOrder(CreateOrderRequest request);

        Task<OrderResponse> QueryOrder(QueryOrderRequest request, int receiveWindow = 5000);

        Task<CancelOrderResponse> CancelOrder(CancelOrderRequest request, int receiveWindow = 5000);

        Task<List<OrderResponse>> GetCurrentOpenOrders(
          CurrentOpenOrdersRequest request,
          int receiveWindow = 5000);

        Task<List<OrderResponse>> GetAllOrders(AllOrdersRequest request, int receiveWindow = 5000);

        Task<AccountInformationResponse> GetAccountInformation(int receiveWindow = 5000);

        Task<List<AccountTradeReponse>> GetAccountTrades(AllTradesRequest request, int receiveWindow = 5000);

        Task<WithdrawResponse> CreateWithdrawRequest(WithdrawRequest request, int receiveWindow = 5000);

        Task<DepositListResponse> GetDepositHistory(FundHistoryRequest request, int receiveWindow = 5000);

        Task<ExchangeInfoResponse> GetExchangeInfo();
    }

}
