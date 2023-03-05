using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.DREAM_BREATH, EvokerSpells.DREAM_BREATH_2, EvokerSpells.ETERNITY_SURGE, EvokerSpells.ETERNITY_SURGE_2,
        EvokerSpells.FIRE_BREATH, EvokerSpells.FIRE_BREATH, EvokerSpells.SPIRITBLOOM, EvokerSpells.SPIRITBLOOM_2)]
    public class apell_evoker_animosity : SpellScript, ISpellAfterHit
    {
        public void AfterHit()
        {
            var player = GetCaster().ToPlayer();

            if (player != null && player.HasSpell(EvokerSpells.ANIMOSITY) 
                && player.TryGetAura(EvokerSpells.DRAGONRAGE, out var aura))
            {
                aura.ModDuration(GetEffectInfo(0).BasePoints);
            }
        }
    }
}
