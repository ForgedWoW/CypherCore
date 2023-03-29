// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(197908)]
public class spell_monk_mana_tea : SpellScript, ISpellAfterCast, ISpellBeforeCast
{
    private readonly SpellModifier mod = null;

    public void AfterCast()
    {
        if (mod != null)
        {
            var _player = Caster.AsPlayer;

            if (_player != null)
                _player.AddSpellMod(mod, false);
        }
    }

    public void BeforeCast()
    {
        var _player = Caster.AsPlayer;

        if (_player != null)
        {
            var stacks = 0;

            var manaTeaStacks = _player.GetAura(MonkSpells.MANA_TEA_STACKS);

            if (manaTeaStacks != null)
            {
                stacks = manaTeaStacks.StackAmount;

                var newDuration = stacks * Time.InMilliseconds;


                var mod = new SpellModifierByClassMask(manaTeaStacks);
                mod.Op = SpellModOp.Duration;
                mod.Type = SpellModType.Flat;
                mod.SpellId = MonkSpells.MANA_TEA_REGEN;
                ((SpellModifierByClassMask)mod).Value = newDuration;
                mod.Mask[1] = 0x200000;
                mod.Mask[2] = 0x1;

                _player.AddSpellMod(mod, true);
            }
        }
    }
}