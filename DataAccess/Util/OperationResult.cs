using DataAccess.DataRequests;

namespace DataAccess.Util
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ErrorType ErrorType { get; set; }

        public OperationResult()
        {

        }
    }
}
