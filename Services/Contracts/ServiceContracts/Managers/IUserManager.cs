using System;
using System.ServiceModel;
using Services.DTO.DataContract;
using Services.DTO.Request;

namespace Services.Contracts.ServiceContracts.Managers
{
    /// <summary>
    /// Handles Getting, Creating and Updating Users and Players 
    /// </summary>
    [ServiceContract]
    public interface IUserManager
    {
        /// <summary>
        /// Gets the Player for the matching userID, used to get a player's profile post-authentication
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>The Player's profile including their User if the userID matches the one of a registered user,
        /// otherwise returns NULL, returns NULL in case of database exception</returns>
        [OperationContract]
        Player GetPlayerByUserID(Guid userID);

        /// <summary>
        /// Creates a new account
        /// </summary>
        /// <param name="svPlayer">The new Player (Mandatory: Username. Optional: Name, Last name)
        /// including their User (Mandatory email, and password)
        /// Email must be one of the accepted addresses. (@gmail.com, @outlook.com, @estudiantes.uv.mx)
        /// Password must adhere to the security policy
        /// </param>
        /// <returns>A SignInRequest, IsSuccess == True if the account was created, otherwise IsSuccess == False,
        /// along one of the following StatusCode and specific errors:
        /// <para>MISSING_DATA if svPlayer ot their User is null</para>
        /// <para>WRONG_DATA if one of the Player or User fields is invalid. (Check IsEmailValid or IsUsernameValid)</para>
        /// <para>UNALLOWED if the username or email is duplicated (Check IsEmailDuplicate or IsUsernameDuplicate)</para>
        /// <para>SERVER_ERROR if the registration failed due to Database exception</para>
        /// </returns>
        [OperationContract]
        SignInRequest SignIn(Player svPlayer);
        
        /// <summary>
        /// Allows an user to update their profile, which may include: Email, Username, AvatarID, Name, LastName, FacebookUsername, InstagramUsername, DiscordUsername
        /// </summary>
        /// <param name="updatedPlayer">A Player which must include the User's and their UserID</param>
        /// <returns>A CommunicationRequest IsSuccess == True if the update was successful.
        /// Otherwise Isucccess == False, along one of the following StatusCode:
        /// <para>SERVER_ERROR fi the update failed due to a Database Exception</para>
        /// <para>NOT_FOUND if the user or their player couldn't be found. (Provided PlayerID or UserID had no match)</para>
        /// <para>WRONG_DATA if any of the fields has the wrong format (Eg. Too long, unaccepted email)</para>
        /// <para>UNALLOWED if the new Username or Email is already in use by another user</para>
        /// </returns>
        [OperationContract]
        CommunicationRequest UpdateProfile(Player updatedPlayer);
    }
}
