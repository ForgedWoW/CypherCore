// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 47540 - Penance
internal class spell_pri_penance : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (target)
            if (!caster.IsFriendlyTo(target))
            {
                if (!caster.IsValidAttackTarget(target))
                    return SpellCastResult.BadTargets;

                if (!caster.IsInFront(target))
                    return SpellCastResult.NotInfront;
            }

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (target)
        {
            if (caster.IsFriendlyTo(target))
                caster.CastSpell(target, PriestSpells.PENANCE_CHANNEL_HEALING, new CastSpellExtraArgs().SetTriggeringSpell(Spell));
            else
                caster.CastSpell(target, PriestSpells.PENANCE_CHANNEL_DAMAGE, new CastSpellExtraArgs().SetTriggeringSpell(Spell));
        }
    }
}