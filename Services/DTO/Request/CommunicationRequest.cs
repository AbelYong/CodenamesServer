using System.Runtime.Serialization;

namespace Services.DTO.Request
{
    /// <summary>
    /// General request used when the Success or failure of the operation, (plus additional information through the stautus code)
    /// is the only information needed by the client
    /// </summary>
    [DataContract]
    public class CommunicationRequest : Request
    {
    }
}
