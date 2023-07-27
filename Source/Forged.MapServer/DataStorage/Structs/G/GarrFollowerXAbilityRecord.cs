namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrFollowerXAbilityRecord
{
    public uint Id;
    public byte OrderIndex;
    public byte FactionIndex;
    public ushort GarrAbilityID;
    public uint GarrFollowerID;
}