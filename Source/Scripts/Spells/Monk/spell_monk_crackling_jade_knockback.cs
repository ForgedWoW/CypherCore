// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(117962)]
public class spell_monk_crackling_jade_knockback : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var target = HitUnit;
        var caster = Caster;

        if (caster != null && target != null && caster.HasAura(CracklingJade.CRACKLING_JADE_LIGHTNING_TALENT))
            caster.CastSpell(target, CracklingJade.CRACKLING_JAD_LIGHTNING_TALENT_SPEED, true);
    }
}