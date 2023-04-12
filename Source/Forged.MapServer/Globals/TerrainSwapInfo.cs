// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class TerrainSwapInfo
{
    public TerrainSwapInfo() { }

    public TerrainSwapInfo(uint id)
    {
        Id = id;
    }

    public uint Id { get; set; }
    public List<uint> UiMapPhaseIDs { get; set; } = new();
}