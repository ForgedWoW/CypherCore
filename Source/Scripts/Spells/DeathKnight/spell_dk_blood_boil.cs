// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[Script] // 50842 - Blood Boil
internal class SpellDkBloodBoil : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        Caster.SpellFactory.CastSpell(HitUnit, DeathKnightSpells.BloodPlague, true);
    }
}