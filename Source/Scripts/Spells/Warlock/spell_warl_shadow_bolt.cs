// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(686)] // 686 - Shadow Bolt
internal class SpellWarlShadowBolt : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        Caster.SpellFactory.CastSpell(Caster, WarlockSpells.SHADOW_BOLT_SHOULSHARD, true);
    }
}