using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO
{
    public enum StatusCode
    {
        OK,
        CREATED,
        UPDATED,
        MISSING_DATA,
        WRONG_DATA,
        UNAUTHORIZED,
        SERVER_ERROR,
        SERVER_UNAVAIBLE,
    }
}
