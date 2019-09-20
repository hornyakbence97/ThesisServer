using System;
using System.Net;
using System.Net.WebSockets;

namespace ThesisServer.Infrastructure.ExceptionHandle
{
    public abstract class HandledException : Exception
    {
        protected string _message;
        protected HttpStatusCode _returnStatusCode;
        protected bool _isWebSocketException;
        protected WebSocket _webSocket;

        protected HandledException(string message, HttpStatusCode returnStatusCode, bool isWebSocketException = false, WebSocket webSocket = null)
        {
            _message = message;
            _returnStatusCode = returnStatusCode;
            _isWebSocketException = isWebSocketException;
            _webSocket = webSocket;
        }

        protected HandledException() { }

        public string ErrorMessage()
        {
            return _message;
        }

        public HttpStatusCode StatusCode()
        {
            return _returnStatusCode;
        }

        public bool IsWebSocketException()
        {
            return _isWebSocketException;
        }

        public WebSocket WebSocket()
        {
            return _webSocket;
        }
    }
}
