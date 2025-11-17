namespace Services.DTO
{
    public enum StatusCode
    {
        //General code meaning the request was successful
        OK,
        //Means the requested object was created sucessfully
        CREATED,
        //Means a request succesfully updated an object
        UPDATED,
        CONFLICT, 
        //Means a request failed because the requester client could not be reached
        CLIENT_DISCONNECT,
        //Means a request failed because a client, who is not the requester, could  not be reached
        CLIENT_UNREACHABLE,
        //General code that means a request cannot be fulfilled because critical data was not provided
        MISSING_DATA,
        //General code that means a request cannot be fulfilled because the provided data is flawed
        WRONG_DATA,
        //General code that means something requested by the client could not be found
        NOT_FOUND,
        UNAUTHORIZED,
        //Means the request could not be fulfilled because a rule would be infringed
        UNALLOWED,
        SERVER_ERROR,
        SERVER_UNAVAIBLE,
        SERVER_TIMEOUT,
        //Means that someone tried to log in and is banned.
        ACCOUNT_BANNED,
        //Means a report was successful
        REPORT_CREATED,
        //Means that the user to be reported has already been reported before (non-repudiation)
        REPORT_DUPLICATED,
        //Means the user was reported and the accumulation resulted in an immediate ban (Kick + Ban)
        USER_KICKED_AND_BANNED
    }
}
