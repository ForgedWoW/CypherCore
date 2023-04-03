// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgQueueData
{
    public string bestCompatible = "";
    public byte dps;
    public List<uint> dungeons;
    public byte healers;
    public long joinTime;
    public Dictionary<ObjectGuid, LfgRoles> roles;
    public byte tanks;

    public LfgQueueData()
    {
        joinTime = GameTime.CurrentTime;
        tanks = SharedConst.LFGTanksNeeded;
        healers = SharedConst.LFGHealersNeeded;
        dps = SharedConst.LFGDPSNeeded;
    }

    public LfgQueueData(long _joinTime, List<uint> _dungeons, Dictionary<ObjectGuid, LfgRoles> _roles)
    {
        joinTime = _joinTime;
        tanks = SharedConst.LFGTanksNeeded;
        healers = SharedConst.LFGHealersNeeded;
        dps = SharedConst.LFGDPSNeeded;
        dungeons = _dungeons;
        roles = _roles;
    }
}