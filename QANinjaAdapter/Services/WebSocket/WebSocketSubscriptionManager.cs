using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Logging;

namespace QANinjaAdapter.Services.WebSocket
{
    /// <summary>
    /// Manages WebSocket subscriptions and message sending
    /// </summary>
    public class WebSocketSubscriptionManager
    {
        private static WebSocketSubscriptionManager _instance;
        
        // Message queue and synchronization for WebSocket send operations
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentQueue<SendOperation> _sendQueue = new ConcurrentQueue<SendOperation>();
        private bool _processingQueue = false;
        
        // Class to represent a send operation in the queue
        private class SendOperation
        {
            public ClientWebSocket WebSocket { get; set; }
            public string Message { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }

        /// <summary>
        /// Gets the singleton instance of the WebSocketSubscriptionManager
        /// </summary>
        public static WebSocketSubscriptionManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WebSocketSubscriptionManager();
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private WebSocketSubscriptionManager()
        {
        }

        /// <summary>
        /// Subscribes to a symbol in the specified mode
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentToken">The instrument token</param>
        /// <param name="mode">The subscription mode (ltp, quote, full)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SubscribeAsync(ClientWebSocket ws, int instrumentToken, string mode)
        {
            // First subscribe to the instrument
            string subscribeMsg = $"{{\"a\":\"subscribe\",\"v\":[{instrumentToken}]}}";
            await SendTextMessageAsync(ws, subscribeMsg);

            // Then set the mode
            string modeMsg = $"{{\"a\":\"mode\",\"v\":[\"{mode}\",[{instrumentToken}]]}}";
            await SendTextMessageAsync(ws, modeMsg);

            AppLogger.Log($"Subscribed to token {instrumentToken} in {mode} mode", QANinjaAdapter.Logging.LogLevel.Information);
        }
        
        /// <summary>
        /// Batch subscribes to multiple instruments in the specified mode
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentTokens">List of instrument tokens to subscribe to</param>
        /// <param name="mode">The subscription mode (ltp, quote, full)</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task BatchSubscribeAsync(ClientWebSocket ws, List<int> instrumentTokens, string mode)
        {
            if (instrumentTokens == null || !instrumentTokens.Any())
            {
                AppLogger.Log("Cannot subscribe with empty instrument token list", QANinjaAdapter.Logging.LogLevel.Warning);
                return;
            }

            try
            {
                // Subscribe to all instruments at once
                string subscribeMsg = $"{{\"a\":\"subscribe\",\"v\":[{string.Join(",", instrumentTokens)}]}}";
                AppLogger.Log($"Sending subscription message for {instrumentTokens.Count} instruments", QANinjaAdapter.Logging.LogLevel.Information);
                await SendTextMessageAsync(ws, subscribeMsg);

                // Set mode for all instruments
                string modeMsg = $"{{\"a\":\"mode\",\"v\":[\"{mode}\",[{string.Join(",", instrumentTokens)}]]}}";
                AppLogger.Log($"Setting {mode} mode for {instrumentTokens.Count} instruments", QANinjaAdapter.Logging.LogLevel.Information);
                await SendTextMessageAsync(ws, modeMsg);
                
                // Add a small delay to ensure message is processed before continuing
                await Task.Delay(100);
                
                AppLogger.Log($"Successfully subscribed to {instrumentTokens.Count} instruments in {mode} mode", QANinjaAdapter.Logging.LogLevel.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error during batch subscription: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Unsubscribes from a symbol
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="instrumentToken">The instrument token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task UnsubscribeAsync(ClientWebSocket ws, int instrumentToken)
        {
            string unsubscribeMsg = $"{{\"a\":\"unsubscribe\",\"v\":[{instrumentToken}]}}";
            await SendTextMessageAsync(ws, unsubscribeMsg);
            AppLogger.Log($"Unsubscribed from token {instrumentToken}", QANinjaAdapter.Logging.LogLevel.Information);
        }

        /// <summary>
        /// Sends a text message over the WebSocket using a queue to ensure only one send operation is active at a time
        /// </summary>
        /// <param name="ws">The WebSocket client</param>
        /// <param name="message">The message to send</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task SendTextMessageAsync(ClientWebSocket ws, string message)
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not in an open state");
            }

            // Create a new operation and add it to the queue
            var operation = new SendOperation
            {
                WebSocket = ws,
                Message = message,
                CompletionSource = new TaskCompletionSource<bool>()
            };

            AppLogger.Log($"Queueing WebSocket message: {message}", QANinjaAdapter.Logging.LogLevel.Debug);
            _sendQueue.Enqueue(operation);
            
            // Start processing the queue if not already processing
            await _sendSemaphore.WaitAsync();
            try
            {
                if (!_processingQueue)
                {
                    _processingQueue = true;
                    // Fire and forget, but handle exceptions
                    _ = ProcessSendQueueAsync().ContinueWith(t => 
                    {
                        if (t.IsFaulted)
                        {
                            AppLogger.Log($"Error processing send queue: {t.Exception.InnerException?.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                        }
                    });
                }
            }
            finally
            {
                _sendSemaphore.Release();
            }

            // Wait for this particular send operation to complete
            await operation.CompletionSource.Task;
        }
        
        /// <summary>
        /// Processes the queue of WebSocket send operations
        /// </summary>
        private async Task ProcessSendQueueAsync()
        {
            try
            {
                while (_sendQueue.TryDequeue(out var operation))
                {
                    try
                    {
                        AppLogger.Log($"Processing queued message: {operation.Message}", QANinjaAdapter.Logging.LogLevel.Debug);
                        
                        // Only send if the WebSocket is still open
                        if (operation.WebSocket.State == WebSocketState.Open)
                        {
                            byte[] messageBytes = Encoding.UTF8.GetBytes(operation.Message);
                            await operation.WebSocket.SendAsync(
                                new ArraySegment<byte>(messageBytes),
                                WebSocketMessageType.Text, true, CancellationToken.None);
                            
                            AppLogger.Log($"Message sent successfully", QANinjaAdapter.Logging.LogLevel.Debug);
                            
                            // Mark this operation as completed successfully
                            operation.CompletionSource.SetResult(true);
                        }
                        else
                        {
                            // WebSocket is closed, fail the operation
                            AppLogger.Log($"WebSocket is not open (State: {operation.WebSocket.State})", QANinjaAdapter.Logging.LogLevel.Warning);
                            operation.CompletionSource.SetException(new InvalidOperationException($"WebSocket is not open (State: {operation.WebSocket.State})"));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Log($"Error sending message: {ex.Message}", QANinjaAdapter.Logging.LogLevel.Error);
                        operation.CompletionSource.SetException(ex);
                    }
                    
                    // Add a small delay between sends to avoid overwhelming the connection
                    await Task.Delay(5);
                }
            }
            finally
            {
                // Mark queue as no longer processing
                await _sendSemaphore.WaitAsync();
                _processingQueue = false;
                _sendSemaphore.Release();
            }
        }
    }
}
