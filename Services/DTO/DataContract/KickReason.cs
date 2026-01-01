using System.Runtime.Serialization;

namespace Services.DTO.DataContract
{
    /// <summary>
    /// Specifies the reason why an user has been expulsed from the Server
    /// </summary>
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