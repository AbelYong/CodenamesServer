using DataAccess.DataRequests;
using DataAccess.Users; // Asegura tener acceso a la entidad Player
using System.Collections.Generic;

namespace DataAccess.DataRequests
{
    public class PlayerListRequest : DataRequest
    {
        public List<Player> Players { get; set; }

        public PlayerListRequest()
        {
            Players = new List<Player>();
        }
    }
}