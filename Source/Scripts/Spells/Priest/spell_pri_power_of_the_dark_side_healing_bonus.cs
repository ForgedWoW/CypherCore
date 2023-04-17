// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 47750 - Penance (Healing)
internal class SpellPriPowerOfTheDarkSideHealingBonus : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleLaunchTarget, 0, SpellEffectName.Heal, SpellScriptHookType.LaunchTarget));
    }

    private void HandleLaunchTarget(int effIndex)
    {
        var powerOfTheDarkSide = Caster.GetAuraEffect(PriestSpells.POWER_OF_THE_DARK_SIDE, 0);

        if (powerOfTheDarkSide != null)
        {
            PreventHitDefaultEffect(effIndex);

            var healingBonus = Caster.SpellHealingBonusDone(HitUnit, SpellInfo, (uint)EffectValue, DamageEffectType.Heal, EffectInfo, 1, Spell);
            var value = healingBonus + healingBonus * EffectVariance;
            value *= 1.0f + (powerOfTheDarkSide.Amount / 100.0f);
            value = HitUnit.SpellHealingBonusTaken(Caster, SpellInfo, (uint)value, DamageEffectType.Heal);
            HitHeal = (int)value;
        }
    }
}