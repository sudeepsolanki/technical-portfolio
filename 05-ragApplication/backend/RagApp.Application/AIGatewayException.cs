using System;

namespace RagApp.Application
{
    public class AIGatewayException : Exception
    {
        public AIGatewayException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}