using System.Runtime.Serialization;

namespace AutoCheckin.Exceptions
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(Exception? innerEx = null) : base("Request failed due to not being logged in (tokens may have been invalidated)", innerEx)
        {
        }
    }
}