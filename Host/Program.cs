using log4net;
using System;
using System.ServiceModel;
using System.Threading;

namespace Host
{
    internal static class Program
    {
        private static readonly ManualResetEvent _quitEvent = new ManualResetEvent(false);
        private static ServiceHost _authenticationHost;
        private static ServiceHost _sessionHost;
        private static ServiceHost _userHost;
        private static ServiceHost _friendHost;
        private static ServiceHost _emailHost;
        private static ServiceHost _lobbyHost;
        private static ServiceHost _matchmakingHost;
        private static ServiceHost _moderationHost;
        private static ServiceHost _matchHost;
        private static ServiceHost _scoreboardHost;

        static void Main()
        {
            log4net.Config.XmlConfigurator.Configure();
            _authenticationHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.AuthenticationService));
            _userHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.UserService));
            _friendHost = new ServiceHost(typeof(Services.FriendService));
            _emailHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.EmailService));
            _sessionHost = new ServiceHost(Services.Contracts.ServiceContracts.Services.SessionService.Instance);
            _lobbyHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.LobbyService));
            _matchmakingHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.MatchmakingService));
            _moderationHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.ModerationService));
            _matchHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.MatchService));
            _scoreboardHost = new ServiceHost(typeof(Services.Contracts.ServiceContracts.Services.ScoreboardService));

            try
            {
                Console.CancelKeyPress += (sender, eArgs) => {
                    eArgs.Cancel = true;
                    _quitEvent.Set();
                };

                StartServices();
                
                Console.WriteLine("=== Codenames: Duet - Server ===");
                HostLogger.Log.Info("Codenames Server is online");
                Console.WriteLine("Running... press Ctrl+C to exit.");
                
                _quitEvent.WaitOne();

                CloseServices();
                HostLogger.Log.Info("Codenames Server shutting down, services closed");
            }
            catch (Exception ex) when (ex is TimeoutException || ex is CommunicationException)
            {
                AbortServices();
                HostLogger.Log.Debug("An exception occured: ", ex);
                HostLogger.Log.Info("Codenames Server shutting down, services aborted");
            }
            catch (Exception ex)
            {
                AbortServices();
                HostLogger.Log.Error("An unexpected exception occured: ", ex);
                HostLogger.Log.Info("Codenames Server shutting down, services aborted");
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
            _scoreboardHost.Open();
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
            _scoreboardHost.Close();
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
            _scoreboardHost.Abort();
        }

        private static class HostLogger
        {
            public static readonly ILog Log = LogManager.GetLogger(typeof(HostLogger));
        }
    }
}
