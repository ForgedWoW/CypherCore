// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemDemonBroiledSurprise : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }

    public SpellCastResult CheckCast()
    {
        var player = Caster.AsPlayer;

        if (player.GetQuestStatus(QuestIds.SUPER_HOT_STEW) != QuestStatus.Incomplete)
            return SpellCastResult.CantDoThatRightNow;

        var creature = player.FindNearestCreature(CreatureIds.ABYSSAL_FLAMEBRINGER, 10, false);

        if (creature)
            if (creature.IsDead)
                return SpellCastResult.SpellCastOk;

        return SpellCastResult.NotHere;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var player = Caster;
        player.SpellFactory.CastSpell(player, ItemSpellIds.CREATE_DEMON_BROILED_SURPRISE, false);
    }
}