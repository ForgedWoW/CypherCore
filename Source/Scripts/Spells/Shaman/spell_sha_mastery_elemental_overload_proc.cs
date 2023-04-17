// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 285466 - Lava Burst Overload
[SpellScript(285466)]
internal class SpellShaMasteryElementalOverloadProc : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(ApplyDamageModifier, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void ApplyDamageModifier(int effIndex)
    {
        var elementalOverload = Caster.GetAuraEffect(ShamanSpells.MasteryElementalOverload, 1);

        if (elementalOverload != null)
            HitDamage = MathFunctions.CalculatePct(HitDamage, elementalOverload.Amount);
    }
}