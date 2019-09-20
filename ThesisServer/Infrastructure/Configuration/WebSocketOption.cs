namespace ThesisServer.Infrastructure.Configuration
{
    public class WebSocketOption
    {
        public int KeepAliveIntervalInMillisecs { get; set; }
        public int ReceiveBufferSize { get; set; }
        public string RequestType { get; set; }
    }
}
