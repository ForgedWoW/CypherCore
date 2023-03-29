﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 63845 - Create Lance
internal class spell_gen_create_lance : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        var target = HitPlayer;

        if (target)
        {
            if (target.Team == TeamFaction.Alliance)
                Caster.CastSpell(target, GenericSpellIds.CreateLanceAlliance, true);
            else
                Caster.CastSpell(target, GenericSpellIds.CreateLanceHorde, true);
        }
    }
}