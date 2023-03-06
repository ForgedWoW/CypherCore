using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.DREAM_BREATH, EvokerSpells.DREAM_BREATH_2)]
    internal class spell_evoker_dream_breath : SpellScript, ISpellOnEpowerSpellEnd
    {
        public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
        {
            GetCaster().CastSpell(EvokerSpells.DREAM_BREATH_CHARGED, true, stage.Stage);
        }
    }
}
