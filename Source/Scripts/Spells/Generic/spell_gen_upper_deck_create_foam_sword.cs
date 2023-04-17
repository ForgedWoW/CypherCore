// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenUpperDeckCreateFoamSword : SpellScript, IHasSpellEffects
{
    //                       green  pink   blue   red    yellow
    private static readonly uint[] ItemId =
    {
        45061, 45176, 45177, 45178, 45179
    };

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var player = HitPlayer;

        if (player)
        {
            // player can only have one of these items
            for (byte i = 0; i < 5; ++i)
                if (player.HasItemCount(ItemId[i], 1, true))
                    return;

            CreateItem(ItemId[RandomHelper.URand(0, 4)], ItemContext.None);
        }
    }
}