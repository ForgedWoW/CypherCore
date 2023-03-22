// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.AI;

public class TurretAI : CreatureAI
{
	readonly float _minRange;

	public TurretAI(Creature creature) : base(creature)
	{
		if (creature.Spells[0] == 0)
			Log.outError(LogFilter.Server, $"TurretAI set for creature with spell1=0. AI will do nothing ({creature.GUID})");

		var spellInfo = Global.SpellMgr.GetSpellInfo(creature.Spells[0], creature.Map.DifficultyID);
		_minRange = spellInfo != null ? spellInfo.GetMinRange(false) : 0;
		creature.CombatDistance = spellInfo != null ? spellInfo.GetMaxRange(false) : 0;
		creature.SightDistance = creature.CombatDistance;
	}

	public override bool CanAIAttack(Unit victim)
	{
		// todo use one function to replace it
		if (!Me.IsWithinCombatRange(victim, Me.CombatDistance) || (_minRange != 0 && Me.IsWithinCombatRange(victim, _minRange)))
			return false;

		return true;
	}

	public override void AttackStart(Unit victim)
	{
		if (victim != null)
			Me.Attack(victim, false);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		DoSpellAttackIfReady(Me.Spells[0]);
	}
}