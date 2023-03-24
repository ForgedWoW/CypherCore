// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.Common.DataStorage.Structs.G;

public sealed class GarrSiteLevelPlotInstRecord
{
	public uint Id;
	public Vector2 UiMarkerPos;
	public ushort GarrSiteLevelID;
	public byte GarrPlotInstanceID;
	public byte UiMarkerSize;
}
