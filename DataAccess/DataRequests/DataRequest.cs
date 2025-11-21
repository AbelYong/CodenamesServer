
namespace DataAccess.DataRequests
{
    public abstract class DataRequest
    {
        public bool IsSuccess { get; set; }
        public ErrorType ErrorType { get; set; }
    }
}
