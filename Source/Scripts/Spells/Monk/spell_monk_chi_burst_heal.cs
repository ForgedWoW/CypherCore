// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(130654)]
public class SpellMonkChiBurstHeal : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHeal(int effIndex)
    {
        var caster = Caster;
        var unit = HitUnit;

        if (caster == null || unit == null)
            return;

        var spellInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.CHI_BURST_HEAL, Difficulty.None);

        if (spellInfo == null)
            return;

        var effectInfo = spellInfo.GetEffect(0);

        if (!effectInfo.IsEffect())
            return;

        var damage = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 4.125f;
        damage = caster.SpellDamageBonusDone(unit, spellInfo, damage, DamageEffectType.Heal, effectInfo, 1, Spell);
        damage = unit.SpellDamageBonusTaken(caster, spellInfo, damage, DamageEffectType.Heal);

        HitHeal = damage;
    }
}