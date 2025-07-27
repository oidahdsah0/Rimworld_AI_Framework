using System;

namespace RimAI.Framework.Shared.Exceptions
{
    public class LLMException : FrameworkException
    {
        public LLMException()
        {

        }

        public LLMException(string message) : base(message)
        {

        }

        public LLMException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}