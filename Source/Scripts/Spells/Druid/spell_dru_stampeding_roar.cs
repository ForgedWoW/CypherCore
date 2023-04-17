// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 106898 - Stampeding Roar
internal class SpellDruStampedingRoar : SpellScript, ISpellBeforeCast
{
    public void BeforeCast()
    {
        // Change into cat form
        if (Caster.ShapeshiftForm != ShapeShiftForm.BearForm)
            Caster.SpellFactory.CastSpell(Caster, DruidSpellIds.BearForm, true);
    }
}