// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemSearchNameRecord
{
    public int AllowableClass;
    public long AllowableRace;
    public string Display;
    public int ExpansionID;
    public int[] Flags = new int[4];
    public uint Id;
    public ushort ItemLevel;
    public ushort MinFactionID;
    public int MinReputation;
    public byte OverallQualityID;
    public uint RequiredAbility;
    public sbyte RequiredLevel;
    public ushort RequiredSkill;
    public ushort RequiredSkillRank;
}