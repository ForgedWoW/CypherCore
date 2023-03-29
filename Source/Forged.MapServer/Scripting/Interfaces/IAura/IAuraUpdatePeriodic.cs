// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraUpdatePeriodic : IAuraEffectHandler
{
    void UpdatePeriodic(AuraEffect aurEff);
}

public class AuraEffectUpdatePeriodicHandler : AuraEffectHandler, IAuraUpdatePeriodic
{
    private readonly Action<AuraEffect> _fn;

    public AuraEffectUpdatePeriodicHandler(Action<AuraEffect> fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectUpdatePeriodic)
    {
        _fn = fn;
    }

    public void UpdatePeriodic(AuraEffect aurEff)
    {
        _fn(aurEff);
    }
}