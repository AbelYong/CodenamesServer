using DataAccess.Users;
using Services.Contracts.Callback;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.DataContract;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Text;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class LobbyService : ILobbyManager
    {
        private readonly ICallbackProvider _callbackProvider;
        private readonly IPlayerRepository _playerRepository;
        private readonly IEmailOperation _emailOperation;
        private readonly ConcurrentDictionary<Guid, ILobbyCallback> _connectedPlayers;
        private readonly ConcurrentDictionary<string, Party> _lobbies;
        private readonly ConcurrentDictionary<Guid, string> _playerLobbyMap;
        private static readonly Random _random = new Random();

        public LobbyService() : this (new CallbackProvider(), new PlayerRepository(), new EmailOperation())
        {
            _connectedPlayers = new ConcurrentDictionary<Guid, ILobbyCallback>();
            _lobbies = new ConcurrentDictionary<string, Party>();
            _playerLobbyMap = new ConcurrentDictionary<Guid, string>();
        }

        public LobbyService(ICallbackProvider callbackProvider, IPlayerRepository playerRepository, IEmailOperation emailOperation)
        {
            _playerRepository = playerRepository;
            _callbackProvider = callbackProvider;
            _connectedPlayers = new ConcurrentDictionary<Guid, ILobbyCallback>();
            _lobbies = new ConcurrentDictionary<string, Party>();
            _playerLobbyMap = new ConcurrentDictionary<Guid, string>();
            _emailOperation = emailOperation;
        }

        public CommunicationRequest Connect(Guid playerID)
        {
            CommunicationRequest request = new CommunicationRequest();
            ILobbyCallback currentClientChannel = _callbackProvider.GetCallback<ILobbyCallback>();
            bool hasConnected = _connectedPlayers.TryAdd(playerID, currentClientChannel);
            if (hasConnected)
            {
                request.IsSuccess = true;
                request.StatusCode = StatusCode.OK;

                string audit = string.Format("{0} has connected to LobbyService", ServerLogger.GetPlayerIdentifier(playerID));
                ServerLogger.Log.Info(audit);
            }
            else
            {
                Disconnect(playerID);
                hasConnected = _connectedPlayers.TryAdd(playerID, currentClientChannel);
                request.IsSuccess = hasConnected;
                request.StatusCode = hasConnected ? StatusCode.OK : StatusCode.UNAUTHORIZED;

                string audit = string.Format("Connection request to LobbyService by {0} procesed with code {1}", ServerLogger.GetPlayerIdentifier(playerID), request.StatusCode);
                ServerLogger.Log.Info(audit);
            }
            return request;
        }

        public void Disconnect(Guid playerID)
        {
            if (_connectedPlayers.TryRemove(playerID, out _))
            {
                string audit = string.Format("{0} has disconnected from LobbyService", ServerLogger.GetPlayerIdentifier(playerID));
                ServerLogger.Log.Info(audit);
            }
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

            if (party.PartyHost.PlayerID == leavingPlayerID)
            {
                _lobbies.TryRemove(party.LobbyCode, out _);

                if (party.PartyGuest != null)
                {
                    Guid partyGuestID = (Guid)party.PartyGuest.PlayerID;
                    _playerLobbyMap.TryRemove(partyGuestID, out _);
                    NotifyPlayerLeft(leavingPlayerID, partyGuestID);
                }
            }
            else if (party.PartyGuest != null && party.PartyGuest.PlayerID == leavingPlayerID)
            {
                Guid partyHostID = (Guid)party.PartyHost.PlayerID;
                party.PartyGuest = null;
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
            if (player == null || !player.PlayerID.HasValue)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }

            Guid auxID = (Guid)player.PlayerID;
            if (!_connectedPlayers.TryGetValue(auxID, out _))
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.CLIENT_DISCONNECT;
                return request;
            }

            if (!_playerLobbyMap.ContainsKey(auxID))
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

                    string audit = string.Format("Party created successfully for {0}", ServerLogger.GetPlayerIdentifier(auxID));
                    ServerLogger.Log.Info(audit);
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
            return request;
        }

        public CommunicationRequest SendEmailInvitation(Guid partyHostID, string email)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (_playerLobbyMap.TryGetValue(partyHostID, out string lobbyCode))
            {
                request = VerifyPlayerCanInvite(partyHostID, lobbyCode);
                if (!request.IsSuccess)
                {
                    return request;
                }

                if (_lobbies.TryGetValue(lobbyCode, out Party party))
                {
                    bool wasEmailSent = _emailOperation.SendGameInvitationEmail(party.PartyHost.Username, email, lobbyCode);

                    request.IsSuccess = wasEmailSent;
                    request.StatusCode = wasEmailSent ? StatusCode.OK : StatusCode.CLIENT_UNREACHABLE;
                }
                else
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.NOT_FOUND;
                }
                string audit = string.Format("Email invitation request by {0} processed with code {1}", ServerLogger.GetPlayerIdentifier(partyHostID), request.StatusCode);
                ServerLogger.Log.Info(audit);
                return request;
                
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNALLOWED;
                return request;
            }
        }

        public CommunicationRequest InviteToParty(Player partyHost, Guid friendToInviteID, string lobbyCode)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (partyHost != null && partyHost.PlayerID.HasValue)
            {
                Guid partyHostID = (Guid)partyHost.PlayerID;
                request = VerifyPlayerCanInvite(partyHostID, lobbyCode);
                if (!request.IsSuccess)
                {
                    return request;
                }

                bool wasGameInviteSent = SendInGameInvitation(friendToInviteID, partyHost, lobbyCode);

                string friendEmailAddress = _playerRepository.GetEmailByPlayerID(friendToInviteID);
                bool wasEmailSent = _emailOperation.SendGameInvitationEmail(partyHost.Username, friendEmailAddress, lobbyCode);

                request.IsSuccess = (wasGameInviteSent || wasEmailSent);
                request.StatusCode = wasEmailSent ? StatusCode.OK : StatusCode.CLIENT_UNREACHABLE;

                string audit = string.Format("Party invitation request by {0} processed with code {1}", ServerLogger.GetPlayerIdentifier(partyHostID), request.StatusCode);
                ServerLogger.Log.Info(audit);
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
            }
            return request;
        }

        private bool SendInGameInvitation(Guid friendToInviteID, Player partyHost, string lobbyCode)
        {
            bool isFriendOnline = _connectedPlayers.TryGetValue(friendToInviteID, out ILobbyCallback friendChannel);
            if (isFriendOnline)
            {
                try
                {
                    friendChannel.NotifyMatchInvitationReceived(partyHost, lobbyCode);
                    return true;
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
            return false;
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
                        request.StatusCode = StatusCode.CONFLICT;
                        return request;
                    }

                    party.PartyGuest = joiningPlayer;
                    _playerLobbyMap.TryAdd(joiningPlayerID, lobbyCode);
                }

                Guid partyHostID = (Guid)party.PartyHost.PlayerID;
                bool wasHostReached = NotifyJoinedToParty(partyHostID, joiningPlayer);
                if (!wasHostReached)
                {
                    party.PartyGuest = null;
                    _playerLobbyMap.TryRemove(joiningPlayerID, out _);

                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.CLIENT_UNREACHABLE;
                    return request;
                }

                request.IsSuccess = true;
                request.StatusCode = StatusCode.OK;
                request.Party = party;

                string guestIdentifier = ServerLogger.GetPlayerIdentifier(joiningPlayerID);
                string hostIdentifier = ServerLogger.GetPlayerIdentifier(partyHostID);
                string audit = string.Format("{0} has joined {1}'s lobby", guestIdentifier, hostIdentifier);
                ServerLogger.Log.Info(audit);
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
