using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TransmogIllusionRecord
{
    public uint Id;
    public int UnlockConditionID;
    public int TransmogCost;
    public int SpellItemEnchantmentID;
    public int Flags;

    public TransmogIllusionFlags GetFlags() { return (TransmogIllusionFlags)Flags; }
}