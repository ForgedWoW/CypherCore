﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_RENEWING_BLAZE)]
public class AuraEvokerRenewingBlaze : AuraScript, IAuraOnApply
{
    public void AuraApply()
    {
        if (!Caster.TryGetAsPlayer(out var player) || !player.HasSpell(EvokerSpells.FOCI_OF_LIFE))
            Aura.GetEffect(2).SetAmount(0);
    }
}