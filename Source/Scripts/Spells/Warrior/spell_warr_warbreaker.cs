// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 262161 Warbreaker
[SpellScript(262161)]
public class SpellWarrWarbreaker : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitSrcAreaEnemy));
    }

    private void HandleOnHitTarget(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = HitUnit;

            if (target != null)
                caster.SpellFactory.CastSpell(target, WarriorSpells.COLOSSUS_SMASH_BUFF, true);
        }
    }

    private void CountTargets(List<WorldObject> targets)
    {
        var caster = Caster;

        if (caster != null)
        {
            var inForTheKill = caster.GetAura(248621);

            if (inForTheKill != null) // In For The Kill
            {
                var hpPct = inForTheKill.SpellInfo.GetEffect(2).CalcValue(caster);
                var hastePct = inForTheKill.GetEffect(0).Amount;

                for (var itr = targets.GetEnumerator(); itr.MoveNext();)
                {
                    var target = itr.Current.AsUnit;

                    if (target != null)
                        if (target.HealthBelowPct(hpPct))
                        {
                            hastePct = inForTheKill.SpellInfo.GetEffect(1).CalcValue(caster);

                            break;
                        }
                }

                caster.SpellFactory.CastSpell(caster, 248622, new CastSpellExtraArgs(SpellValueMod.DurationPct, hastePct)); // In For The Kill
            }
        }
    }
}