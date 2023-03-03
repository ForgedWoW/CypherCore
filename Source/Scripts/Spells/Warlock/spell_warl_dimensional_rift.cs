using System.Collections.Generic;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.DIMENSIONAL_RIFT)]
    public class spell_warl_dimensional_rift : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new List<ISpellEffect>();

        private readonly List<uint> _spells = new List<uint>()
        {
            WarlockSpells.SHADOWY_TEAR,
            WarlockSpells.UNSTABLE_TEAR,
            WarlockSpells.CHAOS_TEAR
        };

        public void HandleScriptEffect(int effectIndex)
        {
            GetCaster().CastSpell(_spells.SelectRandom(), true);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, Framework.Constants.SpellEffectName.ScriptEffect, Framework.Constants.SpellScriptHookType.EffectHitTarget));
        }
    }
}
