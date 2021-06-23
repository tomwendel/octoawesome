using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Network
{
    public enum OfficialCommand : ushort
    {
        //0 - 100 System Commands
        //100 - 200 General Commands
        Whoami = 101,
        GetUniverse = 102,
        GetPlanet = 103,
        LoadColumn = 104,
        SaveColumn = 105,
        //400 - 500 Notifications
        EntityNotification = 401,
        ChunkNotification = 402,
        FunctionalBlockNotification = 403
    }
}
