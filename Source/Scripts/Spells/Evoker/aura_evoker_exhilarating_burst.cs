// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ESSENCE_BURST_AURA)]
public class aura_evoker_exhilarating_burst : AuraScript, IAuraOnApply
{
    public void AuraApplied()
    {
        if (TryGetCasterAsPlayer(out var player) && player.HasSpell(EvokerSpells.EXHILERATING_BURST))
            player.AddAura(EvokerSpells.EXHILERATING_BURST_AURA);
    }
}