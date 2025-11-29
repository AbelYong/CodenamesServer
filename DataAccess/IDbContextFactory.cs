
namespace DataAccess
{
    public interface IDbContextFactory
    {
        ICodenamesContext Create();
    }
}
