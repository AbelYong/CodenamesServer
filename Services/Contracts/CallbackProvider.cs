
namespace Services.Contracts
{
    public class CallbackProvider : ICallbackProvider
    {
        public T GetCallback<T>()
        {
            return System.ServiceModel.OperationContext.Current.GetCallbackChannel<T>();
        }
    }
}
