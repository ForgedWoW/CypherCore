// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Call Dreadstalkers - 104316
[SpellScript(104316)]
public class spell_warlock_call_dreadstalkers : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void AfterCast()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null || target == null)
            return;

        var dreadstalkers = caster.GetCreatureListWithEntryInGrid(98035);

        foreach (var dreadstalker in dreadstalkers)
            if (dreadstalker.OwnerUnit == caster)
            {
                dreadstalker.SetLevel(caster.Level);
                dreadstalker.SetMaxHealth(caster.MaxHealth / 3);
                dreadstalker.SetHealth(caster.Health / 3);
                dreadstalker.AI.AttackStart(target);
            }

        var impsToSummon = caster.GetAuraEffectAmount(WarlockSpells.IMPROVED_DREADSTALKERS, 0);

        for (uint i = 0; i < impsToSummon; ++i)
            caster.CastSpell(target.GetRandomNearPosition(3.0f), WarlockSpells.WILD_IMP_SUMMON, true);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        for (var i = 0; i < EffectValue; ++i)
            caster.CastSpell(caster, WarlockSpells.CALL_DREADSTALKERS_SUMMON, true);

        var player = caster.AsPlayer;

        if (player == null)
            return;

        // Check if player has aura with ID 387485
        var aura = caster.GetAura(387485);

        if (aura != null)
        {
            var effect = aura.GetEffect(0);

            if (RandomHelper.randChance(effect.BaseAmount))
                caster.CastSpell(caster, WarlockSpells.CALL_DREADSTALKERS_SUMMON, true);
        }
    }
}