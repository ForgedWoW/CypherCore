// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

public class BossBoundaryEntry
{
    public BossBoundaryEntry(uint bossId, AreaBoundary boundary)
    {
        BossId = bossId;
        Boundary = boundary;
    }

    public uint BossId { get; set; }
    public AreaBoundary Boundary { get; set; }
}