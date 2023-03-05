using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.ANCIENT_FLAME)]
    public class aura_evoker_ancient_flame : AuraScript, IAuraOnProc
    {
        public void OnProc(ProcEventInfo info)
        {
            if (GetCaster() == GetTarget())
                GetCaster().AddAura(EvokerSpells.ANCIENT_FLAME_AURA);
        }
    }
}
