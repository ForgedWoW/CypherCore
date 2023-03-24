// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.I;

public sealed class ItemSearchNameRecord
{
	public uint Id;
	public long AllowableRace;
	public string Display;
	public byte OverallQualityID;
	public int ExpansionID;
	public ushort MinFactionID;
	public int MinReputation;
	public int AllowableClass;
	public sbyte RequiredLevel;
	public ushort RequiredSkill;
	public ushort RequiredSkillRank;
	public uint RequiredAbility;
	public ushort ItemLevel;
	public int[] Flags = new int[4];
}
