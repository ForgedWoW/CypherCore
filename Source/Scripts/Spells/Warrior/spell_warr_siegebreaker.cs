// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

//280772 - Siegebreaker
[SpellScript(280772)]
public class SpellWarrSiegebreaker : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        caster.SpellFactory.CastSpell(null, WarriorSpells.SIEGEBREAKER_BUFF, true);
    }
}