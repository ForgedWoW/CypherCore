// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

[SpellScript(327193)] // 327193 - Moment of Glory
internal class SpellPalMomentOfGlory : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        Caster.SpellHistory.ResetCooldown(PaladinSpells.AVENGERS_SHIELD);
    }
}