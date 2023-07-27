namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemAppearanceRecord
{
    public uint Id;
    public int DisplayType;
    public uint ItemDisplayInfoID;
    public int DefaultIconFileDataID;
    public int UiOrder;
    public int PlayerConditionID;
}