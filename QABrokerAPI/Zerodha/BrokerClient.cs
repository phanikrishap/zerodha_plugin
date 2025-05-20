using QABrokerAPI.Zerodha;
using QABrokerAPI.Common.Caching;
using QABrokerAPI.Common.Caching.Interfaces;
using QABrokerAPI.Common.Interfaces;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Request;
using QABrokerAPI.Common.Models.Response;
using QABrokerAPI.Common.Models.Response.Abstract;
using QABrokerAPI.Common.Utility;
using log4net;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable disable
namespace QABrokerAPI.Zerodha
{
    public class BrokerClient : IBrokerClient
    {
        private TimeSpan _timestampOffset;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _accessToken;
        private readonly IAPIProcessor _apiProcessor;
        private readonly int _defaultReceiveWindow;
        private readonly ILog _logger;

        public TimeSpan TimestampOffset
        {
            get => this._timestampOffset;
            set
            {
                this._timestampOffset = value;
                ZerodhaRequestClient.SetTimestampOffset(this._timestampOffset);
            }
        }

        public BrokerClient(ClientConfiguration configuration, IAPIProcessor apiProcessor = null)
        {
            this._logger = configuration.Logger ?? LogManager.GetLogger(typeof(BrokerClient));
            Guard.AgainstNull((object)configuration);
            Guard.AgainstNullOrEmpty(configuration.ApiKey);
            Guard.AgainstNull((object)configuration.SecretKey);

            this._defaultReceiveWindow = configuration.DefaultReceiveWindow;
            this._apiKey = configuration.ApiKey;
            this._secretKey = configuration.SecretKey;
            this._accessToken = configuration.AccessToken; // Added for Zerodha

            ZerodhaRequestClient.SetTimestampOffset(configuration.TimestampOffset);
            ZerodhaRequestClient.SetRateLimiting(configuration.EnableRateLimiting);
            ZerodhaRequestClient.SetAPIKey(this._apiKey);
            ZerodhaRequestClient.SetAccessToken(this._accessToken); // Added for Zerodha

            if (apiProcessor == null)
            {
                this._apiProcessor = (IAPIProcessor)new APIProcessor(this._apiKey, this._secretKey, this._accessToken, (IAPICacheManager)new APICacheManager());
                this._apiProcessor.SetCacheTime(configuration.CacheTime);
            }
            else
                this._apiProcessor = apiProcessor;
        }

        // Method to get authentication details for WebSocket
        public async Task<AuthenticationDetails> GetAuthenticationDetails()
        {
            return new AuthenticationDetails
            {
                ApiKey = this._apiKey,
                AccessToken = this._accessToken
            };
        }

        public async Task<UserDataStreamResponse> StartUserDataStream()
        {
            return await this._apiProcessor.ProcessPostRequest<UserDataStreamResponse>(ZerodhaEndpoints.UserStream.StartUserDataStream);
        }

        public async Task<UserDataStreamResponse> KeepAliveUserDataStream(string userDataListenKey)
        {
            Guard.AgainstNullOrEmpty(userDataListenKey);
            return await this._apiProcessor.ProcessPutRequest<UserDataStreamResponse>(ZerodhaEndpoints.UserStream.KeepAliveUserDataStream(userDataListenKey));
        }

        public async Task<UserDataStreamResponse> CloseUserDataStream(string userDataListenKey)
        {
            Guard.AgainstNullOrEmpty(userDataListenKey);
            return await this._apiProcessor.ProcessDeleteRequest<UserDataStreamResponse>(ZerodhaEndpoints.UserStream.CloseUserDataStream(userDataListenKey));
        }

        public async Task<EmptyResponse> TestConnectivity()
        {
            return await this._apiProcessor.ProcessGetRequest<EmptyResponse>(ZerodhaEndpoints.General.TestConnectivity);
        }

        public async Task<ServerTimeResponse> GetServerTime()
        {
            return await this._apiProcessor.ProcessGetRequest<ServerTimeResponse>(ZerodhaEndpoints.General.ServerTime);
        }

        public async Task<ExchangeInfoResponse> GetExchangeInfo()
        {
            return await this._apiProcessor.ProcessGetRequest<ExchangeInfoResponse>(ZerodhaEndpoints.General.ExchangeInfo);
        }

        public async Task<OrderBookResponse> GetOrderBook(string symbol, bool useCache = false, int limit = 100)
        {
            Guard.AgainstNull((object)symbol);
            if (limit > 5000)
                throw new ArgumentException("When requesting the order book, you can't request more than 5000 at a time.", nameof(limit));
            return await this._apiProcessor.ProcessGetRequest<OrderBookResponse>(ZerodhaEndpoints.MarketData.OrderBook(symbol, limit, useCache));
        }

        public async Task<List<CompressedAggregateTradeResponse>> GetCompressedAggregateTrades(
          GetCompressedAggregateTradesRequest request,
          MarketType mt)
        {
            Guard.AgainstNull((object)request);
            Guard.AgainstNull((object)request.Symbol);
            int? limit = request.Limit;
            if (limit.HasValue)
            {
                limit = request.Limit;
                int num1 = 0;
                if (!(limit.GetValueOrDefault() <= num1 & limit.HasValue))
                {
                    limit = request.Limit;
                    int num2 = 500;
                    if (!(limit.GetValueOrDefault() > num2 & limit.HasValue))
                        goto label_4;
                }
            }
            request.Limit = new int?(500);
        label_4:
            return await this._apiProcessor.ProcessGetRequest<List<CompressedAggregateTradeResponse>>(ZerodhaEndpoints.MarketData.CompressedAggregateTrades(request, mt));
        }

        public async Task<List<KlineCandleStickResponse>> GetKlinesCandlesticks(
          GetKlinesCandlesticksRequest request,
          MarketType mt)
        {
            Guard.AgainstNull((object)request.Symbol);
            Guard.AgainstNull((object)request.Interval);
            int? limit1 = request.Limit;
            int num1 = 0;
            if (!(limit1.GetValueOrDefault() == num1 & limit1.HasValue))
            {
                int? limit2 = request.Limit;
                int num2 = 500;
                if (!(limit2.GetValueOrDefault() > num2 & limit2.HasValue))
                    goto label_3;
            }
            request.Limit = new int?(500);
        label_3:
            return await this._apiProcessor.ProcessGetRequest<List<KlineCandleStickResponse>>(ZerodhaEndpoints.MarketData.KlineCandlesticks(request, mt));
        }

        public async Task<SymbolPriceChangeTickerResponse> GetDailyTicker(string symbol)
        {
            Guard.AgainstNull((object)symbol);
            return await this._apiProcessor.ProcessGetRequest<SymbolPriceChangeTickerResponse>(ZerodhaEndpoints.MarketData.DayPriceTicker(symbol));
        }

        public async Task<List<SymbolPriceResponse>> GetSymbolsPriceTicker()
        {
            return await this._apiProcessor.ProcessGetRequest<List<SymbolPriceResponse>>(ZerodhaEndpoints.MarketData.AllSymbolsPriceTicker);
        }

        public async Task<List<SymbolOrderBookResponse>> GetSymbolOrderBookTicker()
        {
            return await this._apiProcessor.ProcessGetRequest<List<SymbolOrderBookResponse>>(ZerodhaEndpoints.MarketData.SymbolsOrderBookTicker);
        }

        public async Task<SymbolOrderBookResponse> GetSymbolOrderBookTicker(string symbol)
        {
            Guard.AgainstNull((object)symbol);
            return await this._apiProcessor.ProcessGetRequest<SymbolOrderBookResponse>(ZerodhaEndpoints.MarketData.BookTicker(symbol));
        }

        public async Task<SymbolPriceResponse> GetPrice(string symbol)
        {
            Guard.AgainstNull((object)symbol);
            return await this._apiProcessor.ProcessGetRequest<SymbolPriceResponse>(ZerodhaEndpoints.MarketData.CurrentPrice(symbol));
        }

        public async Task<List<SymbolPriceResponse>> GetAllPrices()
        {
            return await this._apiProcessor.ProcessGetRequest<List<SymbolPriceResponse>>(ZerodhaEndpoints.MarketData.AllPrices);
        }

        public async Task<BaseCreateOrderResponse> CreateOrder(CreateOrderRequest request)
        {
            Guard.AgainstNull((object)request.Symbol);
            Guard.AgainstNull((object)request.Side);
            Guard.AgainstNull((object)request.Type);
            Guard.AgainstNull((object)request.Quantity);

            // Zerodha doesn't support different response types, so we use a single method
            return await this._apiProcessor.ProcessPostRequest<BaseCreateOrderResponse>(ZerodhaEndpoints.Account.NewOrder(request));
        }

        public async Task<EmptyResponse> CreateTestOrder(CreateOrderRequest request)
        {
            Guard.AgainstNull((object)request.Symbol);
            Guard.AgainstNull((object)request.Side);
            Guard.AgainstNull((object)request.Type);
            Guard.AgainstNull((object)request.Quantity);
            return await this._apiProcessor.ProcessPostRequest<EmptyResponse>(ZerodhaEndpoints.Account.NewOrderTest(request));
        }

        public async Task<OrderResponse> QueryOrder(QueryOrderRequest request, int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            Guard.AgainstNull((object)request.Symbol);
            return await this._apiProcessor.ProcessGetRequest<OrderResponse>(ZerodhaEndpoints.Account.QueryOrder(request), receiveWindow);
        }

        public async Task<CancelOrderResponse> CancelOrder(CancelOrderRequest request, int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            Guard.AgainstNull((object)request.Symbol);
            return await this._apiProcessor.ProcessDeleteRequest<CancelOrderResponse>(ZerodhaEndpoints.Account.CancelOrder(request), receiveWindow);
        }

        public async Task<List<OrderResponse>> GetCurrentOpenOrders(
          CurrentOpenOrdersRequest request,
          int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            return await this._apiProcessor.ProcessGetRequest<List<OrderResponse>>(ZerodhaEndpoints.Account.CurrentOpenOrders(request), receiveWindow);
        }

        public async Task<List<OrderResponse>> GetAllOrders(AllOrdersRequest request, int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            Guard.AgainstNull((object)request.Symbol);
            return await this._apiProcessor.ProcessGetRequest<List<OrderResponse>>(ZerodhaEndpoints.Account.AllOrders(request), receiveWindow);
        }

        public async Task<AccountInformationResponse> GetAccountInformation(int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            return await this._apiProcessor.ProcessGetRequest<AccountInformationResponse>(ZerodhaEndpoints.Account.AccountInformation, receiveWindow);
        }

        public async Task<List<AccountTradeReponse>> GetAccountTrades(
          AllTradesRequest request,
          int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            return await this._apiProcessor.ProcessGetRequest<List<AccountTradeReponse>>(ZerodhaEndpoints.Account.AccountTradeList(request), receiveWindow);
        }

        private int SetReceiveWindow(int receiveWindow)
        {
            if (receiveWindow == -1)
                receiveWindow = this._defaultReceiveWindow;
            return receiveWindow;
        }

        // Zerodha-specific methods

        // Get instrument details by symbol
        public async Task<InstrumentResponse> GetInstrumentDetails(string symbol, int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            Guard.AgainstNull((object)symbol);
            return await this._apiProcessor.ProcessGetRequest<InstrumentResponse>(ZerodhaEndpoints.MarketData.InstrumentDetails(symbol), receiveWindow);
        }

        // Get all instruments
        public async Task<List<InstrumentResponse>> GetAllInstruments(int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            return await this._apiProcessor.ProcessGetRequest<List<InstrumentResponse>>(ZerodhaEndpoints.MarketData.AllInstruments, receiveWindow);
        }

        // Get margin used
        public async Task<MarginResponse> GetMarginUsed(int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            return await this._apiProcessor.ProcessGetRequest<MarginResponse>(ZerodhaEndpoints.Account.MarginUsed, receiveWindow);
        }

        // Get holdings
        public async Task<List<HoldingResponse>> GetHoldings(int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            return await this._apiProcessor.ProcessGetRequest<List<HoldingResponse>>(ZerodhaEndpoints.Account.Holdings, receiveWindow);
        }

        // Get positions
        public async Task<PositionsResponse> GetPositions(int receiveWindow = -1)
        {
            receiveWindow = this.SetReceiveWindow(receiveWindow);
            return await this._apiProcessor.ProcessGetRequest<PositionsResponse>(ZerodhaEndpoints.Account.Positions, receiveWindow);
        }

        public Task<WithdrawResponse> CreateWithdrawRequest(WithdrawRequest request, int receiveWindow = 5000)
        {
            throw new NotImplementedException();
        }

        public Task<DepositListResponse> GetDepositHistory(FundHistoryRequest request, int receiveWindow = 5000)
        {
            throw new NotImplementedException();
        }
    }

    // Additional class for WebSocket authentication
    public class AuthenticationDetails
    {
        public string ApiKey { get; set; }
        public string AccessToken { get; set; }
    }

    // Additional model classes for Zerodha-specific responses
    public class InstrumentResponse
    {
        public string InstrumentToken { get; set; }
        public string ExchangeToken { get; set; }
        public string TradingSymbol { get; set; }
        public string Name { get; set; }
        public decimal LastPrice { get; set; }
        public string Exchange { get; set; }
        public string Segment { get; set; }
        public string InstrumentType { get; set; }
        public decimal TickSize { get; set; }
        public decimal LotSize { get; set; }
    }

    public class MarginResponse
    {
        public decimal Available { get; set; }
        public decimal Used { get; set; }
        public decimal Total { get; set; }
    }

    public class HoldingResponse
    {
        public string TradingSymbol { get; set; }
        public string Exchange { get; set; }
        public string InstrumentToken { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal LastPrice { get; set; }
        public int Quantity { get; set; }
        public decimal PnL { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Value { get; set; }
    }

    public class PositionsResponse
    {
        public List<PositionData> Day { get; set; }
        public List<PositionData> Net { get; set; }
    }

    public class PositionData
    {
        public string TradingSymbol { get; set; }
        public string Exchange { get; set; }
        public string InstrumentToken { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal LastPrice { get; set; }
        public decimal PnL { get; set; }
        public int M2M { get; set; }
        public decimal Multiplier { get; set; }
    }
}