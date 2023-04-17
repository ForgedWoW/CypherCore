// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(110959)]
public class SpellMageGreaterInvisibility : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster.TryGetAura(MageSpells.INCANTATION_OF_SWIFTNESS, out var incantation))
            caster.SpellFactory.CastSpell(caster, 382294, (0.4 * incantation.GetEffect(0).Amount), false);
    }
}