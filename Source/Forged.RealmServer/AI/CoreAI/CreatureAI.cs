// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Forged.RealmServer.Combat;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Spells;
using Game.Common.Entities;
using Game.Common.Entities.AreaTriggers;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.AI;

public class CreatureAI : UnitAI
{
	protected new readonly Creature Me;

	protected EventMap Events = new();
	protected TaskScheduler SchedulerProtected = new();
	protected InstanceScript Script;
	bool _isEngaged;
	bool _moveInLosLocked;
	List<AreaBoundary> _boundary = new();
	bool _negateBoundary;

	public TaskScheduler Scheduler
	{
		get { return SchedulerProtected; }
	}

	public List<AreaBoundary> Boundary => _boundary;

	public bool IsEngaged => _isEngaged;

	public CreatureAI(Creature creature) : base(creature)
	{
		Me = creature;
		_moveInLosLocked = false;
	}

	public void Talk(uint id, WorldObject whisperTarget = null)
	{
		Global.CreatureTextMgr.SendChat(Me, (byte)id, whisperTarget);
	}

	public override void OnCharmed(bool isNew)
	{
		if (isNew && !Me.IsCharmed && !Me.LastCharmerGuid.IsEmpty)
		{
			if (!Me.HasReactState(ReactStates.Passive))
			{
				var lastCharmer = Global.ObjAccessor.GetUnit(Me, Me.LastCharmerGuid);

				if (lastCharmer != null)
					Me.EngageWithTarget(lastCharmer);
			}

			Me.LastCharmerGuid.Clear();
		}

		base.OnCharmed(isNew);
	}

	public void DoZoneInCombat(Creature creature = null)
	{
		if (creature == null)
			creature = Me;

		var map = creature.Map;

		if (!map.IsDungeon) // use IsDungeon instead of Instanceable, in case Battlegrounds will be instantiated
		{
			Log.outError(LogFilter.Server, "DoZoneInCombat call for map that isn't an instance (creature entry = {0})", creature.IsTypeId(TypeId.Unit) ? creature.AsCreature.Entry : 0);

			return;
		}

		if (!map.HavePlayers)
			return;

		foreach (var player in map.Players)
			if (player != null)
			{
				if (!player.IsAlive || !CombatManager.CanBeginCombat(creature, player))
					continue;

				creature.EngageWithTarget(player);

				foreach (var pet in player.Controlled)
					creature.EngageWithTarget(pet);

				var vehicle = player.VehicleBase;

				if (vehicle != null)
					creature.EngageWithTarget(vehicle);
			}
	}

	public virtual void MoveInLineOfSight_Safe(Unit who)
	{
		if (_moveInLosLocked)
			return;

		_moveInLosLocked = true;
		MoveInLineOfSight(who);
		_moveInLosLocked = false;
	}

	public virtual void MoveInLineOfSight(Unit who)
	{
		if (Me.IsEngaged)
			return;

		if (Me.HasReactState(ReactStates.Aggressive) && Me.CanStartAttack(who, false))
			Me.EngageWithTarget(who);
	}

	// Distract creature, if player gets too close while stealthed/prowling
	public void TriggerAlert(Unit who)
	{
		// If there's no target, or target isn't a player do nothing
		if (!who || !who.IsTypeId(TypeId.Player))
			return;

		// If this unit isn't an NPC, is already distracted, is fighting, is confused, stunned or fleeing, do nothing
		if (!Me.IsTypeId(TypeId.Unit) || Me.IsEngaged || Me.HasUnitState(UnitState.Confused | UnitState.Stunned | UnitState.Fleeing | UnitState.Distracted))
			return;

		// Only alert for hostiles!
		if (Me.IsCivilian || Me.HasReactState(ReactStates.Passive) || !Me.IsHostileTo(who) || !Me._IsTargetAcceptable(who))
			return;

		// Send alert sound (if any) for this creature
		Me.SendAIReaction(AiReaction.Alert);

		// Face the unit (stealthed player) and set distracted state for 5 seconds
		Me.
			// Face the unit (stealthed player) and set distracted state for 5 seconds
			MotionMaster.MoveDistract(5 * Time.InMilliseconds, Me.Location.GetAbsoluteAngle(who.Location));
	}

	// adapted from logic in Spell:EffectSummonType
	public static bool ShouldFollowOnSpawn(SummonPropertiesRecord properties)
	{
		// Summons without SummonProperties are generally scripted summons that don't belong to any owner
		if (properties == null)
			return false;

		switch (properties.Control)
		{
			case SummonCategory.Pet:
				return true;
			case SummonCategory.Wild:
			case SummonCategory.Ally:
			case SummonCategory.Unk:
				if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup))
					return true;

				switch (properties.Title)
				{
					case SummonTitle.Pet:
					case SummonTitle.Guardian:
					case SummonTitle.Runeblade:
					case SummonTitle.Minion:
					case SummonTitle.Companion:
						return true;
					default:
						return false;
				}
			default:
				return false;
		}
	}

	// Called when creature appears in the world (spawn, respawn, grid load etc...)
	public virtual void JustAppeared()
	{
		if (!IsEngaged)
		{
			var summon = Me.ToTempSummon();

			if (summon != null)
				// Only apply this to specific types of summons
				if (!summon.Vehicle1 && ShouldFollowOnSpawn(summon.SummonPropertiesRecord) && summon.CanFollowOwner())
				{
					var owner = summon.CharmerOrOwner;

					if (owner != null)
					{
						summon.MotionMaster.Clear();
						summon.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, summon.FollowAngle);
					}
				}
		}
	}

	public override void JustEnteredCombat(Unit who)
	{
		if (!IsEngaged && !Me.CanHaveThreatList)
			EngagementStart(who);
	}

	// Called for reaction at stopping attack at no attackers or targets
	public virtual void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
	{
		if (!_EnterEvadeMode(why))
			return;

		Log.outDebug(LogFilter.Unit, $"CreatureAI::EnterEvadeMode: entering evade mode (why: {why}) ({Me.GUID})");

		if (Me.Vehicle1 == null) // otherwise me will be in evade mode forever
		{
			var owner = Me.CharmerOrOwner;

			if (owner != null)
			{
				Me.MotionMaster.Clear();
				Me.MotionMaster.MoveFollow(owner, SharedConst.PetFollowDist, Me.FollowAngle);
			}
			else
			{
				// Required to prevent attacking creatures that are evading and cause them to reenter combat
				// Does not apply to MoveFollow
				Me.AddUnitState(UnitState.Evade);
				Me.MotionMaster.MoveTargetedHome();
			}
		}

		Reset();
	}

	public bool UpdateVictim()
	{
		if (!IsEngaged)
			return false;

		if (!Me.IsAlive)
		{
			EngagementOver();

			return false;
		}

		if (!Me.HasReactState(ReactStates.Passive))
		{
			var victim = Me.SelectVictim();

			if (victim != null && victim != Me.Victim)
				AttackStart(victim);

			return Me.Victim != null;
		}
		else if (!Me.IsInCombat)
		{
			EnterEvadeMode(EvadeReason.NoHostiles);

			return false;
		}
		else if (Me.Victim != null)
		{
			Me.AttackStop();
		}

		return true;
	}

	public void EngagementStart(Unit who)
	{
		if (_isEngaged)
		{
			Log.outError(LogFilter.ScriptsAi, $"CreatureAI::EngagementStart called even though creature is already engaged. Creature debug info:\n{Me.GetDebugInfo()}");

			return;
		}

		_isEngaged = true;

		Me.AtEngage(who);
	}

	public void EngagementOver()
	{
		if (!_isEngaged)
		{
			Log.outDebug(LogFilter.ScriptsAi, $"CreatureAI::EngagementOver called even though creature is not currently engaged. Creature debug info:\n{Me.GetDebugInfo()}");

			return;
		}

		_isEngaged = false;

		Me.AtDisengage();
	}

	public bool _EnterEvadeMode(EvadeReason why = EvadeReason.Other)
	{
		if (Me.IsInEvadeMode)
			return false;

		if (!Me.IsAlive)
		{
			EngagementOver();

			return false;
		}

		Me.RemoveAurasOnEvade();

		// sometimes bosses stuck in combat?
		Me.CombatStop(true);
		Me.SetTappedBy(null);
		Me.ResetPlayerDamageReq();
		Me.LastDamagedTime = 0;
		Me.SetCannotReachTarget(false);
		Me.DoNotReacquireSpellFocusTarget();
		Me.SetTarget(ObjectGuid.Empty);
		Me.SpellHistory.ResetAllCooldowns();
		EngagementOver();

		return true;
	}

	public CypherStrings VisualizeBoundary(TimeSpan duration, Unit owner = null, bool fill = false)
	{
		if (owner == null)
			return 0;

		if (_boundary.Empty())
			return CypherStrings.CreatureMovementNotBounded;

		List<KeyValuePair<int, int>> q = new();
		List<KeyValuePair<int, int>> alreadyChecked = new();
		List<KeyValuePair<int, int>> outOfBounds = new();

		Position startPosition = owner.Location;

		if (!IsInBoundary(startPosition)) // fall back to creature position
		{
			startPosition = Me.Location;

			if (!IsInBoundary(startPosition))
			{
				startPosition = Me.HomePosition;

				if (!IsInBoundary(startPosition)) // fall back to creature home position
					return CypherStrings.CreatureNoInteriorPointFound;
			}
		}

		var spawnZ = startPosition.Z + SharedConst.BoundaryVisualizeSpawnHeight;

		var boundsWarning = false;
		q.Add(new KeyValuePair<int, int>(0, 0));

		while (!q.Empty())
		{
			var front = q.First();
			var hasOutOfBoundsNeighbor = false;

			foreach (var off in new List<KeyValuePair<int, int>>()
					{
						new(1, 0),
						new(0, 1),
						new(-1, 0),
						new(0, -1)
					})
			{
				var next = new KeyValuePair<int, int>(front.Key + off.Key, front.Value + off.Value);

				if (next.Key > SharedConst.BoundaryVisualizeFailsafeLimit || next.Key < -SharedConst.BoundaryVisualizeFailsafeLimit || next.Value > SharedConst.BoundaryVisualizeFailsafeLimit || next.Value < -SharedConst.BoundaryVisualizeFailsafeLimit)
				{
					boundsWarning = true;

					continue;
				}

				if (!alreadyChecked.Contains(next)) // never check a coordinate twice
				{
					Position nextPos = new(startPosition.X + next.Key * SharedConst.BoundaryVisualizeStepSize, startPosition.Y + next.Value * SharedConst.BoundaryVisualizeStepSize, startPosition.Z);

					if (IsInBoundary(nextPos))
					{
						q.Add(next);
					}
					else
					{
						outOfBounds.Add(next);
						hasOutOfBoundsNeighbor = true;
					}

					alreadyChecked.Add(next);
				}
				else if (outOfBounds.Contains(next))
				{
					hasOutOfBoundsNeighbor = true;
				}
			}

			if (fill || hasOutOfBoundsNeighbor)
			{
				var pos = new Position(startPosition.X + front.Key * SharedConst.BoundaryVisualizeStepSize, startPosition.Y + front.Value * SharedConst.BoundaryVisualizeStepSize, spawnZ);
				var point = owner.SummonCreature(SharedConst.BoundaryVisualizeCreature, pos, TempSummonType.TimedDespawn, duration);

				if (point)
				{
					point.ObjectScale = SharedConst.BoundaryVisualizeCreatureScale;
					point.SetUnitFlag(UnitFlags.Stunned);
					point.SetImmuneToAll(true);

					if (!hasOutOfBoundsNeighbor)
						point.SetUnitFlag(UnitFlags.Uninteractible);
				}

				q.Remove(front);
			}
		}

		return boundsWarning ? CypherStrings.CreatureMovementMaybeUnbounded : 0;
	}

	public bool IsInBoundary(Position who = null)
	{
		if (_boundary == null)
			return true;

		if (who == null)
			who = Me.Location;

		return IsInBounds(_boundary, who) != _negateBoundary;
	}

	public virtual bool CheckInRoom()
	{
		if (IsInBoundary())
		{
			return true;
		}
		else
		{
			EnterEvadeMode(EvadeReason.Boundary);

			return false;
		}
	}

	public Creature DoSummon(uint entry, Position pos, TimeSpan despawnTime, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
	{
		return Me.SummonCreature(entry, pos, summonType, despawnTime);
	}

	public Creature DoSummon(uint entry, WorldObject obj, float radius = 5.0f, TimeSpan despawnTime = default, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
	{
		var pos = obj.GetRandomNearPosition(radius);

		return Me.SummonCreature(entry, pos, summonType, despawnTime);
	}

	public Creature DoSummonFlyer(uint entry, WorldObject obj, float flightZ, float radius = 5.0f, TimeSpan despawnTime = default, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
	{
		var pos = obj.GetRandomNearPosition(radius);
		pos.Z += flightZ;

		return Me.SummonCreature(entry, pos, summonType, despawnTime);
	}

	public static bool IsInBounds(List<AreaBoundary> boundary, Position pos)
	{
		foreach (var areaBoundary in boundary)
			if (!areaBoundary.IsWithinBoundary(pos))
				return false;

		return true;
	}

	public void SetBoundary(List<AreaBoundary> boundary, bool negateBoundaries = false)
	{
		_boundary = boundary;
		_negateBoundary = negateBoundaries;
		Me.DoImmediateBoundaryCheck();
	}

	// Called for reaction whenever a new non-offline unit is added to the threat list
	public virtual void JustStartedThreateningMe(Unit who)
	{
		if (!IsEngaged)
			EngagementStart(who);
	}

	// Called for reaction when initially engaged - this will always happen _after_ JustEnteredCombat
	public virtual void JustEngagedWith(Unit who) { }

	// Called when the creature is killed
	public virtual void JustDied(Unit killer) { }

	// Called when the creature kills a unit
	public virtual void KilledUnit(Unit victim) { }

	// Called when the creature summon successfully other creature
	public virtual void JustSummoned(Creature summon) { }
	public virtual void IsSummonedBy(WorldObject summoner) { }

	public virtual void SummonedCreatureDespawn(Creature summon) { }
	public virtual void SummonedCreatureDies(Creature summon, Unit killer) { }

	// Called when the creature successfully summons a gameobject
	public virtual void JustSummonedGameobject(GameObject gameobject) { }
	public virtual void SummonedGameobjectDespawn(GameObject gameobject) { }

	// Called when the creature successfully registers a dynamicobject
	public virtual void JustRegisteredDynObject(DynamicObject dynObject) { }
	public virtual void JustUnregisteredDynObject(DynamicObject dynObject) { }

	// Called when the creature successfully registers an areatrigger
	public virtual void JustRegisteredAreaTrigger(AreaTrigger areaTrigger) { }
	public virtual void JustUnregisteredAreaTrigger(AreaTrigger areaTrigger) { }

	// Called when hit by a spell
	public virtual void SpellHit(WorldObject caster, SpellInfo spellInfo) { }

	// Called when spell hits a target
	public virtual void SpellHitTarget(WorldObject target, SpellInfo spellInfo) { }

	// Called when a spell finishes
	public virtual void OnSpellCast(SpellInfo spell) { }

	// Called when a spell fails
	public virtual void OnSpellFailed(SpellInfo spell) { }

	// Called when a spell starts
	public virtual void OnSpellStart(SpellInfo spell) { }

	// Called when a channeled spell finishes
	public virtual void OnChannelFinished(SpellInfo spell) { }

	// Should return true if the NPC is currently being escorted
	public virtual bool IsEscorted()
	{
		return false;
	}

	// Called at waypoint reached or point movement finished
	public virtual void MovementInform(MovementGeneratorType type, uint id) { }

	// Called at reaching home after evade
	public virtual void JustReachedHome() { }

	// Called at text emote receive from player
	public virtual void ReceiveEmote(Player player, TextEmotes emoteId) { }

	// Called when owner takes damage
	public virtual void OwnerAttackedBy(Unit attacker)
	{
		OnOwnerCombatInteraction(attacker);
	}

	// Called when owner attacks something
	public virtual void OwnerAttacked(Unit target)
	{
		OnOwnerCombatInteraction(target);
	}

	// called when the corpse of this creature gets removed
	public virtual void CorpseRemoved(long respawnDelay) { }

	/// == Gossip system ================================

	// Called when the dialog status between a player and the creature is requested.
	public virtual QuestGiverStatus? GetDialogStatus(Player player)
	{
		return null;
	}

	// Called when a player opens a gossip dialog with the creature.
	public virtual bool OnGossipHello(Player player)
	{
		return false;
	}

	// Called when a player selects a gossip item in the creature's gossip menu.
	public virtual bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
	{
		return false;
	}

	// Called when a player selects a gossip with a code in the creature's gossip menu.
	public virtual bool OnGossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
	{
		return false;
	}

	// Called when a player accepts a quest from the creature.
	public virtual void OnQuestAccept(Player player, Quest quest) { }

	// Called when a player completes a quest and is rewarded, opt is the selected item's index or 0
	public virtual void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt) { }

	/// == Waypoints system =============================
	public virtual void WaypointStarted(uint nodeId, uint pathId) { }

	public virtual void WaypointReached(uint nodeId, uint pathId) { }

	public virtual void WaypointPathEnded(uint nodeId, uint pathId) { }

	public virtual void PassengerBoarded(Unit passenger, sbyte seatId, bool apply) { }

	public virtual void OnSpellClick(Unit clicker, ref bool spellClickHandled) { }

	public virtual bool CanSeeAlways(WorldObject obj)
	{
		return false;
	}

	// Called when a player is charmed by the creature
	// If a PlayerAI* is returned, that AI is placed on the player instead of the default charm AI
	// Object destruction is handled by Unit::RemoveCharmedBy
	public virtual PlayerAI GetAIForCharmedPlayer(Player who)
	{
		return null;
	}

	void OnOwnerCombatInteraction(Unit target)
	{
		if (target == null || !Me.IsAlive)
			return;

		if (!Me.HasReactState(ReactStates.Passive) && Me.CanStartAttack(target, true))
			Me.EngageWithTarget(target);
	}
}