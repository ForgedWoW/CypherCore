using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed class TransmogIllusionRecord
{
    public uint Id;
    public int UnlockConditionID;
    public int TransmogCost;
    public int SpellItemEnchantmentID;
    public int Flags;

    public TransmogIllusionFlags GetFlags() { return (TransmogIllusionFlags)Flags; }
}