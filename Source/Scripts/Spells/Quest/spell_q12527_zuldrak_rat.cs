// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 50894 - Zul'Drak Rat
internal class SpellQ12527ZuldrakRat : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        if (GetHitAura() != null &&
            GetHitAura().StackAmount >= SpellInfo.StackAmount)
        {
            HitUnit.SpellFactory.CastSpell((Unit)null, QuestSpellIds.SUMMON_GORGED_LURKING_BASILISK, true);
            var basilisk = HitUnit.AsCreature;

            if (basilisk)
                basilisk.DespawnOrUnsummon();
        }
    }
}