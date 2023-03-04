using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using static Game.Entities.GameObjectTemplate;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.DREAM_BREATH_CHARGED)]
    internal class aura_evoker_dream_breath_charged : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            switch (GetAura().EmpoweredStage)
            {
                case 1:
                    GetAura().SetDuration(12000, true);
                    break;
                case 2:
                    GetAura().SetDuration(8000, true);
                    break;
                case 3:
                    GetAura().SetDuration(4000, true);
                    break;
                default:
                    GetAura().SetDuration(16000, true);
                    break;
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(Apply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        }
    }
}
