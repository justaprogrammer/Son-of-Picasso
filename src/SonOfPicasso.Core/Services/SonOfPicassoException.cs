using System;

namespace SonOfPicasso.Core.Services
{
    public class SonOfPicassoException : Exception
    {
        public SonOfPicassoException()
        {
        }

        public SonOfPicassoException(string message) : base(message)
        {
        }

        public SonOfPicassoException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}