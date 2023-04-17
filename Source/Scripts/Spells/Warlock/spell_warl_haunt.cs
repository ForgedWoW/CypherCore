// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[Script] // 48181 - Haunt
internal class SpellWarlHaunt : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var aura = GetHitAura();

        if (aura != null)
        {
            var aurEff = aura.GetEffect(1);

            aurEff?.SetAmount(MathFunctions.CalculatePct(HitDamage, aurEff.Amount));
        }
    }
}