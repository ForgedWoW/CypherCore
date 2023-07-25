using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TraitCurrencyRecord
{
    public uint Id;
    public int Type;
    public int CurrencyTypesID;
    public int Flags;
    public int Icon;

    public TraitCurrencyType GetCurrencyType() { return (TraitCurrencyType)Type; }
}