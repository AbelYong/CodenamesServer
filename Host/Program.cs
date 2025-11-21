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
        private static ServiceHost _lobbyHost;
        private static ServiceHost _matchmakingHost;
        private static ServiceHost _moderationHost;
        private static ServiceHost _matchHost;

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            _authenticationHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.AuthenticationService));
            _userHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.UserService));
            _friendHost = new ServiceHost(typeof(Services.FriendService));
            _emailHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.EmailService));
            _sessionHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.SessionService));
            _lobbyHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.LobbyService));
            _matchmakingHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.MatchmakingService));
            _moderationHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.ModerationService));
            _matchHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.MatchService));

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
            _lobbyHost.Open();
            _matchmakingHost.Open();
            _moderationHost.Open();
            _matchHost.Open();
        }

        private static void CloseServices()
        {
            _authenticationHost.Close();
            _sessionHost.Close();
            _userHost.Close();
            _friendHost.Close();
            _emailHost.Close();
            _lobbyHost.Close();
            _matchmakingHost.Close();
            _moderationHost.Close();
            _matchHost.Close();
        }

        private static void AbortServices()
        {
            _authenticationHost.Abort();
            _sessionHost.Abort();
            _userHost.Abort();
            _friendHost.Abort();
            _emailHost.Abort();
            _lobbyHost.Abort();
            _matchmakingHost.Abort();
            _moderationHost.Abort();
            _matchHost.Abort();
        }
    }
}
