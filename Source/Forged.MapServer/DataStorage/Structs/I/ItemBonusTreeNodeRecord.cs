namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemBonusTreeNodeRecord
{
    public uint Id;
    public byte ItemContext;
    public ushort ChildItemBonusTreeID;
    public ushort ChildItemBonusListID;
    public ushort ChildItemLevelSelectorID;
    public uint ChildItemBonusListGroupID;
    public uint IblGroupPointsModSetID;
    public int MinMythicPlusLevel;
    public int MaxMythicPlusLevel;
    public uint ParentItemBonusTreeID;
}