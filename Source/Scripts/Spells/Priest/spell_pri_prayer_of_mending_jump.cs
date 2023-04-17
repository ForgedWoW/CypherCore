﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 155793 - prayer of mending (Jump) - PRAYER_OF_MENDING_JUMP
internal class SpellPriPrayerOfMendingJump : SpellScript, IHasSpellEffects
{
    private SpellEffectInfo _healEffectDummy;
    private SpellInfo _spellInfoHeal;
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        _spellInfoHeal = Global.SpellMgr.GetSpellInfo(PriestSpells.PRAYER_OF_MENDING_HEAL, Difficulty.None);
        _healEffectDummy = _spellInfoHeal.GetEffect(0);

        return true;
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(OnTargetSelect, 0, Targets.UnitSrcAreaAlly));
        SpellEffects.Add(new EffectHandler(HandleJump, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void OnTargetSelect(List<WorldObject> targets)
    {
        // Find the best Target - prefer players over pets
        var foundPlayer = false;

        foreach (var worldObject in targets)
            if (worldObject.IsPlayer)
            {
                foundPlayer = true;

                break;
            }

        if (foundPlayer)
            targets.RemoveAll(new ObjectTypeIdCheck(TypeId.Player, false));

        // choose one random Target from targets
        if (targets.Count > 1)
        {
            var selected = targets.SelectRandom();
            targets.Clear();
            targets.Add(selected);
        }
    }

    private void HandleJump(int effIndex)
    {
        var origCaster = OriginalCaster; // the one that started the prayer of mending chain
        var target = HitUnit;            // the Target we decided the aura should Jump to

        if (origCaster)
        {
            var basePoints = origCaster.SpellHealingBonusDone(target, _spellInfoHeal, (uint)_healEffectDummy.CalcValue(origCaster), DamageEffectType.Heal, _healEffectDummy, 1, Spell);
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.AuraStack, EffectValue);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
            origCaster.SpellFactory.CastSpell(target, PriestSpells.PRAYER_OF_MENDING_AURA, args);
        }
    }
}