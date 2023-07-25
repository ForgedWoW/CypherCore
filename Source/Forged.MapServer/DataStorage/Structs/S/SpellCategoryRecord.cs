using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellCategoryRecord
{
    public uint Id;
    public string Name;
    public SpellCategoryFlags Flags;
    public byte UsesPerWeek;
    public byte MaxCharges;
    public int ChargeRecoveryTime;
    public int TypeMask;
}