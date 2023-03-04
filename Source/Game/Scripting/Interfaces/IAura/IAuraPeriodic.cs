// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraPeriodic : IAuraEffectHandler
    {
        void HandlePeriodic(AuraEffect aurEff);
    }

    public class AuraEffectPeriodicHandler : AuraEffectHandler, IAuraPeriodic
    {
        private readonly Action<AuraEffect> _fn;

        public AuraEffectPeriodicHandler(Action<AuraEffect> fn, int effectIndex, AuraType auraType) : base(effectIndex, auraType, AuraScriptHookType.EffectPeriodic)
        {
            _fn = fn;
        }

        public void HandlePeriodic(AuraEffect aurEff)
        {
            _fn(aurEff);
        }
    }
}