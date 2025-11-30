
namespace Services.Operations
{
    public interface IEmailOperation
    {
        string GenerateSixDigitCode();
        bool SendVerificationEmail(string toAddress, string code);
        bool SendGameInvitationEmail(string fromUsername, string toAddress, string lobbyCode);
    }
}
