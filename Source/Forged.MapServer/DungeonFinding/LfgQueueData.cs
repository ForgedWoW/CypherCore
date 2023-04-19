// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgQueueData
{
    public string BestCompatible { get; set; } = "";
    public byte Dps { get; set; }
    public List<uint> Dungeons { get; set; }
    public byte Healers { get; set; }
    public long JoinTime { get; set; }
    public Dictionary<ObjectGuid, LfgRoles> Roles { get; set; }
    public byte Tanks { get; set; }

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