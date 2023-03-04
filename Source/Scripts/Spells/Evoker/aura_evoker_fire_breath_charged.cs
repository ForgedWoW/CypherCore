using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using static Game.AI.SmartEvent;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.FIRE_BREATH_CHARGED)]
    internal class aura_evoker_fire_breath_charged : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            var aur = GetAura();

            switch (aur.EmpoweredStage)
            {
                case 1:
                    aur.SetMaxDuration(14000);
                    aur.SetDuration(14000, true);
                    break;
                case 2:
                    aur.SetMaxDuration(8000);
                    aur.SetDuration(8000, true);
                    break;
                case 3:
                    aur.SetMaxDuration(2000);
                    aur.SetDuration(2000, true);
                    break;
                default:
                    aur.SetMaxDuration(20000);
                    aur.SetDuration(20000, true);
                    break;
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(Apply, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        }
    }
}
