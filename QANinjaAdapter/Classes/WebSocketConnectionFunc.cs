using System;

namespace QANinjaAdapter.Classes
{
    // Renamed to avoid ambiguity with QABrokerAPI.Zerodha.Websockets.WebSocketConnectionFunc
    public class ZerodhaWebSocketConnectionFunc
    {
        public Func<bool> ExitFunction { get; }
        public bool IsTimeout { get; }
        public TimeSpan Timeout { get; }

        public ZerodhaWebSocketConnectionFunc(Func<bool> exitFunction, bool isTimeout = false, TimeSpan? timeout = null)
        {
            ExitFunction = exitFunction ?? throw new ArgumentNullException(nameof(exitFunction));
            IsTimeout = isTimeout;
            Timeout = timeout ?? TimeSpan.Zero;
        }
    }
}
