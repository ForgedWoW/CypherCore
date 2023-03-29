// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(33110)]
public class spell_pri_prayer_of_mending_heal : SpellScript, IHasSpellEffects
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
            var aurEff = caster.GetAuraEffect(PriestSpells.T9_HEALING_2P, 0);

            if (aurEff != null)
            {
                var heal = HitHeal;
                MathFunctions.AddPct(ref heal, aurEff.Amount);
                HitHeal = heal;
            }
        }
    }
}