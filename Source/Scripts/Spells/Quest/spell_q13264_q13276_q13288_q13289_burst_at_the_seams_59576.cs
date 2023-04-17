// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 59576 - Burst at the Seams
internal class SpellQ13264Q13276Q13288Q13289BurstAtTheSeams59576 : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var creature = Caster.AsCreature;

        if (creature != null)
        {
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.BLOATED_ABOMINATION_FEIGN_DEATH, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.BURST_AT_THE_SEAMS59579, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.BURST_AT_THE_SEAMS_BONE, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.BURST_AT_THE_SEAMS_BONE, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.BURST_AT_THE_SEAMS_BONE, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.EXPLODE_ABOMINATION_MEAT, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.EXPLODE_ABOMINATION_BLOODY_MEAT, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.EXPLODE_ABOMINATION_BLOODY_MEAT, true);
            creature.SpellFactory.CastSpell(creature, QuestSpellIds.EXPLODE_ABOMINATION_BLOODY_MEAT, true);
        }
    }
}