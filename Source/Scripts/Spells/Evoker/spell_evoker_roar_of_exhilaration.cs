// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.QUELL)]
internal class spell_evoker_roar_of_exhilaration : SpellScript, ISpellOnSucessfulInterrupt
{
    public void SucessfullyInterrupted(Spell spellInterrupted)
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.ROAR_OF_EXHILARATION))
            player.CastSpell(player, EvokerSpells.ROAR_OF_EXHILARATION_ENERGIZE, TriggerCastFlags.TriggeredAllowProc);
    }
}