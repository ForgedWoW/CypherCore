// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 219415 - Dimension Ripper
[SpellScript(219415)]
public class SpellWarlockArtifactDimensionRipper : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        PreventDefaultAction();
        var caster = Caster;

        if (caster == null)
            return;

        caster.SpellHistory.RestoreCharge(Global.SpellMgr.GetSpellInfo(WarlockSpells.DIMENSIONAL_RIFT, Difficulty.None).ChargeCategoryId);
    }
}