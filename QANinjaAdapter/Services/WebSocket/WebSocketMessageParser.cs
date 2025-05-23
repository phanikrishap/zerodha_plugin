using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Models.MarketData;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Parses WebSocket messages from Zerodha's WebSocket API
    /// </summary>
    public class WebSocketMessageParser
    {
        private static WebSocketMessageParser _instance;

        /// <summary>
        /// Gets the singleton instance of the WebSocketMessageParser
        /// </summary>
        public static WebSocketMessageParser Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WebSocketMessageParser();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private WebSocketMessageParser()
        {
        }

        /// <summary>
        /// Parses a binary message from Zerodha WebSocket
        /// </summary>
        /// <param name="data">The binary data</param>
        /// <param name="expectedToken">The expected instrument token</param>
        /// <param name="nativeSymbolName">The native symbol name</param>
        /// <param name="isMcxSegment">True if the instrument belongs to MCX segment, false otherwise</param>
        /// <returns>A MarketDataEventArgs object containing all market data fields</returns>
        public MarketDataEventArgs ParseBinaryMessage(byte[] data, long expectedToken, string nativeSymbolName, bool isMcxSegment = false)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                using (var reader = new BinaryReader(ms))
                {
                    // Check if the data is valid
                    if (data == null || data.Length < 2)
                    {
                        AppLogger.Log(LoggingLevel.Error, $"Invalid binary message data for {nativeSymbolName}");
                        return null;
                    }

                    // Parse the message
                    int packetCount = ReadInt16BE(data, 0);
                    int offset = 2;

                    AppLogger.Log(LoggingLevel.Debug, $"Packet count: {packetCount} for {nativeSymbolName}");

                    for (int i = 0; i < packetCount; i++)
                    {
                        // Check if we have enough data for packet length
                        if (offset + 2 > data.Length)
                            break;

                        int packetLength = ReadInt16BE(data, offset);
                        offset += 2;

                        // Check if we have enough data for the packet content
                        if (offset + packetLength > data.Length)
                            break;

                        // Check if this packet is for the expected instrument token
                        int instrumentToken = ReadInt32BE(data, offset);
                        
                        if (instrumentToken != expectedToken)
                        {
                            offset += packetLength;
                            continue;
                        }

                        // Parse the packet based on its size
                        int packetSize = packetLength;
                        offset += 4; // Skip instrument token

                        // Last traded price (4 bytes)
                        float lastPrice = SwapEndianness(BitConverter.ToSingle(data, offset));
                        offset += 4;

                        var tickData = new MarketDataEventArgs
                        {
                            InstrumentToken = instrumentToken,
                            LastPrice = lastPrice,
                            NativeSymbolName = nativeSymbolName
                        };
                        
                        // LTP packet is 8 bytes
                        if (packetSize == 8)
                        {
                            return tickData;
                        }
                        
                        // Quote packet is at least 28 bytes
                        if (packetSize >= 28)
                        {
                            // Last traded quantity (4 bytes)
                            tickData.LastQuantity = BitConverter.ToUInt32(data, offset);
                            offset += 4;
                            
                            // Average traded price (4 bytes)
                            tickData.AveragePrice = SwapEndianness(BitConverter.ToSingle(data, offset));
                            offset += 4;
                            
                            // Volume traded today (4 bytes)
                            tickData.Volume = BitConverter.ToUInt32(data, offset);
                            offset += 4;
                            
                            // Buy quantity (4 bytes) - Total buy quantity in the order book
                            tickData.BuyQuantity = BitConverter.ToUInt32(data, offset);
                            offset += 4;
                            
                            // Sell quantity (4 bytes) - Total sell quantity in the order book
                            tickData.SellQuantity = BitConverter.ToUInt32(data, offset);
                            offset += 4;
                            
                            // Open price (4 bytes)
                            tickData.OpenPrice = SwapEndianness(BitConverter.ToSingle(data, offset));
                            offset += 4;
                            
                            // High price (4 bytes)
                            tickData.HighPrice = SwapEndianness(BitConverter.ToSingle(data, offset));
                            offset += 4;
                            
                            // Low price (4 bytes)
                            tickData.LowPrice = SwapEndianness(BitConverter.ToSingle(data, offset));
                            offset += 4;
                            
                            // Close price (4 bytes) - Previous day's closing price
                            tickData.ClosePrice = SwapEndianness(BitConverter.ToSingle(data, offset));
                            offset += 4;
                        }
                        
                        // Full packet has market depth and other details (at least 44 bytes)
                        if (packetSize >= 44)
                        {
                            // Last trade time (4 bytes)
                            uint lastTradeTimeSeconds = BitConverter.ToUInt32(data, offset);
                            tickData.LastTradeTime = DateTimeOffset.FromUnixTimeSeconds(lastTradeTimeSeconds).DateTime;
                            offset += 4;
                            
                            // Open Interest (4 bytes) - Only for derivatives
                            tickData.OpenInterest = BitConverter.ToUInt32(data, offset);
                            offset += 4;
                            
                            // Open Interest Day High (4 bytes)
                            tickData.OpenInterestDayHigh = BitConverter.ToUInt32(data, offset);
                            offset += 4;
                            
                            // Open Interest Day Low (4 bytes)
                            tickData.OpenInterestDayLow = BitConverter.ToUInt32(data, offset);
                            offset += 4;
                            
                            // Market depth - 5 levels each for bid and ask
                            if (packetSize >= 184)
                            {
                                tickData.Bids = new List<DepthItem>();
                                tickData.Asks = new List<DepthItem>();
                                
                                // Read 5 bid levels
                                for (int j = 0; j < 5; j++)
                                {
                                    var depthItem = new DepthItem
                                    {
                                        Price = SwapEndianness(BitConverter.ToSingle(data, offset)),
                                        Quantity = BitConverter.ToUInt32(data, offset + 4),
                                        Orders = BitConverter.ToUInt16(data, offset + 8)
                                    };
                                    offset += 10;
                                    
                                    tickData.Bids.Add(depthItem);
                                }
                                
                                // Read 5 ask levels
                                for (int j = 0; j < 5; j++)
                                {
                                    var depthItem = new DepthItem
                                    {
                                        Price = SwapEndianness(BitConverter.ToSingle(data, offset)),
                                        Quantity = BitConverter.ToUInt32(data, offset + 4),
                                        Orders = BitConverter.ToUInt16(data, offset + 8)
                                    };
                                    offset += 10;
                                    
                                    tickData.Asks.Add(depthItem);
                                }
                            }
                        }
                        
                        return tickData;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                AppLogger.Log(LoggingLevel.Error, $"Error parsing binary message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads a 16-bit integer in big-endian format
        /// </summary>
        private int ReadInt16BE(byte[] data, int offset)
        {
            return (data[offset] << 8) | data[offset + 1];
        }

        /// <summary>
        /// Reads a 32-bit integer in big-endian format
        /// </summary>
        private int ReadInt32BE(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }

        /// <summary>
        /// Swaps the endianness of a float value
        /// </summary>
        private float SwapEndianness(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
    }
}
