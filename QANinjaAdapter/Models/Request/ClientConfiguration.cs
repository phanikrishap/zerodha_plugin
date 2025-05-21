using System;
using log4net;

namespace QABrokerAPI.Common.Models.Request
{
    /// <summary>
    /// Represents client configuration for the broker API
    /// </summary>
    public class ClientConfiguration
    {
        /// <summary>
        /// Gets or sets the API key for the broker
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// Gets or sets the API secret for the broker
        /// </summary>
        public string ApiSecret { get; set; }
        
        /// <summary>
        /// Gets or sets the secret key for the broker (alias for ApiSecret for compatibility)
        /// </summary>
        public string SecretKey 
        { 
            get { return ApiSecret; }
            set { ApiSecret = value; }
        }
        
        /// <summary>
        /// Gets or sets the access token for the broker
        /// </summary>
        public string AccessToken { get; set; }
        
        /// <summary>
        /// Gets or sets the client ID for the broker
        /// </summary>
        public string ClientId { get; set; }
        
        /// <summary>
        /// Gets or sets the broker name
        /// </summary>
        public string BrokerName { get; set; }
        
        /// <summary>
        /// Gets or sets the logger for the broker
        /// </summary>
        public ILog Logger { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when this configuration was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
