using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.SOUL_LEECH_ABSORB)]
    internal class aura_warl_soul_leech_absorb : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

        private double OnAbsorb(AuraEffect effect, DamageInfo dmgInfo, double amount)
        {
            if (TryGetCasterAsPlayer(out var player) && player.TryGetAura(WarlockSpells.FEL_ARMOR, out var felArmor))
            {

            }

            return amount;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
        }
    }
}
