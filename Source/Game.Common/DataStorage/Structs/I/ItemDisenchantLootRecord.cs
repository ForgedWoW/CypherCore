// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.I;

public sealed class ItemDisenchantLootRecord
{
	public uint Id;
	public sbyte Subclass;
	public byte Quality;
	public ushort MinLevel;
	public ushort MaxLevel;
	public ushort SkillRequired;
	public sbyte ExpansionID;
	public uint Class;
}
