using DataAccess;
using DataAccess.Users;
using Services.Contracts;
using Services.Contracts.ServiceContracts.Managers;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO;
using Services.DTO.Request;
using System;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerCall)]
    public class ModerationService : IModerationManager
    {
        private readonly IReportDAO _reportDAO;
        private readonly IBanDAO _banDAO;

        public ModerationService()
        {
            _reportDAO = new ReportDAO();
            _banDAO = new BanDAO();
        }

        public CommunicationRequest ReportPlayer(Guid reportedUserID, string reason)
        {
            CommunicationRequest result = new CommunicationRequest();

            var callbackChannel = OperationContext.Current.GetCallbackChannel<ISessionCallback>();
            Guid reporterUserID = SessionService.Instance.GetPlayerIdByCallback(callbackChannel);

            if (reporterUserID == Guid.Empty)
            {
                result.IsSuccess = false;
                result.StatusCode = StatusCode.UNAUTHORIZED;
                return result;
            }

            try
            {
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
                    case 4: banDuration = TimeSpan.FromHours(1); break;
                    case 6: banDuration = TimeSpan.FromHours(8); break;
                    case 8: banDuration = TimeSpan.FromHours(12); break;
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
                    SessionService.Instance.KickUser(reportedUserID, kickReason);

                    result.IsSuccess = true;
                    result.StatusCode = StatusCode.USER_KICKED_AND_BANNED;
                }
                else
                {
                    result.IsSuccess = true;
                    result.StatusCode = StatusCode.REPORT_CREATED;
                }
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