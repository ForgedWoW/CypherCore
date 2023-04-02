// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Globals;

namespace Forged.MapServer.Phasing;

public struct VisibleMapIdRef
{
    public int References;

    public TerrainSwapInfo VisibleMapInfo;

    public VisibleMapIdRef(int references, TerrainSwapInfo visibleMapInfo)
    {
        References = references;
        VisibleMapInfo = visibleMapInfo;
    }
}