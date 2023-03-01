using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.FIRE_BREATH)]
    internal class spell_evoker_fire_breath : SpellScript, ISpellOnEpowerSpellEnd
    {
        public void EmpowerSpellEnd(SpellEmpowerStageRecord stage, uint stageDelta)
        {
            GetCaster().CastSpell(EvokerSpells.FIRE_BREATH_CHARGED, true);
        }
    }
}
