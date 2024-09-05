using System.Runtime.Serialization;

namespace AutoCheckin.Exceptions
{
    public class SystemBusyException : Exception
    {
        public SystemBusyException(Exception? innerEx = null) : base("The system is currently busy or the request is malformed", innerEx)
        {
        }
    }
}