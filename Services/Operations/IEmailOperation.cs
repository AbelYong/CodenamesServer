using Services.DTO.DataContract;

namespace Services.Operations
{
    public interface IEmailOperation
    {
        string GenerateSixDigitCode();
        bool SendVerificationEmail(string toAddress, string code, EmailType type);
        bool SendGameInvitationEmail(string fromUsername, string toAddress, string lobbyCode);
        bool ValidateEmailFormat(string email);
    }
}
