using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

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
