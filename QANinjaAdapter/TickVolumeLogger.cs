using System;
using System.IO;
using System.Text;
using System.Threading;

namespace QANinjaAdapter
{
    /// <summary>
    /// Logger class specifically for recording tick volume data in CSV format
    /// </summary>
    public static class TickVolumeLogger
    {
        private static readonly string _logFolderPath;
        private static readonly string _logFilePath;
        private static bool _initialized = false;
        private static readonly object _lockObject = new object();
        private static StreamWriter _writer;

        static TickVolumeLogger()
        {
            try
            {
                // Get the user's Documents folder path
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                _logFolderPath = Path.Combine(documentsPath, "NinjaTrader 8", "QAAdapter", "Logs");

                // Create the log directory if it doesn't exist
                if (!Directory.Exists(_logFolderPath))
                {
                    Directory.CreateDirectory(_logFolderPath);
                }

                // Create log file name with date
                string fileName = $"TickVolume_{DateTime.Now:yyyy-MM-dd}.csv";
                _logFilePath = Path.Combine(_logFolderPath, fileName);

                Initialize();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to initialize tick volume logger: {ex.Message}",
                    "Logging Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (!_initialized)
                {
                    try
                    {
                        bool fileExists = File.Exists(_logFilePath);
                        
                        // Open the file for appending
                        _writer = new StreamWriter(_logFilePath, true, Encoding.UTF8);
                        
                        // Write header if file is new
                        if (!fileExists)
                        {
                            _writer.WriteLine("Timestamp,Symbol,ReceivedTime,ExchangeTime,ParsedTime,LTP,LTQ,Volume,VolumeDelta,LatencyMs");
                            _writer.Flush();
                        }
                        
                        _initialized = true;
                        
                        // Log initialization
                        QANinjaAdapter.Logger.Info($"Tick volume logger initialized. Data will be saved to: {_logFilePath}");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to initialize tick volume logger: {ex.Message}",
                            "Logging Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Log tick volume data to CSV file
        /// </summary>
        /// <param name="symbol">Symbol name</param>
        /// <param name="receivedTime">Time when the websocket message was received</param>
        /// <param name="exchangeTime">Exchange timestamp from the message</param>
        /// <param name="parsedTime">Time after parsing the message</param>
        /// <param name="ltp">Last traded price</param>
        /// <param name="ltq">Last traded quantity</param>
        /// <param name="volume">Total volume</param>
        /// <param name="volumeDelta">Volume delta from previous tick</param>
        public static void LogTickVolume(
            string symbol,
            DateTime receivedTime,
            DateTime exchangeTime,
            DateTime parsedTime,
            double ltp,
            int ltq,
            int volume,
            int volumeDelta)
        {
            if (!_initialized)
            {
                Initialize();
            }

            try
            {
                lock (_lockObject)
                {
                    if (_writer != null)
                    {
                        // Calculate latency in milliseconds between exchange time and received time
                        double latencyMs = (receivedTime - exchangeTime).TotalMilliseconds;
                        
                        // Format: Timestamp,Symbol,ReceivedTime,ExchangeTime,ParsedTime,LTP,LTQ,Volume,VolumeDelta,LatencyMs
                        _writer.WriteLine(
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}," +
                            $"{symbol}," +
                            $"{receivedTime:HH:mm:ss.fff}," +
                            $"{exchangeTime:HH:mm:ss.fff}," +
                            $"{parsedTime:HH:mm:ss.fff}," +
                            $"{ltp:F2}," +
                            $"{ltq}," +
                            $"{volume}," +
                            $"{volumeDelta}," +
                            $"{latencyMs:F2}");
                        
                        // Flush to ensure data is written immediately
                        _writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error using the main logger
                QANinjaAdapter.Logger.Error($"Error logging tick volume: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Close the log file writer
        /// </summary>
        public static void Shutdown()
        {
            lock (_lockObject)
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }
    }
}
