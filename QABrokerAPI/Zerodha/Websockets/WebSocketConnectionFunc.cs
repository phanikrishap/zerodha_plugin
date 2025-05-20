using QABrokerAPI.Common.Utility;
using System;

#nullable disable
namespace QABrokerAPI.Zerodha.Websockets;

//public class WebSocketConnectionFunc
//{
//  public Func<bool> ExitFunction;

//  public int Timeout { get; }

//  public bool IsTimeout => this.Timeout > 0;

//  public WebSocketConnectionFunc(int timeout = 5000) => this.Timeout = timeout;

//  public WebSocketConnectionFunc(Func<bool> exitFunction) => this.ExitFunction = exitFunction;
//}


public class WebSocketConnectionFunc
{
    private readonly Func<bool> _exitFunction;

    public WebSocketConnectionFunc(Func<bool> exitFunction)
    {
        Guard.AgainstNull((object)exitFunction, nameof(exitFunction));
        this._exitFunction = exitFunction;
    }

    public bool ExitFunction()
    {
        return this._exitFunction();
    }

    // Add these missing properties
    public bool IsTimeout { get { return false; } } // Default to false
    public int Timeout { get { return 0; } } // Default to 0 (no timeout)
}