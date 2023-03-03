using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.FIRE_BREATH_CHARGED)]
    internal class aura_evoker_fire_breath_charged : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            var caster = GetCaster();

            if (!caster.TryGetAura(EvokerSpells.FIRE_BREATH, out var fbAura))
                caster.TryGetAura(EvokerSpells.FIRE_BREATH_2, out fbAura);

            if (fbAura != null)
            {
                switch (fbAura.EmpowerStage)
                {
                    case 1:
                        GetAura().SetDuration(2000, true);
                        break;
                    case 2:
                        GetAura().SetDuration(8000, true);
                        break;
                    case 3:
                        GetAura().SetDuration(2000, true);
                        break;
                    default:
                        GetAura().SetDuration(20000, true);
                        break;
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(Apply, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        }
    }
}
