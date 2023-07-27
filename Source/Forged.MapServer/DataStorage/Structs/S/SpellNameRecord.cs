using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellNameRecord
{
    public uint Id; // SpellID
    public LocalizedString Name;
}