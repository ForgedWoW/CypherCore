// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 71564 - Deadly Precision
internal class SpellItemDeadlyPrecision : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleStackDrop, 0, AuraType.ModRating, AuraScriptHookType.EffectProc));
    }

    private void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Target.RemoveAuraFromStack(Id, Target.GUID);
    }
}