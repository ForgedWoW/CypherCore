using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LFGDungeonsRecord
{
    public uint Id;
    public LocalizedString Name;
    public string Description;
    public LfgType TypeID;
    public sbyte Subtype;
    public sbyte Faction;
    public int IconTextureFileID;
    public int RewardsBgTextureFileID;
    public int PopupBgTextureFileID;
    public byte ExpansionLevel;
    public short MapID;
    public Difficulty DifficultyID;
    public float MinGear;
    public byte GroupID;
    public byte OrderIndex;
    public uint RequiredPlayerConditionId;
    public ushort RandomID;
    public ushort ScenarioID;
    public ushort FinalEncounterID;
    public byte CountTank;
    public byte CountHealer;
    public byte CountDamage;
    public byte MinCountTank;
    public byte MinCountHealer;
    public byte MinCountDamage;
    public ushort BonusReputationAmount;
    public ushort MentorItemLevel;
    public byte MentorCharLevel;
    public uint ContentTuningID;
    public LfgFlags[] Flags = new LfgFlags[2];

    // Helpers
    public uint Entry() { return (uint)(Id + ((int)TypeID << 24)); }
}