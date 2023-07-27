using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SkillLineRecord
{
    public LocalizedString DisplayName;
    public string AlternateVerb;
    public string Description;
    public string HordeDisplayName;
    public string OverrideSourceInfoDisplayName;
    public uint Id;
    public SkillCategory CategoryID;
    public int SpellIconFileID;
    public sbyte CanLink;
    public uint ParentSkillLineID;
    public int ParentTierIndex;
    public ushort Flags;
    public int SpellBookSpellID;
    public int ExpansionNameSharedStringID;
    public int HordeExpansionNameSharedStringID;

    public SkillLineFlags GetFlags() => (SkillLineFlags)Flags;
}