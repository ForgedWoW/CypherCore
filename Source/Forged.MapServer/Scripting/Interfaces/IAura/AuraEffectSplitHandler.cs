// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public class AuraEffectSplitHandler : AuraEffectHandler, IAuraSplitHandler
{
    private readonly Func<AuraEffect, DamageInfo, double, double> _fn;

    public AuraEffectSplitHandler(Func<AuraEffect, DamageInfo, double, double> fn, int effectIndex) : base(effectIndex, AuraType.SplitDamagePct, AuraScriptHookType.EffectSplit)
    {
        _fn = fn;
    }

    public double Split(AuraEffect aura, DamageInfo damageInfo, double splitAmount)
    {
        return _fn(aura, damageInfo, splitAmount);
    }
}