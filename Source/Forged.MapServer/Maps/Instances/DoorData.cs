// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

public class DoorData
{
    public DoorData(uint entry, uint bossid, DoorType doorType)
    {
        Entry = entry;
        BossId = bossid;
        Type = doorType;
    }

    public uint BossId { get; set; }
    public uint Entry { get; set; }
    public DoorType Type { get; set; }
}