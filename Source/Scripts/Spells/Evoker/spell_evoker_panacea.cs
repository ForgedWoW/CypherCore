// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_BLOSSOM)]
internal class SpellEvokerPanacea : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (TryGetCaster(out Player player) && player.HasSpell(EvokerSpells.PANACEA))
            player.SpellFactory.CastSpell(player, EvokerSpells.PANACEA_HEAL, TriggerCastFlags.TriggeredAllowProc);
    }
}