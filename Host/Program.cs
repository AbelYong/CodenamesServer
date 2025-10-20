using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Host
{
    internal static class Program
    {
        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            ServiceHost authenticationHost = new ServiceHost(typeof(Services.AuthenticationService));
            ServiceHost userHost = new ServiceHost(typeof(Services.UserService));
            ServiceHost friendHost = new ServiceHost(typeof(Services.FriendService));

            try
            {
                authenticationHost.Open();
                userHost.Open();
                friendHost.Open();

                Console.CancelKeyPress += (sender, eArgs) => {
                    eArgs.Cancel = true;
                    _quitEvent.Set();
                };
                Console.WriteLine("=== Codenames: Duet - Server ===");
                Console.WriteLine("Running... press Ctrl+C to exit.");
                _quitEvent.WaitOne();

                authenticationHost.Close();
                userHost.Close();
                friendHost.Close();
            }
            catch (CommunicationException cex)
            {
                Console.WriteLine("An exception occured: {0}", cex.Message);
                authenticationHost.Abort();
                userHost.Abort();
                friendHost.Abort();
            }
        }
    }
}
