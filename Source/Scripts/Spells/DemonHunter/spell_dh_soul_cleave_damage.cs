// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(228478)]
public class SpellDhSoulCleaveDamage : SpellScript, IHasSpellEffects, ISpellOnHit
{
    private readonly int _mExtraSpellCost = 0;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void OnHit()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var dmg = HitDamage * 2;
        dmg *= caster.VariableStorage.GetValue<double>("lastSoulCleaveMod", 0);
        HitDamage = dmg;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.WeaponPercentDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDamage(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var dmg = HitDamage * 2;
        dmg = (int)((double)dmg * (((double)_mExtraSpellCost + 300.0f) / 600.0f));
        HitDamage = dmg;

        caster.SetPower(PowerType.Pain, caster.GetPower(PowerType.Pain) - _mExtraSpellCost);
        caster.AsPlayer.SetPower(PowerType.Pain, caster.GetPower(PowerType.Pain) - _mExtraSpellCost);

        if (caster.HasAura(DemonHunterSpells.GLUTTONY_BUFF))
            caster.RemoveAura(DemonHunterSpells.GLUTTONY_BUFF);
    }
}