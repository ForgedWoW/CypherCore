// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellScriptHookType
{
    Launch = SpellScriptState.Unloading + 1,
    LaunchTarget,
    EffectHit,
    EffectHitTarget,
    EffectSuccessfulDispel,
    BeforeHit,
    Hit,
    AfterHit,
    ObjectAreaTargetSelect,
    ObjectTargetSelect,
    DestinationTargetSelect,
    CheckCast,
    BeforeCast,
    OnCast,
    OnResistAbsorbCalculation,
    AfterCast,
    CalcCritChance,
    OnPrecast,
    CalcCastTime,
    CalcMultiplier
}