// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.BLUE_SOURCE_OF_MAGIC)]
public class AuraEvokerRegenerativeMagic : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo info)
    {
        var owner = Aura.OwnerAsUnit;

        if (!owner.TryGetAura(EvokerSpells.REGENERATIVE_MAGIC, out var rmAura) || !owner.HealthBelowPct(rmAura.GetEffect(1).Amount))
            return;

        owner.SpellFactory.CastSpell(owner, EvokerSpells.REGENERATIVE_MAGIC_HEAL, info.HealInfo.Heal * (rmAura.GetEffect(0).Amount * 0.01));
    }
}