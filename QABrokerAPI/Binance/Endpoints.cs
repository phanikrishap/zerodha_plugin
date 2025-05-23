using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Models.Request.Interfaces;
using QABrokerAPI.Common.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QABrokerAPI.Binance
{
    public static class Endpoints
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ContractResolver = (IContractResolver)new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Decimal
        };
        internal static string APIBaseUrl = "https://api.binance.com/api";
        internal static string APIBaseUrlCoinMFut = "https://dapi.binance.com/dapi";
        internal static string APIBaseUrlUsdMFutures = "https://fapi.binance.com/fapi";
        internal static string WAPIBaseUrl = "https://api.binance.com/wapi";

        private static string APIPrefix { get; } = Endpoints.APIBaseUrl ?? "";

        private static string APIPrefixCoinMFut { get; } = Endpoints.APIBaseUrlCoinMFut ?? "";

        private static string APIPrefixUsdMFut { get; } = Endpoints.APIBaseUrlUsdMFutures ?? "";

        private static string WAPIPrefix { get; } = Endpoints.WAPIBaseUrl ?? "";

        private static string GenerateQueryStringFromData(IRequest request)
        {
            if (request == null)
                throw new Exception("No request data provided - query string can't be created");
            return string.Join("&", ((JToken)JsonConvert.DeserializeObject(JsonConvert.SerializeObject((object)request, Endpoints._settings), Endpoints._settings)).Children().Cast<JProperty>().Where<JProperty>((Func<JProperty, bool>)(j => j.Value != null)).Select<JProperty, string>((Func<JProperty, string>)(j => $"{j.Name}={WebUtility.UrlEncode(j.Value.ToString())}")));
        }

        public static class UserStream
        {
            internal static string ApiVersion = "v1";

            public static BrokerEndpointData StartUserDataStream
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.UserStream.ApiVersion}/userDataStream"), EndpointSecurityType.ApiKey);
                }
            }

            public static BrokerEndpointData KeepAliveUserDataStream(string listenKey)
            {
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.UserStream.ApiVersion}/userDataStream?listenKey={listenKey}"), EndpointSecurityType.ApiKey);
            }

            public static BrokerEndpointData CloseUserDataStream(string listenKey)
            {
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.UserStream.ApiVersion}/userDataStream?listenKey={listenKey}"), EndpointSecurityType.ApiKey);
            }
        }

        public static class General
        {
            internal static string ApiVersion = "v1";

            public static BrokerEndpointData TestConnectivity
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.General.ApiVersion}/ping"), EndpointSecurityType.None);
                }
            }

            public static BrokerEndpointData ServerTime
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.General.ApiVersion}/time"), EndpointSecurityType.None);
                }
            }

            public static BrokerEndpointData ExchangeInfo
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.General.ApiVersion}/exchangeInfo"), EndpointSecurityType.None);
                }
            }
        }

        public static class MarketData
        {
            internal static string ApiVersion = "v1";

            public static BrokerEndpointData OrderBook(string symbol, int limit, bool useCache = false)
            {
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketData.ApiVersion}/depth?symbol={symbol}&limit={limit}"), EndpointSecurityType.None, useCache);
            }

            public static BrokerEndpointData CompressedAggregateTrades(
              GetCompressedAggregateTradesRequest request,
              MarketType mt)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                BrokerEndpointData binanceEndpointData = new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketData.ApiVersion}/aggTrades?{queryStringFromData}"), EndpointSecurityType.None);
                if (mt == MarketType.UsdM)
                    binanceEndpointData = new BrokerEndpointData(new Uri($"{Endpoints.APIPrefixUsdMFut}/{Endpoints.MarketData.ApiVersion}/aggTrades?{queryStringFromData}"), EndpointSecurityType.None);
                if (mt == MarketType.CoinM)
                    binanceEndpointData = new BrokerEndpointData(new Uri($"{Endpoints.APIPrefixCoinMFut}/{Endpoints.MarketData.ApiVersion}/aggTrades?{queryStringFromData}"), EndpointSecurityType.None);
                return binanceEndpointData;
            }

            public static BrokerEndpointData KlineCandlesticks(
              GetKlinesCandlesticksRequest request,
              MarketType mt)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                BrokerEndpointData binanceEndpointData = new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketData.ApiVersion}/klines?{queryStringFromData}"), EndpointSecurityType.None);
                if (mt == MarketType.UsdM)
                    binanceEndpointData = new BrokerEndpointData(new Uri($"{Endpoints.APIPrefixUsdMFut}/{Endpoints.MarketData.ApiVersion}/klines?{queryStringFromData}"), EndpointSecurityType.None);
                if (mt == MarketType.CoinM)
                    binanceEndpointData = new BrokerEndpointData(new Uri($"{Endpoints.APIPrefixCoinMFut}/{Endpoints.MarketData.ApiVersion}/klines?{queryStringFromData}"), EndpointSecurityType.None);
                return binanceEndpointData;
            }

            public static BrokerEndpointData DayPriceTicker(string symbol)
            {
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketData.ApiVersion}/ticker/24hr?symbol={symbol}"), EndpointSecurityType.None);
            }

            public static BrokerEndpointData AllSymbolsPriceTicker
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketData.ApiVersion}/ticker/allPrices"), EndpointSecurityType.ApiKey);
                }
            }

            public static BrokerEndpointData SymbolsOrderBookTicker
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketData.ApiVersion}/ticker/allBookTickers"), EndpointSecurityType.ApiKey);
                }
            }
        }

        public static class MarketDataV3
        {
            internal static string ApiVersion = "v3";

            public static BrokerEndpointData CurrentPrice(string symbol)
            {
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketDataV3.ApiVersion}/ticker/price?symbol={symbol}"), EndpointSecurityType.None);
            }

            public static BrokerEndpointData AllPrices
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketDataV3.ApiVersion}/ticker/price"), EndpointSecurityType.None);
                }
            }

            public static BrokerEndpointData BookTicker(string symbol)
            {
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.MarketDataV3.ApiVersion}/ticker/bookTicker?symbol={symbol}"), EndpointSecurityType.None);
            }
        }

        public static class Account
        {
            internal static string ApiVersion = "v3";

            public static BrokerEndpointData NewOrder(CreateOrderRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/order?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData NewOrderTest(CreateOrderRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/order/test?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData QueryOrder(QueryOrderRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/order?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData CancelOrder(CancelOrderRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/order?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData CurrentOpenOrders(CurrentOpenOrdersRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/openOrders?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData AllOrders(AllOrdersRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/allOrders?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData AccountInformation
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/account"), EndpointSecurityType.Signed);
                }
            }

            public static BrokerEndpointData AccountTradeList(AllTradesRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.APIPrefix}/{Endpoints.Account.ApiVersion}/myTrades?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData Withdraw(WithdrawRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.WAPIPrefix}/{Endpoints.Account.ApiVersion}/withdraw.html?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData DepositHistory(FundHistoryRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.WAPIPrefix}/{Endpoints.Account.ApiVersion}/depositHistory.html?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData WithdrawHistory(FundHistoryRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.WAPIPrefix}/{Endpoints.Account.ApiVersion}/withdrawHistory.html?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData DepositAddress(DepositAddressRequest request)
            {
                string queryStringFromData = Endpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{Endpoints.WAPIPrefix}/{Endpoints.Account.ApiVersion}/depositAddress.html?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData SystemStatus()
            {
                return new BrokerEndpointData(new Uri($"{Endpoints.WAPIPrefix}/{Endpoints.Account.ApiVersion}/systemStatus.html"), EndpointSecurityType.None);
            }
        }
    }
}
