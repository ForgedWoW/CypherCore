// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[Script] // 252216 - Tiger Dash
internal class spell_dru_tiger_dash : SpellScript, ISpellBeforeCast
{
    public void BeforeCast()
    {
        // Change into cat form
        if (Caster.ShapeshiftForm != ShapeShiftForm.CatForm)
            Caster.CastSpell(Caster, DruidSpellIds.CatForm, true);
    }
}