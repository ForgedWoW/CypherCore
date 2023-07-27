namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemLimitCategoryConditionRecord
{
    public uint Id;
    public sbyte AddQuantity;
    public uint PlayerConditionID;
    public uint ParentItemLimitCategoryID;
}