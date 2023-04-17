// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(585)]
public class SpellPriSmite : SpellScript, IHasSpellEffects, ISpellAfterCast
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        var caster = Caster.AsPlayer;

        if (caster == null)
            return;

        if (caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
            if (caster.SpellHistory.HasCooldown(PriestSpells.HOLY_WORD_CHASTISE))
                caster.SpellHistory.ModifyCooldown(PriestSpells.HOLY_WORD_CHASTISE, TimeSpan.FromSeconds(-6 * Time.IN_MILLISECONDS));
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster.AsPlayer;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        if (!caster.AsPlayer)
            return;

        var dmg = HitDamage;

        if (caster.HasAura(PriestSpells.HOLY_WORDS) || caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
            if (caster.SpellHistory.HasCooldown(PriestSpells.HOLY_WORD_CHASTISE))
                caster.SpellHistory.ModifyCooldown(PriestSpells.HOLY_WORD_CHASTISE, TimeSpan.FromSeconds(-4 * Time.IN_MILLISECONDS));
    }
}