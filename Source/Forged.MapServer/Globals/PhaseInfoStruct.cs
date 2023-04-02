// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;

namespace Forged.MapServer.Globals;

public class PhaseInfoStruct
{
    public List<uint> Areas = new();
    public uint Id;
    public PhaseInfoStruct(uint id)
    {
        Id = id;
    }

    public bool IsAllowedInArea(uint areaId)
    {
        return Areas.Any(areaToCheck => Global.DB2Mgr.IsInArea(areaId, areaToCheck));
    }
}