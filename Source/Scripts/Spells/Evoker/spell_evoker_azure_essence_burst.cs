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
    [SpellScript(EvokerSpells.AZURE_STRIKE)]
    public class spell_evoker_azure_essence_burst : SpellScript, ISpellAfterHit
    {
        public void AfterHit()
        {
            var player = GetCaster().ToPlayer();

            if (player != null && player.HasSpell(EvokerSpells.AZURE_ESSENCE_BURST))
                player.AddAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
        }
    }
}
