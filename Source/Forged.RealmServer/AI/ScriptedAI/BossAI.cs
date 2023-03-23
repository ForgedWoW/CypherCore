// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Maps;

namespace Forged.RealmServer.AI;

public class BossAI : ScriptedAI
{
	public InstanceScript Instance;
	public SummonList Summons;
	readonly uint _bossId;

	public BossAI(Creature creature, uint bossId) : base(creature)
	{
		Instance = creature.InstanceScript;
		Summons = new SummonList(creature);
		_bossId = bossId;

		if (Instance != null)
			SetBoundary(Instance.GetBossBoundary(bossId));

		Scheduler.SetValidator(() => !Me.HasUnitState(UnitState.Casting));
	}

	public void _Reset()
	{
		if (!Me.IsAlive)
			return;

		Me.CombatPulseDelay = 0;
		Me.ResetLootMode();
		Events.Reset();
		Summons.DespawnAll();
		Scheduler.CancelAll();

		if (Instance != null && Instance.GetBossState(_bossId) != EncounterState.Done)
			Instance.SetBossState(_bossId, EncounterState.NotStarted);
	}

	public void _JustDied()
	{
		Events.Reset();
		Summons.DespawnAll();
		Scheduler.CancelAll();

		if (Instance != null)
			Instance.SetBossState(_bossId, EncounterState.Done);
	}

	public void _JustEngagedWith(Unit who)
	{
		if (Instance != null)
		{
			// bosses do not respawn, check only on enter combat
			if (!Instance.CheckRequiredBosses(_bossId, who.AsPlayer))
			{
				EnterEvadeMode(EvadeReason.SequenceBreak);

				return;
			}

			Instance.SetBossState(_bossId, EncounterState.InProgress);
		}

		Me.CombatPulseDelay = 5;
		Me.SetActive(true);
		DoZoneInCombat();
		ScheduleTasks();
	}

	public void TeleportCheaters()
	{
		foreach (var pair in Me.GetCombatManager().PvECombatRefs)
		{
			var target = pair.Value.GetOther(Me);

			if (target.IsControlledByPlayer && !IsInBoundary(target.Location))
				target.NearTeleportTo(Me.Location);
		}
	}

	public override void JustSummoned(Creature summon)
	{
		Summons.Summon(summon);

		if (Me.IsEngaged)
			DoZoneInCombat(summon);
	}

	public override void SummonedCreatureDespawn(Creature summon)
	{
		Summons.Despawn(summon);
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Events.Update(diff);

		if (Me.HasUnitState(UnitState.Casting))
			return;


		Events.ExecuteEvents(eventId =>
		{
			ExecuteEvent(eventId);

			if (Me.HasUnitState(UnitState.Casting))
				return;
		});

		DoMeleeAttackIfReady();
	}

	public void _DespawnAtEvade()
	{
		_DespawnAtEvade(TimeSpan.FromSeconds(30));
	}

	public void _DespawnAtEvade(TimeSpan delayToRespawn, Creature who = null)
	{
		if (delayToRespawn < TimeSpan.FromSeconds(2))
		{
			Log.outError(LogFilter.ScriptsAi, $"BossAI::_DespawnAtEvade: called with delay of {delayToRespawn} seconds, defaulting to 2 (me: {Me.GUID})");
			delayToRespawn = TimeSpan.FromSeconds(2);
		}

		if (!who)
			who = Me;

		var whoSummon = who.ToTempSummon();

		if (whoSummon)
		{
			Log.outWarn(LogFilter.ScriptsAi, $"BossAI::_DespawnAtEvade: called on a temporary summon (who: {who.GUID})");
			whoSummon.UnSummon();

			return;
		}

		who.DespawnOrUnsummon(TimeSpan.Zero, delayToRespawn);

		if (Instance != null && who == Me)
			Instance.SetBossState(_bossId, EncounterState.Fail);
	}

	public virtual void ExecuteEvent(uint eventId) { }

	public virtual void ScheduleTasks() { }

	public override void Reset()
	{
		_Reset();
	}

	public override void JustEngagedWith(Unit who)
	{
		_JustEngagedWith(who);
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
	}

	public override void JustReachedHome()
	{
		_JustReachedHome();
	}

	public override bool CanAIAttack(Unit victim)
	{
		return IsInBoundary(victim.Location);
	}

	public void _JustReachedHome()
	{
		Me.SetActive(false);
	}

	public uint GetBossId()
	{
		return _bossId;
	}

	void ForceCombatStopForCreatureEntry(uint entry, float maxSearchRange = 250.0f, bool reset = true)
	{
		Log.outDebug(LogFilter.ScriptsAi, $"BossAI::ForceStopCombatForCreature: called on {Me.GUID}. Debug info: {Me.GetDebugInfo()}");

		var creatures = Me.GetCreatureListWithEntryInGrid(entry, maxSearchRange);

		foreach (var creature in creatures)
		{
			creature.CombatStop(true);
			creature.DoNotReacquireSpellFocusTarget();
			creature.MotionMaster.Clear(MovementGeneratorPriority.Normal);

			if (reset)
			{
				creature.LoadCreaturesAddon();
				creature.SetTappedBy(null);
				creature.ResetPlayerDamageReq();
				creature.LastDamagedTime = 0;
				creature.SetCannotReachTarget(false);
			}
		}
	}
}