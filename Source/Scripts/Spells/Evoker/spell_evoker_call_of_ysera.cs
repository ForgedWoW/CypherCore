// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_VERDANT_EMBRACE_HEAL)]
public class SpellEvokerCallOfYsera : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.TryGetAsPlayer(out var player))
            player.AddAura(EvokerSpells.CALL_OF_YSERA_AURA);
    }
}