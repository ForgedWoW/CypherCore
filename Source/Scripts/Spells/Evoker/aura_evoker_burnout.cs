using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.BURNOUT)]
    public class aura_evoker_burnout : AuraScript, IAuraCheckProc, IAuraOnProc
    {
        public bool CheckProc(ProcEventInfo info)
        {
            return info.GetProcSpell().GetSpellInfo().Id == EvokerSpells.FIRE_BREATH_CHARGED;
        }

        public void OnProc(ProcEventInfo info)
        {
            GetCaster().AddAura(EvokerSpells.BURNOUT_AURA);
        }
    }
}
