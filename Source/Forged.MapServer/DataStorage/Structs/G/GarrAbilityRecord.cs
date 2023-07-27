using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrAbilityRecord
{
    public uint Id;
    public string Name;
    public string Description;
    public byte GarrAbilityCategoryID;
    public sbyte GarrFollowerTypeID;
    public int IconFileDataID;
    public ushort FactionChangeGarrAbilityID;
    public GarrisonAbilityFlags Flags;
}