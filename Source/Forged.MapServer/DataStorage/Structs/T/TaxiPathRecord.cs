namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TaxiPathRecord
{
    public uint Id;
    public ushort FromTaxiNode;
    public ushort ToTaxiNode;
    public uint Cost;
}