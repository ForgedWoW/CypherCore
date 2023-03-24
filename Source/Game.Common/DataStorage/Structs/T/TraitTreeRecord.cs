// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.DataStorage.Structs.T;

public sealed class TraitTreeRecord
{
	public uint Id;
	public int TraitSystemID;
	public int Unused1000_1;
	public int FirstTraitNodeID;
	public int PlayerConditionID;
	public int Flags;
	public float Unused1000_2;
	public float Unused1000_3;

	public TraitTreeFlag GetFlags()
	{
		return (TraitTreeFlag)Flags;
	}
}
