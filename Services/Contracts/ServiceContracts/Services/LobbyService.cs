using DataAccess.Users;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Text;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class LobbyService : ILobbyManager
    {
        private readonly ICallbackProvider _callbackProvider;
        private readonly IPlayerDAO _playerDAO;
        private readonly IEmailOperation _emailOperation;
        private readonly ConcurrentDictionary<Guid, ILobbyCallback> _connectedPlayers;
        private readonly ConcurrentDictionary<string, Party> _lobbies;
        private readonly ConcurrentDictionary<Guid, string> _playerLobbyMap;
        private static readonly Random _random = new Random();

        public LobbyService() : this (new PlayerDAO(), new CallbackProvider(), new EmailOperation())
        {
            _connectedPlayers = new ConcurrentDictionary<Guid, ILobbyCallback>();
            _lobbies = new ConcurrentDictionary<string, Party>();
            _playerLobbyMap = new ConcurrentDictionary<Guid, string>();
        }

        public LobbyService(IPlayerDAO playerDAO, ICallbackProvider callbackProvider, IEmailOperation emailOperation)
        {
            _playerDAO = playerDAO;
            _callbackProvider = callbackProvider;
            _connectedPlayers = new ConcurrentDictionary<Guid, ILobbyCallback>();
            _lobbies = new ConcurrentDictionary<string, Party>();
            _playerLobbyMap = new ConcurrentDictionary<Guid, string>();
            _emailOperation = emailOperation;
        }

        public CommunicationRequest Connect(Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            ILobbyCallback _currentClientChannel = _callbackProvider.GetCallback<ILobbyCallback>();
            bool hasConnected = _connectedPlayers.TryAdd(playerID, _currentClientChannel);
            if (hasConnected)
            {
                request.IsSuccess = true;
                request.StatusCode = StatusCode.OK;
            }
            else
            {
                Disconnect(playerID);
                hasConnected = _connectedPlayers.TryAdd(playerID, _currentClientChannel);
                request.IsSuccess = hasConnected;
                request.StatusCode = hasConnected ? StatusCode.OK : StatusCode.UNAUTHORIZED;
            }
            return request;
        }

        public void Disconnect(Guid playerID)
        {
            _connectedPlayers.TryRemove(playerID, out _);
            if (_playerLobbyMap.TryRemove(playerID, out string lobbyCode) &&
                !string.IsNullOrEmpty(lobbyCode) &&
                _lobbies.TryGetValue(lobbyCode, out Party party))
            {
                AbandonParty(party, playerID);
            }
        }

        private void AbandonParty(Party party, Guid leavingPlayerID)
        {
            _playerLobbyMap.TryRemove(leavingPlayerID, out _);

            //If the player who abandoned is the party's host, also remove their lobby and notify the guest
            if (party.PartyHost.PlayerID == leavingPlayerID)
            {
                _lobbies.TryRemove(party.LobbyCode, out _);

                if (party.PartyGuest != null)
                {
                    _playerLobbyMap.TryRemove((Guid)party.PartyGuest.PlayerID, out _);
                    NotifyPlayerLeft(leavingPlayerID, (Guid)party.PartyGuest.PlayerID);
                }
            }
            else if (party.PartyGuest != null && party.PartyGuest.PlayerID == leavingPlayerID)
            {
                Guid partyHostID = (Guid)party.PartyHost.PlayerID;
                party.PartyGuest = null; //Free the guest slot
                NotifyPlayerLeft(leavingPlayerID, partyHostID);
            }
        }

        private void NotifyPlayerLeft(Guid leavingPlayerID, Guid toNotifyID)
        {
            try
            {
                if (_connectedPlayers.TryGetValue(toNotifyID, out ILobbyCallback channelToNotify))
                {
                    channelToNotify.NotifyPartyAbandoned(leavingPlayerID);
                }
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                RemoveFaultedChannel(toNotifyID);
                ServerLogger.Log.Warn("Exception while sending player left lobby notification", ex);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending player left lobby notification", ex);
            }
        }

        public CreateLobbyRequest CreateParty(Player player)
        {
            CreateLobbyRequest request = new CreateLobbyRequest();
            if (player != null && player.PlayerID.HasValue)
            {
                Guid auxID = (Guid)player.PlayerID;
                bool isPlayerInParty = VerifyIsPlayerInParty(auxID);
                if (!isPlayerInParty)
                {
                    Player host = player;
                    string code = GetRandomLobbyCode();
                    Party party = new Party(host, code);
                    if (_lobbies.TryAdd(code, party))
                    {
                        _playerLobbyMap.TryAdd(auxID, code);
                        request.LobbyCode = code;
                        request.IsSuccess = true;
                        request.StatusCode = StatusCode.CREATED;
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
                    request.StatusCode = StatusCode.UNALLOWED;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
            }
            return request;
        }

        private bool VerifyIsPlayerInParty(Guid playerID)
        {
            return _playerLobbyMap.ContainsKey(playerID);
        }

        public CommunicationRequest InviteToParty(Player partyHost, Guid friendToInviteID, string lobbyCode)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (partyHost != null && partyHost.PlayerID.HasValue)
            {
                Guid hostPlayerID = (Guid)partyHost.PlayerID;
                request = VerifyPlayerCanInvite(hostPlayerID, lobbyCode);
                if (!request.IsSuccess)
                {
                    return request;
                }

                SendInGameInvatation(friendToInviteID, partyHost, lobbyCode);

                string friendEmailAddress = _playerDAO.GetEmailByPlayerID(friendToInviteID);
                bool wasEmailSent = _emailOperation.SendGameInvitationEmail(partyHost.Username, friendEmailAddress, lobbyCode);

                request.IsSuccess = true;
                request.StatusCode = wasEmailSent ? StatusCode.OK : StatusCode.CLIENT_UNREACHABLE;
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
            }
            return request;
        }

        private void SendInGameInvatation(Guid friendToInviteID, Player partyHost, string lobbyCode)
        {
            bool isFriendOnline = _connectedPlayers.TryGetValue(friendToInviteID, out ILobbyCallback friendChannel);
            if (isFriendOnline)
            {
                try
                {
                    friendChannel.NotifyMatchInvitationReceived(partyHost, lobbyCode);
                }
                catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
                {
                    ServerLogger.Log.Warn("Exception while sending lobby invitation: ", ex);
                }
                catch (Exception ex)
                {
                    ServerLogger.Log.Error("Unexpected exception while sending lobby invitation: ", ex);
                }
            }
        }

        private CommunicationRequest VerifyPlayerCanInvite(Guid partyHostID, string lobbyCode)
        {
            CommunicationRequest request = new CommunicationRequest();
            bool partyFound = _lobbies.TryGetValue(lobbyCode, out Party party);

            if (!partyFound)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.NOT_FOUND;
                return request;
            }

            if (party.PartyHost.PlayerID != partyHostID)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNAUTHORIZED;
                return request;
            }

            if (party.PartyGuest != null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNALLOWED;
                return request;
            }

            request.IsSuccess = true;
            request.StatusCode = StatusCode.OK;
            return request;
        }

        public JoinPartyRequest JoinParty(Player joiningPlayer, string lobbyCode)
        {
            JoinPartyRequest request = new JoinPartyRequest();
            if (joiningPlayer != null && joiningPlayer.PlayerID.HasValue)
            {
                Guid joiningPlayerID = (Guid)joiningPlayer.PlayerID;
                bool isGuestOnline = _connectedPlayers.TryGetValue(joiningPlayerID, out _);

                if (!isGuestOnline)
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.CLIENT_DISCONNECT;
                    return request;
                }

                if (!_lobbies.TryGetValue(lobbyCode, out Party party))
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.NOT_FOUND;
                    return request;
                }

                lock (party)
                {
                    if (party.PartyGuest != null)
                    {
                        request.IsSuccess = false;
                        request.StatusCode = StatusCode.CONFLICT; // Party is full
                        return request;
                    }

                    party.PartyGuest = joiningPlayer;
                    _playerLobbyMap.TryAdd(joiningPlayerID, lobbyCode);
                }

                Guid partyHostID = (Guid)party.PartyHost.PlayerID;
                bool wasHostReached = NotifyJoinedToParty(partyHostID, joiningPlayer);
                if (!wasHostReached)
                {
                    // Party host is disconnected. Rollback the join.
                    party.PartyGuest = null;
                    _playerLobbyMap.TryRemove(joiningPlayerID, out _);

                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.CLIENT_UNREACHABLE;
                    return request;
                }

                request.IsSuccess = true;
                request.StatusCode = StatusCode.OK;
                request.Party = party;
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }
            return request;
        }

        private bool NotifyJoinedToParty(Guid partyHostID, Player joiningFriend)
        {
            try
            {
                if (_connectedPlayers.TryGetValue(partyHostID, out ILobbyCallback hostChannel))
                {
                    hostChannel.NotifyMatchInvitationAccepted(joiningFriend);
                    return true;
                }
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException)
            {
                RemoveFaultedChannel(partyHostID);
                ServerLogger.Log.Warn("Exception while sending player joined party notification", ex);
            }
            catch (Exception ex)
            {
                ServerLogger.Log.Error("Unexpected exception while sending player joined party notification: ", ex);
            }
            return false;
        }

        private void RemoveFaultedChannel(Guid playerID)
        {
            _connectedPlayers.TryRemove(playerID, out ILobbyCallback faultedChannel);
            if (faultedChannel is ICommunicationObject communicationObject)
            {
                communicationObject.Abort();
            }
        }

        public void LeaveParty(Guid playerID, string lobbyCode)
        {
            bool partyFound = _lobbies.TryGetValue(lobbyCode, out Party party);
            if (partyFound)
            {
                AbandonParty(party, playerID);
            }
        }

        private static string GetRandomLobbyCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int codeLength = 6;
            StringBuilder code = new StringBuilder();
            for (int i = 0; i < codeLength; i++)
            {
                int index = _random.Next(chars.Length);
                code.Append(chars[index]);
            }
            return code.ToString();
        }
    }
}
