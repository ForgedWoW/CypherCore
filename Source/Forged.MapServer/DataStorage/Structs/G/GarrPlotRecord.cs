namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrPlotRecord
{
    public uint Id;
    public string Name;
    public byte PlotType;
    public uint HordeConstructObjID;
    public uint AllianceConstructObjID;
    public byte Flags;
    public byte UiCategoryID;
    public uint[] UpgradeRequirement = new uint[2];
}