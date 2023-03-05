using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using static Game.AI.SmartEvent;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.FIRE_BREATH_CHARGED)]
    internal class aura_evoker_blast_furnace : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public void AfterApply(AuraEffect aura, AuraEffectHandleModes auraMode)
        {
            if (!GetOwner().ToUnit().TryGetAura(EvokerSpells.BLAST_FURNACE, out var bfAura))
                return;

            GetAura().SetDuration(GetAura().GetMaxDuration() + (bfAura.GetEffect(0).Amount * 1000), true, true);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        }
    }
}
