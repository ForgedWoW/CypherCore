// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin
{
    // Holy Shield - 152261
    [SpellScript(152261)]
    public class spell_pal_holy_shield : AuraScript, IAuraCheckProc, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public bool CheckProc(ProcEventInfo eventInfo)
        {
            return (eventInfo.GetHitMask() & ProcFlagsHit.Block) != 0;
        }

        private void HandleCalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
        {
            amount.Value = 0;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalcAmount, 2, AuraType.SchoolAbsorb));
        }
    }
}
