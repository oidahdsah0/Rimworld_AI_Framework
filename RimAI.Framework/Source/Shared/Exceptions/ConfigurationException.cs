using System;

namespace RimAI.Framework.Shared.Exceptions
{
    public class ConfigurationException : FrameworkException
    {
        public ConfigurationException()
        {

        }

        public ConfigurationException(string message) : base(message)
        {

        }

        public ConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}