// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(8936)]
public class SpellDruRegrowth : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHealEffect, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHealEffect(int effIndex)
    {
        if (Caster.HasAura(DruidSpells.Bloodtalons))
            Caster.AddAura(DruidSpells.BloodtalonsTriggered, Caster);

        var clearcasting = Caster.GetAura(DruidSpells.Clearcasting);

        if (clearcasting != null)
        {
            if (Caster.HasAura(DruidSpells.MomentOfClarity))
            {
                var amount = clearcasting.GetEffect(0).Amount;
                clearcasting.GetEffect(0).SetAmount(amount - 1);

                if (amount == -102)
                    Caster.RemoveAura(DruidSpells.Clearcasting);
            }
            else
            {
                Caster.RemoveAura(DruidSpells.Clearcasting);
            }
        }
    }
}