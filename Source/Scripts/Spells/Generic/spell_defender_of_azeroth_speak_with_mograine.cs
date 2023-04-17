// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellDefenderOfAzerothSpeakWithMograine : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        if (!Caster)
            return;

        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var nazgrim = HitUnit.FindNearestCreature(CreatureIds.NAZGRIM, 10.0f);

        nazgrim?.HandleEmoteCommand(Emote.OneshotPoint, player);

        var trollbane = HitUnit.FindNearestCreature(CreatureIds.TROLLBANE, 10.0f);

        trollbane?.HandleEmoteCommand(Emote.OneshotPoint, player);

        var whitemane = HitUnit.FindNearestCreature(CreatureIds.WHITEMANE, 10.0f);

        whitemane?.HandleEmoteCommand(Emote.OneshotPoint, player);

        // @TODO: spawntracking - show death gate for casting player
    }
}