// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(new uint[]
{
    237680, 49184
})]
public class spell_dk_howling_blast : SpellScript, IHasSpellEffects
{
    public const uint VISUAL_ID_HOWLING_BLAST = 66812;
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleFrostFever, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));

        if (ScriptSpellId == DeathKnightSpells.HOWLING_BLAST_AREA_DAMAGE)
            SpellEffects.Add(new EffectHandler(HandleSpellVisual, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        else
            SpellEffects.Add(new EffectHandler(HandleAreaDamage, 1, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveIf((WorldObject target) =>
        {
            if (SpellInfo.Id == DeathKnightSpells.HOWLING_BLAST_AREA_DAMAGE)
            {
                if (Spell.CustomArg.has_value())
                    return target.GUID == (ObjectGuid)Spell.CustomArg;
            }
            else
            {
                return ExplTargetUnit != target;
            }

            return false;
        });
    }

    private void HandleFrostFever(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
            caster.CastSpell(HitUnit, DeathKnightSpells.FROST_FEVER);
    }

    private void HandleAreaDamage(int effIndex)
    {
        Caster.CastSpell(ExplTargetUnit, DeathKnightSpells.HOWLING_BLAST_AREA_DAMAGE, new CastSpellExtraArgs().SetCustomArg(ExplTargetUnit.GUID));
    }

    private void HandleSpellVisual(int effIndex)
    {
        if (!Spell.CustomArg.has_value())
            return;

        var caster = Caster;

        if (caster != null)
        {
            var primaryTarget = ObjectAccessor.Instance.GetUnit(caster, (ObjectGuid)Spell.CustomArg);

            if (primaryTarget != null)
                primaryTarget.SendPlaySpellVisual(HitUnit, VISUAL_ID_HOWLING_BLAST, 0, 0, 0.0f);
        }
    }
}