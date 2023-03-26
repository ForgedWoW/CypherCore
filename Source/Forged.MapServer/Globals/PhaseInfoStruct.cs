using System.Collections.Generic;
using System.Linq;

namespace Forged.MapServer.Globals;

public class PhaseInfoStruct
{
    public uint Id;
    public List<uint> Areas = new();

    public PhaseInfoStruct(uint id)
    {
        Id = id;
    }

    public bool IsAllowedInArea(uint areaId)
    {
        return Areas.Any(areaToCheck => Global.DB2Mgr.IsInArea(areaId, areaToCheck));
    }
}