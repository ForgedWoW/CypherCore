using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.CAUSALITY)]
    public class aura_evoker_causality : AuraScript, IAuraCheckProc
    {
        public bool CheckProc(ProcEventInfo info)
        {
            var id = info.GetProcSpell().GetSpellInfo().Id;
            return id.EqualsAny(EvokerSpells.DISINTEGRATE, EvokerSpells.ECHO, EvokerSpells.PYRE) 
                || (id == EvokerSpells.EMERALD_BLOSSOM && GetCaster().TryGetAsPlayer(out var player) 
                && player.HasSpell(EvokerSpells.IMPROVED_EMERALD_BLOSSOM));
        }
    }
}
