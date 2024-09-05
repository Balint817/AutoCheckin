using System.Runtime.Serialization;

namespace AutoCheckin.Exceptions
{
    public class InvalidRegionException : Exception
    {
        public readonly string RegionKey;
        public InvalidRegionException(string regionKey, Exception? innerEx = null) : base("Request failed due to not having an account in the given region (or setting the wrong UID)", innerEx)
        {
        }
    }
}