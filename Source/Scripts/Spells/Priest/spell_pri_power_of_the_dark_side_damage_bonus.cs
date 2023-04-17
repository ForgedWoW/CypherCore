// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 47666 - Penance (Damage)
internal class SpellPriPowerOfTheDarkSideDamageBonus : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.LaunchTarget));
    }

    private void HandleLaunchTarget(int effIndex)
    {
        var powerOfTheDarkSide = Caster.GetAuraEffect(PriestSpells.POWER_OF_THE_DARK_SIDE, 0);

        if (powerOfTheDarkSide != null)
        {
            PreventHitDefaultEffect(effIndex);

            var damageBonus = Caster.SpellDamageBonusDone(HitUnit, SpellInfo, (uint)EffectValue, DamageEffectType.SpellDirect, EffectInfo, 1, Spell);
            var value = damageBonus + damageBonus * EffectVariance;
            value *= 1.0f + (powerOfTheDarkSide.Amount / 100.0f);
            value = HitUnit.SpellDamageBonusTaken(Caster, SpellInfo, (uint)value, DamageEffectType.SpellDirect);
            HitDamage = (int)value;
        }
    }
}