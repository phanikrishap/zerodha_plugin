﻿using System;
using log4net;

namespace QABrokerAPI.Binance
{
    public class ClientConfiguration
    {
        public string ApiKey { get; set; }

        public string SecretKey { get; set; }

        public bool EnableRateLimiting { get; set; }

        public TimeSpan CacheTime { get; set; } = TimeSpan.FromMinutes(30.0);

        public TimeSpan TimestampOffset { get; set; } = TimeSpan.FromMilliseconds(0.0);

        public ILog Logger { get; set; }

        public int DefaultReceiveWindow { get; set; } = 5000;
    }
}
