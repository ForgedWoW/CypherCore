// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(new uint[]
{
    47750, 47666
})]
public class spell_pri_penance_heal_damage : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        if (ScriptSpellId == PriestSpells.PENANCE_HEAL)
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));

        if (ScriptSpellId == PriestSpells.PENANCE_DAMAGE)
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        if (Caster.GetAuraEffect(PriestSpells.CONTRITION, 0) != null)
            foreach (var auApp in Caster.GetAppliedAurasQuery().HasSpellId(PriestSpells.ATONEMENT_AURA).GetResults())
                Caster.CastSpell(auApp.Target, PriestSpells.CONTRITION_HEAL, true);

        var powerOfTheDarkSide = Caster.GetAuraEffect(PriestSpells.POWER_OF_THE_DARK_SIDE_MARKER, 0);

        if (powerOfTheDarkSide != null)
        {
            if (SpellInfo.Id == PriestSpells.PENANCE_HEAL)
            {
                var heal = HitHeal;
                MathFunctions.AddPct(ref heal, powerOfTheDarkSide.Amount);
                HitHeal = heal;
            }
            else
            {
                var damage = HitDamage;
                MathFunctions.AddPct(ref damage, powerOfTheDarkSide.Amount);
                HitDamage = damage;
            }
        }
    }
}