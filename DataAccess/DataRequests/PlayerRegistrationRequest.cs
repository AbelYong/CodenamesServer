using System;

namespace DataAccess.DataRequests
{
    public class PlayerRegistrationRequest : DataRequest
    {
        public Guid? NewPlayerID { get; set; }
        public bool IsUsernameDuplicate { get; set; }
        public bool IsEmailDuplicate { get; set; }
        public bool IsEmailValid { get; set; }
        public bool IsPasswordValid { get; set; }
    }
}
