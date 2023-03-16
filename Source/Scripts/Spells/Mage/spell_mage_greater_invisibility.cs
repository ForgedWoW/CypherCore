using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(110959)]
public class spell_mage_greater_invisibility : SpellScript, ISpellOnCast
{
    
    public void OnCast()
    {
        var caster = Caster;
        if (caster.TryGetAura(MageSpells.INCANTATION_OF_SWIFTNESS, out var incantation))
        {
            caster.CastSpell(caster, 382294, (0.4 * incantation.GetEffect(0).Amount), false);
        }
    }
}

