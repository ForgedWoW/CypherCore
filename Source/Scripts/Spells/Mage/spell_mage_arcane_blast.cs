// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(30451)]
public class SpellMageArcaneBlast : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var caster = Caster;

        if (caster != null)
        {
            var threes = caster.GetAura(MageSpells.RULE_OF_THREES_BUFF);

            if (threes != null)
                threes.Remove();
        }
    }
}