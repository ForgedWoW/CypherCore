// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(83381)]
public class spell_hun_kill_command_proc : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDamage(int effIndex)
    {
        var caster = Caster;
        var owner = caster.OwnerUnit;
        var target = ExplTargetUnit;

        // (1.5 * (rap * 3) * bmMastery * lowNerf * (1 + versability))
        double dmg = 4.5f * owner.UnitData.RangedAttackPower;
        var lowNerf = Math.Min((int)owner.Level, 20) * 0.05f;

        var ownerPlayer = owner.AsPlayer;

        if (ownerPlayer != null)
            dmg = MathFunctions.AddPct(ref dmg, ownerPlayer.ActivePlayerData.Mastery);

        dmg *= lowNerf;

        dmg = caster.SpellDamageBonusDone(target, SpellInfo, dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, Spell);
        dmg = target.SpellDamageBonusTaken(caster, SpellInfo, dmg, DamageEffectType.Direct);

        HitDamage = dmg;
    }
}