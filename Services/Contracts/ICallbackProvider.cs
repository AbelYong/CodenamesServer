
namespace Services.Contracts
{
    public interface ICallbackProvider
    {
        T GetCallback<T>();
    }
}
