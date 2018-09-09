using System;
using System.Net;
using System.Runtime.Serialization;

namespace PexelsNet
{
    [Serializable]
    internal class PexelsNetException : Exception
    {
        private HttpStatusCode statusCode;
        private string body;

        public PexelsNetException()
        {
        }

        public PexelsNetException(string message) : base(message)
        {
        }

        public PexelsNetException(HttpStatusCode statusCode, string body)
        {
            this.statusCode = statusCode;
            this.body = body;
        }

        public PexelsNetException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PexelsNetException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}