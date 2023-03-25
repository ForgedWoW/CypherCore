// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class TraitEdgeRecord
{
	public uint Id;
	public int VisualStyle;
	public int LeftTraitNodeID;
	public int RightTraitNodeID;
	public int Type;
}