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

namespace QABrokerAPI.Zerodha
{
    public static class ZerodhaEndpoints
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ContractResolver = (IContractResolver)new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Decimal
        };
        internal static string APIBaseUrl = "https://api.kite.trade";

        private static string APIPrefix { get; } = ZerodhaEndpoints.APIBaseUrl ?? "";

        private static string GenerateQueryStringFromData(IRequest request)
        {
            if (request == null)
                throw new Exception("No request data provided - query string can't be created");
            return string.Join("&", ((JToken)JsonConvert.DeserializeObject(JsonConvert.SerializeObject((object)request, ZerodhaEndpoints._settings), ZerodhaEndpoints._settings)).Children().Cast<JProperty>().Where<JProperty>((Func<JProperty, bool>)(j => j.Value != null)).Select<JProperty, string>((Func<JProperty, string>)(j => $"{j.Name}={WebUtility.UrlEncode(j.Value.ToString())}")));
        }

        public static class UserStream
        {
            internal static string ApiVersion = "v3";

            public static BrokerEndpointData StartUserDataStream
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.UserStream.ApiVersion}/ws/session"), EndpointSecurityType.ApiKey);
                }
            }

            public static BrokerEndpointData KeepAliveUserDataStream(string sessionId)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.UserStream.ApiVersion}/ws/session/ping?session_id={sessionId}"), EndpointSecurityType.ApiKey);
            }

            public static BrokerEndpointData CloseUserDataStream(string sessionId)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.UserStream.ApiVersion}/ws/session?session_id={sessionId}"), EndpointSecurityType.ApiKey);
            }
        }

        public static class General
        {
            internal static string ApiVersion = "v3";

            public static BrokerEndpointData TestConnectivity
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.General.ApiVersion}/ping"), EndpointSecurityType.None);
                }
            }

            public static BrokerEndpointData ServerTime
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.General.ApiVersion}/time"), EndpointSecurityType.None);
                }
            }

            public static BrokerEndpointData ExchangeInfo
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.General.ApiVersion}/exchangeinfo"), EndpointSecurityType.None);
                }
            }
        }

        public static class MarketData
        {
            internal static string ApiVersion = "v3";

            public static BrokerEndpointData OrderBook(string symbol, int limit, bool useCache = false)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/depth?instrument_token={symbol}&depth={limit}"), EndpointSecurityType.Signed, useCache);
            }

            public static BrokerEndpointData CompressedAggregateTrades(
              GetCompressedAggregateTradesRequest request,
              MarketType mt)
            {
                string queryStringFromData = ZerodhaEndpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/trades?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData KlineCandlesticks(
              GetKlinesCandlesticksRequest request,
              MarketType mt)
            {
                string queryStringFromData = ZerodhaEndpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/historical/{request.Symbol}/{request.Interval}?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData DayPriceTicker(string symbol)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/quote?instrument_token={symbol}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData AllSymbolsPriceTicker
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/quote"), EndpointSecurityType.Signed);
                }
            }

            public static BrokerEndpointData SymbolsOrderBookTicker
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/depth"), EndpointSecurityType.Signed);
                }
            }

            public static BrokerEndpointData CurrentPrice(string symbol)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/ltp?instrument_token={symbol}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData AllPrices
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/ltp"), EndpointSecurityType.Signed);
                }
            }

            public static BrokerEndpointData BookTicker(string symbol)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/market/depth?instrument_token={symbol}"), EndpointSecurityType.Signed);
            }

            // Zerodha-specific endpoints
            public static BrokerEndpointData InstrumentDetails(string symbol)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/instruments/{symbol}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData AllInstruments
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.MarketData.ApiVersion}/instruments"), EndpointSecurityType.Signed);
                }
            }
        }

        public static class Account
        {
            internal static string ApiVersion = "v3";

            public static BrokerEndpointData NewOrder(CreateOrderRequest request)
            {
                string queryStringFromData = ZerodhaEndpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/orders?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData NewOrderTest(CreateOrderRequest request)
            {
                string queryStringFromData = ZerodhaEndpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/orders/test?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData QueryOrder(QueryOrderRequest request)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/orders/{request.OrderId}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData CancelOrder(CancelOrderRequest request)
            {
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/orders/{request.OrderId}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData CurrentOpenOrders(CurrentOpenOrdersRequest request)
            {
                string queryStringFromData = ZerodhaEndpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/orders?status=open&{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData AllOrders(AllOrdersRequest request)
            {
                string queryStringFromData = ZerodhaEndpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/orders?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            public static BrokerEndpointData AccountInformation
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/user/profile"), EndpointSecurityType.Signed);
                }
            }

            public static BrokerEndpointData AccountTradeList(AllTradesRequest request)
            {
                string queryStringFromData = ZerodhaEndpoints.GenerateQueryStringFromData((IRequest)request);
                return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/trades?{queryStringFromData}"), EndpointSecurityType.Signed);
            }

            // Zerodha-specific endpoints
            public static BrokerEndpointData MarginUsed
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/user/margins"), EndpointSecurityType.Signed);
                }
            }

            public static BrokerEndpointData Holdings
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/portfolio/holdings"), EndpointSecurityType.Signed);
                }
            }

            public static BrokerEndpointData Positions
            {
                get
                {
                    return new BrokerEndpointData(new Uri($"{ZerodhaEndpoints.APIPrefix}/{ZerodhaEndpoints.Account.ApiVersion}/portfolio/positions"), EndpointSecurityType.Signed);
                }
            }
        }
    }
}