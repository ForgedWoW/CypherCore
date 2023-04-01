// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class ProcEventInfo
{
    public Unit Actor { get; }

    public Unit ActionTarget { get; }

    public Unit ProcTarget { get; }

    public ProcFlagsInit TypeMask { get; }

    public ProcFlagsSpellType SpellTypeMask { get; }

    public ProcFlagsSpellPhase SpellPhaseMask { get; }

    public ProcFlagsHit HitMask { get; }

    public SpellInfo SpellInfo
    {
        get
        {
            if (ProcSpell)
                return ProcSpell.SpellInfo;

            if (DamageInfo != null)
                return DamageInfo.SpellInfo;

            return HealInfo?.SpellInfo;
        }
    }

    public SpellSchoolMask SchoolMask
    {
        get
        {
            if (ProcSpell)
                return ProcSpell.SpellInfo.GetSchoolMask();

            if (DamageInfo != null)
                return DamageInfo.SchoolMask;

            if (HealInfo != null)
                return HealInfo.SchoolMask;

            return SpellSchoolMask.None;
        }
    }

    public DamageInfo DamageInfo { get; }

    public HealInfo HealInfo { get; }

    public Spell ProcSpell { get; }

    public ProcEventInfo(Unit actor, Unit actionTarget, Unit procTarget, ProcFlagsInit typeMask, ProcFlagsSpellType spellTypeMask,
                         ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
    {
        Actor = actor;
        ActionTarget = actionTarget;
        ProcTarget = procTarget;
        TypeMask = typeMask;
        SpellTypeMask = spellTypeMask;
        SpellPhaseMask = spellPhaseMask;
        HitMask = hitMask;
        ProcSpell = spell;
        DamageInfo = damageInfo;
        HealInfo = healInfo;
    }
}