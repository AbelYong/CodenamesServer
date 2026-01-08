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

        public override bool Equals(object obj)
        {
            if (obj is  PlayerRegistrationRequest other)
            {
                return
                    IsSuccess.Equals(other.IsSuccess) &&
                    ErrorType.Equals(other.ErrorType) &&
                    NewPlayerID.Equals(other.NewPlayerID) &&
                    IsUsernameDuplicate.Equals(other.IsUsernameDuplicate) &&
                    IsEmailDuplicate.Equals(other.IsEmailDuplicate) &&
                    IsEmailValid.Equals(other.IsEmailValid) &&
                    IsPasswordValid.Equals(other.IsPasswordValid);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return new
            { 
                IsSuccess, ErrorType, NewPlayerID, IsUsernameDuplicate, IsEmailDuplicate, IsEmailValid, IsPasswordValid
            }.GetHashCode();
        }
    }
}
