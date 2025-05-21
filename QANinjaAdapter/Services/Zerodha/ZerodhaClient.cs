using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QANinjaAdapter.Services.Configuration;

namespace QANinjaAdapter.Services.Zerodha
{
    /// <summary>
    /// Handles communication with the Zerodha API
    /// </summary>
    public class ZerodhaClient
    {
        private static ZerodhaClient _instance;
        private readonly ConfigurationManager _configManager;
        private readonly HttpClient _httpClient;

        // Base URL for Zerodha API
        private const string BASE_URL = "https://api.kite.trade";

        /// <summary>
        /// Gets the singleton instance of the ZerodhaClient
        /// </summary>
        public static ZerodhaClient Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ZerodhaClient();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private ZerodhaClient()
        {
            _configManager = ConfigurationManager.Instance;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BASE_URL)
            };
        }

        /// <summary>
        /// Checks if the connection to the Zerodha API is valid
        /// </summary>
        /// <returns>True if the connection is valid, false otherwise</returns>
        public bool CheckConnection()
        {
            try
            {
                // Set up credentials
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Kite-Apikey", _configManager.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_configManager.ApiKey}:{_configManager.AccessToken}");

                // Try a valid endpoint - for example, the user profile endpoint
                var response = _httpClient.GetAsync("/user/profile").Result;

                // Log response details for troubleshooting
                Logger.Info($"Zerodha API connection response: {response.StatusCode} - {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    string content = response.Content.ReadAsStringAsync().Result;
                    Logger.Info($"Response content: {content}");

                    // Parse JSON response to check for token errors
                    try
                    {
                        JObject errorJson = JObject.Parse(content);
                        string errorType = errorJson["error_type"]?.ToString();
                        string errorMessage = errorJson["message"]?.ToString();

                        if (errorType == "TokenException" ||
                            errorMessage?.Contains("token") == true ||
                            errorMessage?.Contains("authorization") == true ||
                            errorMessage?.Contains("access") == true)
                        {
                            // Specific log for token errors
                            Logger.Error($"Access token invalid or expired: {errorMessage}");
                            NinjaTrader.NinjaScript.NinjaScript.Log($"Authentication Error: Access token invalid or expired. Please update your token.", NinjaTrader.Cbi.LogLevel.Error);
                        }
                        else
                        {
                            // General API error
                            NinjaTrader.NinjaScript.NinjaScript.Log(content, NinjaTrader.Cbi.LogLevel.Error);
                        }
                    }
                    catch
                    {
                        // If JSON parsing fails, log the raw content
                        NinjaTrader.NinjaScript.NinjaScript.Log(content, NinjaTrader.Cbi.LogLevel.Error);
                    }
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger.Error($"Connection check error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new HttpClient with the appropriate headers for Zerodha API requests
        /// </summary>
        /// <returns>A configured HttpClient instance</returns>
        public HttpClient CreateAuthorizedClient()
        {
            var client = new HttpClient();
            
            // Set up credentials
            client.DefaultRequestHeaders.Add("X-Kite-Apikey", _configManager.ApiKey);
            client.DefaultRequestHeaders.Add("Authorization", $"token {_configManager.ApiKey}:{_configManager.AccessToken}");
            
            return client;
        }

        /// <summary>
        /// Gets the WebSocket URL for Zerodha
        /// </summary>
        /// <returns>The WebSocket URL with authentication parameters</returns>
        public string GetWebSocketUrl()
        {
            return $"wss://ws.kite.trade?api_key={_configManager.ApiKey}&access_token={_configManager.AccessToken}";
        }
    }
}
