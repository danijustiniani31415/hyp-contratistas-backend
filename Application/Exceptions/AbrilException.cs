namespace Abril_Backend.Application.Exceptions
{
    public class AbrilException : Exception
    {
        public int StatusCode { get; }
        public AbrilException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}