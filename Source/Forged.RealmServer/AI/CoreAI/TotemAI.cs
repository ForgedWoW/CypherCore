// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;
using Game.Common.Maps;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Creatures;
using Forged.RealmServer.Entities.Objects;
using Forged.RealmServer.Entities.Units;

namespace Forged.RealmServer.AI;

public class TotemAI : NullCreatureAI
{
	ObjectGuid _victimGuid;

	public TotemAI(Creature creature) : base(creature)
	{
		_victimGuid = ObjectGuid.Empty;
	}

	public override void UpdateAI(uint diff)
	{
		if (Me.ToTotem().GetTotemType() != TotemType.Active)
			return;

		if (!Me.IsAlive || Me.IsNonMeleeSpellCast(false))
			return;

		// Search spell
		var spellInfo = Global.SpellMgr.GetSpellInfo(Me.ToTotem().GetSpell(), Me.Map.DifficultyID);

		if (spellInfo == null)
			return;

		// Get spell range
		var max_range = spellInfo.GetMaxRange(false);

		// SpellModOp.Range not applied in this place just because not existence range mods for attacking totems

		var victim = !_victimGuid.IsEmpty ? Global.ObjAccessor.GetUnit(Me, _victimGuid) : null;

		// Search victim if no, not attackable, or out of range, or friendly (possible in case duel end)
		if (victim == null || !victim.IsTargetableForAttack() || !Me.IsWithinDistInMap(victim, max_range) || Me.IsFriendlyTo(victim) || !Me.CanSeeOrDetect(victim))
		{
			var extraSearchRadius = max_range > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
			var u_check = new NearestAttackableUnitInObjectRangeCheck(Me, Me.CharmerOrOwnerOrSelf, max_range);
			var checker = new UnitLastSearcher(Me, u_check, GridType.All);
			Cell.VisitGrid(Me, checker, max_range + extraSearchRadius);
			victim = checker.GetTarget();
		}

		// If have target
		if (victim != null)
		{
			// remember
			_victimGuid = victim.GUID;

			// attack
			Me.CastSpell(victim, Me.ToTotem().GetSpell());
		}
		else
		{
			_victimGuid.Clear();
		}
	}

	public override void AttackStart(Unit victim) { }
}