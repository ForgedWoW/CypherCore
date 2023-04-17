// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(25914)] // 25914 - Holy Shock
internal class SpellPalHolyShockHealVisual : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        Caster.SendPlaySpellVisual(HitUnit, IsHitCrit ? SpellVisual.HOLY_SHOCK_HEAL_CRIT : SpellVisual.HOLY_SHOCK_HEAL, 0, 0, 0.0f, false);
    }
}