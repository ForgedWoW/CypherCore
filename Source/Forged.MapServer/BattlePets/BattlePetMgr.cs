﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.BattlePet;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.BattlePets;

public class BattlePetMgr
{
    private readonly WorldSession _owner;
    private readonly CliDB _cliDB;
    private readonly WorldManager _worldManager;
    private readonly LoginDatabase _loginDatabase;
    private readonly GameObjectManager _objectManager;
    private readonly ushort _trapLevel;
    private readonly Dictionary<ulong, BattlePet> _pets = new();
    private readonly List<BattlePetSlot> _slots = new();
    private bool _hasJournalLock;

	public bool IsJournalLockAcquired => _worldManager.IsBattlePetJournalLockAcquired(_owner.BattlenetAccountGUID);

	public WorldSession Owner => _owner;

	public ushort TrapLevel => _trapLevel;

	public List<BattlePetSlot> Slots => _slots;

	public bool HasJournalLock => _hasJournalLock;

	public bool IsBattlePetSystemEnabled => GetSlot(BattlePetSlots.Slot0).Locked != true;

	public BattlePetMgr(WorldSession owner, CliDB cliDB, WorldManager worldManager, LoginDatabase loginDatabase, GameObjectManager objectManager)
	{
		_owner = owner;
        _cliDB = cliDB;
        _worldManager = worldManager;
        _loginDatabase = loginDatabase;
        _objectManager = objectManager;

        for (byte i = 0; i < (int)BattlePetSlots.Count; ++i)
		{
			BattlePetSlot slot = new()
			{
				Index = i
			};

			_slots.Add(slot);
		}
	}


	public void LoadFromDB(SQLResult petsResult, SQLResult slotsResult)
	{
		if (!petsResult.IsEmpty())
			do
			{
				var species = petsResult.Read<uint>(1);
				var ownerGuid = !petsResult.IsNull(11) ? ObjectGuid.Create(HighGuid.Player, petsResult.Read<ulong>(11)) : ObjectGuid.Empty;

				var speciesEntry = _cliDB.BattlePetSpeciesStorage.LookupByKey(species);

				if (speciesEntry != null)
				{
					if (speciesEntry.GetFlags().HasFlag(BattlePetSpeciesFlags.NotAccountWide))
					{
						if (ownerGuid.IsEmpty)
						{
							Log.Logger.Error($"Battlenet account with id {_owner.BattlenetAccountId} has battle pet of species {species} with BattlePetSpeciesFlags::NotAccountWide but no owner");

							continue;
						}
					}
					else
					{
						if (!ownerGuid.IsEmpty)
						{
							Log.Logger.Error($"Battlenet account with id {_owner.BattlenetAccountId} has battle pet of species {species} without BattlePetSpeciesFlags::NotAccountWide but with owner");

							continue;
						}
					}

					if (HasMaxPetCount(speciesEntry, ownerGuid))
					{
						if (ownerGuid.IsEmpty)
							Log.Logger.Error($"Battlenet account with id {_owner.BattlenetAccountId} has more than maximum battle pets of species {species}");
						else
							Log.Logger.Error($"Battlenet account with id {_owner.BattlenetAccountId} has more than maximum battle pets of species {species} for player {ownerGuid}");

						continue;
					}

					BattlePet pet = new();
					pet.PacketInfo.Guid = ObjectGuid.Create(HighGuid.BattlePet, petsResult.Read<ulong>(0));
					pet.PacketInfo.Species = species;
					pet.PacketInfo.Breed = petsResult.Read<ushort>(2);
					pet.PacketInfo.DisplayID = petsResult.Read<uint>(3);
					pet.PacketInfo.Level = petsResult.Read<ushort>(4);
					pet.PacketInfo.Exp = petsResult.Read<ushort>(5);
					pet.PacketInfo.Health = petsResult.Read<uint>(6);
					pet.PacketInfo.Quality = petsResult.Read<byte>(7);
					pet.PacketInfo.Flags = petsResult.Read<ushort>(8);
					pet.PacketInfo.Name = petsResult.Read<string>(9);
					pet.NameTimestamp = petsResult.Read<long>(10);
					pet.PacketInfo.CreatureID = speciesEntry.CreatureID;

					if (!petsResult.IsNull(12))
					{
						pet.DeclinedName = new DeclinedName();

						for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
							pet.DeclinedName.Name[i] = petsResult.Read<string>(12 + i);
					}

					if (!ownerGuid.IsEmpty)
					{
						BattlePetStruct.BattlePetOwnerInfo battlePetOwnerInfo = new()
						{
							Guid = ownerGuid,
							PlayerVirtualRealm = _worldManager.VirtualRealmAddress,
							PlayerNativeRealm = _worldManager.VirtualRealmAddress
						};

						pet.PacketInfo.OwnerInfo = battlePetOwnerInfo;
					}

					pet.SaveInfo = BattlePetSaveInfo.Unchanged;
					pet.CalculateStats();
					_pets[pet.PacketInfo.Guid.Counter] = pet;
				}
			} while (petsResult.NextRow());

		if (!slotsResult.IsEmpty())
		{
			byte i = 0; // slots.GetRowCount() should equal MAX_BATTLE_PET_SLOTS

			do
			{
				_slots[i].Index = slotsResult.Read<byte>(0);
				var battlePet = _pets.LookupByKey(slotsResult.Read<ulong>(1));

				if (battlePet != null)
					_slots[i].Pet = battlePet.PacketInfo;

				_slots[i].Locked = slotsResult.Read<bool>(2);
				i++;
			} while (slotsResult.NextRow());
		}
	}

	public void SaveToDB(SQLTransaction trans)
	{
		PreparedStatement stmt;

		foreach (var pair in _pets)
			if (pair.Value != null)
				switch (pair.Value.SaveInfo)
				{
					case BattlePetSaveInfo.New:
						stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PETS);
						stmt.AddValue(0, pair.Key);
						stmt.AddValue(1, _owner.BattlenetAccountId);
						stmt.AddValue(2, pair.Value.PacketInfo.Species);
						stmt.AddValue(3, pair.Value.PacketInfo.Breed);
						stmt.AddValue(4, pair.Value.PacketInfo.DisplayID);
						stmt.AddValue(5, pair.Value.PacketInfo.Level);
						stmt.AddValue(6, pair.Value.PacketInfo.Exp);
						stmt.AddValue(7, pair.Value.PacketInfo.Health);
						stmt.AddValue(8, pair.Value.PacketInfo.Quality);
						stmt.AddValue(9, pair.Value.PacketInfo.Flags);
						stmt.AddValue(10, pair.Value.PacketInfo.Name);
						stmt.AddValue(11, pair.Value.NameTimestamp);

						if (pair.Value.PacketInfo.OwnerInfo.HasValue)
						{
							stmt.AddValue(12, pair.Value.PacketInfo.OwnerInfo.Value.Guid.Counter);
							stmt.AddValue(13, _worldManager.RealmId.Index);
						}
						else
						{
							stmt.AddNull(12);
							stmt.AddNull(13);
						}

						trans.Append(stmt);

						if (pair.Value.DeclinedName != null)
						{
							stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PET_DECLINED_NAME);
							stmt.AddValue(0, pair.Key);

							for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
								stmt.AddValue(i + 1, pair.Value.DeclinedName.Name[i]);

							trans.Append(stmt);
						}


						pair.Value.SaveInfo = BattlePetSaveInfo.Unchanged;

						break;
					case BattlePetSaveInfo.Changed:
						stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_BATTLE_PETS);
						stmt.AddValue(0, pair.Value.PacketInfo.Level);
						stmt.AddValue(1, pair.Value.PacketInfo.Exp);
						stmt.AddValue(2, pair.Value.PacketInfo.Health);
						stmt.AddValue(3, pair.Value.PacketInfo.Quality);
						stmt.AddValue(4, pair.Value.PacketInfo.Flags);
						stmt.AddValue(5, pair.Value.PacketInfo.Name);
						stmt.AddValue(6, pair.Value.NameTimestamp);
						stmt.AddValue(7, _owner.BattlenetAccountId);
						stmt.AddValue(8, pair.Key);
						trans.Append(stmt);

						stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME);
						stmt.AddValue(0, pair.Key);
						trans.Append(stmt);

						if (pair.Value.DeclinedName != null)
						{
							stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PET_DECLINED_NAME);
							stmt.AddValue(0, pair.Key);

							for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
								stmt.AddValue(i + 1, pair.Value.DeclinedName.Name[i]);

							trans.Append(stmt);
						}

						pair.Value.SaveInfo = BattlePetSaveInfo.Unchanged;

						break;
					case BattlePetSaveInfo.Removed:
						stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME);
						stmt.AddValue(0, pair.Key);
						trans.Append(stmt);

						stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS);
						stmt.AddValue(0, _owner.BattlenetAccountId);
						stmt.AddValue(1, pair.Key);
						trans.Append(stmt);
						_pets.Remove(pair.Key);

						break;
				}

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_SLOTS);
		stmt.AddValue(0, _owner.BattlenetAccountId);
		trans.Append(stmt);

		foreach (var slot in _slots)
		{
			stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_BATTLE_PET_SLOTS);
			stmt.AddValue(0, slot.Index);
			stmt.AddValue(1, _owner.BattlenetAccountId);
			stmt.AddValue(2, slot.Pet.Guid.Counter);
			stmt.AddValue(3, slot.Locked);
			trans.Append(stmt);
		}
	}

	public BattlePet GetPet(ObjectGuid guid)
	{
		return _pets.LookupByKey(guid.Counter);
	}

	public void AddPet(uint species, uint display, ushort breed, BattlePetBreedQuality quality, ushort level = 1)
	{
		var battlePetSpecies = _cliDB.BattlePetSpeciesStorage.LookupByKey(species);

		if (battlePetSpecies == null) // should never happen
			return;

		if (!battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.WellKnown)) // Not learnable
			return;

		BattlePet pet = new();
		pet.PacketInfo.Guid = ObjectGuid.Create(HighGuid.BattlePet, _objectManager.GetGenerator(HighGuid.BattlePet).Generate());
		pet.PacketInfo.Species = species;
		pet.PacketInfo.CreatureID = battlePetSpecies.CreatureID;
		pet.PacketInfo.DisplayID = display;
		pet.PacketInfo.Level = level;
		pet.PacketInfo.Exp = 0;
		pet.PacketInfo.Flags = 0;
		pet.PacketInfo.Breed = breed;
		pet.PacketInfo.Quality = (byte)quality;
		pet.PacketInfo.Name = "";
		pet.CalculateStats();
		pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

		var player = _owner.Player;

		if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.NotAccountWide))
		{
			BattlePetStruct.BattlePetOwnerInfo battlePetOwnerInfo = new()
			{
				Guid = player.GUID,
				PlayerVirtualRealm = _worldManager.VirtualRealmAddress,
				PlayerNativeRealm = _worldManager.VirtualRealmAddress
			};

			pet.PacketInfo.OwnerInfo = battlePetOwnerInfo;
		}

		pet.SaveInfo = BattlePetSaveInfo.New;

		_pets[pet.PacketInfo.Guid.Counter] = pet;

		List<BattlePet> updates = new();
		updates.Add(pet);
		SendUpdates(updates, true);

		player.UpdateCriteria(CriteriaType.UniquePetsOwned);
		player.UpdateCriteria(CriteriaType.LearnedNewPet, species);
	}

	public void RemovePet(ObjectGuid guid)
	{
		if (!HasJournalLock)
			return;

		var pet = GetPet(guid);

		if (pet == null)
			return;

		pet.SaveInfo = BattlePetSaveInfo.Removed;
	}

	public void ClearFanfare(ObjectGuid guid)
	{
		var pet = GetPet(guid);

		if (pet == null)
			return;

		pet.PacketInfo.Flags &= (ushort)~BattlePetDbFlags.FanfareNeeded;

		if (pet.SaveInfo != BattlePetSaveInfo.New)
			pet.SaveInfo = BattlePetSaveInfo.Changed;
	}

	public void ModifyName(ObjectGuid guid, string name, DeclinedName declinedName)
	{
		if (!HasJournalLock)
			return;

		var pet = GetPet(guid);

		if (pet == null)
			return;

		pet.PacketInfo.Name = name;
		pet.NameTimestamp = GameTime.GetGameTime();

		pet.DeclinedName = new DeclinedName();

		if (declinedName != null)
			pet.DeclinedName = declinedName;

		if (pet.SaveInfo != BattlePetSaveInfo.New)
			pet.SaveInfo = BattlePetSaveInfo.Changed;

		// Update the timestamp if the battle pet is summoned
		var summonedBattlePet = _owner.Player.GetSummonedBattlePet();

		if (summonedBattlePet != null)
			if (summonedBattlePet.BattlePetCompanionGUID == guid)
				summonedBattlePet.BattlePetCompanionNameTimestamp = (uint)pet.NameTimestamp;
	}

	public byte GetPetCount(BattlePetSpeciesRecord battlePetSpecies, ObjectGuid ownerGuid)
	{
		return (byte)_pets.Values.Count(battlePet =>
		{
			if (battlePet == null || battlePet.PacketInfo.Species != battlePetSpecies.Id)
				return false;

			if (battlePet.SaveInfo == BattlePetSaveInfo.Removed)
				return false;

			if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.NotAccountWide))
				if (!ownerGuid.IsEmpty && battlePet.PacketInfo.OwnerInfo.HasValue)
					if (battlePet.PacketInfo.OwnerInfo.Value.Guid != ownerGuid)
						return false;

			return true;
		});
	}

	public bool HasMaxPetCount(BattlePetSpeciesRecord battlePetSpecies, ObjectGuid ownerGuid)
	{
		var maxPetsPerSpecies = battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.LegacyAccountUnique) ? 1 : SharedConst.DefaultMaxBattlePetsPerSpecies;

		return GetPetCount(battlePetSpecies, ownerGuid) >= maxPetsPerSpecies;
	}

	public uint GetPetUniqueSpeciesCount()
	{
		HashSet<uint> speciesIds = new();

		foreach (var pair in _pets)
			if (pair.Value != null)
				speciesIds.Add(pair.Value.PacketInfo.Species);

		return (uint)speciesIds.Count;
	}

	public void UnlockSlot(BattlePetSlots slot)
	{
		if (slot >= BattlePetSlots.Count)
			return;

		var slotIndex = (byte)slot;

		if (!_slots[slotIndex].Locked)
			return;

		_slots[slotIndex].Locked = false;

		PetBattleSlotUpdates updates = new();
		updates.Slots.Add(_slots[slotIndex]);
		updates.AutoSlotted = false; // what's this?
		updates.NewSlot = true;      // causes the "new slot unlocked" bubble to appear
		_owner.SendPacket(updates);
	}

	public ushort GetMaxPetLevel()
	{
		ushort level = 0;

		foreach (var pet in _pets)
			if (pet.Value != null && pet.Value.SaveInfo != BattlePetSaveInfo.Removed)
				level = Math.Max(level, pet.Value.PacketInfo.Level);

		return level;
	}

	public void CageBattlePet(ObjectGuid guid)
	{
		if (!HasJournalLock)
			return;

		var pet = GetPet(guid);

		if (pet == null)
			return;

		var battlePetSpecies = _cliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);

		if (battlePetSpecies != null)
			if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.NotTradable))
				return;

		if (IsPetInSlot(guid))
			return;

		if (pet.PacketInfo.Health < pet.PacketInfo.MaxHealth)
			return;

		List<ItemPosCount> dest = new();

		if (_owner.Player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, SharedConst.BattlePetCageItemId, 1) != InventoryResult.Ok)
			return;

		var item = _owner.Player.StoreNewItem(dest, SharedConst.BattlePetCageItemId, true);

		if (!item)
			return;

		item.SetModifier(ItemModifier.BattlePetSpeciesId, pet.PacketInfo.Species);
		item.SetModifier(ItemModifier.BattlePetBreedData, (uint)(pet.PacketInfo.Breed | (pet.PacketInfo.Quality << 24)));
		item.SetModifier(ItemModifier.BattlePetLevel, pet.PacketInfo.Level);
		item.SetModifier(ItemModifier.BattlePetDisplayId, pet.PacketInfo.DisplayID);

		_owner.Player.SendNewItem(item, 1, true, false);

		RemovePet(guid);

		BattlePetDeleted deletePet = new()
		{
			PetGuid = guid
		};

		_owner.SendPacket(deletePet);

		// Battle pet despawns if it's summoned
		var player = _owner.Player;
		var summonedBattlePet = player.GetSummonedBattlePet();

		if (summonedBattlePet != null)
			if (summonedBattlePet.BattlePetCompanionGUID == guid)
			{
				summonedBattlePet.DespawnOrUnsummon();
				player.SetBattlePetData();
			}
	}

	public void ChangeBattlePetQuality(ObjectGuid guid, BattlePetBreedQuality quality)
	{
		if (!HasJournalLock)
			return;

		var pet = GetPet(guid);

		if (pet == null)
			return;

		if (quality > BattlePetBreedQuality.Rare)
			return;

		var battlePetSpecies = _cliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);

		if (battlePetSpecies != null)
			if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.CantBattle))
				return;

		var qualityValue = (byte)quality;

		if (pet.PacketInfo.Quality >= qualityValue)
			return;

		pet.PacketInfo.Quality = qualityValue;
		pet.CalculateStats();
		pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

		if (pet.SaveInfo != BattlePetSaveInfo.New)
			pet.SaveInfo = BattlePetSaveInfo.Changed;

		List<BattlePet> updates = new();
		updates.Add(pet);
		SendUpdates(updates, false);

		// UF::PlayerData::CurrentBattlePetBreedQuality isn't updated (Intended)
		// _owner.GetPlayer().SetCurrentBattlePetBreedQuality(qualityValue);
	}

	public void GrantBattlePetExperience(ObjectGuid guid, ushort xp, BattlePetXpSource xpSource)
	{
		if (!HasJournalLock)
			return;

		var pet = GetPet(guid);

		if (pet == null)
			return;

		if (xp <= 0 || xpSource >= BattlePetXpSource.Count)
			return;

		var battlePetSpecies = _cliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);

		if (battlePetSpecies != null)
			if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.CantBattle))
				return;

		var level = pet.PacketInfo.Level;

		if (level >= SharedConst.MaxBattlePetLevel)
			return;

		var xpEntry = _cliDB.BattlePetXPGameTable.GetRow(level);

		if (xpEntry == null)
			return;

		var player = _owner.Player;
		var nextLevelXp = (ushort)CliDB.GetBattlePetXPPerLevel(xpEntry);

		if (xpSource == BattlePetXpSource.PetBattle)
			xp = (ushort)(xp * player.GetTotalAuraMultiplier(AuraType.ModBattlePetXpPct));

		xp += pet.PacketInfo.Exp;

		while (xp >= nextLevelXp && level < SharedConst.MaxBattlePetLevel)
		{
			xp -= nextLevelXp;

			xpEntry = _cliDB.BattlePetXPGameTable.GetRow(++level);

			if (xpEntry == null)
				return;

			nextLevelXp = (ushort)CliDB.GetBattlePetXPPerLevel(xpEntry);

			player.UpdateCriteria(CriteriaType.BattlePetReachLevel, pet.PacketInfo.Species, level);

			if (xpSource == BattlePetXpSource.PetBattle)
				player.UpdateCriteria(CriteriaType.ActivelyEarnPetLevel, pet.PacketInfo.Species, level);
		}

		pet.PacketInfo.Level = level;
		pet.PacketInfo.Exp = (ushort)(level < SharedConst.MaxBattlePetLevel ? xp : 0);
		pet.CalculateStats();
		pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

		if (pet.SaveInfo != BattlePetSaveInfo.New)
			pet.SaveInfo = BattlePetSaveInfo.Changed;

		List<BattlePet> updates = new();
		updates.Add(pet);
		SendUpdates(updates, false);
	}

	public void GrantBattlePetLevel(ObjectGuid guid, ushort grantedLevels)
	{
		if (!HasJournalLock)
			return;

		var pet = GetPet(guid);

		if (pet == null)
			return;

		var battlePetSpecies = _cliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);

		if (battlePetSpecies != null)
			if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.CantBattle))
				return;

		var level = pet.PacketInfo.Level;

		if (level >= SharedConst.MaxBattlePetLevel)
			return;

		while (grantedLevels > 0 && level < SharedConst.MaxBattlePetLevel)
		{
			++level;
			--grantedLevels;

			_owner.Player.UpdateCriteria(CriteriaType.BattlePetReachLevel, pet.PacketInfo.Species, level);
		}

		pet.PacketInfo.Level = level;

		if (level >= SharedConst.MaxBattlePetLevel)
			pet.PacketInfo.Exp = 0;

		pet.CalculateStats();
		pet.PacketInfo.Health = pet.PacketInfo.MaxHealth;

		if (pet.SaveInfo != BattlePetSaveInfo.New)
			pet.SaveInfo = BattlePetSaveInfo.Changed;

		var updates = new List<BattlePet>();
		updates.Add(pet);
		SendUpdates(updates, false);
	}

	public void HealBattlePetsPct(byte pct)
	{
		// TODO: After each Pet Battle, any injured companion will automatically
		// regain 50 % of the damage that was taken during combat
		List<BattlePet> updates = new();

		foreach (var pet in _pets.Values)
			if (pet != null && pet.PacketInfo.Health != pet.PacketInfo.MaxHealth)
			{
				pet.PacketInfo.Health += MathFunctions.CalculatePct(pet.PacketInfo.MaxHealth, pct);
				// don't allow Health to be greater than MaxHealth
				pet.PacketInfo.Health = Math.Min(pet.PacketInfo.Health, pet.PacketInfo.MaxHealth);

				if (pet.SaveInfo != BattlePetSaveInfo.New)
					pet.SaveInfo = BattlePetSaveInfo.Changed;

				updates.Add(pet);
			}

		SendUpdates(updates, false);
	}

	public void UpdateBattlePetData(ObjectGuid guid)
	{
		var pet = GetPet(guid);

		if (pet == null)
			return;

		var player = _owner.Player;

		// Update battle pet related update fields
		var summonedBattlePet = player.GetSummonedBattlePet();

		if (summonedBattlePet != null)
			if (summonedBattlePet.BattlePetCompanionGUID == guid)
			{
				summonedBattlePet.WildBattlePetLevel = pet.PacketInfo.Level;
				player.SetBattlePetData(pet);
			}
	}

	public void SummonPet(ObjectGuid guid)
	{
		var pet = GetPet(guid);

		if (pet == null)
			return;

		var speciesEntry = _cliDB.BattlePetSpeciesStorage.LookupByKey(pet.PacketInfo.Species);

		if (speciesEntry == null)
			return;

		var player = _owner.Player;
		player.SetBattlePetData(pet);

		CastSpellExtraArgs args = new();
		var summonSpellId = speciesEntry.SummonSpellID;

		if (summonSpellId == 0)
		{
			summonSpellId = SharedConst.SpellSummonBattlePet;
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)speciesEntry.CreatureID);
		}

		player.CastSpell(_owner.Player, summonSpellId, args);
	}

	public void DismissPet()
	{
		var player = _owner.Player;
		var summonedBattlePet = player.GetSummonedBattlePet();

		if (summonedBattlePet)
		{
			summonedBattlePet.DespawnOrUnsummon();
			player.SetBattlePetData(null);
		}
	}

	public void SendJournal()
	{
		if (!HasJournalLock)
			SendJournalLockStatus();

		BattlePetJournal battlePetJournal = new()
		{
			Trap = _trapLevel,
			HasJournalLock = _hasJournalLock
		};

		foreach (var pet in _pets)
			if (pet.Value != null && pet.Value.SaveInfo != BattlePetSaveInfo.Removed)
				if (!pet.Value.PacketInfo.OwnerInfo.HasValue || pet.Value.PacketInfo.OwnerInfo.Value.Guid == _owner.Player.GUID)
					battlePetJournal.Pets.Add(pet.Value.PacketInfo);

		battlePetJournal.Slots = _slots;
		_owner.SendPacket(battlePetJournal);
	}

	public void SendError(BattlePetError error, uint creatureId)
	{
		BattlePetErrorPacket battlePetError = new()
		{
			Result = error,
			CreatureID = creatureId
		};

		_owner.SendPacket(battlePetError);
	}

	public void SendJournalLockStatus()
	{
		if (!IsJournalLockAcquired)
			ToggleJournalLock(true);

		if (HasJournalLock)
			_owner.SendPacket(new BattlePetJournalLockAcquired());
		else
			_owner.SendPacket(new BattlePetJournalLockDenied());
	}

	public BattlePetSlot GetSlot(BattlePetSlots slot)
	{
		return slot < BattlePetSlots.Count ? _slots[(byte)slot] : null;
	}

	public void ToggleJournalLock(bool on)
	{
		_hasJournalLock = on;
	}


    private bool IsPetInSlot(ObjectGuid guid)
	{
		foreach (var slot in _slots)
			if (slot.Pet.Guid == guid)
				return true;

		return false;
	}

    private void SendUpdates(List<BattlePet> pets, bool petAdded)
	{
		BattlePetUpdates updates = new();

		foreach (var pet in pets)
			updates.Pets.Add(pet.PacketInfo);

		updates.PetAdded = petAdded;
		_owner.SendPacket(updates);
	}
}