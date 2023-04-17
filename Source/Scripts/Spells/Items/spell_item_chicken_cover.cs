// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemChickenCover : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster.AsPlayer;
        var target = HitUnit;

        if (target)
            if (!target.HasAura(ItemSpellIds.CHICKEN_NET) &&
                (caster.GetQuestStatus(QuestIds.CHICKEN_PARTY) == QuestStatus.Incomplete || caster.GetQuestStatus(QuestIds.FLOWN_THE_COOP) == QuestStatus.Incomplete))
            {
                caster.SpellFactory.CastSpell(caster, ItemSpellIds.CAPTURE_CHICKEN_ESCAPE, true);
                target.KillSelf();
            }
    }
}