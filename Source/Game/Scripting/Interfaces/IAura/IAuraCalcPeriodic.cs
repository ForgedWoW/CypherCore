// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Models;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraCalcPeriodic : IAuraEffectHandler
    {
        void CalcPeriodic(AuraEffect aura, BoxedValue<bool> isPeriodic, BoxedValue<int> amplitude);
    }

    public class AuraEffectCalcPeriodicHandler : AuraEffectHandler, IAuraCalcPeriodic
    {
        private readonly Action<AuraEffect, BoxedValue<bool>, BoxedValue<int>> _fn;

        public AuraEffectCalcPeriodicHandler(Action<AuraEffect, BoxedValue<bool>, BoxedValue<int>> fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectCalcPeriodic)
        {
            _fn = fn;
        }

        public void CalcPeriodic(AuraEffect aura, BoxedValue<bool> isPeriodic, BoxedValue<int> amplitude)
        {
            _fn(aura, isPeriodic, amplitude);
        }
    }
}