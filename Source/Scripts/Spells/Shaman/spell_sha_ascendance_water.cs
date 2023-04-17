// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// Ascendance (Water) - 114052
[SpellScript(114052)]
public class SpellShaAscendanceWater : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.HealInfo != null && eventInfo.SpellInfo != null && eventInfo.SpellInfo.Id == ESpells.RESTORATIVE_MISTS)
            return false;

        if (eventInfo.HealInfo == null)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var bp0 = eventInfo.HealInfo.Heal;

        if (bp0 != 0)
            eventInfo.ActionTarget.SpellFactory.CastSpell(eventInfo.Actor, ESpells.RESTORATIVE_MISTS, new CastSpellExtraArgs(aurEff).AddSpellMod(SpellValueMod.BasePoint0, (int)bp0));
    }

    private struct ESpells
    {
        public const uint RESTORATIVE_MISTS = 114083;
    }
}