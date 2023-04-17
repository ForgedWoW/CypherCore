// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Warlock;

//204730 - Fear (effect)
[SpellScript(118699)]
public class AuraWarlFearBuff : AuraScript, IAuraOnRemove
{
    public void AuraRemoved(AuraRemoveMode removeMode)
    {
        if (Caster.TryGetAura(WarlockSpells.NIGHTMARE, out var ability))
            Caster.SpellFactory.CastSpell(Target, WarlockSpells.NIGHTMARE_DEBUFF, ability.GetEffect(0).Amount);
    }
}