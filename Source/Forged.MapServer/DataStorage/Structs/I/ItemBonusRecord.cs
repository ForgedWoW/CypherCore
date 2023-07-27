using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemBonusRecord
{
    public uint Id;
    public int[] Value = new int[4];
    public ushort ParentItemBonusListID;
    public ItemBonusType BonusType;
    public byte OrderIndex;
}