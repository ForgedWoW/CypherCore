// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrSiteLevelPlotInstRecord
{
    public byte GarrPlotInstanceID;
    public ushort GarrSiteLevelID;
    public uint Id;
    public Vector2 UiMarkerPos;
    public byte UiMarkerSize;
}