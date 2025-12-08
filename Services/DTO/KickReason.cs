using System.Runtime.Serialization;

namespace Services.DTO
{
    [DataContract]
    public enum KickReason
    {
        [EnumMember]
        TEMPORARY_BAN,

        [EnumMember]
        PERMANTENT_BAN,

        [EnumMember]
        DUPLICATE_LOGIN
    }
}