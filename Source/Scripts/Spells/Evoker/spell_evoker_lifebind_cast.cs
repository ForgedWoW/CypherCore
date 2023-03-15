// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BRONZE_GOLDEN_HOUR_HEAL, 
                EvokerSpells.CYCLE_OF_LIFE_HEAL,
                EvokerSpells.FLUTTERING_SEEDLINGS_HEAL,
                EvokerSpells.GREEN_EMERALD_BLOSSOM_HEAL,
                EvokerSpells.GREEN_VERDANT_EMBRACE_HEAL,
                EvokerSpells.LIFE_GIVERS_FLAME_HEAL,
                EvokerSpells.RED_LIVING_FLAME_HEAL,
                EvokerSpells.GREEN_DREAM_BREATH_CHARGED,
                EvokerSpells.SPIRITBLOOM_CHARGED,
                EvokerSpells.RED_CAUTERIZING_FLAME)]
internal class spell_evoker_lifebind_cast : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster;

        if (!caster.HasSpell(EvokerSpells.LIFEBIND) || !caster.TryGetAura(EvokerSpells.LIFEBIND_AURA, out var aura))
            return;

        if (Spell.HealingInEffects == 0)
            return;

        var heal = Spell.HealingInEffects * (SpellManager.Instance.GetSpellInfo(EvokerSpells.LIFEBIND).GetEffect(0).BasePoints * 0.01);

        aura.ForEachAuraScript<IAuraScriptValues>(a =>
        {
            if (a.ScriptValues.TryGetValue("target", out var val))
            {
                var otherTarget = (Unit)val;

                if (ExplTargetUnit == caster)
                    caster.CastSpell(otherTarget, EvokerSpells.LIFEBIND_HEAL, heal);
                else if(ExplTargetUnit == otherTarget)
                    caster.CastSpell(caster, EvokerSpells.LIFEBIND_HEAL, heal);
            }
        });
    }
}