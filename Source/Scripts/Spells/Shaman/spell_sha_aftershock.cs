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

// 273221 - Aftershock
[SpellScript(273221)]
internal class SpellShaAftershock : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.ProcSpell;

        if (procSpell != null)
        {
            var cost = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);

            if (cost.HasValue)
                return cost > 0 && RandomHelper.randChance(aurEff.Amount);
        }

        return false;
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.ProcSpell;
        var energize = procSpell.GetPowerTypeCostAmount(PowerType.Maelstrom);

        eventInfo.Actor
                 .SpellFactory.CastSpell(eventInfo.Actor,
                                         ShamanSpells.AFTERSHOCK_ENERGIZE,
                                         new CastSpellExtraArgs(energize != 0)
                                             .AddSpellMod(SpellValueMod.BasePoint0, energize.Value));
    }
}