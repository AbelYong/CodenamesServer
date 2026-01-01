
namespace Services.DTO.DataContract
{
    /// <summary>
    /// Used by EmailService to determine what type of code needs to be send/verified
    /// </summary>
    public enum EmailType
    {
        EMAIL_VERIFICATION,
        PASSWORD_RESET
    }
}
