using System.Net;
using ThesisServer.Infrastructure.ExceptionHandle;

namespace ThesisServer.Infrastructure.Middleware.Helper.Exception
{
    public class RequestTypeNotProvidedException : HandledException
    {
        public RequestTypeNotProvidedException(
            string message = "The requestType not provided in the request!",
            HttpStatusCode returnStatusCode = HttpStatusCode.BadRequest) 
            : base(message, returnStatusCode)
        {
        }
    }
}
