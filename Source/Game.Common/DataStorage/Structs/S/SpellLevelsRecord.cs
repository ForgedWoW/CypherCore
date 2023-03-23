using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellLevelsRecord
{
	public uint Id;
	public byte DifficultyID;
	public ushort MaxLevel;
	public byte MaxPassiveAuraLevel;
	public ushort BaseLevel;
	public ushort SpellLevel;
	public uint SpellID;
}
