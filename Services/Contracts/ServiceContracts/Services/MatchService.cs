using DataAccess.Scoreboards;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class MatchService : IMatchManager
    {
        private readonly ScoreboardDAO _scoreboardDAO;
        private readonly ConcurrentDictionary<Guid, IMatchCallback> _connectedPlayers;
        private readonly ConcurrentDictionary<Guid, OngoingMatch> _matches;
        private readonly ConcurrentDictionary<Guid, Guid> _playersOngoingMatchesMap;

        public MatchService()
        {
            _scoreboardDAO = new ScoreboardDAO();
            _connectedPlayers = new ConcurrentDictionary<Guid, IMatchCallback>();
            _matches = new ConcurrentDictionary<Guid, OngoingMatch>();
            _playersOngoingMatchesMap = new ConcurrentDictionary<Guid, Guid>();
        }

        public CommunicationRequest Connect(Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            IMatchCallback currentClientChannel = OperationContext.Current.GetCallbackChannel<IMatchCallback>();
            bool hasConnected = _connectedPlayers.TryAdd(playerID, currentClientChannel);
            if (hasConnected)
            {
                request.IsSuccess = true;
                request.StatusCode = StatusCode.OK;
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNAUTHORIZED;
            }
            return request;
        }

        public void Disconnect(Guid playerID)
        {
            bool hasDisconnected = _connectedPlayers.TryRemove(playerID, out _);
            if (hasDisconnected)
            {
                bool wasInMatch = _playersOngoingMatchesMap.TryRemove(playerID, out Guid ongoingMatchID);
                if (wasInMatch)
                {
                    HandleMatchAbandoned(ongoingMatchID, playerID);
                }
            }
        }

        private void HandleMatchAbandoned(Guid ongoingMatchID, Guid leavingPlayerID)
        {
            bool matchExists = _matches.TryGetValue(ongoingMatchID, out OngoingMatch ongoingMatch);
            if (matchExists)
            {
                Guid toNotifyID;
                if (ongoingMatch.CurrentSpymasterID == leavingPlayerID)
                {
                    toNotifyID = ongoingMatch.CurrentGuesserID;
                    NotifyMatchAbandoned(toNotifyID);
                }
                else if (ongoingMatch.CurrentGuesserID == leavingPlayerID)
                {
                    toNotifyID = ongoingMatch.CurrentSpymasterID;
                    NotifyMatchAbandoned(toNotifyID);
                }
                RemoveMatch(ongoingMatch);
            }
        }

        private void NotifyMatchAbandoned(Guid toNotifyID)
        {
            bool isPlayerOnline = _connectedPlayers.TryGetValue(toNotifyID, out IMatchCallback channel);
            if (isPlayerOnline)
            {
                try
                {
                    channel.NotifyCompanionDisconnect();
                }
                catch (CommunicationException ex)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Warn("Player could not be notified of companion disconnect: ", ex);
                }
            }
        }

        public CommunicationRequest JoinMatch(Match match, Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (match == null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }
            bool matchExists = _matches.TryGetValue(match.MatchID, out OngoingMatch ongoingMatch);
            lock (_matches)
            {
                if (matchExists)
                {
                    if (match.Requester.PlayerID == playerID)
                    {
                        return JoinMatchAsRequester(ongoingMatch, playerID);
                    }
                    else if (match.Companion.PlayerID == playerID)
                    {
                        return JoinMatchAsCompanion(ongoingMatch, playerID);
                    }
                    else
                    {
                        request.IsSuccess = false;
                        request.StatusCode = StatusCode.WRONG_DATA;
                        return request;
                    }
                }
                else
                {
                    return StartMatch(match, playerID);
                }
            }
        }

        private CommunicationRequest JoinMatchAsRequester(OngoingMatch ongoingMatch, Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (ongoingMatch.CurrentSpymasterID == Guid.Empty)
            {
                bool playerAddedToMatches = _playersOngoingMatchesMap.TryAdd(playerID, ongoingMatch.MatchID);
                if (playerAddedToMatches)
                {
                    ongoingMatch.CurrentSpymasterID = playerID;
                    request.StatusCode = StatusCode.OK;
                }
                else
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.UNALLOWED;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.WRONG_DATA;
            }
            return request;
        }

        private CommunicationRequest JoinMatchAsCompanion(OngoingMatch ongoingMatch, Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (ongoingMatch.CurrentGuesserID == Guid.Empty)
            {
                bool playerAddedToMatches = _playersOngoingMatchesMap.TryAdd(playerID, ongoingMatch.MatchID);
                if (playerAddedToMatches)
                {
                    ongoingMatch.CurrentGuesserID = playerID;
                    request.StatusCode = StatusCode.OK;
                }
                else
                {
                    request.IsSuccess = false;
                    request.StatusCode= StatusCode.UNALLOWED;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.WRONG_DATA;
            }
            return request;
        }

        private CommunicationRequest StartMatch(Match match, Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            OngoingMatch newMatch = new OngoingMatch(match);
            bool wasMatchAdded = _matches.TryAdd(newMatch.MatchID, newMatch);
            if (wasMatchAdded)
            {
                _matches.TryGetValue(newMatch.MatchID, out OngoingMatch newlyAddedMatch);
                if (match.Requester.PlayerID == playerID)
                {
                    return JoinMatchAsRequester(newlyAddedMatch, playerID);
                }
                else if (match.Companion.PlayerID == playerID)
                {
                    return JoinMatchAsCompanion(newlyAddedMatch, playerID);
                }
                else
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.WRONG_DATA;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode= StatusCode.SERVER_ERROR;
            }
            return request;
        }

        public void SendClue(Guid senderID, string clue)
        {
            _playersOngoingMatchesMap.TryGetValue(senderID, out Guid matchID);
            bool matchFound = _matches.TryGetValue(matchID, out OngoingMatch ongoingMatch);
            if (matchFound)
            {
                Guid sendToID = ongoingMatch.CurrentGuesserID;
                if (_connectedPlayers.TryGetValue(sendToID, out IMatchCallback sendToChannel))
                {
                    try
                    {
                        sendToChannel.NotifyClueReceived(clue);
                    } 
                    catch (CommunicationException ex)
                    {
                        HandleMatchAbandoned(matchID, sendToID);
                        RemoveFaultedChannel(sendToChannel);
                        ServerLogger.Log.Warn("Clue could not be sent", ex);
                    }
                }
            }
        }

        public void NotifyTurnTimeout(Guid senderID, MatchRoleType currentRole)
        {
            _playersOngoingMatchesMap.TryGetValue(senderID, out Guid matchID);
            bool isMatchOngoing = _matches.TryGetValue(matchID, out OngoingMatch ongoingMatch);
            if (isMatchOngoing)
            {
                switch (currentRole)
                {
                    case MatchRoleType.SPYMASTER:
                        NotifyTurnChange(matchID, ongoingMatch.CurrentGuesserID);

                        NotifyTurnChange(matchID, ongoingMatch.CurrentSpymasterID);
                        break;

                    case MatchRoleType.GUESSER:
                        ongoingMatch.TimerTokens--;
                        NotifyGuesserTurnTimeout(ongoingMatch.CurrentSpymasterID, ongoingMatch.TimerTokens);
                        HandleRoleSwitch(ongoingMatch);
                        break;
                }
            }
        }

        private void NotifyTurnChange(Guid matchID, Guid toNotifyID)
        {
            if (_connectedPlayers.TryGetValue(toNotifyID, out IMatchCallback sendToChannel))
            {
                try
                {
                    sendToChannel.NotifyTurnChange();
                }
                catch (CommunicationException ex)
                {
                    HandleMatchAbandoned(matchID, toNotifyID);
                    RemoveFaultedChannel(sendToChannel);
                    ServerLogger.Log.Warn("Turn change due to timeout could not be notified", ex);
                }
            }
        }

        private void NotifyGuesserTurnTimeout(Guid spymasterID, int timerTokens)
        {
            if (_connectedPlayers.TryGetValue(spymasterID, out IMatchCallback spymasterChannel))
            {
                try
                {
                    spymasterChannel.NotifyGuesserTurnTimeout(timerTokens);
                }
                catch (CommunicationException ex)
                {
                    ServerLogger.Log.Warn("Failed to send guesser turn timeout notification", ex);
                }
            }
        }

        private void HandleRoleSwitch(OngoingMatch ongoingMatch)
        {
            bool spymasterNotified = NotifyRolesChanged(ongoingMatch.CurrentSpymasterID);
            bool guesserNotified = NotifyRolesChanged(ongoingMatch.CurrentGuesserID);
            if (spymasterNotified && guesserNotified)
            {
                SwitchRoles(ongoingMatch);
            }
            else
            {
                if (!spymasterNotified)
                {
                    HandleMatchAbandoned(ongoingMatch.MatchID, ongoingMatch.CurrentSpymasterID);
                }
                if (!guesserNotified)
                {
                    HandleMatchAbandoned(ongoingMatch.MatchID, ongoingMatch.CurrentGuesserID);
                }
            }
        }

        private static void SwitchRoles(OngoingMatch match)
        {
            Guid auxCurrentSpymasterID = match.CurrentSpymasterID;
            Guid auxCurrentGuesserID = match.CurrentGuesserID;
            match.CurrentSpymasterID = auxCurrentGuesserID;
            match.CurrentGuesserID = auxCurrentSpymasterID;
        }

        private bool NotifyRolesChanged(Guid toNotifyID)
        {
            if (_connectedPlayers.TryGetValue(toNotifyID, out IMatchCallback channel))
            {
                try
                {
                    channel.NotifyRolesChanged();
                    return true;
                }
                catch (CommunicationException ex)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Warn("Role change could not be notified: ", ex);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void NotifyPickedAgent(AgentPickedNotification notification)
        {
            if (notification  == null)
            {
                return;
            }

            _playersOngoingMatchesMap.TryGetValue(notification.SenderID, out Guid matchID);
            bool matchFound = _matches.TryGetValue(matchID, out OngoingMatch ongoingMatch);
            if (matchFound)
            {
                _connectedPlayers.TryGetValue(ongoingMatch.CurrentSpymasterID, out IMatchCallback spymasterChannel);
                try
                {
                    ongoingMatch.RemainingAgents--;
                    if (ongoingMatch.RemainingAgents > 0)
                    {
                        notification.NewTurnLength = notification.NewTurnLength < MatchRules.MAX_TURN_TIMER ? 
                            notification.NewTurnLength : MatchRules.MAX_TURN_TIMER;
                        spymasterChannel.NotifyAgentPicked(notification);
                    }
                    else
                    {
                        ongoingMatch.StopTimer();
                        HandleMatchWon(ongoingMatch);
                    }
                }
                catch (CommunicationException ex)
                {
                    HandleMatchAbandoned(matchID, ongoingMatch.CurrentSpymasterID);
                    RemoveFaultedChannel(spymasterChannel);
                    ServerLogger.Log.Warn("Spymaster could not be notified an agent was picked: ", ex);
                }
            }
        }

        private void HandleMatchWon(OngoingMatch match)
        {
            NotifyMatchWon(match, match.CurrentSpymasterID);
            NotifyMatchWon(match, match.CurrentGuesserID);
            RemoveMatch(match);
        }

        private void NotifyMatchWon(OngoingMatch match, Guid toNotifyID)
        {
            bool fastestMatchUpdated = _scoreboardDAO.UpdateFastestMatchRecord(toNotifyID, match.GetElapsedTime);
            bool matchesWonUpdated = _scoreboardDAO.UpdateMatchesWon(toNotifyID);
            if (_connectedPlayers.TryGetValue(toNotifyID, out IMatchCallback channel))
            {
                try
                {
                    channel.NotifyMatchWon(match.GetMatchDuration);
                    if (!fastestMatchUpdated || !matchesWonUpdated)
                    {
                        channel.NotifyStatsCouldNotBeSaved();
                    }
                }
                catch (CommunicationException ex)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Warn("Match won could not be notified: ", ex);
                }
            }
        }

        public void NotifyPickedBystander(BystanderPickedNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            _playersOngoingMatchesMap.TryGetValue(notification.SenderID, out Guid matchID);
            if (_matches.TryGetValue(matchID, out OngoingMatch ongoingMatch))
            {
                _connectedPlayers.TryGetValue(ongoingMatch.CurrentSpymasterID, out IMatchCallback spymasterChannel);
                int currentTimerTokens = ongoingMatch.TimerTokens;
                switch (ongoingMatch.Gamemode)
                {
                    case Gamemode.CUSTOM:
                        int currentBystanderTokens = ongoingMatch.BystanderTokens;
                        if (currentBystanderTokens >= 1)
                        {
                            notification.TokenToUpdate = TokenType.BYSTANDER;
                            currentBystanderTokens--;
                            ongoingMatch.BystanderTokens = currentBystanderTokens;
                            notification.RemainingTokens = currentBystanderTokens;
                            HandleBystanderTokenUpdate(ongoingMatch, notification, spymasterChannel);
                        }
                        else
                        {
                            notification.TokenToUpdate = TokenType.TIMER;
                            currentTimerTokens = currentTimerTokens - MatchRules.TIMER_TOKENS_TO_TAKE_CUSTOM;
                            currentTimerTokens = currentTimerTokens != -1 ? currentTimerTokens : 0;
                            ongoingMatch.TimerTokens = currentTimerTokens;
                            notification.RemainingTokens = currentTimerTokens;
                            HandleTimerTokenUpdate(ongoingMatch, notification, spymasterChannel);
                        }
                        break;
                    default:
                        notification.TokenToUpdate = TokenType.TIMER;
                        currentTimerTokens = ongoingMatch.TimerTokens;
                        currentTimerTokens = currentTimerTokens - MatchRules.TIMER_TOKENS_TO_TAKE_NON_CUSTOM;
                        ongoingMatch.TimerTokens = currentTimerTokens;
                        notification.RemainingTokens = currentTimerTokens;
                        HandleTimerTokenUpdate(ongoingMatch, notification, spymasterChannel);
                        break;
                }
            }
        }

        private void HandleTimerTokenUpdate(OngoingMatch match, BystanderPickedNotification notification, IMatchCallback spymasterChannel)
        {
            if (match.TimerTokens >= 0)
            {
                
                try
                {
                    spymasterChannel.NotifyBystanderPicked(notification);
                    HandleRoleSwitch(match);
                }
                catch (CommunicationException ex)
                {
                    HandleMatchAbandoned(match.MatchID, match.CurrentSpymasterID);
                    RemoveFaultedChannel(spymasterChannel);
                }
            }
            else
            {
                match.StopTimer();
                HandleMatchLostTimeout(match);
            }
        }

        private void HandleMatchLostTimeout(OngoingMatch match)
        {
            string finalMatchLength = match.GetMatchDuration;
            NotifyMatchLostTimeout(match.CurrentGuesserID, finalMatchLength);
            NotifyMatchLostTimeout(match.CurrentSpymasterID, finalMatchLength);
            RemoveMatch(match);
        }

        private void HandleBystanderTokenUpdate(OngoingMatch match, BystanderPickedNotification notification, IMatchCallback spymasterChannel)
        {
            try
            {
                spymasterChannel.NotifyBystanderPicked(notification);
                HandleRoleSwitch(match);
            }
            catch (CommunicationException ex)
            {
                HandleMatchAbandoned(match.MatchID, match.CurrentSpymasterID);
                RemoveFaultedChannel(spymasterChannel);
                ServerLogger.Log.Warn("Bystander token update could not be sent: ", ex);
            }
        }

        private void NotifyMatchLostTimeout(Guid toNotifyID, string finalMatchLength)
        {
            if (_connectedPlayers.TryGetValue(toNotifyID, out IMatchCallback channel))
            {
                try
                {
                    channel.NotifyMatchTimeout(finalMatchLength);
                }
                catch (CommunicationException ex)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Warn("Could not notify match lost: ", ex);
                } 
            }
        }

        public void NotifyPickedAssassin(AssassinPickedNotification notification)
        {
            if (notification == null)
            {
                return;
            }
            _playersOngoingMatchesMap.TryGetValue(notification.SenderID, out Guid matchID);
            bool matchFound = _matches.TryGetValue(matchID, out OngoingMatch match);
            if (matchFound)
            {
                match.StopTimer();
                UpdateAssassinsPicked(match.CurrentGuesserID);
                SendAssassinPickedNotification(match.CurrentGuesserID, notification);
                SendAssassinPickedNotification(match.CurrentSpymasterID, notification);
                RemoveMatch(match);
            }
        }

        private void UpdateAssassinsPicked(Guid pickerID)
        {
            bool assassinsPickedUpdated = _scoreboardDAO.UpdateAssassinsPicked(pickerID);
            if (!assassinsPickedUpdated)
            {
                SendStatsNotSavedNotification(pickerID);
            }
        }

        private void SendStatsNotSavedNotification(Guid playerID)
        {
            if (_connectedPlayers.TryGetValue(playerID, out IMatchCallback channel))
            {
                try
                {
                    channel.NotifyStatsCouldNotBeSaved();
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Warn("Failed to notify scores could not be saved: ", ex);
                }
                catch (Exception ex)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Error("Unexpected exception while notifying scores could not be saved: ", ex);
                }
            }

        }

        private void SendAssassinPickedNotification(Guid toNotifyID, AssassinPickedNotification notification)
        {
            if (_connectedPlayers.TryGetValue(toNotifyID, out IMatchCallback channel))
            {
                try
                {
                    channel.NotifyAssassinPicked(notification);
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Warn("Could not notify assassin picked: ", ex);
                }
                catch (Exception ex)
                {
                    RemoveFaultedChannel(channel);
                    ServerLogger.Log.Error("Unexpected exception while notifying assassin picked: ", ex);
                }
            }
        }

        private void RemoveMatch(OngoingMatch match)
        {
            _playersOngoingMatchesMap.TryRemove(match.CurrentSpymasterID, out _);
            _playersOngoingMatchesMap.TryRemove(match.CurrentGuesserID, out _);
            _matches.TryRemove(match.MatchID, out _);
        }

        private static void RemoveFaultedChannel(IMatchCallback faultedChannel)
        {
            if (faultedChannel is ICommunicationObject communicationObject)
            {
                communicationObject.Abort();
            }
        }

        private sealed class OngoingMatch
        {
            const int MAX_MATCH_DURATION = 1;
            private readonly Stopwatch _stopwatch;
            private readonly TimeSpan _matchLimit = TimeSpan.FromHours(MAX_MATCH_DURATION);
            const int INTIAL_REMAINING_AGENTS = 15;
            public Guid MatchID { get; set; }
            public Gamemode Gamemode { get; set; }
            public Guid CurrentSpymasterID { get; set; }
            public Guid CurrentGuesserID { get; set; }
            public int RemainingAgents { get; set; }
            public int TimerTokens { get; set; }
            public int BystanderTokens { get; set; }

            public TimeSpan GetElapsedTime
            {
                get
                {
                    TimeSpan elapsedTime = _stopwatch.Elapsed;

                    if (elapsedTime > _matchLimit)
                    {
                        return _matchLimit;
                    }
                    return elapsedTime;
                }
            }

            public OngoingMatch(Match match)
            {
                _stopwatch = new Stopwatch();
                MatchID = match.MatchID;
                Gamemode = match.Rules.Gamemode;
                RemainingAgents = INTIAL_REMAINING_AGENTS;
                TimerTokens = match.Rules.TimerTokens;
                BystanderTokens = match.Rules.BystanderTokens;
                _stopwatch.Start();
            }

            public string GetMatchDuration
            {
                get
                {
                    TimeSpan time = this.GetElapsedTime;
                    return time.ToString(@"mm\:ss");
                }
            }

            public void StopTimer()
            {
                _stopwatch.Stop();
            }
        }
    }
}
