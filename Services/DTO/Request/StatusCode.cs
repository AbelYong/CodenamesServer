using System.ComponentModel;

namespace Services.DTO.Request
{
    /// <summary>
    /// General Status codes used to provide more precise information to the client in case of a failed request
    /// </summary>
    public enum StatusCode
    {
        [Description("General code that means a request was fulfilled sucessfully")]
        OK,
        [Description("Means the requested object was created sucessfully")]
        CREATED,
        [Description("Means a request succesfully updated an object")]
        UPDATED,
        [Description("Means a request was rejected because a resource or identifier is in use by another user, entity or process")]
        CONFLICT,
        [Description("Means a request failed because one client requested to cancel the operation")]
        CLIENT_CANCEL,
        [Description("Means a request failed because the requester client could not be reached")]
        CLIENT_DISCONNECT,
        [Description("Means a request failed because a client, who is not the requester, could  not be reached")]
        CLIENT_UNREACHABLE,
        [Description("Means a request failed because one client didn't answer in time")]
        CLIENT_TIMEOUT,
        [Description("General code that means a request cannot be fulfilled because critical data was not provided")]
        MISSING_DATA,
        [Description("General code that means a request cannot be fulfilled because the provided data is somehow flawed")]
        WRONG_DATA,
        [Description("General code that means something requested, or needed to fulfill a request by the client could not be found")]
        NOT_FOUND,
        [Description("Means the user does not fulfill the requirements to execute the procedure")]
        UNAUTHORIZED,
        [Description("Means the request could not be fulfilled because a rule would be infringed")]
        UNALLOWED,
        [Description("Means a request failed due to a server side error, such as an exception")]
        SERVER_ERROR,
        [Description("Means a request failed due to an exception while accessing the database")]
        DATABASE_ERROR,
        [Description("(Used on the client side) Means the server failed to fulfill a request for an unknown reason")]
        SERVER_UNAVAIBLE,
        [Description("(Used on the client side) Means the server failed to respond within the timeout")]
        SERVER_TIMEOUT,
        [Description("(Used on the client side) Means the server couldn't be found by the client")]
        SERVER_UNREACHABLE,
        [Description("(Used on the client side) Means the request failed due to a client-side exception ")]
        CLIENT_ERROR,
        [Description("Means that someone tried to log in and is banned")]
        ACCOUNT_BANNED,
        [Description("Means a report was successfully created")]
        REPORT_CREATED,
        [Description("Means that the user to be reported has already been reported before")]
        REPORT_DUPLICATED,
        [Description("Means the user was reported and the accumulation resulted in an immediate ban")]
        USER_KICKED_AND_BANNED,
        FRIEND_REQUEST_SENT,
        FRIEND_ADDED,
        FRIEND_REMOVED,
        FRIEND_REQUEST_REJECTED,
        ALREADY_FRIENDS,
        FRIEND_REQUEST_ALREADY_SENT
    }
}
