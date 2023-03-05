using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.FEL_ARMOR_DMG_DELAY)]
    internal class aura_warl_fel_armor : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

        void PeriodicDamage(AuraEffect eff)
        {
            if (!TryGetCasterAsPlayer(out var player) || !player.TryGetAura(WarlockSpells.FEL_ARMOR_DMG_DELAY_REMAINING, out var felArmor))
                return;

            if (felArmor.TryGetEffect(1, out var remaining))
            {
                var rem = remaining.Amount - eff.Amount;

                if (rem <= 0)
                {
                    player.RemoveAura(felArmor);
                    player.RemoveAura(eff.GetBase());
                }
                else
                    remaining.SetAmount(rem);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicDamage, 0, Framework.Constants.AuraType.PeriodicDamage));
        }
    }
}
