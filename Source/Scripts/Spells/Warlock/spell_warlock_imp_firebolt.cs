// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// 3110 - Firebolt
[SpellScript(3110)]
public class spell_warlock_imp_firebolt : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || !caster.OwnerUnit || target == null)
            return;

        var owner = caster.OwnerUnit;
        var damage = HitDamage;

        if (target.HasAura(WarlockSpells.IMMOLATE_DOT, owner.GUID))
            MathFunctions.AddPct(ref damage, owner.GetAuraEffectAmount(WarlockSpells.FIREBOLT_BONUS, 0));

        HitDamage = damage;
    }
}