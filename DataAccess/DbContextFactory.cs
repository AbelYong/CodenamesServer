
namespace DataAccess
{
    public class DbContextFactory : IDbContextFactory
    {
        public ICodenamesContext Create()
        {
            return new codenamesEntities();
        }
    }
}
