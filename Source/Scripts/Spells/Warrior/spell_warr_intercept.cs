// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

// Intercept (As of Legion) - 198304
[SpellScript(198304)]
public class spell_warr_intercept : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var target = ExplTargetUnit;
        var pos = target.Location;

        if (caster.GetDistance(pos) < 8.0f && !caster.IsFriendlyTo(target))
            return SpellCastResult.TooClose;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (target == null)
            return;

        if (target.IsFriendlyTo(caster))
        {
            caster.CastSpell(target, WarriorSpells.INTERVENE_TRIGGER, true);
        }
        else
        {
            caster.CastSpell(target, WarriorSpells.CHARGE_EFFECT, true);

            if (caster.HasAura(WarriorSpells.WARBRINGER))
                caster.CastSpell(target, WarriorSpells.WARBRINGER_ROOT, true);
            else
                caster.CastSpell(target, WarriorSpells.INTERCEPT_STUN, true);
        }
    }
}