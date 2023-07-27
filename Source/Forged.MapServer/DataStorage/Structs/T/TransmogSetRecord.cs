namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TransmogSetRecord
{
    public string Name;
    public uint Id;
    public int ClassMask;
    public uint TrackingQuestID;
    public int Flags;
    public uint TransmogSetGroupID;
    public int ItemNameDescriptionID;
    public ushort ParentTransmogSetID;
    public byte Unknown810;
    public byte ExpansionID;
    public int PatchID;
    public short UiOrder;
    public uint PlayerConditionID;
}