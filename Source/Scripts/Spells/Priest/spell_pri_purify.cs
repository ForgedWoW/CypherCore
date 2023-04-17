// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(527)]
public class SpellPriPurify : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster != target && target.IsFriendlyTo(caster))
            return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(AfterEffectHit, 0, SpellEffectName.Dispel, SpellScriptHookType.EffectHitTarget));
    }

    private void AfterEffectHit(int effIndex)
    {
        if (HitUnit.IsFriendlyTo(Caster))
        {
            Caster.SpellFactory.CastSpell(HitUnit, PriestSpells.DISPEL_MAGIC_HOSTILE, true);
            Caster.SpellFactory.CastSpell(HitUnit, PriestSpells.CURE_DISEASE, true);
        }
    }
}