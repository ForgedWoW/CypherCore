// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 77756 - Lava Surge
[SpellScript(77756)]
internal class SpellShaLavaSurge : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProcChance, 0, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProcChance(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var procChance = aurEff.Amount;
        var igneousPotential = Target.GetAuraEffect(ShamanSpells.IGNEOUS_POTENTIAL, 0);

        if (igneousPotential != null)
            procChance += igneousPotential.Amount;

        return RandomHelper.randChance(procChance);
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Target.SpellFactory.CastSpell(Target, ShamanSpells.LavaSurge, true);
    }
}