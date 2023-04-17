// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 52510 - Burst at the Seams
internal class SpellQ12690BurstAtTheSeams52510 : SpellScript, IHasSpellEffects
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
                    if (player.GetQuestStatus(Misc.QUEST_FUEL_FOR_THE_FIRE) == QuestStatus.Incomplete)
                    {
                        creature.SpellFactory.CastSpell(creature, QuestSpellIds.BURST_AT_THE_SEAMS_BONE, true);
                        creature.SpellFactory.CastSpell(creature, QuestSpellIds.EXPLODE_ABOMINATION_MEAT, true);
                        creature.SpellFactory.CastSpell(creature, QuestSpellIds.EXPLODE_ABOMINATION_BLOODY_MEAT, true);
                        creature.SpellFactory.CastSpell(creature, QuestSpellIds.BURST_AT_THE_SEAMS52508, true);
                        creature.SpellFactory.CastSpell(creature, QuestSpellIds.BURST_AT_THE_SEAMS59580, true);

                        player.SpellFactory.CastSpell(player, QuestSpellIds.DRAKKARI_SKULLCRUSHER_CREDIT, true);
                        var count = player.GetReqKillOrCastCurrentCount(Misc.QUEST_FUEL_FOR_THE_FIRE, (int)CreatureIds.DRAKKARI_CHIEFTAINK);

                        if ((count % 20) == 0)
                            player.SpellFactory.CastSpell(player, QuestSpellIds.SUMMON_DRAKKARI_CHIEFTAIN, true);
                    }
            }
        }
    }

    private void HandleScript(int effIndex)
    {
        Caster.AsCreature.DespawnOrUnsummon(TimeSpan.FromSeconds(2));
    }
}