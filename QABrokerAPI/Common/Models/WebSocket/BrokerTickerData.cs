using Newtonsoft.Json;
using QABrokerAPI.Common.Models.WebSocket.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QABrokerAPI.Common.Converter;

namespace QABrokerAPI.Common.Models.WebSocket
{
    // Zerodha ticker data model 
    public class BrokerTickerData : IWebSocketResponse
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("tradable")]
        public bool Tradable { get; set; }

        [JsonProperty("instrument_token")]
        public long InstrumentToken { get; set; }

        [JsonProperty("last_price")]
        public decimal LastPrice { get; set; }

        [JsonProperty("ohlc")]
        public OHLC Ohlc { get; set; }

        [JsonProperty("change")]
        public decimal Change { get; set; }

        [JsonProperty("last_quantity")]
        public int LastQuantity { get; set; }

        [JsonProperty("average_price")]
        public decimal AveragePrice { get; set; }

        [JsonProperty("volume")]
        public int Volume { get; set; }

        [JsonProperty("buy_quantity")]
        public int BuyQuantity { get; set; }

        [JsonProperty("sell_quantity")]
        public int SellQuantity { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        // Implementation of IWebSocketResponse interface with proper getters and setters
        private string _eventType = "ticker";
        [JsonIgnore]
        public string EventType
        {
            get { return _eventType; }
            set { _eventType = value; }
        }

        [JsonIgnore]
        public DateTime EventTime
        {
            get { return Timestamp; }
            set { Timestamp = value; }
        }
    }

    public class OHLC
    {
        [JsonProperty("open")]
        public decimal Open { get; set; }

        [JsonProperty("high")]
        public decimal High { get; set; }

        [JsonProperty("low")]
        public decimal Low { get; set; }

        [JsonProperty("close")]
        public decimal Close { get; set; }
    }
}