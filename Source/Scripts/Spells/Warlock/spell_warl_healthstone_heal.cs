// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[Script] // 6262 - Healthstone
internal class SpellWarlHealthstoneHeal : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var heal = (int)MathFunctions.CalculatePct(Caster.GetCreateHealth(), HitHeal);
        HitHeal = heal;
    }
}