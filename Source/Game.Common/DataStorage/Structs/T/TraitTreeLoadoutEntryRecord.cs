using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.T;

public sealed class TraitTreeLoadoutEntryRecord
{
	public uint Id;
	public int TraitTreeLoadoutID;
	public int SelectedTraitNodeID;
	public int SelectedTraitNodeEntryID;
	public int NumPoints;
	public int OrderIndex;
}
