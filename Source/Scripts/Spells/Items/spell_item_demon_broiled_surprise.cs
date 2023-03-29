// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_demon_broiled_surprise : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }

    public SpellCastResult CheckCast()
    {
        var player = Caster.AsPlayer;

        if (player.GetQuestStatus(QuestIds.SuperHotStew) != QuestStatus.Incomplete)
            return SpellCastResult.CantDoThatRightNow;

        var creature = player.FindNearestCreature(CreatureIds.AbyssalFlamebringer, 10, false);

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
        player.CastSpell(player, ItemSpellIds.CreateDemonBroiledSurprise, false);
    }
}