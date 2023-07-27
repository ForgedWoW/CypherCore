namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TraitCostRecord
{
    public string InternalName;
    public uint Id;
    public int Amount;
    public int TraitCurrencyID;
}