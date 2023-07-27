namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellReagentsCurrencyRecord
{
    public uint Id;
    public int SpellID;
    public ushort CurrencyTypesID;
    public ushort CurrencyCount;
}