// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenEjectAllPassengers : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var vehicle = HitUnit.VehicleKit1;

        if (vehicle)
            vehicle.RemoveAllPassengers();
    }
}