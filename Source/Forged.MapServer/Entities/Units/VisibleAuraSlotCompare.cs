// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Spells.Auras;

namespace Forged.MapServer.Entities.Units;

internal class VisibleAuraSlotCompare : IComparer<AuraApplication>
{
    public int Compare(AuraApplication x, AuraApplication y)
    {
        return x.Slot.CompareTo(y.Slot);
    }
}