// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UiMapLinkRecord
{
	public Vector2 UiMin;
	public Vector2 UiMax;
	public uint Id;
	public int ParentUiMapID;
	public int OrderIndex;
	public int ChildUiMapID;
	public int PlayerConditionID;
	public int OverrideHighlightFileDataID;
	public int OverrideHighlightAtlasID;
	public int Flags;
}