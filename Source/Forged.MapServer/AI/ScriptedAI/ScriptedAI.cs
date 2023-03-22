﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Spells;

namespace Game.AI;

public class ScriptedAI : CreatureAI
{
	readonly Difficulty _difficulty;
	readonly bool _isHeroic;
	bool _isCombatMovementAllowed;

	public ScriptedAI(Creature creature) : base(creature)
	{
		_isCombatMovementAllowed = true;
		_isHeroic = Me.Map.IsHeroic;
		_difficulty = Me.Map.DifficultyID;
	}

	public void AttackStartNoMove(Unit target)
	{
		if (target == null)
			return;

		if (Me.Attack(target, true))
			DoStartNoMovement(target);
	}

	// Called before JustEngagedWith even before the creature is in combat.
	public override void AttackStart(Unit target)
	{
		if (IsCombatMovementAllowed())
			base.AttackStart(target);
		else
			AttackStartNoMove(target);
	}

	//Called at World update tick
	public override void UpdateAI(uint diff)
	{
		//Check if we have a current target
		if (!UpdateVictim())
			return;

		DoMeleeAttackIfReady();
	}

	//Start movement toward victim
	public void DoStartMovement(Unit target, float distance = 0.0f, float angle = 0.0f)
	{
		if (target != null)
			Me.MotionMaster.MoveChase(target, distance, angle);
	}

	//Start no movement on victim
	public void DoStartNoMovement(Unit target)
	{
		if (target == null)
			return;

		Me.MotionMaster.MoveIdle();
	}

	//Stop attack of current victim
	public void DoStopAttack()
	{
		if (Me.Victim != null)
			Me.AttackStop();
	}

	//Cast spell by spell info
	public void DoCastSpell(Unit target, SpellInfo spellInfo, bool triggered = false)
	{
		if (target == null || Me.IsNonMeleeSpellCast(false))
			return;

		Me.StopMoving();
		Me.CastSpell(target, spellInfo.Id, triggered);
	}

	//Plays a sound to all nearby players
	public static void DoPlaySoundToSet(WorldObject source, uint soundId)
	{
		if (source == null)
			return;

		if (!CliDB.SoundKitStorage.ContainsKey(soundId))
		{
			Log.outError(LogFilter.ScriptsAi, $"ScriptedAI::DoPlaySoundToSet: Invalid soundId {soundId} used in DoPlaySoundToSet (Source: {source.GUID})");

			return;
		}

		source.PlayDirectSound(soundId);
	}

	/// <summary>
	///  Add specified amount of threat directly to victim (ignores redirection effects) - also puts victim in combat and engages them if necessary
	/// </summary>
	/// <param name="victim"> </param>
	/// <param name="amount"> </param>
	/// <param name="who"> </param>
	public void AddThreat(Unit victim, double amount, Unit who = null)
	{
		if (!victim)
			return;

		if (!who)
			who = Me;

		who.GetThreatManager().AddThreat(victim, amount, null, true, true);
	}

	/// <summary>
	///  Adds/removes the specified percentage from the specified victim's threat (to who, or me if not specified)
	/// </summary>
	/// <param name="victim"> </param>
	/// <param name="pct"> </param>
	/// <param name="who"> </param>
	public void ModifyThreatByPercent(Unit victim, int pct, Unit who = null)
	{
		if (!victim)
			return;

		if (!who)
			who = Me;

		who.GetThreatManager().ModifyThreatByPercent(victim, pct);
	}

	/// <summary>
	///  Resets the victim's threat level to who (or me if not specified) to zero
	/// </summary>
	/// <param name="victim"> </param>
	/// <param name="who"> </param>
	public void ResetThreat(Unit victim, Unit who)
	{
		if (!victim)
			return;

		if (!who)
			who = Me;

		who.GetThreatManager().ResetThreat(victim);
	}

	/// <summary>
	///  Resets the specified unit's threat list (me if not specified) - does not delete entries, just sets their threat to zero
	/// </summary>
	/// <param name="who"> </param>
	public void ResetThreatList(Unit who = null)
	{
		if (!who)
			who = Me;

		who.GetThreatManager().ResetAllThreat();
	}

	/// <summary>
	///  Returns the threat level of victim towards who (or me if not specified)
	/// </summary>
	/// <param name="victim"> </param>
	/// <param name="who"> </param>
	/// <returns> </returns>
	public double GetThreat(Unit victim, Unit who = null)
	{
		if (!victim)
			return 0.0f;

		if (!who)
			who = Me;

		return who.GetThreatManager().GetThreat(victim);
	}

	//Spawns a creature relative to me
	public Creature DoSpawnCreature(uint entry, float offsetX, float offsetY, float offsetZ, float angle, TempSummonType type, TimeSpan despawntime)
	{
		return Me.SummonCreature(entry, new Position(Me.Location.X + offsetX, Me.Location.Y + offsetY, Me.Location.Z + offsetZ, angle), type, despawntime);
	}

	//Returns spells that meet the specified criteria from the creatures spell list
	public SpellInfo SelectSpell(Unit target, SpellSchoolMask school, Mechanics mechanic, SelectTargetType targets, float rangeMin, float rangeMax, SelectEffect effect)
	{
		//No target so we can't cast
		if (target == null)
			return null;

		//Silenced so we can't cast
		if (Me.IsSilenced(school == SpellSchoolMask.None ? SpellSchoolMask.Magic : school))
			return null;

		//Using the extended script system we first create a list of viable spells
		var apSpell = new SpellInfo[SharedConst.MaxCreatureSpells];

		uint spellCount = 0;

		//Check if each spell is viable(set it to null if not)
		for (uint i = 0; i < SharedConst.MaxCreatureSpells; i++)
		{
			var tempSpell = Global.SpellMgr.GetSpellInfo(Me.Spells[i], Me.Map.DifficultyID);
			var aiSpell = GetAISpellInfo(Me.Spells[i], Me.Map.DifficultyID);

			//This spell doesn't exist
			if (tempSpell == null || aiSpell == null)
				continue;

			// Targets and Effects checked first as most used restrictions
			//Check the spell targets if specified
			if (targets != 0 && !Convert.ToBoolean(aiSpell.Targets & (1 << ((int)targets - 1))))
				continue;

			//Check the type of spell if we are looking for a specific spell type
			if (effect != 0 && !Convert.ToBoolean(aiSpell.Effects & (1 << ((int)effect - 1))))
				continue;

			//Check for school if specified
			if (school != 0 && (tempSpell.SchoolMask & school) == 0)
				continue;

			//Check for spell mechanic if specified
			if (mechanic != 0 && tempSpell.Mechanic != mechanic)
				continue;

			// Continue if we don't have the mana to actually cast this spell
			var hasPower = true;

			foreach (var cost in tempSpell.CalcPowerCost(Me, tempSpell.GetSchoolMask()))
				if (cost.Amount > Me.GetPower(cost.Power))
				{
					hasPower = false;

					break;
				}

			if (!hasPower)
				continue;

			//Check if the spell meets our range requirements
			if (rangeMin != 0 && Me.GetSpellMinRangeForTarget(target, tempSpell) < rangeMin)
				continue;

			if (rangeMax != 0 && Me.GetSpellMaxRangeForTarget(target, tempSpell) > rangeMax)
				continue;

			//Check if our target is in range
			if (Me.IsWithinDistInMap(target, Me.GetSpellMinRangeForTarget(target, tempSpell)) || !Me.IsWithinDistInMap(target, Me.GetSpellMaxRangeForTarget(target, tempSpell)))
				continue;

			//All good so lets add it to the spell list
			apSpell[spellCount] = tempSpell;
			++spellCount;
		}

		//We got our usable spells so now lets randomly pick one
		if (spellCount == 0)
			return null;

		return apSpell[RandomHelper.IRand(0, (int)(spellCount - 1))];
	}

	public void DoTeleportTo(float x, float y, float z, uint time = 0)
	{
		Me.Location.Relocate(x, y, z);
		var speed = Me.GetDistance(x, y, z) / (time * 0.001f);
		Me.MonsterMoveWithSpeed(x, y, z, speed);
	}

	public void DoTeleportTo(float[] position)
	{
		Me.NearTeleportTo(position[0], position[1], position[2], position[3]);
	}

	//Teleports a player without dropping threat (only teleports to same map)
	public void DoTeleportPlayer(Unit unit, float x, float y, float z, float o)
	{
		if (unit == null)
			return;

		var player = unit.AsPlayer;

		if (player != null)
			player.TeleportTo(unit.Location.MapId, x, y, z, o, TeleportToOptions.NotLeaveCombat);
		else
			Log.outError(LogFilter.ScriptsAi, $"ScriptedAI::DoTeleportPlayer: Creature {Me.GUID} Tried to teleport non-player unit ({unit.GUID}) to X: {x} Y: {y} Z: {z} O: {o}. Aborted.");
	}

	public void DoTeleportAll(float x, float y, float z, float o)
	{
		var map = Me.Map;

		if (!map.IsDungeon)
			return;

		var PlayerList = map.Players;

		foreach (var player in PlayerList)
			if (player.IsAlive)
				player.TeleportTo(Me.Location.MapId, x, y, z, o, TeleportToOptions.NotLeaveCombat);
	}

	//Returns friendly unit with the most amount of hp missing from max hp
	public Unit DoSelectLowestHpFriendly(float range, uint minHPDiff = 1)
	{
		var u_check = new MostHPMissingInRange<Unit>(Me, range, minHPDiff);
		var searcher = new UnitLastSearcher(Me, u_check, GridType.All);
		Cell.VisitGrid(Me, searcher, range);

		return searcher.GetTarget();
	}

	//Returns a list of friendly CC'd units within range
	public List<Creature> DoFindFriendlyCC(float range)
	{
		List<Creature> list = new();
		var u_check = new FriendlyCCedInRange(Me, range);
		var searcher = new CreatureListSearcher(Me, list, u_check, GridType.All);
		Cell.VisitGrid(Me, searcher, range);

		return list;
	}

	//Returns a list of all friendly units missing a specific buff within range
	public List<Creature> DoFindFriendlyMissingBuff(float range, uint spellId)
	{
		List<Creature> list = new();
		var u_check = new FriendlyMissingBuffInRange(Me, range, spellId);
		var searcher = new CreatureListSearcher(Me, list, u_check, GridType.All);
		Cell.VisitGrid(Me, searcher, range);

		return list;
	}

	//Return a player with at least minimumRange from me
	public Player GetPlayerAtMinimumRange(float minimumRange)
	{
		var check = new PlayerAtMinimumRangeAway(Me, minimumRange);
		var searcher = new PlayerSearcher(Me, check, GridType.World);
		Cell.VisitGrid(Me, searcher, minimumRange);

		return searcher.GetTarget();
	}

	public void SetEquipmentSlots(bool loadDefault, int mainHand = -1, int offHand = -1, int ranged = -1)
	{
		if (loadDefault)
		{
			Me.LoadEquipment(Me.OriginalEquipmentId, true);

			return;
		}

		if (mainHand >= 0)
			Me.SetVirtualItem(0, (uint)mainHand);

		if (offHand >= 0)
			Me.SetVirtualItem(1, (uint)offHand);

		if (ranged >= 0)
			Me.SetVirtualItem(2, (uint)ranged);
	}

	// Used to control if MoveChase() is to be used or not in AttackStart(). Some creatures does not chase victims
	// NOTE: If you use SetCombatMovement while the creature is in combat, it will do NOTHING - This only affects AttackStart
	//       You should make the necessary to make it happen so.
	//       Remember that if you modified _isCombatMovementAllowed (e.g: using SetCombatMovement) it will not be reset at Reset().
	//       It will keep the last value you set.
	public void SetCombatMovement(bool allowMovement)
	{
		_isCombatMovementAllowed = allowMovement;
	}

	public static Creature GetClosestCreatureWithEntry(WorldObject source, uint entry, float maxSearchRange, bool alive = true)
	{
		return source.FindNearestCreature(entry, maxSearchRange, alive);
	}

	public static Creature GetClosestCreatureWithOptions(WorldObject source, float maxSearchRange, FindCreatureOptions options)
	{
		return source.FindNearestCreatureWithOptions(maxSearchRange, options);
	}

	public static GameObject GetClosestGameObjectWithEntry(WorldObject source, uint entry, float maxSearchRange, bool spawnedOnly = true)
	{
		return source.FindNearestGameObject(entry, maxSearchRange, spawnedOnly);
	}

	public bool HealthBelowPct(int pct)
	{
		return Me.HealthBelowPct(pct);
	}

	public bool HealthAbovePct(int pct)
	{
		return Me.HealthAbovePct(pct);
	}

	public bool IsCombatMovementAllowed()
	{
		return _isCombatMovementAllowed;
	}

	// return true for heroic mode. i.e.
	//   - for dungeon in mode 10-heroic,
	//   - for raid in mode 10-Heroic
	//   - for raid in mode 25-heroic
	// DO NOT USE to check raid in mode 25-normal.
	public bool IsHeroic()
	{
		return _isHeroic;
	}

	// return the dungeon or raid difficulty
	public Difficulty GetDifficulty()
	{
		return _difficulty;
	}

	// return true for 25 man or 25 man heroic mode
	public bool Is25ManRaid()
	{
		return _difficulty == Difficulty.Raid25N || _difficulty == Difficulty.Raid25HC;
	}

	public T DungeonMode<T>(T normal5, T heroic10)
	{
		return _difficulty switch
		{
			Difficulty.Normal => normal5,
			_                 => heroic10,
		};
	}

	public T RaidMode<T>(T normal10, T normal25)
	{
		return _difficulty switch
		{
			Difficulty.Raid10N => normal10,
			_                  => normal25,
		};
	}

	public T RaidMode<T>(T normal10, T normal25, T heroic10, T heroic25)
	{
		return _difficulty switch
		{
			Difficulty.Raid10N  => normal10,
			Difficulty.Raid25N  => normal25,
			Difficulty.Raid10HC => heroic10,
			_                   => heroic25,
		};
	}

	/// <summary>
	///  Stops combat, ignoring restrictions, for the given creature
	/// </summary>
	/// <param name="who"> </param>
	/// <param name="reset"> </param>
	void ForceCombatStop(Creature who, bool reset = true)
	{
		if (who == null || !who.IsInCombat)
			return;

		who.CombatStop(true);
		who.DoNotReacquireSpellFocusTarget();
		who.MotionMaster.Clear(MovementGeneratorPriority.Normal);

		if (reset)
		{
			who.LoadCreaturesAddon();
			who.SetTappedBy(null);
			who.ResetPlayerDamageReq();
			who.LastDamagedTime = 0;
			who.SetCannotReachTarget(false);
		}
	}

	/// <summary>
	///  Stops combat, ignoring restrictions, for the found creatures
	/// </summary>
	/// <param name="entry"> </param>
	/// <param name="maxSearchRange"> </param>
	/// <param name="samePhase"> </param>
	/// <param name="reset"> </param>
	void ForceCombatStopForCreatureEntry(uint entry, float maxSearchRange = 250.0f, bool samePhase = true, bool reset = true)
	{
		Log.outDebug(LogFilter.ScriptsAi, $"BossAI::ForceStopCombatForCreature: called on {Me.GUID}. Debug info: {Me.GetDebugInfo()}");

		List<Creature> creatures = new();
		AllCreaturesOfEntryInRange check = new(Me, entry, maxSearchRange);
		CreatureListSearcher searcher = new(Me, creatures, check, GridType.Grid);

		if (!samePhase)
			PhasingHandler.SetAlwaysVisible(Me, true, false);

		Cell.VisitGrid(Me, searcher, maxSearchRange);

		if (!samePhase)
			PhasingHandler.SetAlwaysVisible(Me, false, false);

		foreach (var creature in creatures)
			ForceCombatStop(creature, reset);
	}

	/// <summary>
	///  Stops combat, ignoring restrictions, for the found creatures
	/// </summary>
	/// <param name="creatureEntries"> </param>
	/// <param name="maxSearchRange"> </param>
	/// <param name="samePhase"> </param>
	/// <param name="reset"> </param>
	void ForceCombatStopForCreatureEntry(List<uint> creatureEntries, float maxSearchRange = 250.0f, bool samePhase = true, bool reset = true)
	{
		foreach (var entry in creatureEntries)
			ForceCombatStopForCreatureEntry(entry, maxSearchRange, samePhase, reset);
	}
}