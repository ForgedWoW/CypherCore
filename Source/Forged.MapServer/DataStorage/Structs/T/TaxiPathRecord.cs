namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TaxiPathRecord
{
    public uint Id;
    public ushort FromTaxiNode;
    public ushort ToTaxiNode;
    public uint Cost;
}