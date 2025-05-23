// Decompiled with JetBrains decompiler
// Type: BinanceExchange.API.RequestClient
// Assembly: BinanceAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D349CB21-077C-4B48-99EA-7AB6C64F9B14
// Assembly location: D:\NTConnector References\Binance Adapter\BinanceAdapterInstaller\BinanceAPI.dll

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
namespace QABrokerAPI;

internal static class RequestClient
{
  private static readonly HttpClient HttpClient;
  private static SemaphoreSlim _rateSemaphore;
  private static int _limit = 10;
  public static int SecondsLimit = 10;
  private static string _apiKey = string.Empty;
  private static bool RateLimitingEnabled = false;
  private const string APIHeader = "X-MBX-APIKEY";
  private static readonly Stopwatch Stopwatch;
  private static int _concurrentRequests = 0;
  private static TimeSpan _timestampOffset;
  private static ILog _logger;
  private static readonly object LockObject = new object();

  static RequestClient()
  {
    RequestClient.HttpClient = new HttpClient((HttpMessageHandler) new HttpClientHandler()
    {
      AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate)
    });
    RequestClient.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    RequestClient._rateSemaphore = new SemaphoreSlim(RequestClient._limit, RequestClient._limit);
    RequestClient.Stopwatch = new Stopwatch();
    RequestClient._logger = LogManager.GetLogger(typeof (RequestClient));
  }

  public static void SetRequestLimit(int limit)
  {
    RequestClient._limit = limit;
    RequestClient._rateSemaphore = new SemaphoreSlim(limit, limit);
    RequestClient._logger.Debug((object) $"Request Limit Adjusted to: {limit}");
  }

  public static void SetTimestampOffset(TimeSpan time)
  {
    RequestClient._timestampOffset = time;
    RequestClient._logger.Debug((object) $"Timestamp offset is now : {time}");
  }

  public static void SetRateLimiting(bool enabled)
  {
    string str = enabled ? nameof (enabled) : "disabled";
    RequestClient.RateLimitingEnabled = enabled;
    RequestClient._logger.Debug((object) ("Rate Limiting has been " + str));
  }

  public static void SetSecondsLimit(int limit)
  {
    RequestClient.SecondsLimit = limit;
    RequestClient._logger.Debug((object) $"Rate Limiting seconds limit has been set to {limit}");
  }

  public static void SetAPIKey(string key)
  {
    if (RequestClient.HttpClient.DefaultRequestHeaders.Contains("X-MBX-APIKEY"))
    {
      lock (RequestClient.LockObject)
      {
        if (RequestClient.HttpClient.DefaultRequestHeaders.Contains("X-MBX-APIKEY"))
          RequestClient.HttpClient.DefaultRequestHeaders.Remove("X-MBX-APIKEY");
      }
    }
    RequestClient.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-MBX-APIKEY", (IEnumerable<string>) new string[1]
    {
      key
    });
  }

  public static async Task<HttpResponseMessage> GetRequest(Uri endpoint)
  {
    RequestClient._logger.Debug((object) ("Creating a GET Request to " + endpoint.AbsoluteUri));
    return await RequestClient.CreateRequest(endpoint);
  }

  public static async Task<HttpResponseMessage> SignedGetRequest(
    Uri endpoint,
    string apiKey,
    string secretKey,
    string signatureRawData,
    long receiveWindow = 5000)
  {
    RequestClient._logger.Debug((object) ("Creating a SIGNED GET Request to " + endpoint.AbsoluteUri));
    Uri validUri = RequestClient.CreateValidUri(endpoint, secretKey, signatureRawData, receiveWindow);
    RequestClient._logger.Debug((object) ("Concat URL for request: " + validUri.AbsoluteUri));
    return await RequestClient.CreateRequest(validUri);
  }

  public static async Task<HttpResponseMessage> PostRequest(Uri endpoint)
  {
    RequestClient._logger.Debug((object) ("Creating a POST Request to " + endpoint.AbsoluteUri));
    return await RequestClient.CreateRequest(endpoint, HttpVerb.POST);
  }

  public static async Task<HttpResponseMessage> DeleteRequest(Uri endpoint)
  {
    RequestClient._logger.Debug((object) ("Creating a DELETE Request to " + endpoint.AbsoluteUri));
    return await RequestClient.CreateRequest(endpoint, HttpVerb.DELETE);
  }

  public static async Task<HttpResponseMessage> PutRequest(Uri endpoint)
  {
    RequestClient._logger.Debug((object) ("Creating a PUT Request to " + endpoint.AbsoluteUri));
    return await RequestClient.CreateRequest(endpoint, HttpVerb.PUT);
  }

  public static async Task<HttpResponseMessage> SignedPostRequest(
    Uri endpoint,
    string apiKey,
    string secretKey,
    string signatureRawData,
    long receiveWindow = 5000)
  {
    RequestClient._logger.Debug((object) ("Creating a SIGNED POST Request to " + endpoint.AbsoluteUri));
    return await RequestClient.CreateRequest(RequestClient.CreateValidUri(endpoint, secretKey, signatureRawData, receiveWindow), HttpVerb.POST);
  }

  public static async Task<HttpResponseMessage> SignedDeleteRequest(
    Uri endpoint,
    string apiKey,
    string secretKey,
    string signatureRawData,
    long receiveWindow = 5000)
  {
    RequestClient._logger.Debug((object) ("Creating a SIGNED DELETE Request to " + endpoint.AbsoluteUri));
    return await RequestClient.CreateRequest(RequestClient.CreateValidUri(endpoint, secretKey, signatureRawData, receiveWindow), HttpVerb.DELETE);
  }

  private static Uri CreateValidUri(
    Uri endpoint,
    string secretKey,
    string signatureRawData,
    long receiveWindow)
  {
    string str1 = DateTime.UtcNow.AddMilliseconds(RequestClient._timestampOffset.TotalMilliseconds).ConvertToUnixTime().ToString();
    int num = !string.IsNullOrEmpty(signatureRawData) ? 1 : 0;
    string str2 = $"timestamp={str1}&recvWindow={receiveWindow}";
    string totalParams = !string.IsNullOrEmpty(signatureRawData) ? $"{signatureRawData.Substring(1)}&{str2}" : str2 ?? "";
    string hmacSignature = RequestClient.CreateHMACSignature(secretKey, totalParams);
    string str3 = num == 0 ? "?" : "&";
    return new Uri($"{endpoint}{str3}{str2}&signature={hmacSignature}");
  }

  private static string CreateHMACSignature(string key, string totalParams)
  {
    byte[] bytes = Encoding.UTF8.GetBytes(totalParams);
    return BitConverter.ToString(new HMACSHA256(Encoding.UTF8.GetBytes(key)).ComputeHash(bytes)).Replace("-", "").ToLower();
  }

  private static async Task<HttpResponseMessage> CreateRequest(Uri endpoint, HttpVerb verb = HttpVerb.GET)
  {
    if (RequestClient.RateLimitingEnabled)
    {
      await RequestClient._rateSemaphore.WaitAsync();
      if (RequestClient.Stopwatch.Elapsed.Seconds >= RequestClient.SecondsLimit || RequestClient._rateSemaphore.CurrentCount == 0 || RequestClient._concurrentRequests == RequestClient._limit)
      {
        int num = (RequestClient.SecondsLimit - RequestClient.Stopwatch.Elapsed.Seconds) * 1000;
        Thread.Sleep(num > 0 ? num : num * -1);
        RequestClient._concurrentRequests = 0;
        RequestClient.Stopwatch.Restart();
      }
      ++RequestClient._concurrentRequests;
    }
    Func<Task<HttpResponseMessage>, Task<HttpResponseMessage>> continuationFunction = (Func<Task<HttpResponseMessage>, Task<HttpResponseMessage>>) (t =>
    {
      if (!RequestClient.RateLimitingEnabled)
        return t;
      RequestClient._rateSemaphore.Release();
      if (RequestClient._rateSemaphore.CurrentCount != RequestClient._limit || RequestClient.Stopwatch.Elapsed.Seconds < RequestClient.SecondsLimit)
        return t;
      RequestClient.Stopwatch.Restart();
      --RequestClient._concurrentRequests;
      return t;
    });
    Task<HttpResponseMessage> task;
    switch (verb)
    {
      case HttpVerb.GET:
        task = await RequestClient.HttpClient.GetAsync(endpoint).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
        break;
      case HttpVerb.POST:
        task = await RequestClient.HttpClient.PostAsync(endpoint, (HttpContent) null).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
        break;
      case HttpVerb.DELETE:
        task = await RequestClient.HttpClient.DeleteAsync(endpoint).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
        break;
      case HttpVerb.PUT:
        task = await RequestClient.HttpClient.PutAsync(endpoint, (HttpContent) null).ContinueWith<Task<HttpResponseMessage>>(continuationFunction);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof (verb), (object) verb, (string) null);
    }
    return await task;
  }

  public static void SetLogger(ILog logger) => RequestClient._logger = logger;
}
