using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.LIVING_FLAME_DAMAGE, EvokerSpells.LIVING_FLAME_HEAL)]
    public class spell_evoker_ruby_essence_burst : SpellScript, ISpellAfterHit
    {
        public void AfterHit()
        {
            if (GetCaster().TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.RUBY_ESSENCE_BURST))
                player.AddAura(EvokerSpells.AZURE_RUBY_ESSENCE_BURST_AURA);
        }
    }
}
