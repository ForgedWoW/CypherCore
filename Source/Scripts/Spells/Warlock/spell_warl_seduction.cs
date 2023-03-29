// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(6358)] // 6358 - Seduction (Special Ability)
internal class spell_warl_seduction : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (target)
            if (caster.OwnerUnit &&
                caster.OwnerUnit.HasAura(WarlockSpells.GLYPH_OF_SUCCUBUS))
            {
                target.RemoveAurasByType(AuraType.PeriodicDamage, ObjectGuid.Empty, target.GetAura(WarlockSpells.PRIEST_SHADOW_WORD_DEATH)); // SW:D shall not be Removed.
                target.RemoveAurasByType(AuraType.PeriodicDamagePercent);
                target.RemoveAurasByType(AuraType.PeriodicLeech);
            }
    }
}