// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.Spells;

internal class ProcReflectDelayed : BasicEvent
{
    private readonly ObjectGuid _casterGuid;
    private readonly Unit _victim;

    public ProcReflectDelayed(Unit owner, ObjectGuid casterGuid)
    {
        _victim = owner;
        _casterGuid = casterGuid;
    }

    public override bool Execute(ulong etime, uint pTime)
    {
        var caster = _victim.ObjectAccessor.GetUnit(_victim, _casterGuid);

        if (caster == null)
            return true;

        const ProcFlags typeMaskActor = ProcFlags.None;
        const ProcFlags typeMaskActionTarget = ProcFlags.TakeHarmfulSpell | ProcFlags.TakeHarmfulAbility;
        const ProcFlagsSpellType spellTypeMask = ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
        const ProcFlagsSpellPhase spellPhaseMask = ProcFlagsSpellPhase.None;
        const ProcFlagsHit hitMask = ProcFlagsHit.Reflect;

        _victim.UnitCombatHelpers.ProcSkillsAndAuras(caster, _victim, new ProcFlagsInit(), new ProcFlagsInit(typeMaskActionTarget), spellTypeMask, spellPhaseMask, hitMask, null, null, null);

        return true;
    }
}