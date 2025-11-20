using log4net;

namespace DataAccess.Util
{
    public static class DataAccessLogger
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(DataAccessLogger));
    }
}
