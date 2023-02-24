// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [SpellScript(5740)] // 5740 - Rain of Fire Updated 7.1.5
    internal class spell_warl_rain_of_fire_SpellScript : SpellScript, ISpellOnCast
    {
        public void OnCast()
        {
            if (!TryGetCaster(out Unit caster))
                return;

            caster.RemoveAura(WarlockSpells.RITUAL_OF_RUIN_FREE_CAST_AURA);
            caster.RemoveAuraApplicationCount(WarlockSpells.CRASHING_CHAOS_AURA);
            MadnessOfTheAzjaqir(caster);
            BurnToAshes(caster);
        }

        private void MadnessOfTheAzjaqir(Unit caster)
        {
            if (caster.HasAura(WarlockSpells.MADNESS_OF_THE_AZJAQIR))
                caster.AddAura(WarlockSpells.MADNESS_OF_THE_AZJAQIR_RAIN_OF_FIRE_AURA, caster);
        }

        private void BurnToAshes(Unit caster)
        {
            if (caster.TryGetAura(WarlockSpells.BURN_TO_ASHES, out var burnToAshes))
                for (int i = 0; i != burnToAshes.GetEffect(2).AmountAsInt; i++)
                    caster.AddAura(WarlockSpells.BURN_TO_ASHES_INCINERATE);
        }
    }
}