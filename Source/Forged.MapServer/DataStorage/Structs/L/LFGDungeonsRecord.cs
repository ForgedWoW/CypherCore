// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LFGDungeonsRecord
{
    public ushort BonusReputationAmount;
    public uint ContentTuningID;
    public byte CountDamage;
    public byte CountHealer;
    public byte CountTank;
    public string Description;
    public Difficulty DifficultyID;
    public byte ExpansionLevel;
    public sbyte Faction;
    public ushort FinalEncounterID;
    public LfgFlags[] Flags = new LfgFlags[2];
    public byte GroupID;
    public int IconTextureFileID;
    public uint Id;
    public short MapID;
    public byte MentorCharLevel;
    public ushort MentorItemLevel;
    public byte MinCountDamage;
    public byte MinCountHealer;
    public byte MinCountTank;
    public float MinGear;
    public LocalizedString Name;
    public byte OrderIndex;
    public int PopupBgTextureFileID;
    public ushort RandomID;
    public uint RequiredPlayerConditionId;
    public int RewardsBgTextureFileID;
    public ushort ScenarioID;
    public sbyte Subtype;
    public LfgType TypeID;
    // Helpers
    public uint Entry()
    {
        return (uint)(Id + ((int)TypeID << 24));
    }
}