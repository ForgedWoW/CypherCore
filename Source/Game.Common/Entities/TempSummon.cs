// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Common.DataStorage.Structs.S;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Units;
using Game.Common.Globals;

namespace Game.Common.Entities;

public class TempSummon : Creature
{
	public SummonPropertiesRecord SummonPropertiesRecord;
	TempSummonType _summonType;
	uint _timer;
	uint _lifetime;
	ObjectGuid _summonerGuid;
	uint? _creatureIdVisibleToSummoner;
	uint? _displayIdVisibleToSummoner;
	bool _canFollowOwner;

	public TempSummon(SummonPropertiesRecord propertiesRecord, WorldObject owner, bool isWorldObject) : base(isWorldObject)
	{
		SummonPropertiesRecord = propertiesRecord;
		_summonType = TempSummonType.ManualDespawn;

		_summonerGuid = owner != null ? owner.GUID : ObjectGuid.Empty;
		UnitTypeMask |= UnitTypeMask.Summon;
		_canFollowOwner = true;
	}

	public WorldObject GetSummoner()
	{
		return !_summonerGuid.IsEmpty ? Global.ObjAccessor.GetWorldObject(this, _summonerGuid) : null;
	}

	public void SetSummonerGUID(ObjectGuid summonerGUID)
	{
		_summonerGuid = summonerGUID;
	}

	public Unit GetSummonerUnit()
	{
		var summoner = GetSummoner();

		if (summoner != null)
			return summoner.AsUnit;

		return null;
	}

	public Creature GetSummonerCreatureBase()
	{
		return !_summonerGuid.IsEmpty ? ObjectAccessor.GetCreature(this, _summonerGuid) : null;
	}

	public GameObject GetSummonerGameObject()
	{
		var summoner = GetSummoner();

		if (summoner != null)
			return summoner.AsGameObject;

		return null;
	}

	public override float GetDamageMultiplierForTarget(WorldObject target)
	{
		return 1.0f;
	}

	public override void Update(uint diff)
	{
		base.Update(diff);

		if (DeathState == DeathState.Dead)
		{
			UnSummon();

			return;
		}

		switch (_summonType)
		{
			case TempSummonType.ManualDespawn:
			case TempSummonType.DeadDespawn:
				break;
			case TempSummonType.TimedDespawn:
			{
				if (_timer <= diff)
				{
					UnSummon();

					return;
				}

				_timer -= diff;

				break;
			}
			case TempSummonType.TimedDespawnOutOfCombat:
			{
				if (!IsInCombat)
				{
					if (_timer <= diff)
					{
						UnSummon();

						return;
					}

					_timer -= diff;
				}
				else if (_timer != _lifetime)
				{
					_timer = _lifetime;
				}

				break;
			}

			case TempSummonType.CorpseTimedDespawn:
			{
				if (DeathState == DeathState.Corpse)
				{
					if (_timer <= diff)
					{
						UnSummon();

						return;
					}

					_timer -= diff;
				}

				break;
			}
			case TempSummonType.CorpseDespawn:
			{
				// if m_deathState is DEAD, CORPSE was skipped
				if (DeathState == DeathState.Corpse)
				{
					UnSummon();

					return;
				}

				break;
			}
			case TempSummonType.TimedOrCorpseDespawn:
			{
				if (DeathState == DeathState.Corpse)
				{
					UnSummon();

					return;
				}

				if (!IsInCombat)
				{
					if (_timer <= diff)
					{
						UnSummon();

						return;
					}
					else
					{
						_timer -= diff;
					}
				}
				else if (_timer != _lifetime)
				{
					_timer = _lifetime;
				}

				break;
			}
			case TempSummonType.TimedOrDeadDespawn:
			{
				if (!IsInCombat && IsAlive)
				{
					if (_timer <= diff)
					{
						UnSummon();

						return;
					}
					else
					{
						_timer -= diff;
					}
				}
				else if (_timer != _lifetime)
				{
					_timer = _lifetime;
				}

				break;
			}
			default:
				UnSummon();
				Log.outError(LogFilter.Unit, "Temporary summoned creature (entry: {0}) have unknown type {1} of ", Entry, _summonType);

				break;
		}
	}

	public virtual void InitStats(uint duration)
	{
		_timer = duration;
		_lifetime = duration;

		if (_summonType == TempSummonType.ManualDespawn)
			_summonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;

		var owner = GetSummonerUnit();

		if (owner != null && IsTrigger && Spells[0] != 0)
			if (owner.IsTypeId(TypeId.Player))
				ControlledByPlayer = true;

		if (owner != null && owner.IsPlayer)
		{
			var summonedData = Global.ObjectMgr.GetCreatureSummonedData(Entry);

			if (summonedData != null)
			{
				_creatureIdVisibleToSummoner = summonedData.CreatureIdVisibleToSummoner;

				if (summonedData.CreatureIdVisibleToSummoner.HasValue)
				{
					var creatureTemplateVisibleToSummoner = Global.ObjectMgr.GetCreatureTemplate(summonedData.CreatureIdVisibleToSummoner.Value);
					_displayIdVisibleToSummoner = ObjectManager.ChooseDisplayId(creatureTemplateVisibleToSummoner, null).CreatureDisplayId;
				}
			}
		}

		if (SummonPropertiesRecord == null)
			return;

		if (owner != null)
		{
			var slot = SummonPropertiesRecord.Slot;

			if (slot > 0)
			{
				if (!owner.SummonSlot[slot].IsEmpty && owner.SummonSlot[slot] != GUID)
				{
					var oldSummon = Map.GetCreature(owner.SummonSlot[slot]);

					if (oldSummon != null && oldSummon.IsSummon)
						oldSummon.ToTempSummon().UnSummon();
				}

				owner.SummonSlot[slot] = GUID;
			}

			if (!SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.UseCreatureLevel))
				SetLevel(owner.Level);
		}

		var faction = SummonPropertiesRecord.Faction;

		if (owner && SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.UseSummonerFaction)) // TODO: Determine priority between faction and flag
			faction = owner.Faction;

		if (faction != 0)
			Faction = faction;

		if (SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.SummonFromBattlePetJournal))
			RemoveNpcFlag(NPCFlags.WildBattlePet);
	}

	public virtual void InitSummon()
	{
		var owner = GetSummoner();

		if (owner != null)
		{
			if (owner.IsCreature)
				owner.AsCreature.AI?.JustSummoned(this);
			else if (owner.IsGameObject)
				owner.AsGameObject.AI?.JustSummoned(this);

			if (IsAIEnabled)
				AI.IsSummonedBy(owner);
		}
	}

	public override void UpdateObjectVisibilityOnCreate()
	{
		List<WorldObject> objectsToUpdate = new();
		objectsToUpdate.Add(this);

		var smoothPhasing = GetSmoothPhasing();

		if (smoothPhasing != null)
		{
			var infoForSeer = smoothPhasing.GetInfoForSeer(DemonCreatorGUID);

			if (infoForSeer != null && infoForSeer.ReplaceObject.HasValue && smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
			{
				var original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);

				if (original != null)
					objectsToUpdate.Add(original);
			}
		}

		VisibleChangesNotifier notifier = new(objectsToUpdate, GridType.World);
		Cell.VisitGrid(this, notifier, VisibilityRange);
	}

	public override void UpdateObjectVisibilityOnDestroy()
	{
		List<WorldObject> objectsToUpdate = new();
		objectsToUpdate.Add(this);

		WorldObject original = null;
		var smoothPhasing = GetSmoothPhasing();

		if (smoothPhasing != null)
		{
			var infoForSeer = smoothPhasing.GetInfoForSeer(DemonCreatorGUID);

			if (infoForSeer != null && infoForSeer.ReplaceObject.HasValue && smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
				original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);

			if (original != null)
			{
				objectsToUpdate.Add(original);

				// disable replacement without removing - it is still needed for next step (visibility update)
				var originalSmoothPhasing = original.GetSmoothPhasing();

				if (originalSmoothPhasing != null)
					originalSmoothPhasing.DisableReplacementForSeer(DemonCreatorGUID);
			}
		}

		VisibleChangesNotifier notifier = new(objectsToUpdate, GridType.World);
		Cell.VisitGrid(this, notifier, VisibilityRange);

		if (original != null) // original is only != null when it was replaced
		{
			var originalSmoothPhasing = original.GetSmoothPhasing();

			if (originalSmoothPhasing != null)
				originalSmoothPhasing.ClearViewerDependentInfo(DemonCreatorGUID);
		}
	}

	public void SetTempSummonType(TempSummonType type)
	{
		_summonType = type;
	}

	public virtual void UnSummon()
	{
		UnSummon(TimeSpan.Zero);
	}

	public virtual void UnSummon(TimeSpan msTime)
	{
		if (msTime != TimeSpan.Zero)
		{
			ForcedUnsummonDelayEvent pEvent = new(this);

			Events.AddEvent(pEvent, Events.CalculateTime(msTime));

			return;
		}

		if (IsPet)
		{
			AsPet.Remove(PetSaveMode.NotInSlot);
			return;
		}

		var owner = GetSummoner();

		if (owner != null)
		{
			if (owner.IsCreature)
				owner.AsCreature.AI?.SummonedCreatureDespawn(this);
			else if (owner.IsGameObject)
				owner.AsGameObject.AI?.SummonedCreatureDespawn(this);
		}

		AddObjectToRemoveList();
	}

	public override void RemoveFromWorld()
	{
		if (!IsInWorld)
			return;

		if (SummonPropertiesRecord != null)
		{
			var slot = SummonPropertiesRecord.Slot;

			if (slot > 0)
			{
				var owner = GetSummonerUnit();

				if (owner != null)
					if (owner.SummonSlot[slot] == GUID)
						owner.SummonSlot[slot].Clear();
			}
		}

		if (!OwnerGUID.IsEmpty)
			Log.outError(LogFilter.Unit, "Unit {0} has owner guid when removed from world", Entry);

		base.RemoveFromWorld();
	}

	public override string GetDebugInfo()
	{
		return $"{base.GetDebugInfo()}\nTempSummonType : {GetSummonType()} Summoner: {GetSummonerGUID()} Timer: {GetTimer()}";
	}

	public override void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties) { }

	public ObjectGuid GetSummonerGUID()
	{
		return _summonerGuid;
	}

	public uint GetTimer()
	{
		return _timer;
	}

	public uint? GetCreatureIdVisibleToSummoner()
	{
		return _creatureIdVisibleToSummoner;
	}

	public uint? GetDisplayIdVisibleToSummoner()
	{
		return _displayIdVisibleToSummoner;
	}

	public bool CanFollowOwner()
	{
		return _canFollowOwner;
	}

	public void SetCanFollowOwner(bool can)
	{
		_canFollowOwner = can;
	}

	TempSummonType GetSummonType()
	{
		return _summonType;
	}
}
