using DataAccess;
using DataAccess.Users;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO;
using Services.DTO.Request;
using System;
using System.Data.Entity.Infrastructure;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerCall)]
    public class ModerationService : IModerationManager
    {
        private readonly ISessionManager _sessionService;
        private readonly IReportDAO _reportDAO;
        private readonly IBanDAO _banDAO;
        private readonly IPlayerDAO _playerDAO;

        public ModerationService() : this (new SessionService(), new ReportDAO(), new BanDAO(), new PlayerDAO()) { }

        public ModerationService(ISessionManager sessionService, IReportDAO reportDAO, IBanDAO banDAO, IPlayerDAO playerDAO)
        {
            _sessionService = sessionService;
            _reportDAO = reportDAO;
            _banDAO = banDAO;
            _playerDAO = playerDAO;
        }

        public CommunicationRequest ReportPlayer(Guid reporterPlayerID, Guid reportedPlayerID, string reason)
        {
            CommunicationRequest request = new CommunicationRequest();

            if (!_sessionService.IsPlayerOnline(reporterPlayerID))
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNAUTHORIZED;
                return request;
            }

            try
            {
                var reporterEntity = _playerDAO.GetPlayerById(reporterPlayerID);
                var reportedEntity = _playerDAO.GetPlayerById(reportedPlayerID);

                if (reporterEntity == null || reportedEntity == null)
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.NOT_FOUND;
                    return request;
                }

                Guid reporterUserID = reporterEntity.userID;
                Guid reportedUserID = reportedEntity.userID;


                if (_reportDAO.HasPlayerReportedTarget(reporterUserID, reportedUserID))
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.REPORT_DUPLICATED;
                    return request;
                }

                var report = new Report
                {
                    reportID = Guid.NewGuid(),
                    reporterUserID = reporterUserID,
                    reportedUserID = reportedUserID,
                    reason = reason,
                    reportDatetime = DateTimeOffset.Now
                };
                _reportDAO.AddReport(report);

                int reportCount = _reportDAO.CountUniqueReports(reportedUserID);
                TimeSpan? banDuration = null;
                bool isPermanent = false;

                switch (reportCount)
                {
                    case 3:
                        banDuration = TimeSpan.FromHours(1);
                        break;
                    case 5:
                        banDuration = TimeSpan.FromHours(8);
                        break;
                    case 7:
                        banDuration = TimeSpan.FromHours(12);
                        break;
                    case 10:
                        isPermanent = true;
                        break;
                }

                if (banDuration.HasValue || isPermanent)
                {
                    DateTimeOffset timeout = isPermanent ? DateTimeOffset.MaxValue : DateTimeOffset.Now.Add(banDuration.Value);
                    string banReasonText = $"Cumulative reports ({reportCount}). Last reason: {reason}";

                    var ban = new Ban
                    {
                        banID = Guid.NewGuid(),
                        userID = reportedUserID,
                        reason = banReasonText,
                        banDatetime = DateTimeOffset.Now,
                        timeout = timeout
                    };
                    _banDAO.ApplyBan(ban);

                    KickReason kickReason = isPermanent ? KickReason.PERMANTENT_BAN : KickReason.TEMPORARY_BAN;

                    _sessionService.KickPlayer(reportedPlayerID, kickReason);

                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.USER_KICKED_AND_BANNED;
                }
                else
                {
                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.REPORT_CREATED;
                }
            }
            catch (DbUpdateException)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.REPORT_DUPLICATED;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ModerationService: {ex.Message}");
                request.IsSuccess = false;
                request.StatusCode = StatusCode.SERVER_ERROR;
            }

            return request;
        }
    }
}