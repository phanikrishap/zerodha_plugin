using QABrokerAPI.Common.Enums;
using QABrokerAPI.Common.Extensions;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable disable
namespace QABrokerAPI.Zerodha
{
    internal static class ZerodhaRequestClient
    {
        private static readonly HttpClient HttpClient;
        private static SemaphoreSlim _rateSemaphore;
        private static int _limit = 10;
        public static int SecondsLimit = 10;
        private static string _apiKey = string.Empty;
        private static string _accessToken = string.Empty;
        private static bool RateLimitingEnabled = false;
        private const string APIHeaderKey = "X-Kite-Apikey";
        private const string AccessTokenHeaderKey = "Authorization";
        private static readonly Stopwatch Stopwatch;
        private static int _concurrentRequests = 0;
        private static TimeSpan _timestampOffset;
        private static ILog _logger;
        private static readonly object LockObject = new object();

        static ZerodhaRequestClient()
        {
            ZerodhaRequestClient.HttpClient = new HttpClient((HttpMessageHandler)new HttpClientHandler()
            {
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
            });
            ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ZerodhaRequestClient._rateSemaphore = new SemaphoreSlim(ZerodhaRequestClient._limit, ZerodhaRequestClient._limit);
            ZerodhaRequestClient.Stopwatch = new Stopwatch();
            ZerodhaRequestClient._logger = LogManager.GetLogger(typeof(ZerodhaRequestClient));
        }

        public static void SetRequestLimit(int limit)
        {
            ZerodhaRequestClient._limit = limit;
            ZerodhaRequestClient._rateSemaphore = new SemaphoreSlim(limit, limit);
            ZerodhaRequestClient._logger.Debug((object)$"Request Limit Adjusted to: {limit}");
        }

        public static void SetTimestampOffset(TimeSpan time)
        {
            ZerodhaRequestClient._timestampOffset = time;
            ZerodhaRequestClient._logger.Debug((object)$"Timestamp offset is now : {time}");
        }

        public static void SetRateLimiting(bool enabled)
        {
            string str = enabled ? nameof(enabled) : "disabled";
            ZerodhaRequestClient.RateLimitingEnabled = enabled;
            ZerodhaRequestClient._logger.Debug((object)("Rate Limiting has been " + str));
        }

        public static void SetSecondsLimit(int limit)
        {
            ZerodhaRequestClient.SecondsLimit = limit;
            ZerodhaRequestClient._logger.Debug((object)$"Rate Limiting seconds limit has been set to {limit}");
        }

        public static void SetAPIKey(string key)
        {
            if (ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.Contains(APIHeaderKey))
            {
                lock (ZerodhaRequestClient.LockObject)
                {
                    if (ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.Contains(APIHeaderKey))
                        ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.Remove(APIHeaderKey);
                }
            }
            ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(APIHeaderKey, (IEnumerable<string>)new string[1]
            {
                key
            });
            _apiKey = key;
        }

        public static void SetAccessToken(string token)
        {
            if (ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.Contains(AccessTokenHeaderKey))
            {
                lock (ZerodhaRequestClient.LockObject)
                {
                    if (ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.Contains(AccessTokenHeaderKey))
                        ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.Remove(AccessTokenHeaderKey);
                }
            }
            ZerodhaRequestClient.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(AccessTokenHeaderKey, (IEnumerable<string>)new string[1]
            {
                $"token {_apiKey}:{token}"
            });
            _accessToken = token;
        }

        // Basic request methods
        public static async Task<HttpResponseMessage> GetRequest(Uri endpoint)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a GET Request to " + endpoint.AbsoluteUri));
            return await ZerodhaRequestClient.CreateRequest(endpoint);
        }

        public static async Task<HttpResponseMessage> PostRequest(Uri endpoint)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a POST Request to " + endpoint.AbsoluteUri));
            return await ZerodhaRequestClient.CreateRequest(endpoint, HttpVerb.POST);
        }

        public static async Task<HttpResponseMessage> DeleteRequest(Uri endpoint)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a DELETE Request to " + endpoint.AbsoluteUri));
            return await ZerodhaRequestClient.CreateRequest(endpoint, HttpVerb.DELETE);
        }

        public static async Task<HttpResponseMessage> PutRequest(Uri endpoint)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a PUT Request to " + endpoint.AbsoluteUri));
            return await ZerodhaRequestClient.CreateRequest(endpoint, HttpVerb.PUT);
        }

        // API Key request methods
        public static async Task<HttpResponseMessage> ApiKeyGetRequest(Uri endpoint, string apiKey)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating an API_KEY GET Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            return await ZerodhaRequestClient.CreateRequest(endpoint);
        }

        public static async Task<HttpResponseMessage> ApiKeyPostRequest(Uri endpoint, string apiKey)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating an API_KEY POST Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            return await ZerodhaRequestClient.CreateRequest(endpoint, HttpVerb.POST);
        }

        public static async Task<HttpResponseMessage> ApiKeyDeleteRequest(Uri endpoint, string apiKey)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating an API_KEY DELETE Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            return await ZerodhaRequestClient.CreateRequest(endpoint, HttpVerb.DELETE);
        }

        public static async Task<HttpResponseMessage> ApiKeyPutRequest(Uri endpoint, string apiKey)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating an API_KEY PUT Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            return await ZerodhaRequestClient.CreateRequest(endpoint, HttpVerb.PUT);
        }

        // Signed request methods
        public static async Task<HttpResponseMessage> SignedGetRequest(
            Uri endpoint,
            string apiKey,
            string accessToken,
            string secretKey,
            string signatureRawData)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a SIGNED GET Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            SetAccessToken(accessToken);
            Uri validUri = ZerodhaRequestClient.CreateValidUri(endpoint, secretKey, signatureRawData);
            ZerodhaRequestClient._logger.Debug((object)("Concat URL for request: " + validUri.AbsoluteUri));
            return await ZerodhaRequestClient.CreateRequest(validUri);
        }

        public static async Task<HttpResponseMessage> SignedPostRequest(
            Uri endpoint,
            string apiKey,
            string accessToken,
            string secretKey,
            string signatureRawData)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a SIGNED POST Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            SetAccessToken(accessToken);
            return await ZerodhaRequestClient.CreateRequest(ZerodhaRequestClient.CreateValidUri(endpoint, secretKey, signatureRawData), HttpVerb.POST);
        }

        public static async Task<HttpResponseMessage> SignedDeleteRequest(
            Uri endpoint,
            string apiKey,
            string accessToken,
            string secretKey,
            string signatureRawData)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a SIGNED DELETE Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            SetAccessToken(accessToken);
            return await ZerodhaRequestClient.CreateRequest(ZerodhaRequestClient.CreateValidUri(endpoint, secretKey, signatureRawData), HttpVerb.DELETE);
        }

        public static async Task<HttpResponseMessage> SignedPutRequest(
            Uri endpoint,
            string apiKey,
            string accessToken,
            string secretKey,
            string signatureRawData)
        {
            ZerodhaRequestClient._logger.Debug((object)("Creating a SIGNED PUT Request to " + endpoint.AbsoluteUri));
            SetAPIKey(apiKey);
            SetAccessToken(accessToken);
            return await ZerodhaRequestClient.CreateRequest(ZerodhaRequestClient.CreateValidUri(endpoint, secretKey, signatureRawData), HttpVerb.PUT);
        }

        private static Uri CreateValidUri(
            Uri endpoint,
            string secretKey,
            string signatureRawData)
        {
            // Zerodha uses a different authentication mechanism
            // Instead of adding signature to the URL, we use headers
            // But we'll maintain compatibility with the original interface

            int num = !string.IsNullOrEmpty(signatureRawData) ? 1 : 0;
            string str3 = num == 0 ? "?" : "&";

            // For Zerodha, we primarily use the headers for auth, but we'll keep the URI creation similar
            return new Uri($"{endpoint}{str3}{signatureRawData?.Substring(1) ?? ""}");
        }

        private static string CreateHMACSignature(string key, string totalParams)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(totalParams);
            return BitConverter.ToString(new HMACSHA256(Encoding.UTF8.GetBytes(key)).ComputeHash(bytes)).Replace("-", "").ToLower();
        }

        private static async Task<HttpResponseMessage> CreateRequest(Uri endpoint, HttpVerb verb = HttpVerb.GET)
        {
            if (ZerodhaRequestClient.RateLimitingEnabled)
            {
                await ZerodhaRequestClient._rateSemaphore.WaitAsync();
                if (ZerodhaRequestClient.Stopwatch.Elapsed.Seconds >= ZerodhaRequestClient.SecondsLimit || ZerodhaRequestClient._rateSemaphore.CurrentCount == 0 || ZerodhaRequestClient._concurrentRequests == ZerodhaRequestClient._limit)
                {
                    int num = (ZerodhaRequestClient.SecondsLimit - ZerodhaRequestClient.Stopwatch.Elapsed.Seconds) * 1000;
                    Thread.Sleep(num > 0 ? num : num * -1);
                    ZerodhaRequestClient._concurrentRequests = 0;
                    ZerodhaRequestClient.Stopwatch.Restart();
                }
                ++ZerodhaRequestClient._concurrentRequests;
            }
            Func<Task<HttpResponseMessage>, Task<HttpResponseMessage>> continuationFunction = (Func<Task<HttpResponseMessage>, Task<HttpResponseMessage>>)(t =>
            {
                if (!ZerodhaRequestClient.RateLimitingEnabled)
                    return t;
                ZerodhaRequestClient._rateSemaphore.Release();
                if (ZerodhaRequestClient._rateSemaphore.CurrentCount != ZerodhaRequestClient._limit || ZerodhaRequestClient.Stopwatch.Elapsed.Seconds < ZerodhaRequestClient.SecondsLimit)
                    return t;
                ZerodhaRequestClient.Stopwatch.Restart();
                --ZerodhaRequestClient._concurrentRequests;
                return t;
            });
            Task<HttpResponseMessage> task;
            switch (verb)
            {
                case HttpVerb.GET:
                    task = await ZerodhaRequestClient.HttpClient.GetAsync(endpoint).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
                    break;
                case HttpVerb.POST:
                    task = await ZerodhaRequestClient.HttpClient.PostAsync(endpoint, (HttpContent)null).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
                    break;
                case HttpVerb.DELETE:
                    task = await ZerodhaRequestClient.HttpClient.DeleteAsync(endpoint).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
                    break;
                case HttpVerb.PUT:
                    task = await ZerodhaRequestClient.HttpClient.PutAsync(endpoint, (HttpContent)null).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(verb), (object)verb, (string)null);
            }
            return await task;
        }

        public static void SetLogger(ILog logger) => ZerodhaRequestClient._logger = logger;
    }
}