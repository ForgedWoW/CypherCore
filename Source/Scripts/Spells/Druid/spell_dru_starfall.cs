// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(191034)]
public class SpellDruStarfall : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (Caster)
            if (Caster.GetAuraCount(DruidSpells.StarlordBuff) < 3)
                Caster.SpellFactory.CastSpell(null, DruidSpells.StarlordBuff, true);
    }
}