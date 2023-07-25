using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemNameDescriptionRecord
{
    public uint Id;
    public LocalizedString Description;
    public int Color;
}