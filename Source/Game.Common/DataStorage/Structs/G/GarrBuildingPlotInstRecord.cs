﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.G;

public sealed class GarrBuildingPlotInstRecord
{
	public Vector2 MapOffset;
	public uint Id;
	public byte GarrBuildingID;
	public ushort GarrSiteLevelPlotInstID;
	public ushort UiTextureAtlasMemberID;
}
