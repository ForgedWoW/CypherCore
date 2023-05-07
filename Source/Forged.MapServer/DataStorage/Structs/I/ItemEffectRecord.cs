// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemEffectRecord
{
    public int CategoryCoolDownMSec;
    public short Charges;
    public ushort ChrSpecializationID;
    public int CoolDownMSec;
    public uint Id;
    public byte LegacySlotIndex;
    public ushort SpellCategoryID;
    public int SpellID;
    public ItemSpelltriggerType TriggerType;
}