using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace QANinjaAdapter.Services.Configuration
{
    /// <summary>
    /// Manages loading and accessing configuration settings for the adapter
    /// </summary>
    public class ConfigurationManager
    {
        // Configuration file path
        private const string CONFIG_FILE_PATH = "NinjaTrader 8\\QAAdapter\\config.json";
        
        // Singleton instance
        private static ConfigurationManager _instance;
        
        // Configuration data
        private JObject _config;
        
        // Active broker settings
        private string _activeWebSocketBroker;
        private string _activeHistoricalBroker;
        
        // Credentials
        private string _apiKey = string.Empty;
        private string _secretKey = string.Empty;
        private string _accessToken = string.Empty;

        // Logging configuration
        private bool _enableVerboseTickLogging = false; // Default to false

        /// <summary>
        /// Gets the singleton instance of the ConfigurationManager
        /// </summary>
        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ConfigurationManager();
                return _instance;
            }
        }

        /// <summary>
        /// Gets the API key for the active broker
        /// </summary>
        public string ApiKey => _apiKey;

        /// <summary>
        /// Gets the secret key for the active broker
        /// </summary>
        public string SecretKey => _secretKey;

        /// <summary>
        /// Gets the access token for the active broker
        /// </summary>
        public string AccessToken => _accessToken;

        /// <summary>
        /// Gets the active WebSocket broker name
        /// </summary>
        public string ActiveWebSocketBroker => _activeWebSocketBroker;

        /// <summary>
        /// Gets the active historical data broker name
        /// </summary>
        public string ActiveHistoricalBroker => _activeHistoricalBroker;

        /// <summary>
        /// Gets whether verbose tick logging is enabled
        /// </summary>
        public bool EnableVerboseTickLogging => _enableVerboseTickLogging;

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private ConfigurationManager()
        {
        }

        /// <summary>
        /// Loads configuration from the config file
        /// </summary>
        /// <returns>True if configuration was loaded successfully, false otherwise</returns>
        public bool LoadConfiguration()
        {
            try
            {
                // Get the user's Documents folder path
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fullConfigPath = Path.Combine(documentsPath, CONFIG_FILE_PATH);

                // Check if config file exists
                if (!File.Exists(fullConfigPath))
                {
                    MessageBox.Show($"Configuration file not found at: {fullConfigPath}",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Read JSON configuration
                string jsonConfig = File.ReadAllText(fullConfigPath);
                _config = JObject.Parse(jsonConfig);

                // Get active broker configurations
                JObject activeBrokers = _config["Active"] as JObject;

                if (activeBrokers == null)
                {
                    MessageBox.Show("No active broker specified in the configuration file.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Get websocket and historical broker names
                _activeWebSocketBroker = activeBrokers["Websocket"]?.ToString();
                _activeHistoricalBroker = activeBrokers["Historical"]?.ToString();

                if (string.IsNullOrEmpty(_activeWebSocketBroker) || string.IsNullOrEmpty(_activeHistoricalBroker))
                {
                    MessageBox.Show("Websocket or Historical broker not specified in Active configuration.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Load websocket broker configuration
                JObject webSocketBrokerConfig = _config[_activeWebSocketBroker] as JObject;

                Logger.Info($"Loading configuration for websocket broker: {_activeWebSocketBroker}.");

                if (webSocketBrokerConfig != null)
                {
                    // Update API keys for websocket
                    LoadBrokerCredentials(webSocketBrokerConfig);
                }
                else
                {
                    MessageBox.Show($"Configuration for websocket broker '{_activeWebSocketBroker}' not found.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // If historical broker is different, load its configuration too
                if (_activeWebSocketBroker != _activeHistoricalBroker)
                {
                    JObject historicalBrokerConfig = _config[_activeHistoricalBroker] as JObject;
                    if (historicalBrokerConfig != null)
                    {
                        // You might need separate variables for historical broker
                        // This is just updating the same variables
                        LoadBrokerCredentials(historicalBrokerConfig);
                    }
                    else
                    {
                        MessageBox.Show($"Configuration for historical broker '{_activeHistoricalBroker}' not found.",
                            "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                // Load general settings (like logging)
                JObject generalSettings = _config["GeneralSettings"] as JObject;
                if (generalSettings != null)
                {
                    _enableVerboseTickLogging = generalSettings["EnableVerboseTickLogging"]?.ToObject<bool>() ?? false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}",
                    "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Loads broker credentials from the specified broker configuration
        /// </summary>
        /// <param name="brokerConfig">The broker configuration object</param>
        private void LoadBrokerCredentials(JObject brokerConfig)
        {
            _apiKey = brokerConfig["Api"]?.ToString() ?? _apiKey;
            _secretKey = brokerConfig["Secret"]?.ToString() ?? _secretKey;
            _accessToken = brokerConfig["AccessToken"]?.ToString() ?? _accessToken;

            Logger.Info($"Broker API credentials have been processed.");
        }

        /// <summary>
        /// Gets the credentials for a specific broker
        /// </summary>
        /// <param name="brokerName">The name of the broker</param>
        /// <returns>A tuple containing the API key, secret key, and access token</returns>
        public (string ApiKey, string SecretKey, string AccessToken) GetCredentialsForBroker(string brokerName)
        {
            try
            {
                if (_config == null)
                {
                    // If config is not loaded yet, try to load it
                    if (!LoadConfiguration())
                    {
                        return (_apiKey, _secretKey, _accessToken);
                    }
                }

                JObject brokerConfig = _config[brokerName] as JObject;
                if (brokerConfig == null)
                {
                    return (_apiKey, _secretKey, _accessToken);
                }

                string bApiKey = brokerConfig["Api"]?.ToString() ?? _apiKey;
                string bSecretKey = brokerConfig["Secret"]?.ToString() ?? _secretKey;
                string bAccessToken = brokerConfig["AccessToken"]?.ToString() ?? _accessToken;

                return (bApiKey, bSecretKey, bAccessToken);
            }
            catch
            {
                return (_apiKey, _secretKey, _accessToken);
            }
        }
    }
}
