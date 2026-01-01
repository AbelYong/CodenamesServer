using Services.DTO.Request;
using Services.DTO.DataContract;
using System.ServiceModel;

namespace Services.Contracts.ServiceContracts.Managers
{
    /// <summary>
    /// Handles the sending and reception of verification codes
    /// </summary>
    [ServiceContract]
    public interface IEmailManager
    {
        /// <summary>
        /// Sends a six-digit verification code (wether for account verification or password recovery) to the provided email address
        /// </summary>
        /// <param name="email">The address to which the code will be sent. Must be one of the following domains: 
        /// gmail.com, outlook.com, estudiantes.uv.mx</param>
        /// <param name="emailType">EMAIL_VERIFICATION to request an account verification code, PASSWORD_RESET to request a password reset code</param>
        /// <returns>A CommunicationRequest, IsSuccess == True if the email was successfuly sent, otherwise False along one of the following StatusCode:
        /// <para>WRONG_DATA if the email address is not one of the allowed domains</para>
        /// <para>UNALLOWED if the requested address is already in use, or the use of the address couldn't be verified. (Only applies to EmailType.EMAIL_VERIFICATION)</para>
        /// <para>NOT_FOUND if the requested address doesn't belong to a registered user, or the use of the address couldn't be verified (Only applies to EmailType.PASSWORD_RESET)</para>
        /// <para>SERVER_ERROR if the email couldn't be sent due to a server-side issue</para>
        /// </returns>
        [OperationContract]
        CommunicationRequest SendVerificationCode(string email, EmailType emailType);

        /// <summary>
        /// Allows the client to attempt verify a code sent to their email.
        /// </summary>
        /// <param name="email">The address to which the code will be sent. Must be one of the following domains: 
        /// gmail.com, outlook.com, estudiantes.uv.mx</param>
        /// <param name="code">The six-digit code sent to the provided address</param>
        /// <param name="emailType">EMAIL_VERIFICATION to confirm an account verification, PASSWORD_RESET to authorize a password reset</param>
        /// <returns>A CommunicationRequest, IsSuccess == True if the email was successfuly sent, otherwise False along one of the following StatusCode:
        /// <para>NOT_FOUND if there's no code pending verification associated to the email (Ran out of attempts, code expired or was never requested) Or the email doesn't belong to a registered user.</para>
        /// <para>UNAUTHORIZED if the provided code doesn't match the code sent or the client has ran out of attempts</para>
        /// <para></para>
        /// </returns>
        [OperationContract]
        ConfirmEmailRequest ValidateVerificationCode(string email, string code, EmailType emailType);
    }
}
