// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(MonkSpells.FISTS_OF_FURY_DAMAGE)]
public class SpellMonkFistsOfFuryDamage : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDamage(int effIndex)
    {
        if (!Caster)
            return;

        var lTarget = HitUnit;
        var lPlayer = Caster.AsPlayer;

        if (lTarget == null || lPlayer == null)
            return;

        var lDamage = lPlayer.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 5.25f;
        lDamage = lPlayer.SpellDamageBonusDone(lTarget, SpellInfo, lDamage, DamageEffectType.Direct, SpellInfo.GetEffect(0), 1, Spell);
        lDamage = lTarget.SpellDamageBonusTaken(lPlayer, SpellInfo, lDamage, DamageEffectType.Direct);

        HitDamage = lDamage;
    }
}