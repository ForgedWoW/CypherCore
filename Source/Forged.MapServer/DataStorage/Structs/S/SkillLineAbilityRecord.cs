// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SkillLineAbilityRecord
{
    public string AbilityAllVerb;
    public string AbilityVerb;
    public AbilityLearnType AcquireMethod;
    public int ClassMask;
    public SkillLineAbilityFlags Flags;
    public uint Id;
    public short MinSkillLineRank;
    public byte NumSkillUps;
    public long RaceMask;
    public ushort SkillLine;
    public ushort SkillupSkillLineID;
    public uint Spell;
    public uint SupercedesSpell;
    public short TradeSkillCategoryID;
    public ushort TrivialSkillLineRankHigh;
    public ushort TrivialSkillLineRankLow;
    public short UniqueBit;
}