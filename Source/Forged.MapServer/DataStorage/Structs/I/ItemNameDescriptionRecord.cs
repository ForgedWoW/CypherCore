using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemNameDescriptionRecord
{
    public uint Id;
    public LocalizedString Description;
    public int Color;
}