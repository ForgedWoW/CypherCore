﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 52510 - Burst at the Seams
internal class spell_q12690_burst_at_the_seams_52510 : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.TypeId == TypeId.Unit;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleKnockBack, 1, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleKnockBack(int effIndex)
    {
        Unit creature = HitCreature;

        if (creature != null)
        {
            var charmer = Caster.CharmerOrOwner;

            if (charmer != null)
            {
                var player = charmer.AsPlayer;

                if (player != null)
                    if (player.GetQuestStatus(Misc.QuestFuelForTheFire) == QuestStatus.Incomplete)
                    {
                        creature.CastSpell(creature, QuestSpellIds.BurstAtTheSeamsBone, true);
                        creature.CastSpell(creature, QuestSpellIds.ExplodeAbominationMeat, true);
                        creature.CastSpell(creature, QuestSpellIds.ExplodeAbominationBloodyMeat, true);
                        creature.CastSpell(creature, QuestSpellIds.BurstAtTheSeams52508, true);
                        creature.CastSpell(creature, QuestSpellIds.BurstAtTheSeams59580, true);

                        player.CastSpell(player, QuestSpellIds.DrakkariSkullcrusherCredit, true);
                        var count = player.GetReqKillOrCastCurrentCount(Misc.QuestFuelForTheFire, (int)CreatureIds.DrakkariChieftaink);

                        if ((count % 20) == 0)
                            player.CastSpell(player, QuestSpellIds.SummonDrakkariChieftain, true);
                    }
            }
        }
    }

    private void HandleScript(int effIndex)
    {
        Caster.AsCreature.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
    }
}