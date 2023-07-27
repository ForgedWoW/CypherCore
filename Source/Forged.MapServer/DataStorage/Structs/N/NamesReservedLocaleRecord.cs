namespace Forged.MapServer.DataStorage.Structs.N;

public sealed record NamesReservedLocaleRecord
{
    public uint Id;
    public string Name;
    public byte LocaleMask;
}