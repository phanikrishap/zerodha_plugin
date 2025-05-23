using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text; 
using System.Threading;
using System.Threading.Tasks;
using QANinjaAdapter.Services.WebSocket; 
using NinjaTrader.Cbi; 
using NinjaTrader.NinjaScript; 

namespace QANinjaAdapter.Services
{
    public class BatchSubscriptionManager : IDisposable
    {
        private readonly WebSocketManager _webSocketManager;
        private ClientWebSocket _batchWebSocket;
        private readonly List<int> _pendingInstrumentTokens = new List<int>();
        private readonly object _lock = new object();
        private Timer _batchTimer;
        private readonly TimeSpan _batchInterval = TimeSpan.FromMilliseconds(500); 
        private const int MaxBatchSize = 100; 
        private bool _isDisposed = false;
        private CancellationTokenSource _cts;
        private readonly string _webSocketUrl;
        private readonly string _apiKey;
        private readonly string _accessToken;
        private Task _receiveMessagesTask;

        public BatchSubscriptionManager(WebSocketManager webSocketManager, string webSocketUrl, string apiKey, string accessToken)
        {
            _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
            _webSocketUrl = webSocketUrl ?? throw new ArgumentNullException(nameof(webSocketUrl));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _cts = new CancellationTokenSource();
        }

        public async Task InitializeAsync()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(BatchSubscriptionManager));

            if (_batchWebSocket == null || _batchWebSocket.State != WebSocketState.Open)
            {
                try
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: Attempting to connect WebSocket for batch subscriptions...", NinjaTrader.Cbi.LogLevel.Information); 
                    _batchWebSocket = await _webSocketManager.ConnectAsync(_webSocketUrl, _apiKey, _accessToken);
                    
                    if (_batchWebSocket != null && _batchWebSocket.State == WebSocketState.Open)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: WebSocket connected successfully for batch subscriptions.", NinjaTrader.Cbi.LogLevel.Information); 
                        _receiveMessagesTask = ReceiveMessagesLoopAsync(_batchWebSocket, _cts.Token); 
                    }
                    else
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: Failed to connect WebSocket for batch subscriptions. WebSocket is null or not open.", NinjaTrader.Cbi.LogLevel.Error);
                    }
                }
                catch (Exception ex)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Exception during WebSocket connection: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                     _batchWebSocket?.Dispose(); 
                    _batchWebSocket = null;
                    throw;
                }
            }
        }

        private async Task ReceiveMessagesLoopAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]); 
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) break;

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: WebSocket close message received.", NinjaTrader.Cbi.LogLevel.Information);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server initiated close", CancellationToken.None);
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager Received: {message}", NinjaTrader.Cbi.LogLevel.Information); 
                    // TODO: Process the message if needed (e.g., acknowledgements, errors related to batch subscriptions)
                }
            }
            catch (WebSocketException wsex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: WebSocketException in ReceiveMessagesLoopAsync: {wsex.Message} (State: {webSocket.State})", NinjaTrader.Cbi.LogLevel.Error);
            }
            catch (OperationCanceledException)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: ReceiveMessagesLoopAsync cancelled.", NinjaTrader.Cbi.LogLevel.Information); 
            }
            catch (Exception ex)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Exception in ReceiveMessagesLoopAsync: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
            }
            finally
            {
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing from receive loop finally block", CancellationToken.None);
                    }
                    catch(Exception ex)
                    {
                         NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Exception during final CloseAsync in ReceiveMessagesLoopAsync: {ex.Message}", NinjaTrader.Cbi.LogLevel.Warning);
                    }
                }
                NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: ReceiveMessagesLoopAsync ended.", NinjaTrader.Cbi.LogLevel.Information); 
            }
        }

        public void QueueInstrumentSubscription(int instrumentToken)
        {
            if (_isDisposed)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: Attempted to queue subscription on disposed object.", NinjaTrader.Cbi.LogLevel.Warning);
                return;
            }

            lock (_lock)
            {
                if (!_pendingInstrumentTokens.Contains(instrumentToken))
                {
                    _pendingInstrumentTokens.Add(instrumentToken);
                    NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Queued token {instrumentToken}. Pending: {_pendingInstrumentTokens.Count}", NinjaTrader.Cbi.LogLevel.Information); 
                }

                if (_pendingInstrumentTokens.Count >= MaxBatchSize)
                {
                    _batchTimer?.Change(Timeout.Infinite, Timeout.Infinite); 
                    _ = ProcessBatchAsync(); 
                }
                else if (_pendingInstrumentTokens.Count > 0)
                {
                    _batchTimer?.Dispose();
                    _batchTimer = new Timer(async _ => await ProcessBatchAsync(), null, _batchInterval, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private async Task ProcessBatchAsync()
        {
            if (_isDisposed) return;

            List<int> tokensToSubscribe;
            lock (_lock)
            {
                if (_pendingInstrumentTokens.Count == 0)
                    return;

                tokensToSubscribe = new List<int>(_pendingInstrumentTokens.Take(MaxBatchSize));
                _pendingInstrumentTokens.RemoveRange(0, Math.Min(tokensToSubscribe.Count, _pendingInstrumentTokens.Count));
                
                NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Processing batch of {tokensToSubscribe.Count} tokens. Remaining in queue: {_pendingInstrumentTokens.Count}", NinjaTrader.Cbi.LogLevel.Information); 

                if (_pendingInstrumentTokens.Count > 0)
                {
                    _batchTimer?.Dispose();
                    _batchTimer = new Timer(async _ => await ProcessBatchAsync(), null, _batchInterval, Timeout.InfiniteTimeSpan);
                }
                else
                {
                     _batchTimer?.Change(Timeout.Infinite, Timeout.Infinite); 
                }
            }

            if (tokensToSubscribe.Any())
            {
                if (_batchWebSocket == null || _batchWebSocket.State != WebSocketState.Open)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: WebSocket not connected. Re-queueing tokens.", NinjaTrader.Cbi.LogLevel.Error);
                    ReQueueTokens(tokensToSubscribe);
                    _ = InitializeAsync(); 
                    return;
                }

                try
                {
                    await _webSocketManager.BatchSubscribeAsync(_batchWebSocket, tokensToSubscribe, "full"); 
                    NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Batch subscription sent for {tokensToSubscribe.Count} tokens: {string.Join(",", tokensToSubscribe)}", NinjaTrader.Cbi.LogLevel.Information); 
                }
                catch (WebSocketException wsEx)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: WebSocketException during batch subscription: {wsEx.Message}. WebSocket State: {_batchWebSocket?.State}", NinjaTrader.Cbi.LogLevel.Error);
                    ReQueueTokens(tokensToSubscribe);
                    if (_batchWebSocket == null || _batchWebSocket.State == WebSocketState.Closed || _batchWebSocket.State == WebSocketState.Aborted)
                    {
                        NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: WebSocket closed or aborted. Attempting to reconnect.", NinjaTrader.Cbi.LogLevel.Warning);
                         _batchWebSocket?.Dispose(); 
                         _batchWebSocket = null;
                        _ = InitializeAsync(); 
                    }
                }
                catch (Exception ex)
                {
                    NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Error during batch subscription: {ex.Message}", NinjaTrader.Cbi.LogLevel.Error);
                    ReQueueTokens(tokensToSubscribe);
                }
            }
        }
        
        private void ReQueueTokens(List<int> tokens)
        {
            lock (_lock)
            {
                var distinctTokensToReQueue = tokens.Except(_pendingInstrumentTokens).ToList();
                _pendingInstrumentTokens.InsertRange(0, distinctTokensToReQueue);
                NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Re-queued {distinctTokensToReQueue.Count} tokens. Total pending: {_pendingInstrumentTokens.Count}", NinjaTrader.Cbi.LogLevel.Warning);
            }
        }

        public async Task CloseAsync()
        {
            if (_isDisposed) return;
            NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: CloseAsync called.", NinjaTrader.Cbi.LogLevel.Information); 
            Dispose(); 
            await Task.CompletedTask; 
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: Disposing...", NinjaTrader.Cbi.LogLevel.Information); 
                _cts?.Cancel();
                _batchTimer?.Dispose();

                if (_batchWebSocket != null)
                {
                    if (_batchWebSocket.State == WebSocketState.Open || _batchWebSocket.State == WebSocketState.Connecting)
                    {
                        try
                        {
                            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                            _batchWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", timeoutCts.Token).Wait(timeoutCts.Token); 
                             NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: Batch WebSocket closed during dispose.", NinjaTrader.Cbi.LogLevel.Information); 
                        }
                        catch (OperationCanceledException)
                        {
                            NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: WebSocket close timed out during dispose.", NinjaTrader.Cbi.LogLevel.Warning);
                        }
                        catch (Exception ex)
                        {
                             NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: Error closing WebSocket during dispose: {ex.Message}", NinjaTrader.Cbi.LogLevel.Warning);
                        }
                    }
                    _batchWebSocket.Dispose();
                    _batchWebSocket = null;
                }
                
                _receiveMessagesTask?.ContinueWith(t => {
                    if (t.IsFaulted) NinjaTrader.NinjaScript.NinjaScript.Log($"BatchSubscriptionManager: ReceiveMessagesTask faulted: {t.Exception?.GetBaseException().Message}", NinjaTrader.Cbi.LogLevel.Error);
                }, TaskScheduler.Default);

                _cts?.Dispose();
                _pendingInstrumentTokens.Clear();
            }
            _isDisposed = true;
             NinjaTrader.NinjaScript.NinjaScript.Log("BatchSubscriptionManager: Disposed.", NinjaTrader.Cbi.LogLevel.Information); 
        }
    }
}
