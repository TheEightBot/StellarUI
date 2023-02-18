using System;
using System.Runtime.Serialization;

namespace Stellar.Maui.Exceptions
{
    public class RegisteredServiceNotFoundException : Exception
    {
        public RegisteredServiceNotFoundException()
        {
        }

        public RegisteredServiceNotFoundException(string message)
            : base(message)
        {
        }

        public RegisteredServiceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RegisteredServiceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}