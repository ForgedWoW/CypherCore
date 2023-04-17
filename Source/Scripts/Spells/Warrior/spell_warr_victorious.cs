// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 32216 - Victorious
// 82368 - Victorious
[SpellScript(new uint[]
{
    32216, 82368
})]
public class SpellWarrVictorious : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        PreventDefaultAction();
        Target.RemoveAura(Id);
    }
}