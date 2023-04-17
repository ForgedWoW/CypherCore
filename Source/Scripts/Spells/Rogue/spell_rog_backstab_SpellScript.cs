// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 53 - Backstab
internal class SpellRogBackstabSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHitDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHitDamage(int effIndex)
    {
        var hitUnit = HitUnit;

        if (!hitUnit)
            return;

        var caster = Caster;

        if (hitUnit.IsInBack(caster))
        {
            var currDamage = (double)HitDamage;
            MathFunctions.AddPct(ref currDamage, (double)GetEffectInfo(3).CalcValue(caster));
            HitDamage = (int)currDamage;
        }
    }
}