// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Maps.Grids;
using Forged.RealmServer.Spells;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.AI;

public class SmartAI : CreatureAI
{
	public uint EscortQuestID;
	const int SMART_ESCORT_MAX_PLAYER_DIST = 60;
	const int SMART_MAX_AID_DIST = SMART_ESCORT_MAX_PLAYER_DIST / 2;
	readonly SmartScript _script = new();
	readonly WaypointPath _path = new();

	// Vehicle conditions
	readonly bool _hasConditions;

	bool _isCharmed;
	uint _followCreditType;
	uint _followArrivedTimer;
	uint _followCredit;
	uint _followArrivedEntry;
	ObjectGuid _followGuid;
	float _followDist;
	float _followAngle;

	SmartEscortState _escortState;
	uint _escortNPCFlags;
	uint _escortInvokerCheckTimer;
	uint _currentWaypointNode;
	bool _waypointReached;
	uint _waypointPauseTimer;
	bool _waypointPauseForced;
	bool _repeatWaypointPath;
	bool _OOCReached;
	bool _waypointPathEnded;

	bool _run;
	bool _evadeDisabled;
	bool _canCombatMove;
	uint _invincibilityHpLevel;

	uint _despawnTime;
	uint _despawnState;
	uint _conditionsTimer;

	// Gossip
	bool _gossipReturn;

	public SmartAI(Creature creature) : base(creature)
	{
		_escortInvokerCheckTimer = 1000;
		_run = true;
		_canCombatMove = true;

		_hasConditions = Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, creature.Entry);
	}

	public void StartPath(bool run = false, uint pathId = 0, bool repeat = false, Unit invoker = null, uint nodeId = 1)
	{
		if (HasEscortState(SmartEscortState.Escorting))
			StopPath();

		SetRun(run);

		if (pathId != 0)
			if (!LoadPath(pathId))
				return;

		if (_path.nodes.Empty())
			return;

		_currentWaypointNode = nodeId;
		_waypointPathEnded = false;

		_repeatWaypointPath = repeat;

		// Do not use AddEscortState, removing everything from previous
		_escortState = SmartEscortState.Escorting;

		if (invoker && invoker.IsPlayer)
		{
			_escortNPCFlags = (uint)Me.NpcFlags;
			Me.ReplaceAllNpcFlags(NPCFlags.None);
		}

		Me.MotionMaster.MovePath(_path, _repeatWaypointPath);
	}

	public void PausePath(uint delay, bool forced)
	{
		if (!HasEscortState(SmartEscortState.Escorting))
		{
			Me.PauseMovement(delay, MovementSlot.Default, forced);

			if (Me.MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
			{
				var (nodeId, pathId) = Me.CurrentWaypointInfo;
				GetScript().ProcessEventsFor(SmartEvents.WaypointPaused, null, nodeId, pathId);
			}

			return;
		}

		if (HasEscortState(SmartEscortState.Paused))
		{
			Log.Logger.Error($"SmartAI.PausePath: Creature entry {Me.Entry} wanted to pause waypoint movement while already paused, ignoring.");

			return;
		}

		_waypointPauseTimer = delay;

		if (forced)
		{
			_waypointPauseForced = forced;
			SetRun(_run);
			Me.PauseMovement();
			Me.HomePosition = Me.Location;
		}
		else
		{
			_waypointReached = false;
		}

		AddEscortState(SmartEscortState.Paused);
		GetScript().ProcessEventsFor(SmartEvents.WaypointPaused, null, _currentWaypointNode, GetScript().GetPathId());
	}

	public bool CanResumePath()
	{
		if (!HasEscortState(SmartEscortState.Escorting))
			// The whole resume logic doesn't support this case
			return false;

		return HasEscortState(SmartEscortState.Paused);
	}

	public void StopPath(uint despawnTime = 0, uint quest = 0, bool fail = false)
	{
		if (!HasEscortState(SmartEscortState.Escorting))
		{
			(uint nodeId, uint pathId) waypointInfo = new();

			if (Me.MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
				waypointInfo = Me.CurrentWaypointInfo;

			if (_despawnState != 2)
				SetDespawnTime(despawnTime);

			Me.MotionMaster.MoveIdle();

			if (waypointInfo.Item1 != 0)
				GetScript().ProcessEventsFor(SmartEvents.WaypointStopped, null, waypointInfo.Item1, waypointInfo.Item2);

			if (!fail)
			{
				if (waypointInfo.Item1 != 0)
					GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, waypointInfo.Item1, waypointInfo.Item2);

				if (_despawnState == 1)
					StartDespawn();
			}

			return;
		}

		if (quest != 0)
			EscortQuestID = quest;

		if (_despawnState != 2)
			SetDespawnTime(despawnTime);

		Me.MotionMaster.MoveIdle();

		GetScript().ProcessEventsFor(SmartEvents.WaypointStopped, null, _currentWaypointNode, GetScript().GetPathId());

		EndPath(fail);
	}

	public void EndPath(bool fail = false)
	{
		RemoveEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning);
		_path.nodes.Clear();
		_waypointPauseTimer = 0;

		if (_escortNPCFlags != 0)
		{
			Me.ReplaceAllNpcFlags((NPCFlags)_escortNPCFlags);
			_escortNPCFlags = 0;
		}

		var targets = GetScript().GetStoredTargetList(SharedConst.SmartEscortTargets, Me);

		if (targets != null && EscortQuestID != 0)
		{
			if (targets.Count == 1 && GetScript().IsPlayer(targets.First()))
			{
				var player = targets.First().AsPlayer;

				if (!fail && player.IsAtGroupRewardDistance(Me) && player.GetCorpse() == null)
					player.GroupEventHappens(EscortQuestID, Me);

				if (fail)
					player.FailQuest(EscortQuestID);

				var group = player.Group;

				if (group)
					for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
					{
						var groupGuy = groupRef.Source;

						if (!groupGuy.IsInMap(player))
							continue;

						if (!fail && groupGuy.IsAtGroupRewardDistance(Me) && !groupGuy.GetCorpse())
							groupGuy.AreaExploredOrEventHappens(EscortQuestID);
						else if (fail)
							groupGuy.FailQuest(EscortQuestID);
					}
			}
			else
			{
				foreach (var obj in targets)
					if (GetScript().IsPlayer(obj))
					{
						var player = obj.AsPlayer;

						if (!fail && player.IsAtGroupRewardDistance(Me) && player.GetCorpse() == null)
							player.AreaExploredOrEventHappens(EscortQuestID);
						else if (fail)
							player.FailQuest(EscortQuestID);
					}
			}
		}

		// End Path events should be only processed if it was SUCCESSFUL stop or stop called by SMART_ACTION_WAYPOINT_STOP
		if (fail)
			return;

		var pathid = GetScript().GetPathId();
		GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, _currentWaypointNode, pathid);

		if (_repeatWaypointPath)
		{
			if (IsAIControlled())
				StartPath(_run, GetScript().GetPathId(), _repeatWaypointPath);
		}
		else if (pathid == GetScript().GetPathId()) // if it's not the same pathid, our script wants to start another path; don't override it
		{
			GetScript().SetPathId(0);
		}

		if (_despawnState == 1)
			StartDespawn();
	}

	public void ResumePath()
	{
		GetScript().ProcessEventsFor(SmartEvents.WaypointResumed, null, _currentWaypointNode, GetScript().GetPathId());

		RemoveEscortState(SmartEscortState.Paused);

		_waypointPauseForced = false;
		_waypointReached = false;
		_waypointPauseTimer = 0;

		SetRun(_run);
		Me.ResumeMovement();
	}

	public override void UpdateAI(uint diff)
	{
		if (!Me.IsAlive)
		{
			if (IsEngaged)
				EngagementOver();

			return;
		}

		CheckConditions(diff);

		var hasVictim = UpdateVictim();

		GetScript().OnUpdate(diff);

		UpdatePath(diff);
		UpdateFollow(diff);
		UpdateDespawn(diff);

		if (!IsAIControlled())
			return;

		if (!hasVictim)
			return;

		DoMeleeAttackIfReady();
	}

	public override void WaypointReached(uint nodeId, uint pathId)
	{
		if (!HasEscortState(SmartEscortState.Escorting))
		{
			GetScript().ProcessEventsFor(SmartEvents.WaypointReached, null, nodeId, pathId);

			return;
		}

		_currentWaypointNode = nodeId;

		GetScript().ProcessEventsFor(SmartEvents.WaypointReached, null, _currentWaypointNode, pathId);

		if (_waypointPauseTimer != 0 && !_waypointPauseForced)
		{
			_waypointReached = true;
			Me.PauseMovement();
			Me.HomePosition = Me.Location;
		}
		else if (HasEscortState(SmartEscortState.Escorting) && Me.MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
		{
			if (_currentWaypointNode == _path.nodes.Count)
				_waypointPathEnded = true;
			else
				SetRun(_run);
		}
	}

	public override void WaypointPathEnded(uint nodeId, uint pathId)
	{
		if (!HasEscortState(SmartEscortState.Escorting))
		{
			GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, nodeId, pathId);

			return;
		}
	}

	public override void MovementInform(MovementGeneratorType movementType, uint id)
	{
		if (movementType == MovementGeneratorType.Point && id == EventId.SmartEscortLastOCCPoint)
			Me.ClearUnitState(UnitState.Evade);

		GetScript().ProcessEventsFor(SmartEvents.Movementinform, null, (uint)movementType, id);

		if (!HasEscortState(SmartEscortState.Escorting))
			return;

		if (movementType != MovementGeneratorType.Point && id == EventId.SmartEscortLastOCCPoint)
			_OOCReached = true;
	}

	public void StartAttackOnOwnersInCombatWith()
	{
		if (!Me.TryGetOwner(out Player owner))
			return;

		var summon = Me.ToTempSummon();

		if (summon != null)
		{
			var attack = owner.SelectedUnit;

			if (attack == null)
				attack = owner.Attackers.FirstOrDefault();

			if (attack != null)
				summon.Attack(attack, true);
		}
	}

	public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
	{
		if (_evadeDisabled)
		{
			GetScript().ProcessEventsFor(SmartEvents.Evade);

			return;
		}

		if (!IsAIControlled())
		{
			Me.AttackStop();

			return;
		}

		if (!_EnterEvadeMode())
			return;

		Me.AddUnitState(UnitState.Evade);

		GetScript().ProcessEventsFor(SmartEvents.Evade); // must be after _EnterEvadeMode (spells, auras, ...)

		SetRun(_run);

		var owner = Me.CharmerOrOwner;

		if (owner != null)
		{
			Me.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
			Me.ClearUnitState(UnitState.Evade);
		}
		else if (HasEscortState(SmartEscortState.Escorting))
		{
			AddEscortState(SmartEscortState.Returning);
			ReturnToLastOOCPos();
		}
		else
		{
			var target = !_followGuid.IsEmpty ? Global.ObjAccessor.GetUnit(Me, _followGuid) : null;

			if (target)
			{
				Me.MotionMaster.MoveFollow(target, _followDist, _followAngle);
				// evade is not cleared in MoveFollow, so we can't keep it
				Me.ClearUnitState(UnitState.Evade);
			}
			else
			{
				Me.MotionMaster.MoveTargetedHome();
			}
		}

		if (!Me.HasUnitState(UnitState.Evade))
			GetScript().OnReset();
	}

	public override void MoveInLineOfSight(Unit who)
	{
		if (who == null)
			return;

		GetScript().OnMoveInLineOfSight(who);

		if (!IsAIControlled())
			return;

		if (HasEscortState(SmartEscortState.Escorting) && AssistPlayerInCombatAgainst(who))
			return;

		base.MoveInLineOfSight(who);
	}

	public override void InitializeAI()
	{
		GetScript().OnInitialize(Me);

		_despawnTime = 0;
		_despawnState = 0;
		_escortState = SmartEscortState.None;

		_followGuid.Clear(); //do not reset follower on Reset(), we need it after combat evade
		_followDist = 0;
		_followAngle = 0;
		_followCredit = 0;
		_followArrivedTimer = 1000;
		_followArrivedEntry = 0;
		_followCreditType = 0;
	}

	public override void JustAppeared()
	{
		base.JustAppeared();

		if (Me.IsDead)
			return;

		GetScript().ProcessEventsFor(SmartEvents.Respawn);
		GetScript().OnReset();
	}

	public override void JustReachedHome()
	{
		GetScript().OnReset();
		GetScript().ProcessEventsFor(SmartEvents.ReachedHome);

		var formation = Me.Formation;

		if (formation == null || formation.Leader == Me || !formation.IsFormed)
		{
			if (Me.MotionMaster.GetCurrentMovementGeneratorType(MovementSlot.Default) != MovementGeneratorType.Waypoint)
				if (Me.WaypointPath != 0)
					Me.MotionMaster.MovePath(Me.WaypointPath, true);

			Me.ResumeMovement();
		}
		else if (formation.IsFormed)
		{
			Me.MotionMaster.MoveIdle(); // wait the order of leader
		}
	}

	public override void JustEngagedWith(Unit victim)
	{
		if (IsAIControlled())
			Me.InterruptNonMeleeSpells(false); // must be before ProcessEvents

		GetScript().ProcessEventsFor(SmartEvents.Aggro, victim);
	}

	public override void JustDied(Unit killer)
	{
		if (HasEscortState(SmartEscortState.Escorting))
			EndPath(true);

		GetScript().ProcessEventsFor(SmartEvents.Death, killer);
	}

	public override void KilledUnit(Unit victim)
	{
		GetScript().ProcessEventsFor(SmartEvents.Kill, victim);
	}

	public override void JustSummoned(Creature summon)
	{
		GetScript().ProcessEventsFor(SmartEvents.SummonedUnit, summon);
	}

	public override void SummonedCreatureDies(Creature summon, Unit killer)
	{
		GetScript().ProcessEventsFor(SmartEvents.SummonedUnitDies, summon);
	}

	public override void AttackStart(Unit who)
	{
		// dont allow charmed npcs to act on their own
		if (!IsAIControlled())
		{
			if (who != null)
				Me.Attack(who, true);

			return;
		}

		if (who != null && Me.Attack(who, true))
		{
			Me.MotionMaster.Clear(MovementGeneratorPriority.Normal);
			Me.PauseMovement();

			if (_canCombatMove)
			{
				SetRun(_run);
				Me.MotionMaster.MoveChase(who);
			}
		}
	}

	public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
	{
		GetScript().ProcessEventsFor(SmartEvents.SpellHit, caster.AsUnit, 0, 0, false, spellInfo, caster.AsGameObject);
	}

	public override void SpellHitTarget(WorldObject target, SpellInfo spellInfo)
	{
		GetScript().ProcessEventsFor(SmartEvents.SpellHitTarget, target.AsUnit, 0, 0, false, spellInfo, target.AsGameObject);
	}

	public override void OnSpellCast(SpellInfo spellInfo)
	{
		GetScript().ProcessEventsFor(SmartEvents.OnSpellCast, null, 0, 0, false, spellInfo);
	}

	public override void OnSpellFailed(SpellInfo spellInfo)
	{
		GetScript().ProcessEventsFor(SmartEvents.OnSpellFailed, null, 0, 0, false, spellInfo);
	}

	public override void OnSpellStart(SpellInfo spellInfo)
	{
		GetScript().ProcessEventsFor(SmartEvents.OnSpellStart, null, 0, 0, false, spellInfo);
	}

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		GetScript().ProcessEventsFor(SmartEvents.Damaged, attacker, (uint)damage);

		if (!IsAIControlled()) // don't allow players to use unkillable units
			return;

		if (_invincibilityHpLevel != 0 && (damage >= Me.Health - _invincibilityHpLevel))
			damage = (uint)(Me.Health - _invincibilityHpLevel); // damage should not be nullified, because of player damage req.
	}

	public override void HealReceived(Unit by, double addhealth)
	{
		GetScript().ProcessEventsFor(SmartEvents.ReceiveHeal, by, (uint)addhealth);
	}

	public override void ReceiveEmote(Player player, TextEmotes emoteId)
	{
		GetScript().ProcessEventsFor(SmartEvents.ReceiveEmote, player, (uint)emoteId);
	}

	public override void IsSummonedBy(WorldObject summoner)
	{
		GetScript().ProcessEventsFor(SmartEvents.JustSummoned, summoner.AsUnit, 0, 0, false, null, summoner.AsGameObject);
	}

	public override void DamageDealt(Unit victim, ref double damage, DamageEffectType damageType)
	{
		GetScript().ProcessEventsFor(SmartEvents.DamagedTarget, victim, (uint)damage);
	}

	public override void SummonedCreatureDespawn(Creature summon)
	{
		GetScript().ProcessEventsFor(SmartEvents.SummonDespawned, summon, summon.Entry);
	}

	public override void CorpseRemoved(long respawnDelay)
	{
		GetScript().ProcessEventsFor(SmartEvents.CorpseRemoved, null, (uint)respawnDelay);
	}

	public override void OnDespawn()
	{
		GetScript().ProcessEventsFor(SmartEvents.OnDespawn);
	}

	public override void PassengerBoarded(Unit passenger, sbyte seatId, bool apply)
	{
		GetScript().ProcessEventsFor(apply ? SmartEvents.PassengerBoarded : SmartEvents.PassengerRemoved, passenger, (uint)seatId, 0, apply);
	}

	public override void OnCharmed(bool isNew)
	{
		var charmed = Me.IsCharmed;

		if (charmed) // do this before we change charmed state, as charmed state might prevent these things from processing
			if (HasEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning))
				EndPath(true);

		_isCharmed = charmed;

		if (charmed && !Me.IsPossessed && !Me.IsVehicle)
			Me.MotionMaster.MoveFollow(Me.Charmer, SharedConst.PetFollowDist, Me.FollowAngle);

		if (!charmed && !Me.IsInEvadeMode)
		{
			if (_repeatWaypointPath)
				StartPath(_run, GetScript().GetPathId(), true);
			else
				Me.SetWalk(!_run);

			if (!Me.LastCharmerGuid.IsEmpty)
			{
				if (!Me.HasReactState(ReactStates.Passive))
				{
					var lastCharmer = Global.ObjAccessor.GetUnit(Me, Me.LastCharmerGuid);

					if (lastCharmer != null)
						Me.EngageWithTarget(lastCharmer);
				}

				Me.LastCharmerGuid.Clear();

				if (!Me.IsInCombat)
					EnterEvadeMode(EvadeReason.NoHostiles);
			}
		}

		GetScript().ProcessEventsFor(SmartEvents.Charmed, null, 0, 0, charmed);

		if (!GetScript().HasAnyEventWithFlag(SmartEventFlags.WhileCharmed)) // we can change AI if there are no events with this flag
			base.OnCharmed(isNew);
	}

	public override void DoAction(int param)
	{
		GetScript().ProcessEventsFor(SmartEvents.ActionDone, null, (uint)param);
	}

	public override uint GetData(uint id)
	{
		return 0;
	}

	public override void SetData(uint id, uint value)
	{
		SetData(id, value, null);
	}

	public void SetData(uint id, uint value, Unit invoker)
	{
		GetScript().ProcessEventsFor(SmartEvents.DataSet, invoker, id, value);
	}

	public override void SetGUID(ObjectGuid guid, int id) { }

	public override ObjectGuid GetGUID(int id)
	{
		return ObjectGuid.Empty;
	}

	public void SetRun(bool run)
	{
		Me.SetWalk(!run);
		_run = run;

		foreach (var node in _path.nodes)
			node.moveType = run ? WaypointMoveType.Run : WaypointMoveType.Walk;
	}

	public void SetDisableGravity(bool disable = true)
	{
		Me.SetDisableGravity(disable);
	}

	public void SetEvadeDisabled(bool disable)
	{
		_evadeDisabled = disable;
	}

	public override bool OnGossipHello(Player player)
	{
		_gossipReturn = false;
		GetScript().ProcessEventsFor(SmartEvents.GossipHello, player);

		return _gossipReturn;
	}

	public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
	{
		_gossipReturn = false;
		GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, menuId, gossipListId);

		return _gossipReturn;
	}

	public override bool OnGossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
	{
		return false;
	}

	public override void OnQuestAccept(Player player, Quest quest)
	{
		GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id);
	}

	public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
	{
		GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt);
	}

	public void SetCombatMove(bool on, bool stopMoving = false)
	{
		if (_canCombatMove == on)
			return;

		_canCombatMove = on;

		if (!IsAIControlled())
			return;

		if (Me.IsEngaged)
		{
			if (on)
			{
				if (!Me.HasReactState(ReactStates.Passive) && Me.Victim && !Me.MotionMaster.HasMovementGenerator(movement => { return movement.GetMovementGeneratorType() == MovementGeneratorType.Chase && movement.Mode == MovementGeneratorMode.Default && movement.Priority == MovementGeneratorPriority.Normal; }))
				{
					SetRun(_run);
					Me.MotionMaster.MoveChase(Me.Victim);
				}
			}
			else
			{
				var movement = Me.MotionMaster.GetMovementGenerator(a => a.GetMovementGeneratorType() == MovementGeneratorType.Chase && a.Mode == MovementGeneratorMode.Default && a.Priority == MovementGeneratorPriority.Normal);

				if (movement != null)
				{
					Me.MotionMaster.Remove(movement);

					if (stopMoving)
						Me.StopMoving();
				}
			}
		}
	}

	public void SetFollow(Unit target, float dist, float angle, uint credit, uint end, uint creditType)
	{
		if (target == null)
		{
			StopFollow(false);

			return;
		}

		_followGuid = target.GUID;
		_followDist = dist;
		_followAngle = angle;
		_followArrivedTimer = 1000;
		_followCredit = credit;
		_followArrivedEntry = end;
		_followCreditType = creditType;
		SetRun(_run);
		Me.MotionMaster.MoveFollow(target, _followDist, _followAngle);
	}

	public void StopFollow(bool complete)
	{
		_followGuid.Clear();
		_followDist = 0;
		_followAngle = 0;
		_followCredit = 0;
		_followArrivedTimer = 1000;
		_followArrivedEntry = 0;
		_followCreditType = 0;
		Me.MotionMaster.Clear();
		Me.StopMoving();
		Me.MotionMaster.MoveIdle();

		if (!complete)
			return;

		var player = Global.ObjAccessor.GetPlayer(Me, _followGuid);

		if (player != null)
		{
			if (_followCreditType == 0)
				player.RewardPlayerAndGroupAtEvent(_followCredit, Me);
			else
				player.GroupEventHappens(_followCredit, Me);
		}

		SetDespawnTime(5000);
		StartDespawn();
		GetScript().ProcessEventsFor(SmartEvents.FollowCompleted, player);
	}

	public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker, uint startFromEventId = 0)
	{
		GetScript().SetTimedActionList(e, entry, invoker, startFromEventId);
	}

	public override void OnGameEvent(bool start, ushort eventId)
	{
		GetScript().ProcessEventsFor(start ? SmartEvents.GameEventStart : SmartEvents.GameEventEnd, null, eventId);
	}

	public override void OnSpellClick(Unit clicker, ref bool spellClickHandled)
	{
		if (!spellClickHandled)
			return;

		GetScript().ProcessEventsFor(SmartEvents.OnSpellclick, clicker);
	}

	public override void Reset()
	{
		if (!HasEscortState(SmartEscortState.Escorting)) //dont mess up escort movement after combat
			SetRun(_run);

		GetScript().OnReset();
	}

	public bool HasEscortState(SmartEscortState escortState)
	{
		return (_escortState & escortState) != 0;
	}

	public void AddEscortState(SmartEscortState escortState)
	{
		_escortState |= escortState;
	}

	public void RemoveEscortState(SmartEscortState escortState)
	{
		_escortState &= ~escortState;
	}

	public bool CanCombatMove()
	{
		return _canCombatMove;
	}

	public SmartScript GetScript()
	{
		return _script;
	}

	public void SetInvincibilityHpLevel(uint level)
	{
		_invincibilityHpLevel = level;
	}

	public void SetDespawnTime(uint t, uint r = 0)
	{
		_despawnTime = t;
		_despawnState = t != 0 ? 1 : 0u;
	}

	public void StartDespawn()
	{
		_despawnState = 2;
	}

	public void SetWPPauseTimer(uint time)
	{
		_waypointPauseTimer = time;
	}

	public void SetGossipReturn(bool val)
	{
		_gossipReturn = val;
	}

	bool IsAIControlled()
	{
		return !_isCharmed;
	}

	bool LoadPath(uint entry)
	{
		if (HasEscortState(SmartEscortState.Escorting))
			return false;

		var path = Global.SmartAIMgr.GetPath(entry);

		if (path == null || path.nodes.Empty())
		{
			GetScript().SetPathId(0);

			return false;
		}

		_path.id = path.id;
		_path.nodes.AddRange(path.nodes);

		foreach (var waypoint in _path.nodes)
		{
			waypoint.x = GridDefines.NormalizeMapCoord(waypoint.x);
			waypoint.y = GridDefines.NormalizeMapCoord(waypoint.y);
			waypoint.moveType = _run ? WaypointMoveType.Run : WaypointMoveType.Walk;
		}

		GetScript().SetPathId(entry);

		return true;
	}

	void ReturnToLastOOCPos()
	{
		if (!IsAIControlled())
			return;

		Me.SetWalk(false);
		Me.MotionMaster.MovePoint(EventId.SmartEscortLastOCCPoint, Me.HomePosition);
	}

	bool IsEscortInvokerInRange()
	{
		var targets = GetScript().GetStoredTargetList(SharedConst.SmartEscortTargets, Me);

		if (targets != null)
		{
			float checkDist = Me.InstanceScript != null ? SMART_ESCORT_MAX_PLAYER_DIST * 2 : SMART_ESCORT_MAX_PLAYER_DIST;

			if (targets.Count == 1 && GetScript().IsPlayer(targets.First()))
			{
				var player = targets.First().AsPlayer;

				if (Me.GetDistance(player) <= checkDist)
					return true;

				var group = player.Group;

				if (group)
					for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
					{
						var groupGuy = groupRef.Source;

						if (groupGuy.IsInMap(player) && Me.GetDistance(groupGuy) <= checkDist)
							return true;
					}
			}
			else
			{
				foreach (var obj in targets)
					if (GetScript().IsPlayer(obj))
						if (Me.GetDistance(obj.AsPlayer) <= checkDist)
							return true;
			}

			// no valid target found
			return false;
		}

		// no player invoker was stored, just ignore range check
		return true;
	}

	bool AssistPlayerInCombatAgainst(Unit who)
	{
		if (Me.HasReactState(ReactStates.Passive) || !IsAIControlled())
			return false;

		if (who == null || who.Victim == null)
			return false;

		//experimental (unknown) flag not present
		if (!Me.Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
			return false;

		//not a player
		if (who.Victim.CharmerOrOwnerPlayerOrPlayerItself == null)
			return false;

		if (!who.IsInAccessiblePlaceFor(Me))
			return false;

		if (!CanAIAttack(who))
			return false;

		// we cannot attack in evade mode
		if (Me.IsInEvadeMode)
			return false;

		// or if enemy is in evade mode
		if (who.IsCreature && who.AsCreature.IsInEvadeMode)
			return false;

		if (!Me.IsValidAssistTarget(who.Victim))
			return false;

		//too far away and no free sight
		if (Me.IsWithinDistInMap(who, SMART_MAX_AID_DIST) && Me.IsWithinLOSInMap(who))
		{
			Me.EngageWithTarget(who);

			return true;
		}

		return false;
	}

	void CheckConditions(uint diff)
	{
		if (!_hasConditions)
			return;

		if (_conditionsTimer <= diff)
		{
			var vehicleKit = Me.VehicleKit1;

			if (vehicleKit != null)
				foreach (var pair in vehicleKit.Seats)
				{
					var passenger = Global.ObjAccessor.GetUnit(Me, pair.Value.Passenger.Guid);

					if (passenger != null)
					{
						var player = passenger.AsPlayer;

						if (player != null)
							if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.CreatureTemplateVehicle, Me.Entry, player, Me))
							{
								player.ExitVehicle();

								return; // check other pessanger in next tick
							}
					}
				}

			_conditionsTimer = 1000;
		}
		else
		{
			_conditionsTimer -= diff;
		}
	}

	void UpdatePath(uint diff)
	{
		if (!HasEscortState(SmartEscortState.Escorting))
			return;

		if (_escortInvokerCheckTimer < diff)
		{
			if (!IsEscortInvokerInRange())
			{
				StopPath(0, EscortQuestID, true);

				// allow to properly hook out of range despawn action, which in most cases should perform the same operation as dying
				GetScript().ProcessEventsFor(SmartEvents.Death, Me);
				Me.DespawnOrUnsummon();

				return;
			}

			_escortInvokerCheckTimer = 1000;
		}
		else
		{
			_escortInvokerCheckTimer -= diff;
		}

		// handle pause
		if (HasEscortState(SmartEscortState.Paused) && (_waypointReached || _waypointPauseForced))
		{
			// Resume only if there was a pause timer set
			if (_waypointPauseTimer != 0 && !Me.IsInCombat && !HasEscortState(SmartEscortState.Returning))
			{
				if (_waypointPauseTimer <= diff)
					ResumePath();
				else
					_waypointPauseTimer -= diff;
			}
		}
		else if (_waypointPathEnded) // end path
		{
			_waypointPathEnded = false;
			StopPath();

			return;
		}

		if (HasEscortState(SmartEscortState.Returning))
			if (_OOCReached) //reached OOC WP
			{
				_OOCReached = false;
				RemoveEscortState(SmartEscortState.Returning);

				if (!HasEscortState(SmartEscortState.Paused))
					ResumePath();
			}
	}

	void UpdateFollow(uint diff)
	{
		if (_followGuid.IsEmpty)
		{
			if (_followArrivedTimer < diff)
			{
				if (Me.FindNearestCreature(_followArrivedEntry, SharedConst.InteractionDistance, true))
				{
					StopFollow(true);

					return;
				}

				_followArrivedTimer = 1000;
			}
			else
			{
				_followArrivedTimer -= diff;
			}
		}
	}

	void UpdateDespawn(uint diff)
	{
		if (_despawnState <= 1 || _despawnState > 3)
			return;

		if (_despawnTime < diff)
		{
			if (_despawnState == 2)
			{
				Me.SetVisible(false);
				_despawnTime = 5000;
				_despawnState++;
			}
			else
			{
				Me.DespawnOrUnsummon();
			}
		}
		else
		{
			_despawnTime -= diff;
		}
	}
}