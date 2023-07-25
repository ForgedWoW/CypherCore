using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CharTitlesRecord
{
    public uint Id;
    public LocalizedString Name;
    public LocalizedString Name1;
    public ushort MaskID;
    public sbyte Flags;
}