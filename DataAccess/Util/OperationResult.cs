using DataAccess.DataRequests;
using System.Security.Cryptography.X509Certificates;

namespace DataAccess.Util
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public ErrorType ErrorType { get; set; }

        public OperationResult()
        {

        }

        public override bool Equals(object obj)
        {
            if (obj is OperationResult other)
            {
                return
                    Success.Equals(other.Success) &&
                    ErrorType.Equals(other.ErrorType);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new { Success, ErrorType }.GetHashCode();
        }
    }
}
