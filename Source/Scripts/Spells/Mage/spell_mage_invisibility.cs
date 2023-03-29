// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(66)]
public class spell_mage_invisibility : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster.TryGetAura(MageSpells.INCANTATION_OF_SWIFTNESS, out var incantation))
            caster.CastSpell(caster, 382294, incantation.GetEffect(0).Amount, false);
    }
}