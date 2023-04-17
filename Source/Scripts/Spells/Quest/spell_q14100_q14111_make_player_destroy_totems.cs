// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 66744 - Make Player Destroy Totems
internal class SpellQ14100Q14111MakePlayerDestroyTotems : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        var player = HitPlayer;

        if (player)
            player.SpellFactory.CastSpell(player, QuestSpellIds.TOTEM_OF_THE_EARTHEN_RING, new CastSpellExtraArgs(TriggerCastFlags.FullMask)); // ignore reagent cost, consumed by quest
    }
}