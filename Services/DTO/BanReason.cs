using System.Runtime.Serialization;

namespace Services.DTO
{
    [DataContract]
    public enum BanReason
    {
        [EnumMember]
        TemporaryBan,

        [EnumMember]
        PermanentBan
    }
}