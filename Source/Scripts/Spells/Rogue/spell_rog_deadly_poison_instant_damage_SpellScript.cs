// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(2823)]
public class spell_rog_deadly_poison_instant_damage_SpellScript : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        var _player = Caster.AsPlayer;

        if (_player != null)
        {
            var target = ExplTargetUnit;

            if (target != null)
                if (target.HasAura(RogueSpells.DEADLY_POISON_DOT, _player.GUID))
                    _player.CastSpell(target, RogueSpells.DEADLY_POISON_INSTANT_DAMAGE, true);
        }
    }
}