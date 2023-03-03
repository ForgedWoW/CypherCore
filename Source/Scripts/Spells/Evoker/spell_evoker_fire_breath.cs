using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.FIRE_BREATH, EvokerSpells.FIRE_BREATH_2)]
    internal class spell_evoker_fire_breath : SpellScript, ISpellOnEpowerSpellEnd, ISpellOnEpowerSpellStageChange
    {
        public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
        {
            GetCaster().CastSpell(EvokerSpells.FIRE_BREATH_CHARGED, true);
        }

        public void EmpowerSpellStageChange(SpellEmpowerStageRecord oldStage, SpellEmpowerStageRecord newStage)
        {
            if (!GetCaster().TryGetAura(EvokerSpells.FIRE_BREATH, out var aura))
                GetCaster().TryGetAura(EvokerSpells.FIRE_BREATH_2, out aura);

            aura.EmpowerStage = newStage.Stage;
        }
    }
}
