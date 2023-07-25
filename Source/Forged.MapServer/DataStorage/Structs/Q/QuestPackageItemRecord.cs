using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.Q;

public sealed class QuestPackageItemRecord
{
    public uint Id;
    public ushort PackageID;
    public uint ItemID;
    public byte ItemQuantity;
    public QuestPackageFilter DisplayType;
}