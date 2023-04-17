// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

//232670
[SpellScript(232670)]
public class SpellWarrShadowboltAffliction : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        if (caster.HasAura(WarlockSpells.SHADOW_EMBRACE))
            caster.AddAura(WarlockSpells.SHADOW_EMBRACE_TARGET_DEBUFF, target);
    }
}