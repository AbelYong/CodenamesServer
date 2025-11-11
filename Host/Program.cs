using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Host
{
    internal static class Program
    {
        private static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        private static ServiceHost _authenticationHost;
        private static ServiceHost _sessionHost;
        private static ServiceHost _userHost;
        private static ServiceHost _friendHost;
        private static ServiceHost _emailHost;

        static void Main(string[] args)
        {
            _authenticationHost = new ServiceHost(typeof(Services.AuthenticationService));
            _userHost = new ServiceHost(typeof(Services.UserService));
            _friendHost = new ServiceHost(typeof(Services.FriendService));
            _emailHost = new ServiceHost(typeof(Services.EmailService));
            _sessionHost = new ServiceHost(typeof(Services.Contracts.SessionService));

            try
            {
                StartServices();

                Console.CancelKeyPress += (sender, eArgs) => {
                    eArgs.Cancel = true;
                    _quitEvent.Set();
                };
                Console.WriteLine("=== Codenames: Duet - Server ===");
                Console.WriteLine("Running... press Ctrl+C to exit.");
                _quitEvent.WaitOne();

                CloseServices();
            }
            catch (CommunicationException cex)
            {
                Console.WriteLine("An exception occured: {0}", cex.Message);
                AbortServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occured: {0}", ex.Message);
                AbortServices();
            }
        }

        private static void StartServices()
        {
            _authenticationHost.Open();
            _sessionHost.Open();
            _userHost.Open();
            _friendHost.Open();
            _emailHost.Open();
        }

        private static void CloseServices()
        {
            _authenticationHost.Close();
            _sessionHost.Close();
            _userHost.Close();
            _friendHost.Close();
            _emailHost.Close();
        }

        private static void AbortServices()
        {
            _authenticationHost.Abort();
            _sessionHost.Abort();
            _userHost.Abort();
            _friendHost.Abort();
            _emailHost.Abort();
        }
    }
}
