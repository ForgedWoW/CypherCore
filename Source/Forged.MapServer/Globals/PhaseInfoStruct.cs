// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;

namespace Forged.MapServer.Globals;

public class PhaseInfoStruct
{
    private readonly DB2Manager _db2Manager;

    public PhaseInfoStruct(uint id, DB2Manager db2Manager)
    {
        _db2Manager = db2Manager;
        Id = id;
    }

    public List<uint> Areas { get; set; } = new();
    public uint Id { get; set; }

    public bool IsAllowedInArea(uint areaId)
    {
        return Areas.Any(areaToCheck => _db2Manager.IsInArea(areaId, areaToCheck));
    }
}