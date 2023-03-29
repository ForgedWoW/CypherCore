﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 53099, 57896, 58418, 58420, 59064, 59065, 59439, 60900, 60940
internal class spell_quest_portal_with_condition : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        var target = HitPlayer;

        if (target == null)
            return;

        var spellId = (uint)EffectInfo.CalcValue();
        var questId = (uint)GetEffectInfo(1).CalcValue();

        // This probably should be a way to throw error in SpellCastResult
        if (target.IsActiveQuest(questId))
            target.CastSpell(target, spellId, true);
    }
}