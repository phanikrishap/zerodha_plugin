// Decompiled with JetBrains decompiler
// Type: BinanceExchange.API.Client.BinanceClient
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

using QABrokerAPI.Binance;
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
namespace QABrokerAPI.Binance;

public class BrokerClient : IBrokerClient
{
    private TimeSpan _timestampOffset;
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly IAPIProcessor _apiProcessor;
    private readonly int _defaultReceiveWindow;
    private readonly ILog _logger;

    public TimeSpan TimestampOffset
    {
        get => this._timestampOffset;
        set
        {
            this._timestampOffset = value;
            RequestClient.SetTimestampOffset(this._timestampOffset);
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
        RequestClient.SetTimestampOffset(configuration.TimestampOffset);
        RequestClient.SetRateLimiting(configuration.EnableRateLimiting);
        RequestClient.SetAPIKey(this._apiKey);
        if (apiProcessor == null)
        {
            this._apiProcessor = (IAPIProcessor)new APIProcessor(this._apiKey, this._secretKey, (IAPICacheManager)new APICacheManager());
            this._apiProcessor.SetCacheTime(configuration.CacheTime);
        }
        else
            this._apiProcessor = apiProcessor;
    }

    public async Task<UserDataStreamResponse> StartUserDataStream()
    {
        return await this._apiProcessor.ProcessPostRequest<UserDataStreamResponse>(Endpoints.UserStream.StartUserDataStream);
    }

    public async Task<UserDataStreamResponse> KeepAliveUserDataStream(string userDataListenKey)
    {
        Guard.AgainstNullOrEmpty(userDataListenKey);
        return await this._apiProcessor.ProcessPutRequest<UserDataStreamResponse>(Endpoints.UserStream.KeepAliveUserDataStream(userDataListenKey));
    }

    public async Task<UserDataStreamResponse> CloseUserDataStream(string userDataListenKey)
    {
        Guard.AgainstNullOrEmpty(userDataListenKey);
        return await this._apiProcessor.ProcessDeleteRequest<UserDataStreamResponse>(Endpoints.UserStream.CloseUserDataStream(userDataListenKey));
    }

    public async Task<EmptyResponse> TestConnectivity()
    {
        return await this._apiProcessor.ProcessGetRequest<EmptyResponse>(Endpoints.General.TestConnectivity);
    }

    public async Task<ServerTimeResponse> GetServerTime()
    {
        return await this._apiProcessor.ProcessGetRequest<ServerTimeResponse>(Endpoints.General.ServerTime);
    }

    public async Task<ExchangeInfoResponse> GetExchangeInfo()
    {
        return await this._apiProcessor.ProcessGetRequest<ExchangeInfoResponse>(Endpoints.General.ExchangeInfo);
    }

    public async Task<OrderBookResponse> GetOrderBook(string symbol, bool useCache = false, int limit = 100)
    {
        Guard.AgainstNull((object)symbol);
        if (limit > 5000)
            throw new ArgumentException("When requesting the order book, you can't request more than 5000 at a time.", nameof(limit));
        return await this._apiProcessor.ProcessGetRequest<OrderBookResponse>(Endpoints.MarketData.OrderBook(symbol, limit, useCache));
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
        return await this._apiProcessor.ProcessGetRequest<List<CompressedAggregateTradeResponse>>(Endpoints.MarketData.CompressedAggregateTrades(request, mt));
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
        return await this._apiProcessor.ProcessGetRequest<List<KlineCandleStickResponse>>(Endpoints.MarketData.KlineCandlesticks(request, mt));
    }

    public async Task<SymbolPriceChangeTickerResponse> GetDailyTicker(string symbol)
    {
        Guard.AgainstNull((object)symbol);
        return await this._apiProcessor.ProcessGetRequest<SymbolPriceChangeTickerResponse>(Endpoints.MarketData.DayPriceTicker(symbol));
    }

    public async Task<List<SymbolPriceResponse>> GetSymbolsPriceTicker()
    {
        return await this._apiProcessor.ProcessGetRequest<List<SymbolPriceResponse>>(Endpoints.MarketData.AllSymbolsPriceTicker);
    }

    public async Task<List<SymbolOrderBookResponse>> GetSymbolOrderBookTicker()
    {
        return await this._apiProcessor.ProcessGetRequest<List<SymbolOrderBookResponse>>(Endpoints.MarketData.SymbolsOrderBookTicker);
    }

    public async Task<SymbolOrderBookResponse> GetSymbolOrderBookTicker(string symbol)
    {
        Guard.AgainstNull((object)symbol);
        return await this._apiProcessor.ProcessGetRequest<SymbolOrderBookResponse>(Endpoints.MarketDataV3.BookTicker(symbol));
    }

    public async Task<SymbolPriceResponse> GetPrice(string symbol)
    {
        Guard.AgainstNull((object)symbol);
        return await this._apiProcessor.ProcessGetRequest<SymbolPriceResponse>(Endpoints.MarketDataV3.CurrentPrice(symbol));
    }

    public async Task<List<SymbolPriceResponse>> GetAllPrices()
    {
        return await this._apiProcessor.ProcessGetRequest<List<SymbolPriceResponse>>(Endpoints.MarketDataV3.AllPrices);
    }

    public async Task<BaseCreateOrderResponse> CreateOrder(CreateOrderRequest request)
    {
        Guard.AgainstNull((object)request.Symbol);
        Guard.AgainstNull((object)request.Side);
        Guard.AgainstNull((object)request.Type);
        Guard.AgainstNull((object)request.Quantity);
        switch (request.NewOrderResponseType)
        {
            case NewOrderResponseType.Acknowledge:
                return (BaseCreateOrderResponse)await this._apiProcessor.ProcessPostRequest<AcknowledgeCreateOrderResponse>(Endpoints.Account.NewOrder(request));
            case NewOrderResponseType.Full:
                return (BaseCreateOrderResponse)await this._apiProcessor.ProcessPostRequest<FullCreateOrderResponse>(Endpoints.Account.NewOrder(request));
            default:
                return (BaseCreateOrderResponse)await this._apiProcessor.ProcessPostRequest<ResultCreateOrderResponse>(Endpoints.Account.NewOrder(request));
        }
    }

    public async Task<EmptyResponse> CreateTestOrder(CreateOrderRequest request)
    {
        Guard.AgainstNull((object)request.Symbol);
        Guard.AgainstNull((object)request.Side);
        Guard.AgainstNull((object)request.Type);
        Guard.AgainstNull((object)request.Quantity);
        return await this._apiProcessor.ProcessPostRequest<EmptyResponse>(Endpoints.Account.NewOrderTest(request));
    }

    public async Task<OrderResponse> QueryOrder(QueryOrderRequest request, int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        Guard.AgainstNull((object)request.Symbol);
        return await this._apiProcessor.ProcessGetRequest<OrderResponse>(Endpoints.Account.QueryOrder(request), receiveWindow);
    }

    public async Task<CancelOrderResponse> CancelOrder(CancelOrderRequest request, int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        Guard.AgainstNull((object)request.Symbol);
        return await this._apiProcessor.ProcessDeleteRequest<CancelOrderResponse>(Endpoints.Account.CancelOrder(request), receiveWindow);
    }

    public async Task<List<OrderResponse>> GetCurrentOpenOrders(
      CurrentOpenOrdersRequest request,
      int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        return await this._apiProcessor.ProcessGetRequest<List<OrderResponse>>(Endpoints.Account.CurrentOpenOrders(request), receiveWindow);
    }

    public async Task<List<OrderResponse>> GetAllOrders(AllOrdersRequest request, int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        Guard.AgainstNull((object)request.Symbol);
        return await this._apiProcessor.ProcessGetRequest<List<OrderResponse>>(Endpoints.Account.AllOrders(request), receiveWindow);
    }

    public async Task<AccountInformationResponse> GetAccountInformation(int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        return await this._apiProcessor.ProcessGetRequest<AccountInformationResponse>(Endpoints.Account.AccountInformation, receiveWindow);
    }

    public async Task<List<AccountTradeReponse>> GetAccountTrades(
      AllTradesRequest request,
      int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        return await this._apiProcessor.ProcessGetRequest<List<AccountTradeReponse>>(Endpoints.Account.AccountTradeList(request), receiveWindow);
    }

    public async Task<WithdrawResponse> CreateWithdrawRequest(
      WithdrawRequest request,
      int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        Guard.AgainstNullOrEmpty(request.Asset);
        Guard.AgainstNullOrEmpty(request.Address);
        Guard.AgainstNull((object)request.Amount);
        return await this._apiProcessor.ProcessPostRequest<WithdrawResponse>(Endpoints.Account.Withdraw(request), receiveWindow);
    }

    public async Task<DepositListResponse> GetDepositHistory(
      FundHistoryRequest request,
      int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        return await this._apiProcessor.ProcessGetRequest<DepositListResponse>(Endpoints.Account.DepositHistory(request), receiveWindow);
    }

    public async Task<WithdrawListResponse> GetWithdrawHistory(
      FundHistoryRequest request,
      int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        Guard.AgainstNull((object)request);
        return await this._apiProcessor.ProcessGetRequest<WithdrawListResponse>(Endpoints.Account.WithdrawHistory(request), receiveWindow);
    }

    public async Task<DepositAddressResponse> DepositAddress(
      DepositAddressRequest request,
      int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        Guard.AgainstNull((object)request);
        Guard.AgainstNullOrEmpty(request.Asset);
        return await this._apiProcessor.ProcessGetRequest<DepositAddressResponse>(Endpoints.Account.DepositAddress(request), receiveWindow);
    }

    public async Task<DepositAddressResponse> GetSystemStatus(int receiveWindow = -1)
    {
        receiveWindow = this.SetReceiveWindow(receiveWindow);
        return await this._apiProcessor.ProcessGetRequest<DepositAddressResponse>(Endpoints.Account.SystemStatus(), receiveWindow);
    }

    private int SetReceiveWindow(int receiveWindow)
    {
        if (receiveWindow == -1)
            receiveWindow = this._defaultReceiveWindow;
        return receiveWindow;
    }
}
