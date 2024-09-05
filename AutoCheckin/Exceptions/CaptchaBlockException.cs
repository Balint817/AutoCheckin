using System.Runtime.Serialization;

namespace AutoCheckin.Exceptions
{
    public class CaptchaBlockException : Exception
    {
        public CaptchaBlockException(Exception? innerEx = null) : base("Request was blocked by a captcha check", innerEx)
        {
        }
    }
}