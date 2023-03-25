// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.H;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Transmogification;
using Forged.MapServer.Services;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities.Players;

public class CollectionMgr
{
	static readonly Dictionary<uint, uint> FactionSpecificMounts = new();
	readonly WorldSession _owner;
	readonly Dictionary<uint, ToyFlags> _toys = new();
	readonly Dictionary<uint, HeirloomData> _heirlooms = new();
	readonly Dictionary<uint, MountStatusFlags> _mounts = new();
	readonly MultiMap<uint, ObjectGuid> _temporaryAppearances = new();
	readonly Dictionary<uint, FavoriteAppearanceState> _favoriteAppearances = new();

	readonly uint[] _playerClassByArmorSubclass =
	{
		(int)PlayerClass.ClassMaskAllPlayable,                                                                                                                          //ITEM_SUBCLASS_ARMOR_MISCELLANEOUS
		(1 << ((int)PlayerClass.Priest - 1)) | (1 << ((int)PlayerClass.Mage - 1)) | (1 << ((int)PlayerClass.Warlock - 1)),                                              //ITEM_SUBCLASS_ARMOR_CLOTH
		(1 << ((int)PlayerClass.Rogue - 1)) | (1 << ((int)PlayerClass.Monk - 1)) | (1 << ((int)PlayerClass.Druid - 1)) | (1 << ((int)PlayerClass.DemonHunter - 1)),     //ITEM_SUBCLASS_ARMOR_LEATHER
		(1 << ((int)PlayerClass.Hunter - 1)) | (1 << ((int)PlayerClass.Shaman - 1)),                                                                                    //ITEM_SUBCLASS_ARMOR_MAIL
		(1 << ((int)PlayerClass.Warrior - 1)) | (1 << ((int)PlayerClass.Paladin - 1)) | (1 << ((int)PlayerClass.Deathknight - 1)),                                      //ITEM_SUBCLASS_ARMOR_PLATE
		(int)PlayerClass.ClassMaskAllPlayable,                                                                                                                          //ITEM_SUBCLASS_ARMOR_BUCKLER
		(1 << ((int)PlayerClass.Warrior - 1)) | (1 << ((int)PlayerClass.Paladin - 1)) | (1 << ((int)PlayerClass.Shaman - 1)),                                           //ITEM_SUBCLASS_ARMOR_SHIELD
		1 << ((int)PlayerClass.Paladin - 1),                                                                                                                            //ITEM_SUBCLASS_ARMOR_LIBRAM
		1 << ((int)PlayerClass.Druid - 1),                                                                                                                              //ITEM_SUBCLASS_ARMOR_IDOL
		1 << ((int)PlayerClass.Shaman - 1),                                                                                                                             //ITEM_SUBCLASS_ARMOR_TOTEM
		1 << ((int)PlayerClass.Deathknight - 1),                                                                                                                        //ITEM_SUBCLASS_ARMOR_SIGIL
		(1 << ((int)PlayerClass.Paladin - 1)) | (1 << ((int)PlayerClass.Deathknight - 1)) | (1 << ((int)PlayerClass.Shaman - 1)) | (1 << ((int)PlayerClass.Druid - 1)), //ITEM_SUBCLASS_ARMOR_RELIC
	};

	BitSet _appearances;
	BitSet _transmogIllusions;

	public CollectionMgr(WorldSession owner)
	{
		_owner = owner;
		_appearances = new BitSet(0);
		_transmogIllusions = new BitSet(0);
	}

	public static void LoadMountDefinitions()
	{
		var oldMsTime = Time.MSTime;

		var result = DB.World.Query("SELECT spellId, otherFactionSpellId FROM mount_definitions");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 mount definitions. DB table `mount_definitions` is empty.");

			return;
		}

		do
		{
			var spellId = result.Read<uint>(0);
			var otherFactionSpellId = result.Read<uint>(1);

			if (Global.DB2Mgr.GetMount(spellId) == null)
			{
				Log.Logger.Error("Mount spell {0} defined in `mount_definitions` does not exist in Mount.db2, skipped", spellId);

				continue;
			}

			if (otherFactionSpellId != 0 && Global.DB2Mgr.GetMount(otherFactionSpellId) == null)
			{
				Log.Logger.Error("otherFactionSpellId {0} defined in `mount_definitions` for spell {1} does not exist in Mount.db2, skipped", otherFactionSpellId, spellId);

				continue;
			}

			FactionSpecificMounts[spellId] = otherFactionSpellId;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} mount definitions in {1} ms", FactionSpecificMounts.Count, Time.GetMSTimeDiffToNow(oldMsTime));
	}

	public void LoadToys()
	{
		foreach (var pair in _toys)
			_owner.Player.AddToy(pair.Key, (uint)pair.Value);
	}

	public bool AddToy(uint itemId, bool isFavourite, bool hasFanfare)
	{
		if (UpdateAccountToys(itemId, isFavourite, hasFanfare))
		{
			_owner.Player?.AddToy(itemId, (uint)GetToyFlags(isFavourite, hasFanfare));

			return true;
		}

		return false;
	}

	public void LoadAccountToys(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			var itemId = result.Read<uint>(0);
			_toys.Add(itemId, GetToyFlags(result.Read<bool>(1), result.Read<bool>(2)));
		} while (result.NextRow());
	}

	public void SaveAccountToys(SQLTransaction trans)
	{
		PreparedStatement stmt;

		foreach (var pair in _toys)
		{
			stmt = DB.Login.GetPreparedStatement(LoginStatements.REP_ACCOUNT_TOYS);
			stmt.AddValue((int)0, (uint)_owner.BattlenetAccountId);
			stmt.AddValue(1, pair.Key);
			stmt.AddValue(2, pair.Value.HasAnyFlag(ToyFlags.Favorite));
			stmt.AddValue(3, pair.Value.HasAnyFlag(ToyFlags.HasFanfare));
			trans.Append(stmt);
		}
	}

	public void ToySetFavorite(uint itemId, bool favorite)
	{
		if (!_toys.ContainsKey(itemId))
			return;

		if (favorite)
			_toys[itemId] |= ToyFlags.Favorite;
		else
			_toys[itemId] &= ~ToyFlags.Favorite;
	}

	public void ToyClearFanfare(uint itemId)
	{
		if (!_toys.ContainsKey(itemId))
			return;

		_toys[itemId] &= ~ToyFlags.HasFanfare;
	}

	public void OnItemAdded(Item item)
	{
		if (Global.DB2Mgr.GetHeirloomByItemId(item.Entry) != null)
			AddHeirloom(item.Entry, 0);

		AddItemAppearance(item);
	}

	public void LoadAccountHeirlooms(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			var itemId = result.Read<uint>(0);
			var flags = (HeirloomPlayerFlags)result.Read<uint>(1);

			var heirloom = Global.DB2Mgr.GetHeirloomByItemId(itemId);

			if (heirloom == null)
				continue;

			uint bonusId = 0;

			for (var upgradeLevel = heirloom.UpgradeItemID.Length - 1; upgradeLevel >= 0; --upgradeLevel)
				if (((int)flags & (1 << upgradeLevel)) != 0)
				{
					bonusId = heirloom.UpgradeItemBonusListID[upgradeLevel];

					break;
				}

			_heirlooms[itemId] = new HeirloomData(flags, bonusId);
		} while (result.NextRow());
	}

	public void SaveAccountHeirlooms(SQLTransaction trans)
	{
		PreparedStatement stmt;

		foreach (var heirloom in _heirlooms)
		{
			stmt = DB.Login.GetPreparedStatement(LoginStatements.REP_ACCOUNT_HEIRLOOMS);
			stmt.AddValue((int)0, (uint)_owner.BattlenetAccountId);
			stmt.AddValue(1, heirloom.Key);
			stmt.AddValue(2, (uint)heirloom.Value.Flags);
			trans.Append(stmt);
		}
	}

	public uint GetHeirloomBonus(uint itemId)
	{
		var data = _heirlooms.LookupByKey(itemId);

		if (data != null)
			return data.BonusId;

		return 0;
	}

	public void LoadHeirlooms()
	{
		foreach (var item in _heirlooms)
			_owner.Player.AddHeirloom(item.Key, (uint)item.Value.Flags);
	}

	public void AddHeirloom(uint itemId, HeirloomPlayerFlags flags)
	{
		if (UpdateAccountHeirlooms(itemId, flags))
			_owner.Player.AddHeirloom(itemId, (uint)flags);
	}

	public void UpgradeHeirloom(uint itemId, uint castItem)
	{
		var player = _owner.Player;

		if (!player)
			return;

		var heirloom = Global.DB2Mgr.GetHeirloomByItemId(itemId);

		if (heirloom == null)
			return;

		var data = _heirlooms.LookupByKey(itemId);

		if (data == null)
			return;

		var flags = data.Flags;
		uint bonusId = 0;

		for (var upgradeLevel = 0; upgradeLevel < heirloom.UpgradeItemID.Length; ++upgradeLevel)
			if (heirloom.UpgradeItemID[upgradeLevel] == castItem)
			{
				flags |= (HeirloomPlayerFlags)(1 << upgradeLevel);
				bonusId = heirloom.UpgradeItemBonusListID[upgradeLevel];
			}

		foreach (var item in player.GetItemListByEntry(itemId, true))
			item.AddBonuses(bonusId);

		// Get heirloom offset to update only one part of dynamic field
		List<uint> heirlooms = player.ActivePlayerData.Heirlooms;
		var offset = heirlooms.IndexOf(itemId);

		player.SetHeirloomFlags(offset, (uint)flags);
		data.Flags = flags;
		data.BonusId = bonusId;
	}

	public void CheckHeirloomUpgrades(Item item)
	{
		var player = _owner.Player;

		if (!player)
			return;

		// Check already owned heirloom for upgrade kits
		var heirloom = Global.DB2Mgr.GetHeirloomByItemId(item.Entry);

		if (heirloom != null)
		{
			var data = _heirlooms.LookupByKey(item.Entry);

			if (data == null)
				return;

			// Check for heirloom pairs (normal - heroic, heroic - mythic)
			var heirloomItemId = heirloom.StaticUpgradedItemID;
			uint newItemId = 0;
			HeirloomRecord heirloomDiff;

			while ((heirloomDiff = Global.DB2Mgr.GetHeirloomByItemId(heirloomItemId)) != null)
			{
				if (player.GetItemByEntry(heirloomDiff.ItemID))
					newItemId = heirloomDiff.ItemID;

				var heirloomSub = Global.DB2Mgr.GetHeirloomByItemId(heirloomDiff.StaticUpgradedItemID);

				if (heirloomSub != null)
				{
					heirloomItemId = heirloomSub.ItemID;

					continue;
				}

				break;
			}

			if (newItemId != 0)
			{
				List<uint> heirlooms = player.ActivePlayerData.Heirlooms;
				var offset = heirlooms.IndexOf(item.Entry);

				player.SetHeirloom(offset, newItemId);
				player.SetHeirloomFlags(offset, 0);

				_heirlooms.Remove(item.Entry);
				_heirlooms[newItemId] = null;

				return;
			}

			var bonusListIDs = item.GetBonusListIDs();

			foreach (var bonusId in bonusListIDs)
				if (bonusId != data.BonusId)
				{
					item.ClearBonuses();

					break;
				}

			if (!bonusListIDs.Contains(data.BonusId))
				item.AddBonuses(data.BonusId);
		}
	}

	public void LoadMounts()
	{
		foreach (var m in _mounts.ToList())
			AddMount(m.Key, m.Value);
	}

	public void LoadAccountMounts(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			var mountSpellId = result.Read<uint>(0);
			var flags = (MountStatusFlags)result.Read<byte>(1);

			if (Global.DB2Mgr.GetMount(mountSpellId) == null)
				continue;

			_mounts[mountSpellId] = flags;
		} while (result.NextRow());
	}

	public void SaveAccountMounts(SQLTransaction trans)
	{
		foreach (var mount in _mounts)
		{
			var stmt = DB.Login.GetPreparedStatement(LoginStatements.REP_ACCOUNT_MOUNTS);
			stmt.AddValue(0, _owner.BattlenetAccountId);
			stmt.AddValue(1, mount.Key);
			stmt.AddValue(2, (byte)mount.Value);
			trans.Append(stmt);
		}
	}

	public bool AddMount(uint spellId, MountStatusFlags flags, bool factionMount = false, bool learned = false)
	{
		var player = _owner.Player;

		if (!player)
			return false;

		var mount = Global.DB2Mgr.GetMount(spellId);

		if (mount == null)
			return false;

		var value = FactionSpecificMounts.LookupByKey(spellId);

		if (value != 0 && !factionMount)
			AddMount(value, flags, true, learned);

		_mounts[spellId] = flags;

		// Mount condition only applies to using it, should still learn it.
		if (mount.PlayerConditionID != 0)
		{
			var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mount.PlayerConditionID);

			if (playerCondition != null && !ConditionManager.IsPlayerMeetingCondition(player, playerCondition))
				return false;
		}

		if (!learned)
		{
			if (!factionMount)
				SendSingleMountUpdate(spellId, flags);

			if (!player.HasSpell(spellId))
				player.LearnSpell(spellId, true);
		}

		return true;
	}

	public void MountSetFavorite(uint spellId, bool favorite)
	{
		if (!_mounts.ContainsKey(spellId))
			return;

		if (favorite)
			_mounts[spellId] |= MountStatusFlags.IsFavorite;
		else
			_mounts[spellId] &= ~MountStatusFlags.IsFavorite;

		SendSingleMountUpdate(spellId, _mounts[spellId]);
	}

	public void LoadItemAppearances()
	{
		var owner = _owner.Player;

		foreach (var blockValue in _appearances.ToBlockRange())
			owner.AddTransmogBlock(blockValue);

		foreach (var value in _temporaryAppearances.Keys)
			owner.AddConditionalTransmog(value);
	}

	public void LoadAccountItemAppearances(SQLResult knownAppearances, SQLResult favoriteAppearances)
	{
		if (!knownAppearances.IsEmpty())
		{
			var blocks = new uint[1];

			do
			{
				var blobIndex = knownAppearances.Read<ushort>(0);

				if (blobIndex >= blocks.Length)
					Array.Resize(ref blocks, blobIndex + 1);

				blocks[blobIndex] = knownAppearances.Read<uint>(1);
			} while (knownAppearances.NextRow());

			_appearances = new BitSet(blocks);
		}

		if (!favoriteAppearances.IsEmpty())
			do
			{
				_favoriteAppearances[favoriteAppearances.Read<uint>(0)] = FavoriteAppearanceState.Unchanged;
			} while (favoriteAppearances.NextRow());

		// Static item appearances known by every player
		uint[] hiddenAppearanceItems =
		{
			134110, // Hidden Helm
			134111, // Hidden Cloak
			134112, // Hidden Shoulder
			168659, // Hidden Chestpiece
			142503, // Hidden Shirt
			142504, // Hidden Tabard
			168665, // Hidden Bracers
			158329, // Hidden Gloves
			143539, // Hidden Belt
			168664  // Hidden Boots
		};

		foreach (var hiddenItem in hiddenAppearanceItems)
		{
			var hiddenAppearance = Global.DB2Mgr.GetItemModifiedAppearance(hiddenItem, 0);

			//ASSERT(hiddenAppearance);
			if (_appearances.Length <= hiddenAppearance.Id)
				_appearances.Length = (int)hiddenAppearance.Id + 1;

			_appearances.Set((int)hiddenAppearance.Id, true);
		}
	}

	public void SaveAccountItemAppearances(SQLTransaction trans)
	{
		PreparedStatement stmt;
		ushort blockIndex = 0;

		foreach (var blockValue in _appearances.ToBlockRange())
		{
			if (blockValue != 0) // this table is only appended/bits are set (never cleared) so don't save empty blocks
			{
				stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ITEM_APPEARANCES);
				stmt.AddValue((int)0, (uint)_owner.BattlenetAccountId);
				stmt.AddValue(1, blockIndex);
				stmt.AddValue(2, blockValue);
				trans.Append(stmt);
			}

			++blockIndex;
		}

		foreach (var key in _favoriteAppearances.Keys)
		{
			var appearanceState = _favoriteAppearances[key];

			switch (appearanceState)
			{
				case FavoriteAppearanceState.New:
					stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ITEM_FAVORITE_APPEARANCE);
					stmt.AddValue((int)0, (uint)_owner.BattlenetAccountId);
					stmt.AddValue(1, key);
					trans.Append(stmt);
					_favoriteAppearances[key] = FavoriteAppearanceState.Unchanged;

					break;
				case FavoriteAppearanceState.Removed:
					stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BNET_ITEM_FAVORITE_APPEARANCE);
					stmt.AddValue((int)0, (uint)_owner.BattlenetAccountId);
					stmt.AddValue(1, key);
					trans.Append(stmt);
					_favoriteAppearances.Remove(key);

					break;
				case FavoriteAppearanceState.Unchanged:
					break;
			}
		}
	}

	public void AddItemAppearance(Item item)
	{
		if (!item.IsSoulBound)
			return;

		var itemModifiedAppearance = item.GetItemModifiedAppearance();

		if (!CanAddAppearance(itemModifiedAppearance))
			return;

		if (item.IsBOPTradeable || item.IsRefundable)
		{
			AddTemporaryAppearance(item.GUID, itemModifiedAppearance);

			return;
		}

		AddItemAppearance(itemModifiedAppearance);
	}

	public void AddItemAppearance(uint itemId, uint appearanceModId = 0)
	{
		var itemModifiedAppearance = Global.DB2Mgr.GetItemModifiedAppearance(itemId, appearanceModId);

		if (!CanAddAppearance(itemModifiedAppearance))
			return;

		AddItemAppearance(itemModifiedAppearance);
	}

	public void RemoveTemporaryAppearance(Item item)
	{
		var itemModifiedAppearance = item.GetItemModifiedAppearance();

		if (itemModifiedAppearance == null)
			return;

		var guid = _temporaryAppearances.LookupByKey(itemModifiedAppearance.Id).ToList();

		if (guid.Empty())
			return;

		guid.Remove(item.GUID);

		if (guid.Empty())
		{
			_owner.Player.RemoveConditionalTransmog(itemModifiedAppearance.Id);
			_temporaryAppearances.Remove(itemModifiedAppearance.Id);
		}
	}

	public (bool PermAppearance, bool TempAppearance) HasItemAppearance(uint itemModifiedAppearanceId)
	{
		if (itemModifiedAppearanceId < _appearances.Count && _appearances.Get((int)itemModifiedAppearanceId))
			return (true, false);

		if (_temporaryAppearances.ContainsKey(itemModifiedAppearanceId))
			return (true, true);

		return (false, false);
	}

	public List<ObjectGuid> GetItemsProvidingTemporaryAppearance(uint itemModifiedAppearanceId)
	{
		return _temporaryAppearances.LookupByKey(itemModifiedAppearanceId);
	}

	public List<uint> GetAppearanceIds()
	{
		List<uint> appearances = new();

		foreach (int id in _appearances)
			appearances.Add((uint)CliDB.ItemModifiedAppearanceStorage.LookupByKey(id).ItemAppearanceID);

		return appearances;
	}

	public void SetAppearanceIsFavorite(uint itemModifiedAppearanceId, bool apply)
	{
		var apperanceState = _favoriteAppearances.LookupByKey(itemModifiedAppearanceId);

		if (apply)
		{
			if (!_favoriteAppearances.ContainsKey(itemModifiedAppearanceId))
				_favoriteAppearances[itemModifiedAppearanceId] = FavoriteAppearanceState.New;
			else if (apperanceState == FavoriteAppearanceState.Removed)
				apperanceState = FavoriteAppearanceState.Unchanged;
			else
				return;
		}
		else if (_favoriteAppearances.ContainsKey(itemModifiedAppearanceId))
		{
			if (apperanceState == FavoriteAppearanceState.New)
				_favoriteAppearances.Remove(itemModifiedAppearanceId);
			else
				apperanceState = FavoriteAppearanceState.Removed;
		}
		else
		{
			return;
		}

		_favoriteAppearances[itemModifiedAppearanceId] = apperanceState;

		AccountTransmogUpdate accountTransmogUpdate = new()
		{
			IsFullUpdate = false,
			IsSetFavorite = apply
		};

		accountTransmogUpdate.FavoriteAppearances.Add(itemModifiedAppearanceId);

		_owner.SendPacket(accountTransmogUpdate);
	}

	public void SendFavoriteAppearances()
	{
		AccountTransmogUpdate accountTransmogUpdate = new()
		{
			IsFullUpdate = true
		};

		foreach (var pair in _favoriteAppearances)
			if (pair.Value != FavoriteAppearanceState.Removed)
				accountTransmogUpdate.FavoriteAppearances.Add(pair.Key);

		_owner.SendPacket(accountTransmogUpdate);
	}

	public void AddTransmogSet(uint transmogSetId)
	{
		var items = Global.DB2Mgr.GetTransmogSetItems(transmogSetId);

		if (items.Empty())
			return;

		foreach (var item in items)
		{
			var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(item.ItemModifiedAppearanceID);

			if (itemModifiedAppearance == null)
				continue;

			AddItemAppearance(itemModifiedAppearance);
		}
	}

	public void LoadTransmogIllusions()
	{
		var owner = _owner.Player;

		foreach (var blockValue in _transmogIllusions.ToBlockRange())
			owner.AddIllusionBlock(blockValue);
	}

	public void LoadAccountTransmogIllusions(SQLResult knownTransmogIllusions)
	{
		var blocks = new uint[7];

		if (!knownTransmogIllusions.IsEmpty())
			do
			{
				var blobIndex = knownTransmogIllusions.Read<ushort>(0);

				if (blobIndex >= blocks.Length)
					Array.Resize(ref blocks, blobIndex + 1);

				blocks[blobIndex] = knownTransmogIllusions.Read<uint>(1);
			} while (knownTransmogIllusions.NextRow());

		_transmogIllusions = new BitSet(blocks);

		// Static illusions known by every player
		ushort[] defaultIllusions =
		{
			3,  // Lifestealing
			13, // Crusader
			22, // Striking
			23, // Agility
			34, // Hide Weapon Enchant
			43, // Beastslayer
			44, // Titanguard
		};

		foreach (var illusionId in defaultIllusions)
			_transmogIllusions.Set(illusionId, true);
	}

	public void SaveAccountTransmogIllusions(SQLTransaction trans)
	{
		ushort blockIndex = 0;

		foreach (var blockValue in _transmogIllusions.ToBlockRange())
		{
			if (blockValue != 0) // this table is only appended/bits are set (never cleared) so don't save empty blocks
			{
				var stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_TRANSMOG_ILLUSIONS);
				stmt.AddValue(0, _owner.BattlenetAccountId);
				stmt.AddValue(1, blockIndex);
				stmt.AddValue(2, blockValue);
				trans.Append(stmt);
			}

			++blockIndex;
		}
	}

	public void AddTransmogIllusion(uint transmogIllusionId)
	{
		var owner = _owner.Player;

		if (_transmogIllusions.Count <= transmogIllusionId)
		{
			var numBlocks = (uint)(_transmogIllusions.Count << 2);
			_transmogIllusions.Length = (int)transmogIllusionId + 1;
			numBlocks = (uint)(_transmogIllusions.Count << 2) - numBlocks;

			while (numBlocks-- != 0)
				owner.AddIllusionBlock(0);
		}

		_transmogIllusions.Set((int)transmogIllusionId, true);
		var blockIndex = transmogIllusionId / 32;
		var bitIndex = transmogIllusionId % 32;

		owner.AddIllusionFlag((int)blockIndex, (uint)(1 << (int)bitIndex));
	}

	public bool HasTransmogIllusion(uint transmogIllusionId)
	{
		return transmogIllusionId < _transmogIllusions.Count && _transmogIllusions.Get((int)transmogIllusionId);
	}

	public bool HasToy(uint itemId)
	{
		return _toys.ContainsKey(itemId);
	}

	public Dictionary<uint, ToyFlags> GetAccountToys()
	{
		return _toys;
	}

	public Dictionary<uint, HeirloomData> GetAccountHeirlooms()
	{
		return _heirlooms;
	}

	public Dictionary<uint, MountStatusFlags> GetAccountMounts()
	{
		return _mounts;
	}

	bool UpdateAccountToys(uint itemId, bool isFavourite, bool hasFanfare)
	{
		if (_toys.ContainsKey(itemId))
			return false;

		_toys.Add(itemId, GetToyFlags(isFavourite, hasFanfare));

		return true;
	}

	ToyFlags GetToyFlags(bool isFavourite, bool hasFanfare)
	{
		var flags = ToyFlags.None;

		if (isFavourite)
			flags |= ToyFlags.Favorite;

		if (hasFanfare)
			flags |= ToyFlags.HasFanfare;

		return flags;
	}

	bool UpdateAccountHeirlooms(uint itemId, HeirloomPlayerFlags flags)
	{
		if (_heirlooms.ContainsKey(itemId))
			return false;

		_heirlooms.Add(itemId, new HeirloomData(flags));

		return true;
	}

	void SendSingleMountUpdate(uint spellId, MountStatusFlags mountStatusFlags)
	{
		var player = _owner.Player;

		if (!player)
			return;

		AccountMountUpdate mountUpdate = new()
		{
			IsFullUpdate = false
		};

		mountUpdate.Mounts.Add(spellId, mountStatusFlags);
		player.SendPacket(mountUpdate);
	}

	bool CanAddAppearance(ItemModifiedAppearanceRecord itemModifiedAppearance)
	{
		if (itemModifiedAppearance == null)
			return false;

		if (itemModifiedAppearance.TransmogSourceTypeEnum == 6 || itemModifiedAppearance.TransmogSourceTypeEnum == 9)
			return false;

		if (!CliDB.ItemSearchNameStorage.ContainsKey(itemModifiedAppearance.ItemID))
			return false;

		var itemTemplate = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID);

		if (itemTemplate == null)
			return false;

		if (!_owner.Player)
			return false;

		if (_owner.Player.CanUseItem(itemTemplate) != InventoryResult.Ok)
			return false;

		if (itemTemplate.HasFlag(ItemFlags2.NoSourceForItemVisual) || itemTemplate.Quality == ItemQuality.Artifact)
			return false;

		switch (itemTemplate.Class)
		{
			case ItemClass.Weapon:
			{
				if (!Convert.ToBoolean((long)(_owner.Player.GetWeaponProficiency() & (1 << (int)itemTemplate.SubClass))))
					return false;

				if (itemTemplate.SubClass == (int)ItemSubClassWeapon.Exotic ||
					itemTemplate.SubClass == (int)ItemSubClassWeapon.Exotic2 ||
					itemTemplate.SubClass == (int)ItemSubClassWeapon.Miscellaneous ||
					itemTemplate.SubClass == (int)ItemSubClassWeapon.Thrown ||
					itemTemplate.SubClass == (int)ItemSubClassWeapon.Spear ||
					itemTemplate.SubClass == (int)ItemSubClassWeapon.FishingPole)
					return false;

				break;
			}
			case ItemClass.Armor:
			{
				switch (itemTemplate.InventoryType)
				{
					case InventoryType.Body:
					case InventoryType.Shield:
					case InventoryType.Cloak:
					case InventoryType.Tabard:
					case InventoryType.Holdable:
						break;
					case InventoryType.Head:
					case InventoryType.Shoulders:
					case InventoryType.Chest:
					case InventoryType.Waist:
					case InventoryType.Legs:
					case InventoryType.Feet:
					case InventoryType.Wrists:
					case InventoryType.Hands:
					case InventoryType.Robe:
						if ((ItemSubClassArmor)itemTemplate.SubClass == ItemSubClassArmor.Miscellaneous)
							return false;

						break;
					default:
						return false;
				}

				if (itemTemplate.InventoryType != InventoryType.Cloak)
					if (!Convert.ToBoolean(_playerClassByArmorSubclass[itemTemplate.SubClass] & _owner.Player.ClassMask))
						return false;

				break;
			}
			default:
				return false;
		}

		if (itemTemplate.Quality < ItemQuality.Uncommon)
			if (!itemTemplate.HasFlag(ItemFlags2.IgnoreQualityForItemVisualSource) || !itemTemplate.HasFlag(ItemFlags3.ActsAsTransmogHiddenVisualOption))
				return false;

		if (itemModifiedAppearance.Id < _appearances.Count && _appearances.Get((int)itemModifiedAppearance.Id))
			return false;

		return true;
	}

	//todo  check this
	void AddItemAppearance(ItemModifiedAppearanceRecord itemModifiedAppearance)
	{
		var owner = _owner.Player;

		if (_appearances.Count <= itemModifiedAppearance.Id)
		{
			var numBlocks = (uint)(_appearances.Count << 2);
			_appearances.Length = (int)itemModifiedAppearance.Id + 1;
			numBlocks = (uint)(_appearances.Count << 2) - numBlocks;

			while (numBlocks-- != 0)
				owner.AddTransmogBlock(0);
		}

		_appearances.Set((int)itemModifiedAppearance.Id, true);
		var blockIndex = itemModifiedAppearance.Id / 32;
		var bitIndex = itemModifiedAppearance.Id % 32;
		owner.AddTransmogFlag((int)blockIndex, 1u << (int)bitIndex);
		var temporaryAppearance = _temporaryAppearances.LookupByKey(itemModifiedAppearance.Id).ToList(); // make a copy

		if (!temporaryAppearance.Empty())
		{
			owner.RemoveConditionalTransmog(itemModifiedAppearance.Id);
			_temporaryAppearances.Remove(itemModifiedAppearance.Id);
		}

		var item = CliDB.ItemStorage.LookupByKey(itemModifiedAppearance.ItemID);

		if (item != null)
		{
			var transmogSlot = Item.ItemTransmogrificationSlots[(int)item.inventoryType];

			if (transmogSlot >= 0)
				_owner.Player.UpdateCriteria(CriteriaType.LearnAnyTransmogInSlot, (ulong)transmogSlot, itemModifiedAppearance.Id);
		}

		var sets = Global.DB2Mgr.GetTransmogSetsForItemModifiedAppearance(itemModifiedAppearance.Id);

		foreach (var set in sets)
			if (IsSetCompleted(set.Id))
				_owner.Player.UpdateCriteria(CriteriaType.CollectTransmogSetFromGroup, set.TransmogSetGroupID);
	}

	void AddTemporaryAppearance(ObjectGuid itemGuid, ItemModifiedAppearanceRecord itemModifiedAppearance)
	{
		var itemsWithAppearance = _temporaryAppearances[itemModifiedAppearance.Id];

		if (itemsWithAppearance.Empty())
			_owner.Player.AddConditionalTransmog(itemModifiedAppearance.Id);

		itemsWithAppearance.Add(itemGuid);
	}

	bool IsSetCompleted(uint transmogSetId)
	{
		var transmogSetItems = Global.DB2Mgr.GetTransmogSetItems(transmogSetId);

		if (transmogSetItems.Empty())
			return false;

		var knownPieces = new int[EquipmentSlot.End];

		for (var i = 0; i < EquipmentSlot.End; ++i)
			knownPieces[i] = -1;

		foreach (var transmogSetItem in transmogSetItems)
		{
			var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(transmogSetItem.ItemModifiedAppearanceID);

			if (itemModifiedAppearance == null)
				continue;

			var item = CliDB.ItemStorage.LookupByKey(itemModifiedAppearance.ItemID);

			if (item == null)
				continue;

			var transmogSlot = Item.ItemTransmogrificationSlots[(int)item.inventoryType];

			if (transmogSlot < 0 || knownPieces[transmogSlot] == 1)
				continue;

			var (hasAppearance, isTemporary) = HasItemAppearance(transmogSetItem.ItemModifiedAppearanceID);

			knownPieces[transmogSlot] = (hasAppearance && !isTemporary) ? 1 : 0;
		}

		return !knownPieces.Contains(0);
	}
}