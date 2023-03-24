// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellCategoriesRecord
{
	public uint Id;
	public byte DifficultyID;
	public ushort Category;
	public sbyte DefenseType;
	public sbyte DispelType;
	public sbyte Mechanic;
	public sbyte PreventionType;
	public ushort StartRecoveryCategory;
	public ushort ChargeCategory;
	public uint SpellID;
}
