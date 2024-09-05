using System.Runtime.Serialization;

namespace AutoCheckin.Exceptions
{
    public class RequestMalformedException: Exception
    {
        public RequestMalformedException(Exception? innerEx = null) : base("Request failed due to an unknown error", innerEx)
        {
        }
    }
}