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
        UNAUTHORIZED,
        //Means the request could not be fulfilled because a rule would be infringed
        UNALLOWED,
        SERVER_ERROR,
        SERVER_UNAVAIBLE,
        SERVER_TIMEOUT
    }
}
