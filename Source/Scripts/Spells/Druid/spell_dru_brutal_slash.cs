// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(202028)]
public class SpellDruBrutalSlash : SpellScript, ISpellOnHit
{
    private bool _awardComboPoint = true;

    public void OnHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        // This prevent awarding multiple Combo Points when multiple targets hit with Brutal Slash AoE
        if (_awardComboPoint)
            // Awards the caster 1 Combo Point (get value from the spell data)
            caster.ModifyPower(PowerType.ComboPoints, Global.SpellMgr.GetSpellInfo(DruidSpells.SwipeCat, Difficulty.None).GetEffect(0).BasePoints);

        _awardComboPoint = false;
    }
}