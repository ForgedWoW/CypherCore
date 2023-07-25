namespace Forged.MapServer.DataStorage.Structs.MetaStructs;

public class TaxiPathBySourceAndDestination
{
    public TaxiPathBySourceAndDestination(uint _id, uint _price)
    {
        Id = _id;
        price = _price;
    }

    public uint Id;
    public uint price;
}