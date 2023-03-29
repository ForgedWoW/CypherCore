// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 24195 - Grom's Tribute
internal class spell_quest_uther_grom_tribute : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster.AsPlayer;

        if (!caster)
            return;

        uint spell = caster.Race switch
        {
            Race.Troll    => QuestSpellIds.GromsTrollTribute,
            Race.Tauren   => QuestSpellIds.GromsTaurenTribute,
            Race.Undead   => QuestSpellIds.GromsUndeadTribute,
            Race.Orc      => QuestSpellIds.GromsOrcTribute,
            Race.BloodElf => QuestSpellIds.GromsBloodelfTribute,
            Race.Human    => QuestSpellIds.UthersHumanTribute,
            Race.Gnome    => QuestSpellIds.UthersGnomeTribute,
            Race.Dwarf    => QuestSpellIds.UthersDwarfTribute,
            Race.NightElf => QuestSpellIds.UthersNightelfTribute,
            Race.Draenei  => QuestSpellIds.UthersDraeneiTribute,
            _             => 0
        };

        if (spell != 0)
            caster.CastSpell(caster, spell);
    }
}