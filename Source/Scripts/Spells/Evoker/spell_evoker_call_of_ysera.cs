using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.VERDANT_EMBRACE_HEAL)]
    public class spell_evoker_call_of_ysera : SpellScript, ISpellAfterCast
    {
        public void AfterCast()
        {
            if (GetCaster().TryGetAsPlayer(out var player))
                player.AddAura(EvokerSpells.CALL_OF_YSERA_AURA);
        }
    }
}
