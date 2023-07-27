namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BannedAddonsRecord
{
    public uint Id;
    public string Name;
    public string Version;
    public byte Flags;
}