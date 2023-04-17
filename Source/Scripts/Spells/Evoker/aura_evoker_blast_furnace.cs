// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH_CHARGED)]
internal class AuraEvokerBlastFurnace : AuraScript, IAuraOnApply
{
    public void AuraApply()
    {
        if (!Owner.AsUnit.TryGetAura(EvokerSpells.BLAST_FURNACE, out var bfAura))
            return;

        Aura.ModDuration(bfAura.GetEffect(0).Amount * 1000, true, true);
    }
}