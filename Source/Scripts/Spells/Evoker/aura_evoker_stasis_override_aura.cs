// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS_OVERRIDE_AURA)]
internal class AuraEvokerStasisOverrideAura : AuraScript, IAuraOnRemove, IAuraScriptValues
{
    public Dictionary<string, object> ScriptValues { get; } = new();

    public void AuraRemoved(AuraRemoveMode removeMode)
    {
        if (removeMode == AuraRemoveMode.Expire)
        {
            if (!Caster.TryGetAsPlayer(out var player))
                return;

            player.RemoveAura(EvokerSpells.STASIS_ORB_AURA_1);
            player.RemoveAura(EvokerSpells.STASIS_ORB_AURA_2);
            player.RemoveAura(EvokerSpells.STASIS_ORB_AURA_3);
        }
    }
}