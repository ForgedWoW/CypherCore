// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH_CHARGED)]
internal class spell_evoker_life_givers_flame : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;

        if (caster.HasSpell(EvokerSpells.LIFE_GIVERS_FLAME))
        {
            AuraEffect aurEff = null;

            if (!caster.TryGetAura(EvokerSpells.LIFE_GIVERS_FLAME_AURA, out var aura))
            {
                aura = caster.AddAura(EvokerSpells.LIFE_GIVERS_FLAME_AURA);
                aurEff = aura.GetEffect(0);
                aurEff.SetAmount(0);
            }
            else
            {
                aurEff = aura.GetEffect(0);
            }

            var maxHits = SpellManager.Instance.GetSpellInfo(EvokerSpells.LIFE_GIVERS_FLAME).GetEffect(1).BasePoints;
            var update = false;

            aura.ForEachAuraScript<IAuraScriptValues>(a =>
            {
                if (a.ScriptValues.TryGetValue("LIFE_GIVERS_FLAME_AURA hits", out var val))
                {
                    var intVal = (int)val;

                    if (intVal < maxHits)
                    {
                        intVal++;
                        a.ScriptValues["LIFE_GIVERS_FLAME_AURA hits"] = intVal;
                        update = true;
                    }
                }
            });

            if (update)
                aurEff.ModAmount(Spell.DamageInEffects);
        }
    }
}