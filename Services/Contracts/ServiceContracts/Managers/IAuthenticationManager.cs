using System.ServiceModel;
using Services.DTO.Request;

namespace Services.Contracts.ServiceContracts.Managers
{
    [ServiceContract]
    public interface IAuthenticationManager
    {
        /// <summary>
        /// Checks if the username and password match for an existing player
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>An AuthenticatioRequest. IsSuccess == True and the user's UserID if the credentials match,
        /// otherwise IsSucess == false, a null UserID and one of the following StatusCode:
        /// <para>UNAUTHORIZED if the credentials don't match</para>
        /// <para>SERVER_ERROR if the authentication couldn't proceed due to a database error</para>
        /// <para>ACCOUNT_BANNED if the credentials match, but the user currently has an active ban</para>
        /// </returns>
        [OperationContract]
        AuthenticationRequest Authenticate(string username, string password);

        /// <summary>
        /// Allows an user to recover access to their account, a Password Reset must be requested to the EmailService
        /// for this method to be sucessful, 
        /// </summary>
        /// <param name="email">The email address to which the verification code was sent</param>
        /// <param name="code">The code sent to the provided address</param>
        /// <param name="newPassword">A new, policy-compliant password for the account</param>
        /// <returns> A PassworResetRequest, IsSuccess == True if the code matches the one sent to the email address,
        /// and the new password follows the security policy, otherwise IsSuccess == false
        /// and the number of RemaningAttempts along one of the following StatusCode:
        /// <para>NOT_FOUND if there's no pending password reset for the account. (Code expired, ran out of attempts or never requested)</para>
        /// <para>UNAUTHORIZED if the provided code is not correct </para>
        /// <para>MISSING_DATA if the database returned null after the update request (Fallback value, this code shouldn't ever be seen by the client)</para>
        /// <para>WRONG_DATA if the new password doesn't adhere to the security policies</para>
        /// <para>SERVER_ERROR if the password reset couldn't be completed due to a Database exception</para>
        /// </returns>
        [OperationContract]
        PasswordResetRequest CompletePasswordReset(string email, string code, string newPassword);

        /// <summary>
        /// Allows an user to update their password through re-authenticatication
        /// </summary>
        /// <param name="username">The player's current username</param>
        /// <param name="currentPassword">The player's current password</param>
        /// <param name="newPassword">A new, policy-compliant password for the account</param>
        /// <returns>
        /// A CommunicationRequest IsSuccess == True if the password was updated successfully,
        /// otherwise IsSuccess == False along one of the following StatusCode
        /// <para>WRONG_DATA if the newPassword doesn't adhere to the security policy</para>
        /// <para>NOT_FOUND if no email associated to the provided username was found</para>
        /// <para>UNAUTHORIZED if the credentials don't match</para>
        /// <para>SERVER_ERROR if the password update couldn't be completed due to a Database exception</para>
        /// </returns>
        [OperationContract]
        CommunicationRequest UpdatePassword(string username, string currentPassword, string newPassword);
    }
}
