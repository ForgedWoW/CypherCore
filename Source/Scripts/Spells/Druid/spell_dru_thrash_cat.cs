// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(106830)]
public class SpellDruThrashCat : SpellScript, IHasSpellEffects
{
    private bool _mAwardComboPoint = true;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(EffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void EffectHitTarget(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        // This prevent awarding multiple Combo Points when multiple targets hit with Thrash AoE
        if (_mAwardComboPoint)
            // Awards the caster 1 Combo Point
            caster.ModifyPower(PowerType.ComboPoints, 1);

        _mAwardComboPoint = false;
    }
}