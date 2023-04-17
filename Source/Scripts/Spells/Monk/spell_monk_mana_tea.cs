// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(197908)]
public class SpellMonkManaTea : SpellScript, ISpellAfterCast, ISpellBeforeCast
{
    private readonly SpellModifier _mod = null;

    public void AfterCast()
    {
        if (_mod != null)
        {
            var player = Caster.AsPlayer;

            if (player != null)
                player.AddSpellMod(_mod, false);
        }
    }

    public void BeforeCast()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var stacks = 0;

            var manaTeaStacks = player.GetAura(MonkSpells.MANA_TEA_STACKS);

            if (manaTeaStacks != null)
            {
                stacks = manaTeaStacks.StackAmount;

                var newDuration = stacks * Time.IN_MILLISECONDS;


                var mod = new SpellModifierByClassMask(manaTeaStacks);
                mod.Op = SpellModOp.Duration;
                mod.Type = SpellModType.Flat;
                mod.SpellId = MonkSpells.MANA_TEA_REGEN;
                ((SpellModifierByClassMask)mod).Value = newDuration;
                mod.Mask[1] = 0x200000;
                mod.Mask[2] = 0x1;

                player.AddSpellMod(mod, true);
            }
        }
    }
}