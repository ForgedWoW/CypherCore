using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker
{
    [SpellScript(EvokerSpells.DISINTEGRATE)]
    public class spell_evoker_catalyze : SpellScript, ISpellOnHit, ISpellAfterCast
    {
        public void OnHit()
        {
            if (GetCaster().TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.CATALYZE))
                if (GetHitUnit().TryGetAura(EvokerSpells.FIRE_BREATH_CHARGED, out var aura))
                {
                    var eff = aura.GetEffect(1);
                    _period = eff.Period;
                    eff.Period = _period / 2;
                }
        }

        public void AfterCast()
        {
            if (GetCaster().TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.CATALYZE))
                if (GetHitUnit().TryGetAura(EvokerSpells.FIRE_BREATH_CHARGED, out var aura))
                {
                    aura.GetEffect(1).Period = _period;
                }
        }

        int _period = 0;
    }
}
