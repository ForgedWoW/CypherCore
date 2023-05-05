// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Demonic Gateway - 111771
[SpellScript(111771)]
public class SpellWarlDemonicGateway : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        // don't allow during Arena Preparation
        if (Caster.HasAura(BattlegroundConst.SPELL_ARENA_PREPARATION))
            return SpellCastResult.CantDoThatRightNow;

        // check if player can reach the location
        var spell = Spell;

        if (spell.Targets.HasDst)
        {
            var pos = spell.Targets.Dst.Position;
            var caster = Caster;

            if (caster.Location.Z + 6.0f < pos.Z || caster.Location.Z - 6.0f > pos.Z)
                return SpellCastResult.NoPath;
        }

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleVisual, 0, SpellEffectName.Summon, SpellScriptHookType.Launch));
        SpellEffects.Add(new EffectHandler(HandleLaunch, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
    }

    private void HandleLaunch(int effIndex)
    {
        var caster = Caster;

        // despawn all other gateways
        var targets1 = new List<Creature>();
        var targets2 = new List<Creature>();
        targets1 = caster.GetCreatureListWithEntryInGrid(WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN, 200.0f);
        targets2 = caster.GetCreatureListWithEntryInGrid(WarlockSpells.NPC_WARLOCK_DEMONIC_GATEWAY_PURPLE, 200.0f);

        targets1.AddRange(targets2);

        foreach (var target in targets1)
        {
            if (target.OwnerGUID != caster.GUID)
                continue;

            target.DespawnOrUnsummon(TimeSpan.FromMilliseconds(100)); // despawn at next tick
        }

        var dest = ExplTargetDest;

        if (dest != null)
        {
            caster.SpellFactory.CastSpell(caster, WarlockSpells.DEMONIC_GATEWAY_SUMMON_PURPLE, true);
            caster.SpellFactory.CastSpell(dest, WarlockSpells.DEMONIC_GATEWAY_SUMMON_GREEN, true);
        }
    }

    private void HandleVisual(int effIndex)
    {
        var caster = Caster;
        var pos = ExplTargetDest;

        if (caster == null || pos == null)
            return;

        caster.SendPlaySpellVisual(pos, 20.0f, 63644, 0, 0, 2.0f);
    }
}