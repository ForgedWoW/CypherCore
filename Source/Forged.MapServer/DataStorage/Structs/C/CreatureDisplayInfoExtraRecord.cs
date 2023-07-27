namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CreatureDisplayInfoExtraRecord
{
    public uint Id;
    public sbyte DisplayRaceID;
    public sbyte DisplaySexID;
    public sbyte DisplayClassID;
    public sbyte Flags;
    public int BakeMaterialResourcesID;
    public int HDBakeMaterialResourcesID;
}