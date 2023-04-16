// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Pools;

public class QuestPool
{
    public List<uint> ActiveQuests { get; set; } = new();
    public MultiMap<uint, uint> Members { get; set; } = new();
    public uint NumActive { get; set; }
    public uint PoolId { get; set; }
}