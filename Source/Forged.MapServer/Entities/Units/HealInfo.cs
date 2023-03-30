// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class HealInfo
{
    public Unit Healer { get; }

    public Unit Target { get; }

    public double OriginalHeal { get; }

    public SpellInfo SpellInfo { get; }

    public SpellSchoolMask SchoolMask { get; }

    public double Heal { get; private set; }

    public double EffectiveHeal { get; private set; }

    public double Absorb { get; private set; }

    public ProcFlagsHit HitMask { get; private set; }

    public bool IsCritical
    {
        get { return HitMask.HasFlag(ProcFlagsHit.Critical); }
    }

    public HealInfo(Unit healer, Unit target, double heal, SpellInfo spellInfo, SpellSchoolMask schoolMask)
    {
        Healer = healer;
        Target = target;
        Heal = heal;
        OriginalHeal = heal;
        SpellInfo = spellInfo;
        SchoolMask = schoolMask;
    }

    public void AbsorbHeal(double amount)
    {
        amount = Math.Min(amount, Heal);
        Absorb += amount;
        Heal -= amount;
        amount = Math.Min(amount, EffectiveHeal);
        EffectiveHeal -= amount;
        HitMask |= ProcFlagsHit.Absorb;
    }

    public void SetEffectiveHeal(uint amount)
    {
        EffectiveHeal = amount;
    }
}