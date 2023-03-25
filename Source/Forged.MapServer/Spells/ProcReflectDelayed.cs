// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.Spells;

class ProcReflectDelayed : BasicEvent
{
	readonly Unit _victim;
	readonly ObjectGuid _casterGuid;

	public ProcReflectDelayed(Unit owner, ObjectGuid casterGuid)
	{
		_victim = owner;
		_casterGuid = casterGuid;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		var caster = Global.ObjAccessor.GetUnit(_victim, _casterGuid);

		if (!caster)
			return true;

		var typeMaskActor = ProcFlags.None;
		var typeMaskActionTarget = ProcFlags.TakeHarmfulSpell | ProcFlags.TakeHarmfulAbility;
		var spellTypeMask = ProcFlagsSpellType.Damage | ProcFlagsSpellType.NoDmgHeal;
		var spellPhaseMask = ProcFlagsSpellPhase.None;
		var hitMask = ProcFlagsHit.Reflect;

		Unit.ProcSkillsAndAuras(caster, _victim, new ProcFlagsInit(typeMaskActor), new ProcFlagsInit(typeMaskActionTarget), spellTypeMask, spellPhaseMask, hitMask, null, null, null);

		return true;
	}
}