using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.DREAM_BREATH_CHARGED)]
    internal class aura_evoker_dream_breath_charged : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            var aur = GetAura();

            switch (aur.EmpoweredStage)
            {
                case 1:
                    aur.SetMaxDuration(12000);
                    aur.SetDuration(12000, true);
                    break;
                case 2:
                    aur.SetMaxDuration(8000);
                    aur.SetDuration(8000, true);
                    break;
                case 3:
                    aur.SetMaxDuration(4000);
                    aur.SetDuration(4000, true);
                    break;
                default:
                    aur.SetMaxDuration(16000);
                    aur.SetDuration(16000, true);
                    break;
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(Apply, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        }
    }
}
