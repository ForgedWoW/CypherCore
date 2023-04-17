// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(33110)]
public class SpellPriPrayerOfMendingHeal : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHeal(int effIndex)
    {
        var caster = OriginalCaster;

        if (caster != null)
        {
            var aurEff = caster.GetAuraEffect(PriestSpells.T9_HEALING_2_P, 0);

            if (aurEff != null)
            {
                var heal = HitHeal;
                MathFunctions.AddPct(ref heal, aurEff.Amount);
                HitHeal = heal;
            }
        }
    }
}