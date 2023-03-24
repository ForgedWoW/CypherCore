// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellScalingRecord
{
	public uint Id;
	public uint SpellID;
	public uint MinScalingLevel;
	public uint MaxScalingLevel;
	public ushort ScalesFromItemLevel;
}
