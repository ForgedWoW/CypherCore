using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BattlePetSpeciesRecord
{
    public string Description;
    public string SourceText;
    public uint Id;
    public uint CreatureID;
    public uint SummonSpellID;
    public int IconFileDataID;
    public sbyte PetTypeEnum;
    public int Flags;
    public sbyte SourceTypeEnum;
    public int CardUIModelSceneID;
    public int LoadoutUIModelSceneID;
    public int CovenantID;

    public BattlePetSpeciesFlags GetFlags() { return (BattlePetSpeciesFlags)Flags; }
}