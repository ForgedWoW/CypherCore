// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(new uint[]
{
    1850, 5215, 102280
})]
public class SpellDruActivateCatForm : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (!caster.HasAura(ShapeshiftFormSpells.CatForm))
            caster.SpellFactory.CastSpell(caster, ShapeshiftFormSpells.CatForm, true);
    }
}