using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Scripts.Spells.Shaman;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.LIVING_FLAME_DAMAGE, EvokerSpells.LIVING_FLAME_HEAL)]
    public class spell_evoker_ruby_essence_burst : SpellScript, ISpellAfterHit
    {
        public void AfterHit()
        {
            var player = GetCaster().ToPlayer();

            if (player != null && player.HasSpell(EvokerSpells.RUBY_ESSENCE_BURST))
                player.AddAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
        }
    }
}
