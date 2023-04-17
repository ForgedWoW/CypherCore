// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(25912)] // 25912 - Holy Shock
internal class SpellPalHolyShockDamageVisual : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        Caster.SendPlaySpellVisual(HitUnit, IsHitCrit ? SpellVisual.HOLY_SHOCK_DAMAGE_CRIT : SpellVisual.HOLY_SHOCK_DAMAGE, 0, 0, 0.0f, false);
    }
}