using System.Data.SqlClient;

namespace DataAccess.Tests.Util
{
    public static class SqlExceptionCreator
    {
        public static SqlException Create()
        {
            return (SqlException)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SqlException));
        }
    }
}
