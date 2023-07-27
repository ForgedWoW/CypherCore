using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChrSpecializationRecord
{
    public LocalizedString Name;
    public string FemaleName;
    public string Description;
    public uint Id;
    public byte ClassID;
    public byte OrderIndex;
    public sbyte PetTalentType;
    public sbyte Role;
    public ChrSpecializationFlag Flags;
    public int SpellIconFileID;
    public sbyte PrimaryStatPriority;
    public int AnimReplacements;
    public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];

    public bool IsPetSpecialization()
    {
        return ClassID == 0;
    }
}