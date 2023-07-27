namespace Forged.MapServer.DataStorage.Structs.M;

public sealed record MountTypeXCapabilityRecord
{
    public uint Id;
    public ushort MountTypeID;
    public ushort MountCapabilityID;
    public byte OrderIndex;
}