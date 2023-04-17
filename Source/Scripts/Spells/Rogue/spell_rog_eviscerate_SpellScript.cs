// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 196819 - Eviscerate
internal class SpellRogEviscerateSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(CalculateDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void CalculateDamage(int effIndex)
    {
        var damagePerCombo = HitDamage;
        var t5 = Caster.GetAuraEffect(RogueSpells.T52_P_SET_BONUS, 0);

        if (t5 != null)
            damagePerCombo += t5.Amount;

        var finalDamage = damagePerCombo;
        var costs = Spell.PowerCost;
        var c = costs.Find(cost => cost.Power == PowerType.ComboPoints);

        if (c != null)
            finalDamage *= c.Amount;

        HitDamage = finalDamage;
    }
}