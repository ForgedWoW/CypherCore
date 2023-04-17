﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 28702 - Netherbloom
internal class SpellGenNetherbloom : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var target = HitUnit;

        if (target)
        {
            // 25% chance of casting a random buff
            if (RandomHelper.randChance(75))
                return;

            // triggered spells are 28703 to 28707
            // Note: some sources say, that there was the possibility of
            //       receiving a debuff. However, this seems to be Removed by a patch.

            // don't overwrite an existing aura
            for (byte i = 0; i < 5; ++i)
                if (target.HasAura(GenericSpellIds.NETHER_BLOOM_POLLEN1 + i))
                    return;

            target.SpellFactory.CastSpell(target, GenericSpellIds.NETHER_BLOOM_POLLEN1 + RandomHelper.URand(0, 4), true);
        }
    }
}