﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_chicken_cover : SpellScript, IHasSpellEffects
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
            if (!target.HasAura(ItemSpellIds.ChickenNet) &&
                (caster.GetQuestStatus(QuestIds.ChickenParty) == QuestStatus.Incomplete || caster.GetQuestStatus(QuestIds.FlownTheCoop) == QuestStatus.Incomplete))
            {
                caster.CastSpell(caster, ItemSpellIds.CaptureChickenEscape, true);
                target.KillSelf();
            }
    }
}