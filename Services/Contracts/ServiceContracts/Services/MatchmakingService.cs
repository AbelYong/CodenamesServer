using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class MatchmakingService : IMatchmakingManager
    {
        public const int MAX_MATCH_STORAGE = 60;
        private readonly ICallbackProvider _callbackProvider;
        private readonly ConcurrentDictionary<Guid, IMatchmakingCallback> _connectedPlayers;
        private readonly ConcurrentDictionary<Guid, Match> _pendingMatches;
        private readonly ConcurrentDictionary<Guid, MatchConfirmation> _matchesAwaitingConfirmation;
        private readonly object _confirmationLock;
        private readonly TimeSpan _confirmationTimeout;

        public MatchmakingService() : this (new CallbackProvider())
        {
            _connectedPlayers = new ConcurrentDictionary<Guid, IMatchmakingCallback>();
            _pendingMatches = new ConcurrentDictionary<Guid, Match>();
            _matchesAwaitingConfirmation = new ConcurrentDictionary<Guid, MatchConfirmation>();
            _confirmationLock = new object();
            _confirmationTimeout = TimeSpan.FromSeconds(MAX_MATCH_STORAGE);
        }

        public MatchmakingService(ICallbackProvider callbackProvider)
        {
            _callbackProvider = callbackProvider;
            _connectedPlayers = new ConcurrentDictionary<Guid, IMatchmakingCallback>();
            _pendingMatches = new ConcurrentDictionary<Guid, Match>();
            _matchesAwaitingConfirmation = new ConcurrentDictionary<Guid, MatchConfirmation>();
            _confirmationLock = new object();
            _confirmationTimeout = TimeSpan.FromSeconds(MAX_MATCH_STORAGE);
        }

        public CommunicationRequest Connect(Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            IMatchmakingCallback currentClientChannel = _callbackProvider.GetCallback<IMatchmakingCallback>();
            bool hasConnected = _connectedPlayers.TryAdd(playerID, currentClientChannel);
            if (hasConnected)
            {
                request.IsSuccess = true;
                request.StatusCode = StatusCode.OK;

                string audit = string.Format("{0} has connected to MatchmakingService", ServerLogger.GetPlayerIdentifier(playerID));
                ServerLogger.Log.Info(audit);
            }
            else
            {
                Disconnect(playerID);
                hasConnected = _connectedPlayers.TryAdd(playerID, currentClientChannel);
                request.IsSuccess = hasConnected;
                request.StatusCode = hasConnected ? StatusCode.OK : StatusCode.UNAUTHORIZED;

                string audit = string.Format("Connection request to MatchmakingService by {0} procesed with code {1}", ServerLogger.GetPlayerIdentifier(playerID), request.StatusCode);
                ServerLogger.Log.Info(audit);
            }
            return request;
        }

        public void Disconnect(Guid playerID)
        {
            bool hasDisconnected = _connectedPlayers.TryRemove(playerID, out _);
            if (hasDisconnected)
            {
                CancelPendingMatches(playerID);

                string audit = string.Format("{0} has disconnected from MatchmakingService", ServerLogger.GetPlayerIdentifier(playerID));
                ServerLogger.Log.Info(audit);
            }
        }

        private void CancelPendingMatches(Guid playerID)
        {
            foreach (Match match in _pendingMatches.Values)
            {
                if (match.Requester.PlayerID == playerID)
                {
                    Guid companionID = (Guid)match.Companion.PlayerID;
                    CancelMatch(match.MatchID, companionID);
                }
                else if (match.Companion.PlayerID == playerID)
                {
                    Guid requesterID = (Guid)match.Requester.PlayerID;
                    CancelMatch(match.MatchID, requesterID);
                }
            }
        }

        private void CancelMatch(Guid matchID, Guid playerToNotifyID)
        {
            NotifyMatchCanceled(playerToNotifyID, matchID, StatusCode.CLIENT_CANCEL);

            _matchesAwaitingConfirmation.TryRemove(matchID, out MatchConfirmation confirmation);
            confirmation?.TimeoutTimer?.Dispose();

            _pendingMatches.TryRemove(matchID, out _);
        }

        public CommunicationRequest RequestArrangedMatch(MatchConfiguration configuration)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (configuration == null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }
            if (VerifyArrangedMatchData(configuration))
            {
                Guid requesterID = (Guid)configuration.Requester.PlayerID;
                Guid companionID = (Guid)configuration.Companion.PlayerID;

                if (CheckIsPlayerBusy(requesterID) || CheckIsPlayerBusy(companionID))
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.CONFLICT;
                    return request;
                }

                bool notifySucesss = NotifyRequestReceived(requesterID, companionID);
                if (!notifySucesss)
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.CLIENT_UNREACHABLE;
                    return request;
                }

                Match match = MatchmakingOperation.GenerateMatch(configuration);
                if (_pendingMatches.TryAdd(match.MatchID, match))
                {
                    return SendMatchToPlayers(match, requesterID, companionID);
                }
                else
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.SERVER_ERROR;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
            }
            return request;
        }

        private static bool VerifyArrangedMatchData(MatchConfiguration config)
        {
            bool validRequester = config.Requester != null && config.Requester.PlayerID.HasValue;
            bool validCompanion = config.Companion != null && config.Companion.PlayerID.HasValue;
            return (validRequester && validCompanion);
        }

        private bool CheckIsPlayerBusy(Guid playerID)
        {
            foreach (Match match in _pendingMatches.Values)
            {
                if (match.Requester.PlayerID == playerID || match.Companion.PlayerID == playerID)
                {
                    return true;
                }
            }
            return false;
        }

        private bool NotifyRequestReceived(Guid requesterID, Guid companionID)
        {
            bool requesterConnected = _connectedPlayers.TryGetValue(requesterID, out IMatchmakingCallback requesterChannel);
            bool companionConnected = _connectedPlayers.TryGetValue(companionID, out IMatchmakingCallback companionChannel);

            if (!requesterConnected || !companionConnected)
            {
                return false;
            }

            bool requesterSuccess = SendRequestPendingNotification(requesterChannel, requesterID, companionID);
            bool companionSuccess = SendRequestPendingNotification(companionChannel, requesterID, companionID);

            return requesterSuccess && companionSuccess;
        }

        private bool SendRequestPendingNotification(IMatchmakingCallback channel, Guid requesterID, Guid companionID)
        {
            try
            {
                channel.NotifyRequestPending(requesterID, companionID);
                return true;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                ServerLogger.Log.Warn("Failed to send match request pending notification: ", ex);
                RemoveFaultedChannel(companionID);
                return false;
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending match request pending notification: ", ex);
                RemoveFaultedChannel(companionID);
                return false;
            }
        }

        private CommunicationRequest SendMatchToPlayers(Match match, Guid requesterID, Guid companionID)
        {
            CommunicationRequest request = new CommunicationRequest();
            Timer timer = new Timer(OnMatchConfirmationTimeout, match.MatchID, Timeout.Infinite, Timeout.Infinite);
            MatchConfirmation confirmation = new MatchConfirmation(match.MatchID, timer);
            _matchesAwaitingConfirmation.TryAdd(match.MatchID, confirmation);

            bool sendToRequesterSuccess = SendMatch(requesterID, match.MatchID);
            bool sendToCompanionSuccess = SendMatch(companionID, match.MatchID);

            if (sendToRequesterSuccess && sendToCompanionSuccess)
            {
                timer.Change(_confirmationTimeout, Timeout.InfiniteTimeSpan);

                request.IsSuccess = true;
                request.StatusCode = StatusCode.CREATED;
            }
            else
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
                _matchesAwaitingConfirmation.TryRemove(match.MatchID, out _);
                _pendingMatches.TryRemove(match.MatchID, out _);

                if (sendToRequesterSuccess)
                {
                    NotifyMatchCanceled(requesterID, match.MatchID, StatusCode.CLIENT_UNREACHABLE);
                }
                if (sendToCompanionSuccess)
                {
                    NotifyMatchCanceled(companionID, match.MatchID, StatusCode.CLIENT_UNREACHABLE);
                }
                request.IsSuccess = false;
                request.StatusCode = StatusCode.CLIENT_UNREACHABLE;
            }
            return request;
        }

        private bool SendMatch(Guid sendToID, Guid matchID)
        {
            bool playerChannelFound = _connectedPlayers.TryGetValue(sendToID, out IMatchmakingCallback playerChannel);
            bool matchFound = _pendingMatches.TryGetValue(matchID, out Match match);
            
            if (!playerChannelFound || !matchFound)
            {
                return false;   
            }
            try
            {
                playerChannel?.NotifyMatchReady(match);
                return true;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {

                ServerLogger.Log.Warn("Failed to send Match to player: ", ex);
                RemoveFaultedChannel(sendToID);
                return false;
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending match to player: ", ex);
                RemoveFaultedChannel(sendToID);
                return false;
            }
        }

        private void OnMatchConfirmationTimeout(object state)
        {
            Guid matchID = (Guid)state;
            Match matchToCancel = null;

            lock (_confirmationLock)
            {
                if (_matchesAwaitingConfirmation.TryRemove(matchID, out MatchConfirmation confirmation))
                {
                    confirmation.TimeoutTimer?.Dispose();

                    _pendingMatches.TryRemove(matchID, out matchToCancel);
                }
            }

            if (matchToCancel != null)
            {
                Guid requesterId = (Guid)matchToCancel.Requester.PlayerID;
                Guid companionId = (Guid)matchToCancel.Companion.PlayerID;

                NotifyMatchCanceled(requesterId, matchID, StatusCode.CLIENT_TIMEOUT);
                NotifyMatchCanceled(companionId, matchID, StatusCode.CLIENT_TIMEOUT);
            }
        }

        private void NotifyMatchCanceled(Guid playerToNotifyID, Guid matchID, StatusCode reason)
        {
            if (_connectedPlayers.TryGetValue(playerToNotifyID, out IMatchmakingCallback channel))
            {
                try
                {
                    channel?.NotifyMatchCanceled(matchID, reason);
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    ServerLogger.Log.Warn("Failed to send match canceled notification:", ex);
                    RemoveFaultedChannel(playerToNotifyID);
                }
                catch (Exception ex)
                {
                    ServerLogger.Log.Error("Unexpected exception while sending match canceled notification: ", ex);
                    RemoveFaultedChannel(playerToNotifyID);
                }
            }
        }

        public void ConfirmMatchReceived(Guid playerID, Guid matchID)
        {
            bool shouldNotify = false;

            lock (_confirmationLock)
            {
                if (!_matchesAwaitingConfirmation.TryGetValue(matchID, out MatchConfirmation confirmation))
                {
                    return;
                }

                if (_pendingMatches.TryGetValue(matchID, out Match match))
                {
                    if (match.Requester.PlayerID == playerID)
                    {
                        confirmation.HasRequesterConfirmed = true;
                    }
                    else if (match.Companion.PlayerID == playerID)
                    {
                        confirmation.HasCompanionConfirmed = true;
                    }
                }

                if (confirmation.HasRequesterConfirmed && confirmation.HasCompanionConfirmed &&
                    _matchesAwaitingConfirmation.TryRemove(matchID, out MatchConfirmation removedConfirmation))
                {
                    removedConfirmation.TimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    removedConfirmation.TimeoutTimer?.Dispose();
                    shouldNotify = true;
                }
            }
            if (shouldNotify)
            {
                NotifyPlayersReady(matchID);
                _pendingMatches.TryRemove(matchID, out _);
            }
        }

        private void NotifyPlayersReady(Guid matchID)
        {
            if (_pendingMatches.TryGetValue(matchID, out Match match))
            {
                Guid requesterID = (Guid)match.Requester.PlayerID;
                Guid companionID = (Guid)match.Companion.PlayerID;
                bool requesterConnected = _connectedPlayers.TryGetValue(requesterID, out IMatchmakingCallback requesterChannel);
                bool companionConnected = _connectedPlayers.TryGetValue(companionID, out IMatchmakingCallback companionChannel);
                if (requesterConnected && companionConnected)
                {
                    SendPlayerReadyNotification(requesterID, requesterChannel, matchID);
                    SendPlayerReadyNotification(companionID, companionChannel, matchID);
                }
            }
        }

        private void SendPlayerReadyNotification(Guid toNotifyID, IMatchmakingCallback toNotifyChannel, Guid matchID)
        {
            try
            {
                toNotifyChannel.NotifyPlayersReady(matchID);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                ServerLogger.Log.Warn("Failed to send player's ready notification: ", ex);
                RemoveFaultedChannel(toNotifyID);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending player's ready notification: ", ex);
                RemoveFaultedChannel(toNotifyID);
            }
        }

        public void RequestMatchCancel(Guid playerID)
        {
            CancelPendingMatches(playerID);
        }

        private void RemoveFaultedChannel(Guid playerID)
        {
            _connectedPlayers.TryRemove(playerID, out IMatchmakingCallback faultedChannel);
            if (faultedChannel is ICommunicationObject communicationObject)
            {
                communicationObject.Abort();
            }
        }

        private sealed class MatchConfirmation
        {
            public Guid MatchID { get; set; }
            public Timer TimeoutTimer { get; set; }
            public bool HasRequesterConfirmed { get; set; }
            public bool HasCompanionConfirmed { get; set; }

            public MatchConfirmation(Guid matchID, Timer timer)
            {
                MatchID = matchID;
                TimeoutTimer = timer;
            }
        }
    }
}
