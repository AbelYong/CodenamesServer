using Services.Operations;
using System;
using System.ServiceModel;

namespace Services.Contracts.Callback
{
    public static class CallbackUtil
    {
        /// <summary>
        /// Safely invokes a WCF callback. If the call fails due to connectivity or timeout,
        /// it suppresses the exception and optionally runs a failure handler.
        /// </summary>
        /// <typeparam name="T">The Callback Interface type</typeparam>
        /// <param name="proxy">The callback channel instance</param>
        /// <param name="action">The method on the callback to call (e.g., c => c.Notify())</param>
        /// <param name="onFailure">Optional: A method to run if the callback fails (e.g., remove user)</param>
        public static void SafeInvoke<T>(this T proxy, Action<T> action, Action<T> onFailure = null)
            where T : class
        {
            if (proxy is ICommunicationObject commObj &&
                (commObj.State == CommunicationState.Closed || commObj.State == CommunicationState.Faulted))
            {
                onFailure?.Invoke(proxy); //Already closed/faultd, 
                return;
            }

            try
            {
                action(proxy);
            }
            catch (TimeoutException ex)
            {
                ServerLogger.Log.Warn("Callback method timed out: ", ex);
                onFailure?.Invoke(proxy);
            }
            catch (CommunicationException ex)
            {
                ServerLogger.Log.Warn("Callback channel connection lost: ", ex);
                onFailure?.Invoke(proxy);
            }
            catch (ObjectDisposedException)
            {
                onFailure?.Invoke(proxy);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while using the callback method: ", ex);
                onFailure?.Invoke(proxy);
            }
        }
    }
}
