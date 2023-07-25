namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemChildEquipmentRecord
{
    public uint Id;
    public uint ParentItemID;
    public uint ChildItemID;
    public byte ChildItemEquipSlot;
}