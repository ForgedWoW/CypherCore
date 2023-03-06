using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.CALL_OF_YSERA_AURA)]
    public class aura_evoker_call_of_ysera : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo info)
        {
            return info.GetProcSpell().GetSpellInfo().Id.EqualsAny(EvokerSpells.DREAM_BREATH_CHARGED, EvokerSpells.LIVING_FLAME_HEAL);
        }
    }
}
