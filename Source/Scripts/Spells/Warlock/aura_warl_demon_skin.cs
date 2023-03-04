using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.DEMON_SKIN)]
    internal class aura_warl_demon_skin : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

        void Periodic(AuraEffect eff)
        {

        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(Periodic, 0, AuraType.PeriodicDummy));
        }
    }
}
