// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpellAuraOptionsRecord
{
	public uint Id;
	public byte DifficultyID;
	public ushort CumulativeAura;
	public uint ProcCategoryRecovery;
	public byte ProcChance;
	public int ProcCharges;
	public ushort SpellProcsPerMinuteID;
	public int[] ProcTypeMask = new int[2];
	public uint SpellID;
}