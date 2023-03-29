// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public interface IAuraCalcCritChance : IAuraEffectHandler
{
    double CalcCritChance(AuraEffect aura, Unit victim, double critChance);
}

public class AuraEffectCalcCritChanceHandler : AuraEffectHandler, IAuraCalcCritChance
{
    private readonly Func<AuraEffect, Unit, double, double> _fn;

    public AuraEffectCalcCritChanceHandler(Func<AuraEffect, Unit, double, double> fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcCritChance)
    {
        _fn = fn;
    }

    public double CalcCritChance(AuraEffect aura, Unit victim, double critChance)
    {
        return _fn(aura, victim, critChance);
    }
}