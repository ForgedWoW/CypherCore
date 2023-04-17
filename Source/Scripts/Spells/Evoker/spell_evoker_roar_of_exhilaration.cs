// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.QUELL)]
internal class SpellEvokerRoarOfExhilaration : SpellScript, ISpellOnSucessfulInterrupt
{
    public void SucessfullyInterrupted(Spell spellInterrupted)
    {
        if (Caster.TryGetAsPlayer(out var player) && player.HasSpell(EvokerSpells.ROAR_OF_EXHILARATION))
            player.SpellFactory.CastSpell(player, EvokerSpells.ROAR_OF_EXHILARATION_ENERGIZE, TriggerCastFlags.TriggeredAllowProc);
    }
}