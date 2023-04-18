// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class ProcEventInfo
{
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

    public Unit ActionTarget { get; }
    public Unit Actor { get; }
    public DamageInfo DamageInfo { get; }
    public HealInfo HealInfo { get; }
    public ProcFlagsHit HitMask { get; }
    public Spell ProcSpell { get; }
    public Unit ProcTarget { get; }

    public SpellSchoolMask SchoolMask
    {
        get
        {
            if (ProcSpell != null)
                return ProcSpell.SpellInfo.SchoolMask;

            if (DamageInfo != null)
                return DamageInfo.SchoolMask;

            return HealInfo?.SchoolMask ?? SpellSchoolMask.None;
        }
    }

    public SpellInfo SpellInfo
    {
        get
        {
            if (ProcSpell != null)
                return ProcSpell.SpellInfo;

            return DamageInfo != null ? DamageInfo.SpellInfo : HealInfo?.SpellInfo;
        }
    }

    public ProcFlagsSpellPhase SpellPhaseMask { get; }
    public ProcFlagsSpellType SpellTypeMask { get; }
    public ProcFlagsInit TypeMask { get; }
}