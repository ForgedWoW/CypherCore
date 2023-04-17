// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 33395 Water Elemental's Freeze
internal class SpellMageWaterElementalFreeze : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var owner = Caster.OwnerUnit;

        if (!owner)
            return;

        owner.SpellFactory.CastSpell(owner, MageSpells.FingersOfFrost, true);
    }
}