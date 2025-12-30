namespace Services.DTO.Request
{
    public enum StatusCode
    {
        //General code meaning the request was successful
        OK,
        //Means the requested object was created sucessfully
        CREATED,
        //Means a request succesfully updated an object
        UPDATED,
        //Means a request was rejected because a resource or identificator is in use by another user
        CONFLICT,
        //Means a request failed because one client requested to cancel the operation
        CLIENT_CANCEL,
        //Means a request failed because the requester client could not be reached
        CLIENT_DISCONNECT,
        //Means a request failed because a client, who is not the requester, could  not be reached
        CLIENT_UNREACHABLE,
        //Means a request failed because one client didn't answer in time
        CLIENT_TIMEOUT,
        //General code that means a request cannot be fulfilled because critical data was not provided
        MISSING_DATA,
        //General code that means a request cannot be fulfilled because the provided data is flawed
        WRONG_DATA,
        //General code that means something requested by the client could not be found
        NOT_FOUND,
        //Means the user does not fulfill the requirements to execute the procedure
        UNAUTHORIZED,
        //Means the request could not be fulfilled because a rule would be infringed
        UNALLOWED,
        //Means a request failed due to a server side error, such as an exception
        SERVER_ERROR,
        //(Used on the client side) Means the server failed to fulfill a request for an unknown reason
        SERVER_UNAVAIBLE,
        //(Used on the client side) Means the server failed to respond within the timeout
        SERVER_TIMEOUT,
        //(Used on the client side) Means the server couldn't be found by the client
        SERVER_UNREACHABLE,
        //(Used on the client side) Means the request failed due to a client-side exception 
        CLIENT_ERROR,
        //Means that someone tried to log in and is banned.
        ACCOUNT_BANNED,
        //Means a report was successful
        REPORT_CREATED,
        //Means that the user to be reported has already been reported before (non-repudiation)
        REPORT_DUPLICATED,
        //Means the user was reported and the accumulation resulted in an immediate ban (Kick + Ban)
        USER_KICKED_AND_BANNED,
        FRIEND_REQUEST_SENT,
        FRIEND_ADDED,
        FRIEND_REMOVED,
        FRIEND_REQUEST_REJECTED,
        ALREADY_FRIENDS,
        FRIEND_REQUEST_ALREADY_SENT
    }
}
