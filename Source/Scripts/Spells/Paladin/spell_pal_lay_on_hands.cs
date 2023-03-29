// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(633)] // 633 - Lay on Hands
internal class spell_pal_lay_on_hands : SpellScript, ISpellCheckCast, ISpellAfterHit
{
    public void AfterHit()
    {
        var target = HitUnit;

        if (target)
        {
            Caster.CastSpell(target, PaladinSpells.Forbearance, true);
            Caster.CastSpell(target, PaladinSpells.ImmuneShieldMarker, true);
        }
    }

    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit;

        if (!target ||
            target.HasAura(PaladinSpells.Forbearance))
            return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }
}