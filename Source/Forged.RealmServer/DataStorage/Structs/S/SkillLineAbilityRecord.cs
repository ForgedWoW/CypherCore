// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class SkillLineAbilityRecord
{
	public long RaceMask;
	public string AbilityVerb;
	public string AbilityAllVerb;
	public uint Id;
	public ushort SkillLine;
	public uint Spell;
	public short MinSkillLineRank;
	public int ClassMask;
	public uint SupercedesSpell;
	public AbilityLearnType AcquireMethod;
	public ushort TrivialSkillLineRankHigh;
	public ushort TrivialSkillLineRankLow;
	public SkillLineAbilityFlags Flags;
	public byte NumSkillUps;
	public short UniqueBit;
	public short TradeSkillCategoryID;
	public ushort SkillupSkillLineID;
}