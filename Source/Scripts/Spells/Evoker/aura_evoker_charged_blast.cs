using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.CALL_OF_YSERA_AURA)]
    public class aura_evoker_charged_blast : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo info)
        {
            return info.GetProcSpell().GetSpellInfo().Id.EqualsAny(EvokerSpells.AZURE_STRIKE, EvokerSpells.DISINTEGRATE,
                EvokerSpells.ETERNITY_SURGE_CHARGED, EvokerSpells.SHATTERING_STAR);
        }
    }
}
