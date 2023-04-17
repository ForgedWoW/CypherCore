// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 24195 - Grom's Tribute
internal class SpellQuestUtherGromTribute : SpellScript, IHasSpellEffects
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
            Race.Troll    => QuestSpellIds.GROMS_TROLL_TRIBUTE,
            Race.Tauren   => QuestSpellIds.GROMS_TAUREN_TRIBUTE,
            Race.Undead   => QuestSpellIds.GROMS_UNDEAD_TRIBUTE,
            Race.Orc      => QuestSpellIds.GROMS_ORC_TRIBUTE,
            Race.BloodElf => QuestSpellIds.GROMS_BLOODELF_TRIBUTE,
            Race.Human    => QuestSpellIds.UTHERS_HUMAN_TRIBUTE,
            Race.Gnome    => QuestSpellIds.UTHERS_GNOME_TRIBUTE,
            Race.Dwarf    => QuestSpellIds.UTHERS_DWARF_TRIBUTE,
            Race.NightElf => QuestSpellIds.UTHERS_NIGHTELF_TRIBUTE,
            Race.Draenei  => QuestSpellIds.UTHERS_DRAENEI_TRIBUTE,
            _             => 0
        };

        if (spell != 0)
            caster.SpellFactory.CastSpell(caster, spell);
    }
}