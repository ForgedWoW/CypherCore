// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgQueueData
{
    public string BestCompatible = "";
    public byte Dps;
    public List<uint> Dungeons;
    public byte Healers;
    public long JoinTime;
    public Dictionary<ObjectGuid, LfgRoles> Roles;
    public byte Tanks;

    public LfgQueueData()
    {
        JoinTime = GameTime.CurrentTime;
        Tanks = SharedConst.LFGTanksNeeded;
        Healers = SharedConst.LFGHealersNeeded;
        Dps = SharedConst.LFGDPSNeeded;
    }

    public LfgQueueData(long joinTime, List<uint> dungeons, Dictionary<ObjectGuid, LfgRoles> roles)
    {
        JoinTime = joinTime;
        Tanks = SharedConst.LFGTanksNeeded;
        Healers = SharedConst.LFGHealersNeeded;
        Dps = SharedConst.LFGDPSNeeded;
        Dungeons = dungeons;
        Roles = roles;
    }
}