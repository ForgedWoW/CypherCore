// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

[SpellScript(126661)] // 126661 - Warrior Charge Drop Fire Periodic
internal class SpellWarrChargeDropFirePeriodic : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(DropFireVisual, 0, AuraType.PeriodicTriggerSpell));
    }

    private void DropFireVisual(AuraEffect aurEff)
    {
        PreventDefaultAction();

        if (Target.IsSplineEnabled)
            for (uint i = 0; i < 5; ++i)
            {
                var timeOffset = (int)(6 * i * aurEff.Period / 25);
                var loc = Target.MoveSpline.ComputePosition(timeOffset);
                Target.SendPlaySpellVisual(new Position(loc.X, loc.Y, loc.Z), Misc.SPELL_VISUAL_BLAZING_CHARGE, 0, 0, 1.0f, true);
            }
    }
}