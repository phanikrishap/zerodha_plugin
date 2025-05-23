using System;
using System.Collections.Generic;
using QANinjaAdapter.Logging;
using QANinjaAdapter.Models.MarketData;

namespace QANinjaAdapter.Services.MarketData.Processing.Parsers
{
    /// <summary>
    /// Parser for Zerodha WebSocket binary messages
    /// </summary>
    public class ZerodhaBinaryParser
    {
        /// <summary>
        /// Parse a binary message from Zerodha WebSocket
        /// </summary>
        /// <param name="data">Raw binary data from WebSocket</param>
        /// <returns>List of parsed tick data objects</returns>
        public List<ParsedTickData> ParseBinaryMessage(byte[] data)
        {
            var result = new List<ParsedTickData>();
            
            try
            {
                // Check for a valid message (at least 2 bytes)
                if (data == null || data.Length < 2)
                    return result;

                // Parse the number of packets in this message (first 2 bytes)
                int offset = 0;
                int packetCount = BitConverter.ToInt16(new byte[] { data[1], data[0] }, 0); // Convert from big-endian
                offset += 2;
                
                AppLogger.Log($"Message contains {packetCount} packets", QANinjaAdapter.Logging.LogLevel.Information);
                
                // Log the raw binary data for debugging
                string hexData = BitConverter.ToString(data, 0, Math.Min(data.Length, 64)).Replace("-", "");
                AppLogger.Log($"Parsing binary message: length={data.Length}, packetCount={packetCount}, data={hexData}...", QANinjaAdapter.Logging.LogLevel.Information);
                
                // Process each packet in the message
                for (int i = 0; i < packetCount && offset < data.Length; i++)
                {
                    // According to Zerodha's documentation, the next 2 bytes represent the length of the packet
                    int packetLength = BitConverter.ToInt16(new byte[] { data[offset + 1], data[offset] }, 0); // Convert from big-endian
                    offset += 2;
                    
                    // The first byte of the packet is the packet type
                    byte packetType = data[offset];
                    
                    AppLogger.Log($"Packet {i+1}/{packetCount}: Type={packetType}, Length={packetLength}, Offset={offset}", QANinjaAdapter.Logging.LogLevel.Debug);
                    
                    // Process the packet based on its type
                    switch (packetType)
                    {
                        case 0: // LTP mode packet
                            if (packetLength >= 8) // LTP mode has at least 8 bytes
                            {
                                // Extract instrument token (bytes 0-3)
                                uint instrumentToken = ParseInstrumentToken(data, offset);
                                
                                // Extract LTP (bytes 4-7)
                                int ltpInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 7], data[offset + 6], data[offset + 5], data[offset + 4] }, 0);
                                float ltp = ltpInt / 100.0f; // Convert from paise to rupees
                                
                                AppLogger.Log($"LTP packet: Token={instrumentToken}, LTP={ltp}", QANinjaAdapter.Logging.LogLevel.Debug);
                                
                                result.Add(new ParsedTickData
                                {
                                    InstrumentToken = instrumentToken,
                                    LastPrice = ltp,
                                    Mode = "ltp"
                                });
                            }
                            break;
                            
                        case 1: // Quote/full mode
                            if (packetLength >= 44) // Quote mode has at least 44 bytes
                            {
                                // Extract instrument token (bytes 0-3)
                                uint instrumentToken = ParseInstrumentToken(data, offset);
                                
                                // Extract LTP (bytes 4-7)
                                int ltpInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 7], data[offset + 6], data[offset + 5], data[offset + 4] }, 0);
                                float ltp = ltpInt / 100.0f; // Convert from paise to rupees
                                
                                var tick = new ParsedTickData
                                {
                                    InstrumentToken = instrumentToken,
                                    LastPrice = ltp,
                                    Mode = "quote"
                                };
                                
                                // Last traded quantity (bytes 8-11)
                                tick.LastQuantity = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 11], data[offset + 10], data[offset + 9], data[offset + 8] }, 0);
                                    
                                // Average traded price (bytes 12-15)
                                int avgPriceInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 15], data[offset + 14], data[offset + 13], data[offset + 12] }, 0);
                                tick.AverageTradePrice = avgPriceInt / 100.0f; // Convert from paise to rupees
                                    
                                // Volume traded for the day (bytes 16-19)
                                tick.Volume = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 19], data[offset + 18], data[offset + 17], data[offset + 16] }, 0);
                                    
                                // Total buy quantity (bytes 20-23)
                                tick.BuyQuantity = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 23], data[offset + 22], data[offset + 21], data[offset + 20] }, 0);
                                    
                                // Total sell quantity (bytes 24-27)
                                tick.SellQuantity = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 27], data[offset + 26], data[offset + 25], data[offset + 24] }, 0);
                                
                                // Open price (bytes 28-31)
                                int openInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 31], data[offset + 30], data[offset + 29], data[offset + 28] }, 0);
                                tick.Open = openInt / 100.0f; // Convert from paise to rupees
                                    
                                // High price (bytes 32-35)
                                int highInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 35], data[offset + 34], data[offset + 33], data[offset + 32] }, 0);
                                tick.High = highInt / 100.0f; // Convert from paise to rupees
                                    
                                // Low price (bytes 36-39)
                                int lowInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 39], data[offset + 38], data[offset + 37], data[offset + 36] }, 0);
                                tick.Low = lowInt / 100.0f; // Convert from paise to rupees
                                    
                                // Close price (bytes 40-43)
                                int closeInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 43], data[offset + 42], data[offset + 41], data[offset + 40] }, 0);
                                tick.Close = closeInt / 100.0f; // Convert from paise to rupees
                                
                                // If we have exchange timestamp (bytes 60-63)
                                if (packetLength >= 64)
                                {
                                    int exchangeTimestamp = BitConverter.ToInt32(new byte[] { 
                                        data[offset + 63], data[offset + 62], data[offset + 61], data[offset + 60] }, 0);
                                        
                                    if (exchangeTimestamp > 0)
                                    {
                                        // Convert Unix timestamp to DateTime
                                        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                        tick.Timestamp = epoch.AddSeconds(exchangeTimestamp).ToLocalTime();
                                    }
                                }
                                
                                AppLogger.Log($"Quote packet: Token={instrumentToken}, LTP={tick.LastPrice}, Vol={tick.Volume}", QANinjaAdapter.Logging.LogLevel.Debug);
                                result.Add(tick);
                            }
                            break;
                            
                        case 6: // Index packet (for indices like NIFTY 50 and SENSEX)
                            if (packetLength >= 28) // Index packet has at least 28 bytes
                            {
                                // Extract instrument token (bytes 0-3)
                                uint instrumentToken = ParseInstrumentToken(data, offset);
                                
                                // Extract LTP (bytes 4-7)
                                int ltpInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 7], data[offset + 6], data[offset + 5], data[offset + 4] }, 0);
                                float ltp = ltpInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract High (bytes 8-11)
                                int highInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 11], data[offset + 10], data[offset + 9], data[offset + 8] }, 0);
                                float high = highInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract Low (bytes 12-15)
                                int lowInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 15], data[offset + 14], data[offset + 13], data[offset + 12] }, 0);
                                float low = lowInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract Open (bytes 16-19)
                                int openInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 19], data[offset + 18], data[offset + 17], data[offset + 16] }, 0);
                                float open = openInt / 100.0f; // Convert from paise to rupees
                                    
                                // Extract Close (bytes 20-23)
                                int closeInt = BitConverter.ToInt32(new byte[] { 
                                    data[offset + 23], data[offset + 22], data[offset + 21], data[offset + 20] }, 0);
                                float close = closeInt / 100.0f; // Convert from paise to rupees
                                
                                // Create tick data object
                                var tick = new ParsedTickData
                                {
                                    InstrumentToken = instrumentToken,
                                    LastPrice = ltp,
                                    High = high,
                                    Low = low,
                                    Open = open,
                                    Close = close,
                                    Mode = "index",
                                    IsIndex = true
                                };
                                
                                // If we have exchange timestamp (bytes 28-31)
                                if (packetLength >= 32)
                                {
                                    int exchangeTimestamp = BitConverter.ToInt32(new byte[] { 
                                        data[offset + 31], data[offset + 30], data[offset + 29], data[offset + 28] }, 0);
                                        
                                    if (exchangeTimestamp > 0)
                                    {
                                        // Convert Unix timestamp to DateTime
                                        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                        tick.Timestamp = epoch.AddSeconds(exchangeTimestamp).ToLocalTime();
                                    }
                                }
                                
                                AppLogger.Log($"Index packet: Token={instrumentToken}, LTP={tick.LastPrice}, High={tick.High}, Low={tick.Low}", QANinjaAdapter.Logging.LogLevel.Debug);
                                result.Add(tick);
                            }
                            break;
                            
                        case 123: // Heartbeat/ping packet
                            AppLogger.Log($"Received heartbeat packet (type 123)", QANinjaAdapter.Logging.LogLevel.Debug);
                            // Heartbeat packets don't contain market data, they're just to keep the connection alive
                            break;
                            
                        // Add handling for other packet types as needed
                            
                        default:
                            AppLogger.Log($"Unknown packet type: {packetType}", QANinjaAdapter.Logging.LogLevel.Warning);
                            break;
                    }
                    
                    // Move to the next packet
                    // We've already advanced by 2 bytes for the packet length, now advance by the packet length
                    offset += packetLength;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error parsing binary message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
            }
            
            return result;
        }
        
        /// <summary>
        /// Helper method to parse an instrument token from big-endian bytes
        /// </summary>
        private uint ParseInstrumentToken(byte[] data, int offset)
        {
            // According to Zerodha's documentation, the instrument token is the first 4 bytes of the packet
            // The packet starts at offset, so the token is at offset to offset+3
            byte b0 = data[offset];
            byte b1 = data[offset + 1];
            byte b2 = data[offset + 2];
            byte b3 = data[offset + 3];
            
            // Convert from big-endian format (most significant byte first)
            uint tokenUint = ((uint)b0 << 24) | ((uint)b1 << 16) | ((uint)b2 << 8) | b3;
            
            // Log both the raw bytes and the parsed token
            AppLogger.Log($"Token bytes: {b0:X2} {b1:X2} {b2:X2} {b3:X2} = {tokenUint}", QANinjaAdapter.Logging.LogLevel.Information);
            
            // Also log the reversed token for debugging
            uint tokenReversed = ((uint)b3 << 24) | ((uint)b2 << 16) | ((uint)b1 << 8) | b0;
            AppLogger.Log($"Reversed token: {b3:X2} {b2:X2} {b1:X2} {b0:X2} = {tokenReversed}", QANinjaAdapter.Logging.LogLevel.Information);
            
            return tokenUint;
        }
    }
}
