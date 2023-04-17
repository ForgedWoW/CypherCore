// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(17)] // 17 - Power Word: Shield
internal class SpellPriPowerWordShield : SpellScript, ISpellCheckCast, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (target != null)
            if (!caster.HasAura(PriestSpells.RAPTURE))
                caster.SpellFactory.CastSpell(target, PriestSpells.WEAKENED_SOUL, true);
    }


    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (target != null)
            if (!caster.HasAura(PriestSpells.RAPTURE))
                if (target.HasAura(PriestSpells.WEAKENED_SOUL, caster.GUID))
                    return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }
}