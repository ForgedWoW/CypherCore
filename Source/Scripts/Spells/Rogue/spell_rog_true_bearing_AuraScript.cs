// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(193359)]
public class SpellRogTrueBearingAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var finishers = new List<uint>()
        {
            (uint)TrueBearingIDs.BetweenTheEyes,
            (uint)RogueSpells.ROLL_THE_BONES,
            (uint)RogueSpells.EVISCERATE
        };

        foreach (var finisher in finishers)
            if (eventInfo.SpellInfo.Id == finisher)
                return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var cp = caster.GetPower(PowerType.ComboPoints) + 1;

        var spellIds = new List<uint>()
        {
            (uint)RogueSpells.ADRENALINE_RUSH,
            (uint)RogueSpells.SPRINT,
            (uint)TrueBearingIDs.BetweenTheEyes,
            (uint)TrueBearingIDs.Vanish,
            (uint)TrueBearingIDs.Blind,
            (uint)TrueBearingIDs.CloakOfShadows,
            (uint)TrueBearingIDs.Riposte,
            (uint)TrueBearingIDs.GrapplingHook,
            (uint)RogueSpells.KILLING_SPREE,
            (uint)TrueBearingIDs.MarkedForDeath,
            (uint)TrueBearingIDs.DeathFromAbove
        };

        foreach (var spell in spellIds)
            caster.SpellHistory.ModifyCooldown(spell, TimeSpan.FromSeconds(-2000 * cp));
    }
}