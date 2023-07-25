using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemEffectRecord
{
    public uint Id;
    public byte LegacySlotIndex;
    public ItemSpelltriggerType TriggerType;
    public short Charges;
    public int CoolDownMSec;
    public int CategoryCoolDownMSec;
    public ushort SpellCategoryID;
    public int SpellID;
    public ushort ChrSpecializationID;
}