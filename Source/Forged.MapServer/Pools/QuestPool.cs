// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Pools;

public class QuestPool
{
    public uint PoolId;
    public uint NumActive;
    public MultiMap<uint, uint> Members = new();
    public List<uint> ActiveQuests = new();
}