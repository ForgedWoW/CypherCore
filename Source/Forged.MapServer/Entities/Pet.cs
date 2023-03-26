// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Phasing;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities;

public class Pet : Guardian
{
	public new Dictionary<uint, PetSpell> Spells = new();
	public bool Removed;
	const int PetFocusRegenInterval = 4 * Time.InMilliseconds;
	const int HappinessLevelSize = 333000;
	const float PetXPFactor = 0.05f;
	readonly List<uint> _autospells = new();

	PetType _petType;
	int _duration; // time until unsummon (used mostly for summoned guardians and not used for controlled pets)
	bool _loading;
	uint _focusRegenTimer;
	GroupUpdatePetFlags _mGroupUpdateMask;

	DeclinedName _declinedname;
	ushort _petSpecialization;

	public override float NativeObjectScale
	{
		get
		{
			var creatureFamily = CliDB.CreatureFamilyStorage.LookupByKey(Template.Family);

			if (creatureFamily is { MinScale: > 0.0f } && PetType == PetType.Hunter)
			{
				float scale;

				if (Level >= creatureFamily.MaxScaleLevel)
					scale = creatureFamily.MaxScale;
				else if (Level <= creatureFamily.MinScaleLevel)
					scale = creatureFamily.MinScale;
				else
					scale = creatureFamily.MinScale + (float)(Level - creatureFamily.MinScaleLevel) / creatureFamily.MaxScaleLevel * (creatureFamily.MaxScale - creatureFamily.MinScale);

				return scale;
			}

			return base.NativeObjectScale;
		}
	}

	public override bool IsLoading => _loading;

	public override byte PetAutoSpellSize => (byte)_autospells.Count;

	public Player OwningPlayer => base.OwnerUnit.AsPlayer;

	public PetType PetType
	{
		get => _petType;
		set => _petType = value;
	}

	public bool IsControlled => PetType == PetType.Summon || PetType == PetType.Hunter;

	public bool IsTemporarySummoned => _duration > 0;

	public int Duration => _duration;

	public ushort Specialization => _petSpecialization;

	public GroupUpdatePetFlags GroupUpdateFlag
	{
		get => _mGroupUpdateMask;
		set
		{
			if (OwningPlayer.Group)
			{
				_mGroupUpdateMask |= value;
				OwningPlayer.SetGroupUpdateFlag(GroupUpdateFlags.Pet);
			}
		}
	}

	public Pet(Player owner, PetType type = PetType.Max) : base(null, owner, true)
	{
		_petType = type;
		UnitTypeMask |= UnitTypeMask.Pet;

		if (type == PetType.Hunter)
			UnitTypeMask |= UnitTypeMask.HunterPet;

		if (!UnitTypeMask.HasAnyFlag(UnitTypeMask.ControlableGuardian))
		{
			UnitTypeMask |= UnitTypeMask.ControlableGuardian;
			InitCharmInfo();
		}

		SetName("Pet");
		_focusRegenTimer = PetFocusRegenInterval;
	}

	public override void Dispose()
	{
		_declinedname = null;
		base.Dispose();
	}

	public override void AddToWorld()
	{
		//- Register the pet for guid lookup
		if (!IsInWorld)
		{
			// Register the pet for guid lookup
			base.AddToWorld();
			InitializeAI();
			var zoneScript = ZoneScript1 != null ? ZoneScript1 : InstanceScript;

			if (zoneScript != null)
				zoneScript.OnCreatureCreate(this);
		}

		// Prevent stuck pets when zoning. Pets default to "follow" when added to world
		// so we'll reset flags and let the AI handle things
		if (GetCharmInfo() != null && GetCharmInfo().HasCommandState(CommandStates.Follow))
		{
			GetCharmInfo().SetIsCommandAttack(false);
			GetCharmInfo().SetIsCommandFollow(false);
			GetCharmInfo().SetIsAtStay(false);
			GetCharmInfo().SetIsFollowing(false);
			GetCharmInfo().SetIsReturning(false);
		}
	}

	public override void RemoveFromWorld()
	{
		// Remove the pet from the accessor
		if (IsInWorld)
		{
			// Don't call the function for Creature, normal mobs + totems go in a different storage
			base.RemoveFromWorld();
			Map.ObjectsStore.TryRemove(GUID, out _);
		}
	}

	public static Tuple<PetStable.PetInfo, PetSaveMode> GetLoadPetInfo(PetStable stable, uint petEntry, uint petnumber, PetSaveMode? slot)
	{
		if (petnumber != 0)
		{
			// Known petnumber entry
			for (var activeSlot = 0; activeSlot < stable.ActivePets.Length; ++activeSlot)
				if (stable.ActivePets[activeSlot] != null && stable.ActivePets[activeSlot].PetNumber == petnumber)
					return Tuple.Create(stable.ActivePets[activeSlot], PetSaveMode.FirstActiveSlot + activeSlot);

			for (var stableSlot = 0; stableSlot < stable.StabledPets.Length; ++stableSlot)
				if (stable.StabledPets[stableSlot] != null && stable.StabledPets[stableSlot].PetNumber == petnumber)
					return Tuple.Create(stable.StabledPets[stableSlot], PetSaveMode.FirstStableSlot + stableSlot);

			foreach (var pet in stable.UnslottedPets)
				if (pet.PetNumber == petnumber)
					return Tuple.Create(pet, PetSaveMode.NotInSlot);
		}
		else if (slot.HasValue)
		{
			// Current pet
			if (slot == PetSaveMode.AsCurrent)
				if (stable.GetCurrentActivePetIndex().HasValue && stable.ActivePets[stable.GetCurrentActivePetIndex().Value] != null)
					return Tuple.Create(stable.ActivePets[stable.GetCurrentActivePetIndex().Value], (PetSaveMode)stable.GetCurrentActivePetIndex());

			if (slot is >= PetSaveMode.FirstActiveSlot and < PetSaveMode.LastActiveSlot)
				if (stable.ActivePets[(int)slot.Value] != null)
					return Tuple.Create(stable.ActivePets[(int)slot.Value], slot.Value);

			if (slot is >= PetSaveMode.FirstStableSlot and < PetSaveMode.LastStableSlot)
				if (stable.StabledPets[(int)slot.Value] != null)
					return Tuple.Create(stable.StabledPets[(int)slot.Value], slot.Value);
		}
		else if (petEntry != 0)
		{
			// known petEntry entry (unique for summoned pet, but non unique for hunter pet (only from current or not stabled pets)

			foreach (var pet in stable.UnslottedPets)
				if (pet.CreatureId == petEntry)
					return Tuple.Create(pet, PetSaveMode.NotInSlot);
		}
		else
		{
			// Any current or other non-stabled pet (for hunter "call pet")
			if (stable.ActivePets[0] != null)
				return Tuple.Create(stable.ActivePets[0], PetSaveMode.FirstActiveSlot);

			if (!stable.UnslottedPets.Empty())
				return Tuple.Create(stable.UnslottedPets.First(), PetSaveMode.NotInSlot);
		}

		return Tuple.Create<PetStable.PetInfo, PetSaveMode>(null, PetSaveMode.AsDeleted);
	}

	public bool LoadPetFromDB(Player owner, uint petEntry = 0, uint petnumber = 0, bool current = false, PetSaveMode? forcedSlot = null)
	{
		_loading = true;

		var petStable = owner.PetStable1;

		var ownerid = owner.GUID.Counter;
		var (petInfo, slot) = GetLoadPetInfo(petStable, petEntry, petnumber, forcedSlot);

		if (petInfo == null || slot is >= PetSaveMode.FirstStableSlot and < PetSaveMode.LastStableSlot)
		{
			_loading = false;

			return false;
		}

		// Don't try to reload the current pet
		if (petStable.GetCurrentPet() != null && owner.CurrentPet != null && petStable.GetCurrentPet().PetNumber == petInfo.PetNumber)
			return false;

		var spellInfo = Global.SpellMgr.GetSpellInfo(petInfo.CreatedBySpellId, owner.Map.DifficultyID);

		var isTemporarySummon = spellInfo != null && spellInfo.Duration > 0;

		if (current && isTemporarySummon)
			return false;

		if (petInfo.Type == PetType.Hunter)
		{
			var creatureInfo = Global.ObjectMgr.GetCreatureTemplate(petInfo.CreatureId);

			if (creatureInfo == null || !creatureInfo.IsTameable(owner.CanTameExoticPets))
				return false;
		}

		if (current && owner.IsPetNeedBeTemporaryUnsummoned())
		{
			owner.TemporaryUnsummonedPetNumber = petInfo.PetNumber;

			return false;
		}

		var map = owner.Map;
		var guid = map.GenerateLowGuid(HighGuid.Pet);

		if (!Create(guid, map, petInfo.CreatureId, petInfo.PetNumber))
			return false;

		PhasingHandler.InheritPhaseShift(this, owner);

		PetType = petInfo.Type;
		Faction = owner.Faction;
		SetCreatedBySpell(petInfo.CreatedBySpellId);

		var pos = new Position();

		if (IsCritter)
		{
			owner.GetClosePoint(pos, CombatReach, SharedConst.PetFollowDist, FollowAngle);
			pos.Orientation = owner.Location.Orientation;
			Location.Relocate(pos);

			if (!Location.IsPositionValid)
			{
				Log.Logger.Error("Pet (guidlow {0}, entry {1}) not loaded. Suggested coordinates isn't valid (X: {2} Y: {3})",
								GUID.ToString(),
								Entry,
								Location.X,
								Location.Y);

				return false;
			}

			map.AddToMap(AsCreature);

			return true;
		}

		GetCharmInfo().SetPetNumber(petInfo.PetNumber, IsPermanentPetFor(owner));

		SetDisplayId(petInfo.DisplayId);
		SetNativeDisplayId(petInfo.DisplayId);
		uint petlevel = petInfo.Level;
		ReplaceAllNpcFlags(NPCFlags.None);
		ReplaceAllNpcFlags2(NPCFlags2.None);
		SetName(petInfo.Name);

		switch (PetType)
		{
			case PetType.Summon:
				petlevel = owner.Level;

				Class = PlayerClass.Mage;
				ReplaceAllUnitFlags(UnitFlags.PlayerControlled); // this enables popup window (pet dismiss, cancel)

				break;
			case PetType.Hunter:
				Class = PlayerClass.Warrior;
				Gender = Gender.None;
				Sheath = SheathState.Melee;
				ReplaceAllPetFlags(petInfo.WasRenamed ? UnitPetFlags.CanBeAbandoned : UnitPetFlags.CanBeRenamed | UnitPetFlags.CanBeAbandoned);
				ReplaceAllUnitFlags(UnitFlags.PlayerControlled); // this enables popup window (pet abandon, cancel)

				break;
			default:
				if (!IsPetGhoul())
					Log.Logger.Error("Pet have incorrect type ({0}) for pet loading.", PetType);

				break;
		}

		SetPetNameTimestamp((uint)GameTime.GetGameTime()); // cast can't be helped here
		SetCreatorGUID(owner.GUID);

		InitStatsForLevel(petlevel);
		SetPetExperience(petInfo.Experience);

		SynchronizeLevelWithOwner();

		// Set pet's position after setting level, its size depends on it
		owner.GetClosePoint(pos, CombatReach, SharedConst.PetFollowDist, FollowAngle);
		Location.Relocate(pos);

		if (!Location.IsPositionValid)
		{
			Log.Logger.Error("Pet ({0}, entry {1}) not loaded. Suggested coordinates isn't valid (X: {2} Y: {3})", GUID.ToString(), Entry, Location.X, Location.Y);

			return false;
		}

		ReactState = petInfo.ReactState;
		SetCanModifyStats(true);

		if (PetType == PetType.Summon && !current) //all (?) summon pets come with full health when called, but not when they are current
		{
			SetFullPower(PowerType.Mana);
		}
		else
		{
			var savedhealth = petInfo.Health;
			var savedmana = petInfo.Mana;

			if (savedhealth == 0 && PetType == PetType.Hunter)
			{
				SetDeathState(DeathState.JustDied);
			}
			else
			{
				SetHealth(savedhealth);
				SetPower(PowerType.Mana, (int)savedmana);
			}
		}

		// set current pet as current
		// 0-4=current
		// PET_SAVE_NOT_IN_SLOT(-1) = not stable slot (summoning))
		if (slot == PetSaveMode.NotInSlot)
		{
			var petInfoNumber = petInfo.PetNumber;

			if (petStable.CurrentPetIndex != 0)
				owner.RemovePet(null, PetSaveMode.NotInSlot);

			var unslottedPetIndex = petStable.UnslottedPets.FindIndex(unslottedPet => unslottedPet.PetNumber == petInfoNumber);

			petStable.SetCurrentUnslottedPetIndex((uint)unslottedPetIndex);
		}
		else if (slot is >= PetSaveMode.FirstActiveSlot and <= PetSaveMode.LastActiveSlot)
		{
			var activePetIndex = Array.FindIndex(petStable.ActivePets, pet => pet?.PetNumber == petnumber);

			if (activePetIndex == -1)
				activePetIndex = (int)petnumber;

			petStable.SetCurrentActivePetIndex((uint)activePetIndex);
		}

		// Send fake summon spell cast - this is needed for correct cooldown application for spells
		// Example: 46584 - without this cooldown (which should be set always when pet is loaded) isn't set clientside
		// @todo pets should be summoned from real cast instead of just faking it?
		if (petInfo.CreatedBySpellId != 0)
		{
			SpellGo spellGo = new();
			var castData = spellGo.Cast;

			castData.CasterGUID = owner.GUID;
			castData.CasterUnit = owner.GUID;
			castData.CastID = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, owner.Location.MapId, petInfo.CreatedBySpellId, map.GenerateLowGuid(HighGuid.Cast));
			castData.SpellID = (int)petInfo.CreatedBySpellId;
			castData.CastFlags = SpellCastFlags.Unk9;
			castData.CastTime = Time.MSTime;
			owner.SendMessageToSet(spellGo, true);
		}

		owner.SetMinion(this, true);

		if (!isTemporarySummon)
			GetCharmInfo().LoadPetActionBar(petInfo.ActionBar);

		map.AddToMap(AsCreature);

		//set last used pet number (for use in BG's)
		if (owner.IsPlayer && IsControlled && !IsTemporarySummoned && (PetType == PetType.Summon || PetType == PetType.Hunter))
			owner.AsPlayer.LastPetNumber = petInfo.PetNumber;

		var session = owner.Session;
		var lastSaveTime = petInfo.LastSaveTime;
		var specializationId = petInfo.SpecializationId;

		owner.Session
			.AddQueryHolderCallback(DB.Characters.DelayQueryHolder(new PetLoadQueryHolder(ownerid, petInfo.PetNumber)))
			.AfterComplete(holder =>
			{
				if (session.Player != owner || owner.CurrentPet != this)
					return;

				// passing previous checks ensure that 'this' is still valid
				if (Removed)
					return;

				var timediff = (uint)(GameTime.GetGameTime() - lastSaveTime);
				_LoadAuras(holder.GetResult(PetLoginQueryLoad.Auras), holder.GetResult(PetLoginQueryLoad.AuraEffects), timediff);

				// load action bar, if data broken will fill later by default spells.
				if (!isTemporarySummon)
				{
					_LoadSpells(holder.GetResult(PetLoginQueryLoad.Spells));
					SpellHistory.LoadFromDb<Pet>(holder.GetResult(PetLoginQueryLoad.Cooldowns), holder.GetResult(PetLoginQueryLoad.Charges));
					LearnPetPassives();
					InitLevelupSpellsForLevel();

					if (Map.IsBattleArena)
						RemoveArenaAuras();

					CastPetAuras(current);
				}

				Log.Logger.Debug($"New Pet has {GUID}");

				var specId = specializationId;
				var petSpec = CliDB.ChrSpecializationStorage.LookupByKey(specId);

				if (petSpec != null)
					specId = (ushort)Global.DB2Mgr.GetChrSpecializationByIndex(owner.HasAuraType(AuraType.OverridePetSpecs) ? PlayerClass.Max : 0, petSpec.OrderIndex).Id;

				SetSpecialization(specId);

				// The SetSpecialization function will run these functions if the pet's spec is not 0
				if (Specialization == 0)
				{
					CleanupActionBar(); // remove unknown spells from action bar after load

					owner.PetSpellInitialize();
				}


				GroupUpdateFlag = GroupUpdatePetFlags.Full;

				if (PetType == PetType.Hunter)
				{
					var result = holder.GetResult(PetLoginQueryLoad.DeclinedNames);

					if (!SQLEx.IsEmpty(result))
					{
						_declinedname = new DeclinedName();

						for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
							_declinedname.Name[i] = result.Read<string>(i);
					}
				}

				// must be after SetMinion (owner guid check)
				LoadTemplateImmunities();
				_loading = false;
			});

		return true;
	}

	public void SavePetToDB(PetSaveMode mode)
	{
		if (Entry == 0)
			return;

		// save only fully controlled creature
		if (!IsControlled)
			return;

		// not save not player pets
		if (!OwnerGUID.IsPlayer)
			return;

		var owner = OwningPlayer;

		if (owner == null)
			return;

		// not save pet as current if another pet temporary unsummoned
		if (mode == PetSaveMode.AsCurrent &&
			owner.TemporaryUnsummonedPetNumber != 0 &&
			owner.TemporaryUnsummonedPetNumber != GetCharmInfo().GetPetNumber())
		{
			// pet will lost anyway at restore temporary unsummoned
			if (PetType == PetType.Hunter)
				return;

			// for warlock case
			mode = PetSaveMode.NotInSlot;
		}

		var curhealth = (uint)Health;
		var curmana = GetPower(PowerType.Mana);

		SQLTransaction trans = new();
		// save auras before possibly removing them    
		_SaveAuras(trans);

		if (mode == PetSaveMode.AsCurrent)
		{
			var activeSlot = owner.PetStable1.GetCurrentActivePetIndex();

			if (activeSlot.HasValue)
				mode = (PetSaveMode)activeSlot;
		}

		// stable and not in slot saves
		if (mode < PetSaveMode.FirstActiveSlot || mode >= PetSaveMode.LastActiveSlot)
			RemoveAllAuras();

		_SaveSpells(trans);
		SpellHistory.SaveToDb<Pet>(trans);
		DB.Characters.CommitTransaction(trans);

		// current/stable/not_in_slot
		if (mode != PetSaveMode.AsDeleted)
		{
			var ownerLowGUID = OwnerGUID.Counter;
			trans = new SQLTransaction();

			// remove current data
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
			stmt.AddValue(0, GetCharmInfo().GetPetNumber());
			trans.Append(stmt);

			// save pet
			var actionBar = GenerateActionBarData();

			FillPetInfo(owner.PetStable1.GetCurrentPet());

			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET);
			stmt.AddValue(0, GetCharmInfo().GetPetNumber());
			stmt.AddValue(1, Entry);
			stmt.AddValue(2, ownerLowGUID);
			stmt.AddValue(3, NativeDisplayId);
			stmt.AddValue(4, Level);
			stmt.AddValue(5, UnitData.PetExperience);
			stmt.AddValue(6, (byte)ReactState);
			stmt.AddValue(7, (owner.PetStable1.GetCurrentActivePetIndex().HasValue ? (short)owner.PetStable1.GetCurrentActivePetIndex().Value : (short)PetSaveMode.NotInSlot));
			stmt.AddValue(8, GetName());
			stmt.AddValue(9, HasPetFlag(UnitPetFlags.CanBeRenamed) ? 0 : 1);
			stmt.AddValue(10, curhealth);
			stmt.AddValue(11, curmana);

			stmt.AddValue(12, actionBar);

			stmt.AddValue(13, GameTime.GetGameTime());
			stmt.AddValue(14, UnitData.CreatedBySpell);
			stmt.AddValue(15, (byte)PetType);
			stmt.AddValue(16, Specialization);
			trans.Append(stmt);

			DB.Characters.CommitTransaction(trans);
		}
		// delete
		else
		{
			RemoveAllAuras();
			DeleteFromDB(GetCharmInfo().GetPetNumber());
		}
	}

	public void FillPetInfo(PetStable.PetInfo petInfo)
	{
		petInfo.PetNumber = GetCharmInfo().GetPetNumber();
		petInfo.CreatureId = Entry;
		petInfo.DisplayId = NativeDisplayId;
		petInfo.Level = (byte)Level;
		petInfo.Experience = UnitData.PetExperience;
		petInfo.ReactState = ReactState;
		petInfo.Name = GetName();
		petInfo.WasRenamed = !HasPetFlag(UnitPetFlags.CanBeRenamed);
		petInfo.Health = (uint)Health;
		petInfo.Mana = (uint)GetPower(PowerType.Mana);
		petInfo.ActionBar = GenerateActionBarData();
		petInfo.LastSaveTime = (uint)GameTime.GetGameTime();
		petInfo.CreatedBySpellId = UnitData.CreatedBySpell;
		petInfo.Type = PetType;
		petInfo.SpecializationId = Specialization;
	}

	public static void DeleteFromDB(uint petNumber)
	{
		SQLTransaction trans = new();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_ID);
		stmt.AddValue(0, petNumber);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME);
		stmt.AddValue(0, petNumber);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURA_EFFECTS);
		stmt.AddValue(0, petNumber);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURAS);
		stmt.AddValue(0, petNumber);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELLS);
		stmt.AddValue(0, petNumber);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS);
		stmt.AddValue(0, petNumber);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_CHARGES);
		stmt.AddValue(0, petNumber);
		trans.Append(stmt);

		DB.Characters.CommitTransaction(trans);
	}

	public override void SetDeathState(DeathState s)
	{
		base.SetDeathState(s);

		if (DeathState == DeathState.Corpse)
		{
			if (PetType == PetType.Hunter)
			{
				// pet corpse non lootable and non skinnable
				ReplaceAllDynamicFlags(UnitDynFlags.None);
				RemoveUnitFlag(UnitFlags.Skinnable);
			}
		}
		else if (DeathState == DeathState.Alive)
		{
			CastPetAuras(true);
		}
	}

	public override void Update(uint diff)
	{
		if (Removed) // pet already removed, just wait in remove queue, no updates
			return;

		if (_loading)
			return;

		switch (DeathState)
		{
			case DeathState.Corpse:
			{
				if (PetType != PetType.Hunter || CorpseRemoveTime <= GameTime.GetGameTime())
				{
					Remove(PetSaveMode.NotInSlot); //hunters' pets never get removed because of death, NEVER!

					return;
				}

				break;
			}
			case DeathState.Alive:
			{
				// unsummon pet that lost owner
				var owner = OwningPlayer;

				if (owner == null || (!IsWithinDistInMap(owner, Map.VisibilityRange) && !IsPossessed) || (IsControlled && owner.PetGUID.IsEmpty))
				{
					Remove(PetSaveMode.NotInSlot, true);

					return;
				}

				if (IsControlled)
					if (owner.PetGUID != GUID)
					{
						Log.Logger.Error($"Pet {Entry} is not pet of owner {OwningPlayer.GetName()}, removed");
						Remove(PetSaveMode.NotInSlot);

						return;
					}

				if (_duration > 0)
				{
					if (_duration > diff)
					{
						_duration -= (int)diff;
					}
					else
					{
						Remove(PetType != PetType.Summon ? PetSaveMode.AsDeleted : PetSaveMode.NotInSlot);

						return;
					}
				}

				//regenerate focus for hunter pets or energy for deathknight's ghoul
				if (_focusRegenTimer != 0)
				{
					if (_focusRegenTimer > diff)
						_focusRegenTimer -= diff;
					else
						switch (DisplayPowerType)
						{
							case PowerType.Focus:
								Regenerate(PowerType.Focus);
								_focusRegenTimer += PetFocusRegenInterval - diff;

								if (_focusRegenTimer == 0)
									++_focusRegenTimer;

								// Reset if large diff (lag) causes focus to get 'stuck'
								if (_focusRegenTimer > PetFocusRegenInterval)
									_focusRegenTimer = PetFocusRegenInterval;

								break;
							default:
								_focusRegenTimer = 0;

								break;
						}
				}

				break;
			}
			default:
				break;
		}

		base.Update(diff);
	}

	public void Remove(PetSaveMode mode, bool returnreagent = false)
	{
		OwningPlayer.RemovePet(this, mode, returnreagent);
	}

	public void GivePetXP(uint xp)
	{
		if (PetType != PetType.Hunter)
			return;

		if (xp < 1)
			return;

		if (!IsAlive)
			return;

		var maxlevel = Math.Min(GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel), OwningPlayer.Level);
		var petlevel = Level;

		// If pet is detected to be at, or above(?) the players level, don't hand out XP
		if (petlevel >= maxlevel)
			return;

		uint nextLvlXP = UnitData.PetNextLevelExperience;
		uint curXP = UnitData.PetExperience;
		var newXP = curXP + xp;

		// Check how much XP the pet should receive, and hand off have any left from previous levelups
		while (newXP >= nextLvlXP && petlevel < maxlevel)
		{
			// Subtract newXP from amount needed for nextlevel, and give pet the level
			newXP -= nextLvlXP;
			++petlevel;

			GivePetLevel((int)petlevel);

			nextLvlXP = UnitData.PetNextLevelExperience;
		}

		// Not affected by special conditions - give it new XP
		SetPetExperience(petlevel < maxlevel ? newXP : 0);
	}

	public void GivePetLevel(int level)
	{
		if (level == 0 || level == Level)
			return;

		if (PetType == PetType.Hunter)
		{
			SetPetExperience(0);
			SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel((uint)level) * PetXPFactor));
		}

		InitStatsForLevel((uint)level);
		InitLevelupSpellsForLevel();
	}

	public bool CreateBaseAtCreature(Creature creature)
	{
		if (!CreateBaseAtTamed(creature.Template, creature.Map))
			return false;

		Location.Relocate(creature.Location);

		if (!Location.IsPositionValid)
		{
			Log.Logger.Error("Pet (guidlow {0}, entry {1}) not created base at creature. Suggested coordinates isn't valid (X: {2} Y: {3})",
							GUID.ToString(),
							Entry,
							Location.X,
							Location.Y);

			return false;
		}

		var cinfo = Template;

		if (cinfo == null)
		{
			Log.Logger.Error("CreateBaseAtCreature() failed, creatureInfo is missing!");

			return false;
		}

		SetDisplayId(creature.DisplayId);
		var cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cinfo.Family);

		if (cFamily != null)
			SetName(cFamily.Name[OwningPlayer.Session.SessionDbcLocale]);
		else
			SetName(creature.GetName(Global.WorldMgr.DefaultDbcLocale));

		return true;
	}

	public bool CreateBaseAtCreatureInfo(CreatureTemplate cinfo, Unit owner)
	{
		if (!CreateBaseAtTamed(cinfo, owner.Map))
			return false;

		var cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cinfo.Family);

		if (cFamily != null)
			SetName(cFamily.Name[OwningPlayer.Session.SessionDbcLocale]);

		Location.Relocate(owner.Location);

		return true;
	}

	public bool HaveInDiet(ItemTemplate item)
	{
		if (item.FoodType == 0)
			return false;

		var cInfo = Template;

		if (cInfo == null)
			return false;

		var cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cInfo.Family);

		if (cFamily == null)
			return false;

		uint diet = cFamily.PetFoodMask;
		var FoodMask = (uint)(1 << ((int)item.FoodType - 1));

		return diet.HasAnyFlag(FoodMask);
	}

	public bool LearnSpell(uint spellId)
	{
		// prevent duplicated entires in spell book
		if (!AddSpell(spellId))
			return false;

		if (!_loading)
		{
			PetLearnedSpells packet = new();
			packet.Spells.Add(spellId);
			OwningPlayer.SendPacket(packet);
			OwningPlayer.PetSpellInitialize();
		}

		return true;
	}

	public bool RemoveSpell(uint spellId, bool learnPrev, bool clearActionBar = true)
	{
		var petSpell = Spells.LookupByKey(spellId);

		if (petSpell == null)
			return false;

		if (petSpell.State == PetSpellState.Removed)
			return false;

		if (petSpell.State == PetSpellState.New)
			Spells.Remove(spellId);
		else
			petSpell.State = PetSpellState.Removed;

		RemoveAura(spellId);

		if (learnPrev)
		{
			var prev_id = Global.SpellMgr.GetPrevSpellInChain(spellId);

			if (prev_id != 0)
				LearnSpell(prev_id);
			else
				learnPrev = false;
		}

		// if remove last rank or non-ranked then update action bar at server and client if need
		if (clearActionBar && !learnPrev && GetCharmInfo().RemoveSpellFromActionBar(spellId))
			if (!_loading)
			{
				// need update action bar for last removed rank
				Unit owner = OwningPlayer;

				if (owner)
					if (owner.IsTypeId(TypeId.Player))
						owner.AsPlayer.PetSpellInitialize();
			}

		return true;
	}

	public void InitPetCreateSpells()
	{
		GetCharmInfo().InitPetActionBar();
		Spells.Clear();

		LearnPetPassives();
		InitLevelupSpellsForLevel();

		CastPetAuras(false);
	}

	public void ToggleAutocast(SpellInfo spellInfo, bool apply)
	{
		if (!spellInfo.IsAutocastable)
			return;

		var petSpell = Spells.LookupByKey(spellInfo.Id);

		if (petSpell == null)
			return;

		var hasSpell = _autospells.Contains(spellInfo.Id);

		if (apply)
		{
			if (!hasSpell)
			{
				_autospells.Add(spellInfo.Id);

				if (petSpell.Active != ActiveStates.Enabled)
				{
					petSpell.Active = ActiveStates.Enabled;

					if (petSpell.State != PetSpellState.New)
						petSpell.State = PetSpellState.Changed;
				}
			}
		}
		else
		{
			if (hasSpell)
			{
				_autospells.Remove(spellInfo.Id);

				if (petSpell.Active != ActiveStates.Disabled)
				{
					petSpell.Active = ActiveStates.Disabled;

					if (petSpell.State != PetSpellState.New)
						petSpell.State = PetSpellState.Changed;
				}
			}
		}
	}

	public bool IsPermanentPetFor(Player owner)
	{
		switch (PetType)
		{
			case PetType.Summon:
				switch (owner.Class)
				{
					case PlayerClass.Warlock:
						return Template.CreatureType == CreatureType.Demon;
					case PlayerClass.Deathknight:
						return Template.CreatureType == CreatureType.Undead;
					case PlayerClass.Mage:
						return Template.CreatureType == CreatureType.Elemental;
					default:
						return false;
				}
			case PetType.Hunter:
				return true;
			default:
				return false;
		}
	}

	public bool Create(ulong guidlow, Map map, uint entry, uint petNumber)
	{
		Map = map;

		// TODO: counter should be constructed as (summon_count << 32) | petNumber
		Create(ObjectGuid.Create(HighGuid.Pet, map.Id, entry, guidlow));

		SpawnId = guidlow;
		OriginalEntry = entry;

		if (!InitEntry(entry))
			return false;

		// Force regen flag for player pets, just like we do for players themselves
		SetUnitFlag2(UnitFlags2.RegeneratePower);
		Sheath = SheathState.Melee;

		GetThreatManager().Initialize();

		return true;
	}

	public override bool HasSpell(uint spell)
	{
		var petSpell = Spells.LookupByKey(spell);

		return petSpell != null && petSpell.State != PetSpellState.Removed;
	}

	public void CastPetAura(PetAura aura)
	{
		var auraId = aura.GetAura(Entry);

		if (auraId == 0)
			return;

		CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);

		if (auraId == 35696) // Demonic Knowledge
			args.AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(aura.GetDamage(), GetStat(Stats.Stamina) + GetStat(Stats.Intellect)));

		CastSpell(this, auraId, args);
	}

	public void SynchronizeLevelWithOwner()
	{
		Unit owner = OwningPlayer;

		if (!owner || !owner.IsTypeId(TypeId.Player))
			return;

		switch (PetType)
		{
			// always same level
			case PetType.Summon:
			case PetType.Hunter:
				GivePetLevel((int)owner.Level);

				break;
			default:
				break;
		}
	}

	public override void SetDisplayId(uint modelId, float displayScale = 1f)
	{
		base.SetDisplayId(modelId, displayScale);

		if (!IsControlled)
			return;

		GroupUpdateFlag = GroupUpdatePetFlags.ModelId;
	}

	public override uint GetPetAutoSpellOnPos(byte pos)
	{
		if (pos >= _autospells.Count)
			return 0;
		else
			return _autospells[pos];
	}

	public void SetDuration(uint dur)
	{
		_duration = (int)dur;
	}

	public void SetPetExperience(uint xp)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetExperience), xp);
	}

	public void SetPetNextLevelExperience(uint xp)
	{
		SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.PetNextLevelExperience), xp);
	}

	public void ResetGroupUpdateFlag()
	{
		_mGroupUpdateMask = GroupUpdatePetFlags.None;

		if (OwningPlayer.Group)
			OwningPlayer.RemoveGroupUpdateFlag(GroupUpdateFlags.Pet);
	}

	public void SetSpecialization(uint spec)
	{
		if (_petSpecialization == spec)
			return;

		// remove all the old spec's specalization spells, set the new spec, then add the new spec's spells
		// clearActionBars is false because we'll be updating the pet actionbar later so we don't have to do it now
		RemoveSpecializationSpells(false);

		if (!CliDB.ChrSpecializationStorage.ContainsKey(spec))
		{
			_petSpecialization = 0;

			return;
		}

		_petSpecialization = (ushort)spec;
		LearnSpecializationSpells();

		// resend SMSG_PET_SPELLS_MESSAGE to remove old specialization spells from the pet action bar
		CleanupActionBar();
		OwningPlayer.PetSpellInitialize();

		SetPetSpecialization setPetSpecialization = new()
		{
			SpecID = _petSpecialization
		};

		OwningPlayer.SendPacket(setPetSpecialization);
	}

	public override string GetDebugInfo()
	{
		return $"{base.GetDebugInfo()}\nPetType: {PetType} PetNumber: {GetCharmInfo().GetPetNumber()}";
	}

	public DeclinedName GetDeclinedNames()
	{
		return _declinedname;
	}

	bool CreateBaseAtTamed(CreatureTemplate cinfo, Map map)
	{
		Log.Logger.Debug("CreateBaseForTamed");

		if (!Create(map.GenerateLowGuid(HighGuid.Pet), map, cinfo.Entry, Global.ObjectMgr.GeneratePetNumber()))
			return false;

		SetPetNameTimestamp(0);
		SetPetExperience(0);
		SetPetNextLevelExperience((uint)(Global.ObjectMgr.GetXPForLevel(Level + 1) * PetXPFactor));
		ReplaceAllNpcFlags(NPCFlags.None);
		ReplaceAllNpcFlags2(NPCFlags2.None);

		if (cinfo.CreatureType == CreatureType.Beast)
		{
			Class = PlayerClass.Warrior;
			Gender = Gender.None;
			SetPowerType(PowerType.Focus);
			Sheath = SheathState.Melee;
			ReplaceAllPetFlags(UnitPetFlags.CanBeRenamed | UnitPetFlags.CanBeAbandoned);
		}

		return true;
	}

	void _LoadSpells(SQLResult result)
	{
		if (!result.IsEmpty())
			do
			{
				AddSpell(result.Read<uint>(0), (ActiveStates)result.Read<byte>(1), PetSpellState.Unchanged);
			} while (result.NextRow());
	}

	void _SaveSpells(SQLTransaction trans)
	{
		foreach (var pair in Spells.ToList())
		{
			// prevent saving family passives to DB
			if (pair.Value.Type == PetSpellType.Family)
				continue;

			PreparedStatement stmt;

			switch (pair.Value.State)
			{
				case PetSpellState.Removed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
					stmt.AddValue(0, GetCharmInfo().GetPetNumber());
					stmt.AddValue(1, pair.Key);
					trans.Append(stmt);

					Spells.Remove(pair.Key);

					continue;
				case PetSpellState.Changed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_SPELL_BY_SPELL);
					stmt.AddValue(0, GetCharmInfo().GetPetNumber());
					stmt.AddValue(1, pair.Key);
					trans.Append(stmt);

					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL);
					stmt.AddValue(0, GetCharmInfo().GetPetNumber());
					stmt.AddValue(1, pair.Key);
					stmt.AddValue(2, (byte)pair.Value.Active);
					trans.Append(stmt);

					break;
				case PetSpellState.New:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_SPELL);
					stmt.AddValue(0, GetCharmInfo().GetPetNumber());
					stmt.AddValue(1, pair.Key);
					stmt.AddValue(2, (byte)pair.Value.Active);
					trans.Append(stmt);

					break;
				case PetSpellState.Unchanged:
					continue;
			}

			pair.Value.State = PetSpellState.Unchanged;
		}
	}

	void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
	{
		Log.Logger.Debug("Loading auras for {0}", GUID.ToString());

		ObjectGuid casterGuid = default;
		ObjectGuid itemGuid = default;
		Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new();

		if (!effectResult.IsEmpty())
			do
			{
				int effectIndex = effectResult.Read<byte>(3);
				casterGuid.SetRawValue(effectResult.Read<byte[]>(0));

				if (casterGuid.IsEmpty)
					casterGuid = GUID;

				AuraKey key = new(casterGuid, itemGuid, effectResult.Read<uint>(1), effectResult.Read<uint>(2));

				if (!effectInfo.ContainsKey(key))
					effectInfo[key] = new AuraLoadEffectInfo();

				var info = effectInfo[key];
				info.Amounts[effectIndex] = effectResult.Read<int>(4);
				info.BaseAmounts[effectIndex] = effectResult.Read<int>(5);
			} while (effectResult.NextRow());

		if (!auraResult.IsEmpty())
			do
			{
				// NULL guid stored - pet is the caster of the spell - see Pet._SaveAuras
				casterGuid.SetRawValue(auraResult.Read<byte[]>(0));

				if (casterGuid.IsEmpty)
					casterGuid = GUID;

				AuraKey key = new(casterGuid, itemGuid, auraResult.Read<uint>(1), auraResult.Read<uint>(2));
				var recalculateMask = auraResult.Read<uint>(3);
				var difficulty = (Difficulty)auraResult.Read<byte>(4);
				var stackCount = auraResult.Read<byte>(5);
				var maxDuration = auraResult.Read<int>(6);
				var remainTime = auraResult.Read<int>(7);
				var remainCharges = auraResult.Read<byte>(8);

				var spellInfo = Global.SpellMgr.GetSpellInfo(key.SpellId, difficulty);

				if (spellInfo == null)
				{
					Log.Logger.Error("Pet._LoadAuras: Unknown aura (spellid {0}), ignore.", key.SpellId);

					continue;
				}

				if (difficulty != Difficulty.None && !CliDB.DifficultyStorage.ContainsKey(difficulty))
				{
					Log.Logger.Error($"Pet._LoadAuras: Unknown difficulty {difficulty} (spellid {key.SpellId}), ignore.");

					continue;
				}

				// negative effects should continue counting down after logout
				if (remainTime != -1 && (!spellInfo.IsPositive || spellInfo.HasAttribute(SpellAttr4.AuraExpiresOffline)))
				{
					if (remainTime / Time.InMilliseconds <= timediff)
						continue;

					remainTime -= (int)timediff * Time.InMilliseconds;
				}

				// prevent wrong values of remaincharges
				if (spellInfo.ProcCharges != 0)
				{
					if (remainCharges <= 0)
						remainCharges = (byte)spellInfo.ProcCharges;
				}
				else
				{
					remainCharges = 0;
				}

				var info = effectInfo[key];
				var castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, Location.MapId, spellInfo.Id, Map.GenerateLowGuid(HighGuid.Cast));

				AuraCreateInfo createInfo = new(castId, spellInfo, difficulty, key.EffectMask.ExplodeMask(SpellConst.MaxEffects), this);
				createInfo.SetCasterGuid(casterGuid);
				createInfo.SetBaseAmount(info.BaseAmounts);

				var aura = Aura.TryCreate(createInfo);

				if (aura != null)
				{
					if (!aura.CanBeSaved())
					{
						aura.Remove();

						continue;
					}

					aura.SetLoadedState(maxDuration, remainTime, remainCharges, stackCount, recalculateMask, info.Amounts);
					aura.ApplyForTargets();
					Log.Logger.Information("Added aura spellid {0}, effectmask {1}", spellInfo.Id, key.EffectMask);
				}
			} while (auraResult.NextRow());
	}

	void _SaveAuras(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURA_EFFECTS);
		stmt.AddValue(0, GetCharmInfo().GetPetNumber());
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PET_AURAS);
		stmt.AddValue(0, GetCharmInfo().GetPetNumber());
		trans.Append(stmt);

		byte index;

		foreach (var aura in GetAuraQuery().CanBeSaved().AlsoMatches(a => !IsPetAura(a)).GetResults())
		{
			var key = aura.GenerateKey(out var recalculateMask);

			// don't save guid of caster in case we are caster of the spell - guid for pet is generated every pet load, so it won't match saved guid anyways
			if (key.Caster == GUID)
				key.Caster.Clear();

			index = 0;
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_AURA);
			stmt.AddValue(index++, GetCharmInfo().GetPetNumber());
			stmt.AddValue(index++, key.Caster.GetRawValue());
			stmt.AddValue(index++, key.SpellId);
			stmt.AddValue(index++, key.EffectMask);
			stmt.AddValue(index++, recalculateMask);
			stmt.AddValue(index++, (byte)aura.CastDifficulty);
			stmt.AddValue(index++, aura.StackAmount);
			stmt.AddValue(index++, aura.MaxDuration);
			stmt.AddValue(index++, aura.Duration);
			stmt.AddValue(index++, aura.Charges);
			trans.Append(stmt);

			foreach (var effect in aura.AuraEffects)
			{
				index = 0;
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PET_AURA_EFFECT);
				stmt.AddValue(index++, GetCharmInfo().GetPetNumber());
				stmt.AddValue(index++, key.Caster.GetRawValue());
				stmt.AddValue(index++, key.SpellId);
				stmt.AddValue(index++, key.EffectMask);
				stmt.AddValue(index++, effect.Value.EffIndex);
				stmt.AddValue(index++, effect.Value.Amount);
				stmt.AddValue(index++, effect.Value.BaseAmount);
				trans.Append(stmt);
			}
		}
	}

	bool AddSpell(uint spellId, ActiveStates active = ActiveStates.Decide, PetSpellState state = PetSpellState.New, PetSpellType type = PetSpellType.Normal)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

		if (spellInfo == null)
		{
			// do pet spell book cleanup
			if (state == PetSpellState.Unchanged) // spell load case
			{
				Log.Logger.Error("addSpell: Non-existed in SpellStore spell #{0} request, deleting for all pets in `pet_spell`.", spellId);

				var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_PET_SPELL);

				stmt.AddValue(0, spellId);

				DB.Characters.Execute(stmt);
			}
			else
			{
				Log.Logger.Error("addSpell: Non-existed in SpellStore spell #{0} request.", spellId);
			}

			return false;
		}

		var petSpell = Spells.LookupByKey(spellId);

		if (petSpell != null)
		{
			if (petSpell.State == PetSpellState.Removed)
			{
				state = PetSpellState.Changed;
			}
			else
			{
				if (state == PetSpellState.Unchanged && petSpell.State != PetSpellState.Unchanged)
				{
					// can be in case spell loading but learned at some previous spell loading
					petSpell.State = PetSpellState.Unchanged;

					if (active == ActiveStates.Enabled)
						ToggleAutocast(spellInfo, true);
					else if (active == ActiveStates.Disabled)
						ToggleAutocast(spellInfo, false);

					return false;
				}
			}
		}

		PetSpell newspell = new()
		{
			State = state,
			Type = type
		};

		if (active == ActiveStates.Decide) // active was not used before, so we save it's autocast/passive state here
		{
			if (spellInfo.IsAutocastable)
				newspell.Active = ActiveStates.Disabled;
			else
				newspell.Active = ActiveStates.Passive;
		}
		else
		{
			newspell.Active = active;
		}

		// talent: unlearn all other talent ranks (high and low)
		if (spellInfo.IsRanked)
			foreach (var pair in Spells)
			{
				if (pair.Value.State == PetSpellState.Removed)
					continue;

				var oldRankSpellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);

				if (oldRankSpellInfo == null)
					continue;

				if (spellInfo.IsDifferentRankOf(oldRankSpellInfo))
				{
					// replace by new high rank
					if (spellInfo.IsHighRankOf(oldRankSpellInfo))
					{
						newspell.Active = pair.Value.Active;

						if (newspell.Active == ActiveStates.Enabled)
							ToggleAutocast(oldRankSpellInfo, false);

						UnlearnSpell(pair.Key, false, false);

						break;
					}
					// ignore new lesser rank
					else
					{
						return false;
					}
				}
			}

		Spells[spellId] = newspell;

		if (spellInfo.IsPassive && (spellInfo.CasterAuraState == 0 || HasAuraState(spellInfo.CasterAuraState)))
			CastSpell(this, spellId, true);
		else
			GetCharmInfo().AddSpellToActionBar(spellInfo);

		if (newspell.Active == ActiveStates.Enabled)
			ToggleAutocast(spellInfo, true);

		return true;
	}

	void LearnSpells(List<uint> spellIds)
	{
		PetLearnedSpells packet = new();

		foreach (var spell in spellIds)
		{
			if (!AddSpell(spell))
				continue;

			packet.Spells.Add(spell);
		}

		if (!_loading)
			OwningPlayer.SendPacket(packet);
	}

	void InitLevelupSpellsForLevel()
	{
		var level = Level;
		var levelupSpells = Template.Family != 0 ? Global.SpellMgr.GetPetLevelupSpellList(Template.Family) : null;

		if (levelupSpells != null)
			// PetLevelupSpellSet ordered by levels, process in reversed order
			foreach (var pair in levelupSpells.KeyValueList)
				// will called first if level down
				if (pair.Key > level)
					UnlearnSpell(pair.Value, true); // will learn prev rank if any
				// will called if level up
				else
					LearnSpell(pair.Value); // will unlearn prev rank if any

		// default spells (can be not learned if pet level (as owner level decrease result for example) less first possible in normal game)
		var defSpells = Global.SpellMgr.GetPetDefaultSpellsEntry((int)Entry);

		if (defSpells != null)
			foreach (var spellId in defSpells.Spellid)
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

				if (spellInfo == null)
					continue;

				// will called first if level down
				if (spellInfo.SpellLevel > level)
					UnlearnSpell(spellInfo.Id, true);
				// will called if level up
				else
					LearnSpell(spellInfo.Id);
			}
	}

	bool UnlearnSpell(uint spellId, bool learnPrev, bool clearActionBar = true)
	{
		if (RemoveSpell(spellId, learnPrev, clearActionBar))
		{
			if (!_loading)
			{
				PetUnlearnedSpells packet = new();
				packet.Spells.Add(spellId);
				OwningPlayer.SendPacket(packet);
			}

			return true;
		}

		return false;
	}

	void UnlearnSpells(List<uint> spellIds, bool learnPrev, bool clearActionBar)
	{
		PetUnlearnedSpells packet = new();

		foreach (var spell in spellIds)
		{
			if (!RemoveSpell(spell, learnPrev, clearActionBar))
				continue;

			packet.Spells.Add(spell);
		}

		if (!_loading)
			OwningPlayer.SendPacket(packet);
	}

	void CleanupActionBar()
	{
		for (byte i = 0; i < SharedConst.ActionBarIndexMax; ++i)
		{
			var ab = GetCharmInfo().GetActionBarEntry(i);

			if (ab != null)
				if (ab.GetAction() != 0 && ab.IsActionBarForSpell())
				{
					if (!HasSpell(ab.GetAction()))
					{
						GetCharmInfo().SetActionBar(i, 0, ActiveStates.Passive);
					}
					else if (ab.GetActiveState() == ActiveStates.Enabled)
					{
						var spellInfo = Global.SpellMgr.GetSpellInfo(ab.GetAction(), Difficulty.None);

						if (spellInfo != null)
							ToggleAutocast(spellInfo, true);
					}
				}
		}
	}

	// Get all passive spells in our skill line
	void LearnPetPassives()
	{
		var cInfo = Template;

		if (cInfo == null)
			return;

		var cFamily = CliDB.CreatureFamilyStorage.LookupByKey(cInfo.Family);

		if (cFamily == null)
			return;

		var petStore = Global.SpellMgr.PetFamilySpellsStorage.LookupByKey(cInfo.Family);

		if (petStore != null)
			// For general hunter pets skill 270
			// Passive 01~10, Passive 00 (20782, not used), Ferocious Inspiration (34457)
			// Scale 01~03 (34902~34904, bonus from owner, not used)
			foreach (var spellId in petStore)
				AddSpell(spellId, ActiveStates.Decide, PetSpellState.New, PetSpellType.Family);
	}

	void CastPetAuras(bool current)
	{
		var owner = OwningPlayer;

		if (!IsPermanentPetFor(owner))
			return;

		foreach (var pa in owner.PetAuras)
			if (!current && pa.IsRemovedOnChangePet())
				owner.RemovePetAura(pa);
			else
				CastPetAura(pa);
	}

	bool IsPetAura(Aura aura)
	{
		var owner = OwningPlayer;

		// if the owner has that pet aura, return true
		foreach (var petAura in owner.PetAuras)
			if (petAura.GetAura(Entry) == aura.Id)
				return true;

		return false;
	}

	void LearnSpellHighRank(uint spellid)
	{
		LearnSpell(spellid);
		var next = Global.SpellMgr.GetNextSpellInChain(spellid);

		if (next != 0)
			LearnSpellHighRank(next);
	}

	void LearnSpecializationSpells()
	{
		List<uint> learnedSpells = new();

		var specSpells = Global.DB2Mgr.GetSpecializationSpells(_petSpecialization);

		if (specSpells != null)
			foreach (var specSpell in specSpells)
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo(specSpell.SpellID, Difficulty.None);

				if (spellInfo == null || spellInfo.SpellLevel > Level)
					continue;

				learnedSpells.Add(specSpell.SpellID);
			}

		LearnSpells(learnedSpells);
	}

	void RemoveSpecializationSpells(bool clearActionBar)
	{
		List<uint> unlearnedSpells = new();

		for (uint i = 0; i < PlayerConst.MaxSpecializations; ++i)
		{
			var specialization = Global.DB2Mgr.GetChrSpecializationByIndex(0, i);

			if (specialization != null)
			{
				var specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization.Id);

				if (specSpells != null)
					foreach (var specSpell in specSpells)
						unlearnedSpells.Add(specSpell.SpellID);
			}

			var specialization1 = Global.DB2Mgr.GetChrSpecializationByIndex(PlayerClass.Max, i);

			if (specialization1 != null)
			{
				var specSpells = Global.DB2Mgr.GetSpecializationSpells(specialization1.Id);

				if (specSpells != null)
					foreach (var specSpell in specSpells)
						unlearnedSpells.Add(specSpell.SpellID);
			}
		}

		UnlearnSpells(unlearnedSpells, true, clearActionBar);
	}

	string GenerateActionBarData()
	{
		StringBuilder ss = new();

		for (byte i = SharedConst.ActionBarIndexStart; i < SharedConst.ActionBarIndexEnd; ++i)
			ss.AppendFormat("{0} {1} ", (uint)GetCharmInfo().GetActionBarEntry(i).GetActiveState(), (uint)GetCharmInfo().GetActionBarEntry(i).GetAction());

		return ss.ToString();
	}
}