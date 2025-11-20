using log4net;

namespace Services.Operations
{
    public static class ServerLogger
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(ServerLogger));
    }
}
