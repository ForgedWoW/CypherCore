// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.AI;

public class ArcherAI : CreatureAI
{
	readonly float _minRange;

	public ArcherAI(Creature creature) : base(creature)
	{
		if (creature.Spells[0] == 0)
			Log.outError(LogFilter.ScriptsAi, $"ArcherAI set for creature with spell1=0. AI will do nothing ({Me.GUID})");

		var spellInfo = Global.SpellMgr.GetSpellInfo(creature.Spells[0], creature.Map.DifficultyID);
		_minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;

		if (_minRange == 0)
			_minRange = SharedConst.MeleeRange;

		creature.CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
		creature.SightDistance = creature.CombatDistance;
	}

	public override void AttackStart(Unit who)
	{
		if (who == null)
			return;

		if (Me.IsWithinCombatRange(who, _minRange))
		{
			if (Me.Attack(who, true) && !who.IsFlying)
				Me.MotionMaster.MoveChase(who);
		}
		else
		{
			if (Me.Attack(who, false) && !who.IsFlying)
				Me.MotionMaster.MoveChase(who, Me.CombatDistance);
		}

		if (who.IsFlying)
			Me.MotionMaster.MoveIdle();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		if (!Me.IsWithinCombatRange(Me.Victim, _minRange))
			DoSpellAttackIfReady(Me.Spells[0]);
		else
			DoMeleeAttackIfReady();
	}
}