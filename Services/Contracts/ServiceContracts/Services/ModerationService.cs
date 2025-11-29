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
        private readonly IReportDAO _reportDAO;
        private readonly IBanDAO _banDAO;
        private readonly IPlayerDAO _playerDAO;

        public ModerationService() : this (new ReportDAO(), new BanDAO(), new PlayerDAO())
        {

        }

        public ModerationService(IReportDAO reportDAO, IBanDAO banDAO, IPlayerDAO playerDAO)
        {
            _reportDAO = reportDAO;
            _banDAO = banDAO;
            _playerDAO = playerDAO;
        }

        public CommunicationRequest ReportPlayer(Guid reporterPlayerID, Guid reportedPlayerID, string reason)
        {
            CommunicationRequest result = new CommunicationRequest();

            if (!SessionService.Instance.IsPlayerOnline(reporterPlayerID))
            {
                result.IsSuccess = false;
                result.StatusCode = StatusCode.UNAUTHORIZED;
                return result;
            }

            try
            {
                var reporterEntity = _playerDAO.GetPlayerById(reporterPlayerID);
                var reportedEntity = _playerDAO.GetPlayerById(reportedPlayerID);

                if (reporterEntity == null || reportedEntity == null)
                {
                    result.IsSuccess = false;
                    result.StatusCode = StatusCode.NOT_FOUND;
                    return result;
                }

                Guid reporterUserID = reporterEntity.userID;
                Guid reportedUserID = reportedEntity.userID;


                if (_reportDAO.HasPlayerReportedTarget(reporterUserID, reportedUserID))
                {
                    result.IsSuccess = false;
                    result.StatusCode = StatusCode.REPORT_DUPLICATED;
                    return result;
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
                    case 3: banDuration = TimeSpan.FromHours(1); break;
                    case 5: banDuration = TimeSpan.FromHours(8); break;
                    case 7: banDuration = TimeSpan.FromHours(12); break;
                    case 10: isPermanent = true; break;
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

                    BanReason kickReason = isPermanent ? BanReason.PermanentBan : BanReason.TemporaryBan;

                    SessionService.Instance.KickUser(reportedPlayerID, kickReason);

                    result.IsSuccess = true;
                    result.StatusCode = StatusCode.USER_KICKED_AND_BANNED;
                }
                else
                {
                    result.IsSuccess = true;
                    result.StatusCode = StatusCode.REPORT_CREATED;
                }
            }
            catch (DbUpdateException)
            {
                result.IsSuccess = false;
                result.StatusCode = StatusCode.REPORT_DUPLICATED;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ModerationService: {ex.Message}");
                result.IsSuccess = false;
                result.StatusCode = StatusCode.SERVER_ERROR;
            }

            return result;
        }
    }
}