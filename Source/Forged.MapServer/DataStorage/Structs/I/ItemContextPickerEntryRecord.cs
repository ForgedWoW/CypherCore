namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemContextPickerEntryRecord
{
    public uint Id;
    public byte ItemCreationContext;
    public byte OrderIndex;
    public int PVal;
    public int LabelID;
    public uint Flags;
    public uint PlayerConditionID;
    public uint ItemContextPickerID;
}