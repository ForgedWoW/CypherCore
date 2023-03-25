// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Loot;
using Forged.MapServer.Server;
using Framework.Constants;

namespace Forged.MapServer.Loot;

public class LootItem
{
	public uint itemid;
	public uint LootListId;
	public uint randomBonusListId;
	public List<uint> BonusListIDs = new();
	public ItemContext context;
	public List<Condition> conditions = new(); // additional loot condition
	public List<ObjectGuid> allowedGUIDs = new();
	public ObjectGuid rollWinnerGUID; // Stores the guid of person who won loot, if his bags are full only he can see the item in loot list!
	public byte count;
	public bool is_looted;
	public bool is_blocked;
	public bool freeforall; // free for all
	public bool is_underthreshold;
	public bool is_counted;
	public bool needs_quest; // quest drop
	public bool follow_loot_rules;
	public LootItem() { }

	public LootItem(LootStoreItem li)
	{
		itemid = li.itemid;
		conditions = li.conditions;

		var proto = Global.ObjectMgr.GetItemTemplate(itemid);
		freeforall = proto != null && proto.HasFlag(ItemFlags.MultiDrop);
		follow_loot_rules = !li.needs_quest || (proto != null && proto.FlagsCu.HasAnyFlag(ItemFlagsCustom.FollowLootRules));

		needs_quest = li.needs_quest;

		randomBonusListId = ItemEnchantmentManager.GenerateItemRandomBonusListId(itemid);
	}

	/// <summary>
	///  Basic checks for player/item compatibility - if false no chance to see the item in the loot - used only for loot generation
	/// </summary>
	/// <param name="player"> </param>
	/// <param name="loot"> </param>
	/// <returns> </returns>
	public bool AllowedForPlayer(Player player, Loot loot)
	{
		return AllowedForPlayer(player, loot, itemid, needs_quest, follow_loot_rules, false, conditions);
	}

	public static bool AllowedForPlayer(Player player, Loot loot, uint itemid, bool needs_quest, bool follow_loot_rules, bool strictUsabilityCheck, List<Condition> conditions)
	{
		// DB conditions check
		if (!Global.ConditionMgr.IsObjectMeetToConditions(player, conditions))
			return false;

		var pProto = Global.ObjectMgr.GetItemTemplate(itemid);

		if (pProto == null)
			return false;

		// not show loot for not own team
		if (pProto.HasFlag(ItemFlags2.FactionHorde) && player.Team != TeamFaction.Horde)
			return false;

		if (pProto.HasFlag(ItemFlags2.FactionAlliance) && player.Team != TeamFaction.Alliance)
			return false;

		// Master looter can see all items even if the character can't loot them
		if (loot != null && loot.GetLootMethod() == LootMethod.MasterLoot && follow_loot_rules && loot.GetLootMasterGUID() == player.GUID)
			return true;

		// Don't allow loot for players without profession or those who already know the recipe
		if (pProto.HasFlag(ItemFlags.HideUnusableRecipe))
		{
			if (!player.HasSkill((SkillType)pProto.RequiredSkill))
				return false;

			foreach (var itemEffect in pProto.Effects)
			{
				if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
					continue;

				if (player.HasSpell((uint)itemEffect.SpellID))
					return false;
			}
		}

		// check quest requirements
		if (!pProto.FlagsCu.HasAnyFlag(ItemFlagsCustom.IgnoreQuestStatus) && ((needs_quest || (pProto.StartQuest != 0 && player.GetQuestStatus(pProto.StartQuest) != QuestStatus.None)) && !player.HasQuestForItem(itemid)))
			return false;

		if (strictUsabilityCheck)
		{
			if ((pProto.IsWeapon || pProto.IsArmor) && !pProto.IsUsableByLootSpecialization(player, true))
				return false;

			if (player.CanRollNeedForItem(pProto, null, false) != InventoryResult.Ok)
				return false;
		}

		return true;
	}

	public void AddAllowedLooter(Player player)
	{
		allowedGUIDs.Add(player.GUID);
	}

	public bool HasAllowedLooter(ObjectGuid looter)
	{
		return allowedGUIDs.Contains(looter);
	}

	public LootSlotType? GetUiTypeForPlayer(Player player, Loot loot)
	{
		if (is_looted)
			return null;

		if (!allowedGUIDs.Contains(player.GUID))
			return null;

		if (freeforall)
		{
			var ffaItems = loot.GetPlayerFFAItems().LookupByKey(player.GUID);

			if (ffaItems != null)
			{
				var ffaItemItr = ffaItems.Find(ffaItem => ffaItem.LootListId == LootListId);

				if (ffaItemItr is { is_looted: false })
					return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;
			}

			return null;
		}

		if (needs_quest && !follow_loot_rules)
			return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;

		switch (loot.GetLootMethod())
		{
			case LootMethod.FreeForAll:
				return LootSlotType.Owner;
			case LootMethod.RoundRobin:
				if (!loot.roundRobinPlayer.IsEmpty && loot.roundRobinPlayer != player.GUID)
					return null;

				return LootSlotType.AllowLoot;
			case LootMethod.MasterLoot:
				if (is_underthreshold)
				{
					if (!loot.roundRobinPlayer.IsEmpty && loot.roundRobinPlayer != player.GUID)
						return null;

					return LootSlotType.AllowLoot;
				}

				return loot.GetLootMasterGUID() == player.GUID ? LootSlotType.Master : LootSlotType.Locked;
			case LootMethod.GroupLoot:
			case LootMethod.NeedBeforeGreed:
				if (is_underthreshold)
					if (!loot.roundRobinPlayer.IsEmpty && loot.roundRobinPlayer != player.GUID)
						return null;

				if (is_blocked)
					return LootSlotType.RollOngoing;

				if (rollWinnerGUID.IsEmpty) // all passed
					return LootSlotType.AllowLoot;

				if (rollWinnerGUID == player.GUID)
					return LootSlotType.Owner;

				return null;
			case LootMethod.PersonalLoot:
				return LootSlotType.Owner;
			default:
				break;
		}

		return null;
	}

	public List<ObjectGuid> GetAllowedLooters()
	{
		return allowedGUIDs;
	}
}

public class NotNormalLootItem
{
	public byte LootListId; // position in quest_items or items;
	public bool is_looted;

	public NotNormalLootItem()
	{
		LootListId = 0;
		is_looted = false;
	}

	public NotNormalLootItem(byte _index, bool _islooted = false)
	{
		LootListId = _index;
		is_looted = _islooted;
	}
}

public class PlayerRollVote
{
	public RollVote Vote;
	public byte RollNumber;

	public PlayerRollVote()
	{
		Vote = RollVote.NotValid;
		RollNumber = 0;
	}
}

public class LootRoll
{
	static readonly TimeSpan LOOT_ROLL_TIMEOUT = TimeSpan.FromMinutes(1);
	readonly Dictionary<ObjectGuid, PlayerRollVote> m_rollVoteMap = new();

	Map m_map;
	bool m_isStarted;
	LootItem m_lootItem;
	Loot m_loot;
	RollMask m_voteMask;
	DateTime m_endTime = DateTime.MinValue;

	// Try to start the group roll for the specified item (it may fail for quest item or any condition
	// If this method return false the roll have to be removed from the container to avoid any problem
	public bool TryToStart(Map map, Loot loot, uint lootListId, ushort enchantingSkill)
	{
		if (!m_isStarted)
		{
			if (lootListId >= loot.items.Count)
				return false;

			m_map = map;

			// initialize the data needed for the roll
			m_lootItem = loot.items[(int)lootListId];

			m_loot = loot;
			m_lootItem.is_blocked = true; // block the item while rolling

			uint playerCount = 0;

			foreach (var allowedLooter in m_lootItem.GetAllowedLooters())
			{
				var plr = Global.ObjAccessor.GetPlayer(m_map, allowedLooter);

				if (!plr || !m_lootItem.HasAllowedLooter(plr.GUID)) // check if player meet the condition to be able to roll this item
				{
					m_rollVoteMap[allowedLooter].Vote = RollVote.NotValid;

					continue;
				}

				// initialize player vote map
				m_rollVoteMap[allowedLooter].Vote = plr.PassOnGroupLoot ? RollVote.Pass : RollVote.NotEmitedYet;

				if (!plr.PassOnGroupLoot)
					plr.AddLootRoll(this);

				++playerCount;
			}

			// initialize item prototype and check enchant possibilities for this group
			var itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);
			m_voteMask = RollMask.AllMask;

			if (itemTemplate.HasFlag(ItemFlags2.CanOnlyRollGreed))
				m_voteMask = m_voteMask & ~RollMask.Need;

			var disenchant = GetItemDisenchantLoot();

			if (disenchant == null || disenchant.SkillRequired > enchantingSkill)
				m_voteMask = m_voteMask & ~RollMask.Disenchant;

			if (playerCount > 1) // check if more than one player can loot this item
			{
				// start the roll
				SendStartRoll();
				m_endTime = GameTime.Now() + LOOT_ROLL_TIMEOUT;
				m_isStarted = true;

				return true;
			}

			// no need to start roll if one or less player can loot this item so place it under threshold
			m_lootItem.is_underthreshold = true;
			m_lootItem.is_blocked = false;
		}

		return false;
	}

	// Add vote from playerGuid
	public bool PlayerVote(Player player, RollVote vote)
	{
		var playerGuid = player.GUID;

		if (!m_rollVoteMap.TryGetValue(playerGuid, out var voter))
			return false;

		voter.Vote = vote;

		if (vote != RollVote.Pass && vote != RollVote.NotValid)
			voter.RollNumber = (byte)RandomHelper.URand(1, 100);

		switch (vote)
		{
			case RollVote.Pass: // Player choose pass
			{
				SendRoll(playerGuid, -1, RollVote.Pass, null);

				break;
			}
			case RollVote.Need: // player choose Need
			{
				SendRoll(playerGuid, 0, RollVote.Need, null);
				player.UpdateCriteria(CriteriaType.RollAnyNeed, 1);

				break;
			}
			case RollVote.Greed: // player choose Greed
			{
				SendRoll(playerGuid, -1, RollVote.Greed, null);
				player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);

				break;
			}
			case RollVote.Disenchant: // player choose Disenchant
			{
				SendRoll(playerGuid, -1, RollVote.Disenchant, null);
				player.UpdateCriteria(CriteriaType.RollAnyGreed, 1);

				break;
			}
			default: // Roll removed case
				return false;
		}

		return true;
	}

	// check if we can found a winner for this roll or if timer is expired
	public bool UpdateRoll()
	{
		KeyValuePair<ObjectGuid, PlayerRollVote> winner = default;

		if (AllPlayerVoted(ref winner) || m_endTime <= GameTime.Now())
		{
			Finish(winner);

			return true;
		}

		return false;
	}

	public bool IsLootItem(ObjectGuid lootObject, uint lootListId)
	{
		return m_loot.GetGUID() == lootObject && m_lootItem.LootListId == lootListId;
	}

	// Send the roll for the whole group
	void SendStartRoll()
	{
		var itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);

		foreach (var (playerGuid, roll) in m_rollVoteMap)
		{
			if (roll.Vote != RollVote.NotEmitedYet)
				continue;

			var player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);

			if (player == null)
				continue;

			StartLootRoll startLootRoll = new()
			{
				LootObj = m_loot.GetGUID(),
				MapID = (int)m_map.Id,
				RollTime = (uint)LOOT_ROLL_TIMEOUT.TotalMilliseconds,
				Method = m_loot.GetLootMethod(),
				ValidRolls = m_voteMask
			};

			// In NEED_BEFORE_GREED need disabled for non-usable item for player
			if (m_loot.GetLootMethod() == LootMethod.NeedBeforeGreed && player.CanRollNeedForItem(itemTemplate, m_map, true) != InventoryResult.Ok)
				startLootRoll.ValidRolls &= ~RollMask.Need;

			FillPacket(startLootRoll.Item);
			startLootRoll.Item.UIType = LootSlotType.RollOngoing;

			player.SendPacket(startLootRoll);
		}

		// Handle auto pass option
		foreach (var (playerGuid, roll) in m_rollVoteMap)
		{
			if (roll.Vote != RollVote.Pass)
				continue;

			SendRoll(playerGuid, -1, RollVote.Pass, null);
		}
	}

	// Send all passed message
	void SendAllPassed()
	{
		LootAllPassed lootAllPassed = new()
		{
			LootObj = m_loot.GetGUID()
		};

		FillPacket(lootAllPassed.Item);
		lootAllPassed.Item.UIType = LootSlotType.AllowLoot;
		lootAllPassed.Write();

		foreach (var (playerGuid, roll) in m_rollVoteMap)
		{
			if (roll.Vote != RollVote.NotValid)
				continue;

			var player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);

			if (player == null)
				continue;

			player.SendPacket(lootAllPassed);
		}
	}

	// Send roll of targetGuid to the whole group (included targuetGuid)
	void SendRoll(ObjectGuid targetGuid, int rollNumber, RollVote rollType, ObjectGuid? rollWinner)
	{
		LootRollBroadcast lootRoll = new()
		{
			LootObj = m_loot.GetGUID(),
			Player = targetGuid,
			Roll = rollNumber,
			RollType = rollType,
			Autopassed = false
		};

		FillPacket(lootRoll.Item);
		lootRoll.Item.UIType = LootSlotType.RollOngoing;
		lootRoll.Write();

		foreach (var (playerGuid, roll) in m_rollVoteMap)
		{
			if (roll.Vote == RollVote.NotValid)
				continue;

			if (playerGuid == rollWinner)
				continue;

			var player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);

			if (player == null)
				continue;

			player.SendPacket(lootRoll);
		}

		if (rollWinner.HasValue)
		{
			var player = Global.ObjAccessor.GetPlayer(m_map, rollWinner.Value);

			if (player != null)
			{
				lootRoll.Item.UIType = LootSlotType.AllowLoot;
				lootRoll.Clear();
				player.SendPacket(lootRoll);
			}
		}
	}

	// Send roll 'value' of the whole group and the winner to the whole group
	void SendLootRollWon(ObjectGuid targetGuid, int rollNumber, RollVote rollType)
	{
		// Send roll values
		foreach (var (playerGuid, roll) in m_rollVoteMap)
			switch (roll.Vote)
			{
				case RollVote.Pass:
					break;
				case RollVote.NotEmitedYet:
				case RollVote.NotValid:
					SendRoll(playerGuid, 0, RollVote.Pass, targetGuid);

					break;
				default:
					SendRoll(playerGuid, roll.RollNumber, roll.Vote, targetGuid);

					break;
			}

		LootRollWon lootRollWon = new()
		{
			LootObj = m_loot.GetGUID(),
			Winner = targetGuid,
			Roll = rollNumber,
			RollType = rollType
		};

		FillPacket(lootRollWon.Item);
		lootRollWon.Item.UIType = LootSlotType.Locked;
		lootRollWon.MainSpec = true; // offspec rolls not implemented
		lootRollWon.Write();

		foreach (var (playerGuid, roll) in m_rollVoteMap)
		{
			if (roll.Vote == RollVote.NotValid)
				continue;

			if (playerGuid == targetGuid)
				continue;

			var player1 = Global.ObjAccessor.GetPlayer(m_map, playerGuid);

			if (player1 == null)
				continue;

			player1.SendPacket(lootRollWon);
		}

		var player = Global.ObjAccessor.GetPlayer(m_map, targetGuid);

		if (player != null)
		{
			lootRollWon.Item.UIType = LootSlotType.AllowLoot;
			lootRollWon.Clear();
			player.SendPacket(lootRollWon);
		}
	}

	void FillPacket(LootItemData lootItem)
	{
		lootItem.Quantity = m_lootItem.count;
		lootItem.LootListID = (byte)m_lootItem.LootListId;
		lootItem.CanTradeToTapList = m_lootItem.allowedGUIDs.Count > 1;
		lootItem.Loot = new ItemInstance(m_lootItem);
	}

	/**
	 * \brief Check if all player have voted and return true in that case. Also return current winner.
	 * \param winnerItr > will be different than m_rollCoteMap.end() if winner exist. (Someone voted greed or need)
	 * \returns true if all players voted
	 */
	bool AllPlayerVoted(ref KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
	{
		uint notVoted = 0;
		var isSomeoneNeed = false;

		winnerPair = default;

		foreach (var pair in m_rollVoteMap)
			switch (pair.Value.Vote)
			{
				case RollVote.Need:
					if (!isSomeoneNeed || winnerPair.Value == null || pair.Value.RollNumber > winnerPair.Value.RollNumber)
					{
						isSomeoneNeed = true; // first passage will force to set winner because need is prioritized
						winnerPair = pair;
					}

					break;
				case RollVote.Greed:
				case RollVote.Disenchant:
					if (!isSomeoneNeed) // if at least one need is detected then winner can't be a greed
						if (winnerPair.Value == null || pair.Value.RollNumber > winnerPair.Value.RollNumber)
							winnerPair = pair;

					break;
				// Explicitly passing excludes a player from winning loot, so no action required.
				case RollVote.Pass:
					break;
				case RollVote.NotEmitedYet:
					++notVoted;

					break;
				default:
					break;
			}

		return notVoted == 0;
	}

	ItemDisenchantLootRecord GetItemDisenchantLoot()
	{
		ItemInstance itemInstance = new(m_lootItem);

		BonusData bonusData = new(itemInstance);

		if (!bonusData.CanDisenchant)
			return null;

		var itemTemplate = Global.ObjectMgr.GetItemTemplate(m_lootItem.itemid);
		var itemLevel = Item.GetItemLevel(itemTemplate, bonusData, 1, 0, 0, 0, 0, false, 0);

		return Item.GetDisenchantLoot(itemTemplate, (uint)bonusData.Quality, itemLevel);
	}

	// terminate the roll
	void Finish(KeyValuePair<ObjectGuid, PlayerRollVote> winnerPair)
	{
		m_lootItem.is_blocked = false;

		if (winnerPair.Value == null)
		{
			SendAllPassed();
		}
		else
		{
			m_lootItem.rollWinnerGUID = winnerPair.Key;

			SendLootRollWon(winnerPair.Key, winnerPair.Value.RollNumber, winnerPair.Value.Vote);

			var player = Global.ObjAccessor.FindConnectedPlayer(winnerPair.Key);

			if (player != null)
			{
				if (winnerPair.Value.Vote == RollVote.Need)
					player.UpdateCriteria(CriteriaType.RollNeed, m_lootItem.itemid, winnerPair.Value.RollNumber);
				else if (winnerPair.Value.Vote == RollVote.Disenchant)
					player.UpdateCriteria(CriteriaType.CastSpell, 13262);
				else
					player.UpdateCriteria(CriteriaType.RollGreed, m_lootItem.itemid, winnerPair.Value.RollNumber);

				if (winnerPair.Value.Vote == RollVote.Disenchant)
				{
					var disenchant = GetItemDisenchantLoot();
					Loot loot = new(m_map, m_loot.GetOwnerGUID(), LootType.Disenchanting, null);
					loot.FillLoot(disenchant.Id, LootStorage.Disenchant, player, true, false, LootModes.Default, ItemContext.None);

					if (!loot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot, true))
						for (uint i = 0; i < loot.items.Count; ++i)
						{
							var disenchantLoot = loot.LootItemInSlot(i, player);

							if (disenchantLoot != null)
								player.SendItemRetrievalMail(disenchantLoot.itemid, disenchantLoot.count, disenchantLoot.context);
						}
					else
						m_loot.NotifyItemRemoved((byte)m_lootItem.LootListId, m_map);
				}
				else
				{
					player.StoreLootItem(m_loot.GetOwnerGUID(), (byte)m_lootItem.LootListId, m_loot);
				}
			}
		}

		m_isStarted = false;
	}

	~LootRoll()
	{
		if (m_isStarted)
			SendAllPassed();

		foreach (var (playerGuid, roll) in m_rollVoteMap)
		{
			if (roll.Vote != RollVote.NotEmitedYet)
				continue;

			var player = Global.ObjAccessor.GetPlayer(m_map, playerGuid);

			if (!player)
				continue;

			player.RemoveLootRoll(this);
		}
	}
}

public class Loot
{
	public List<LootItem> items = new();
	public uint gold;
	public byte unlootedCount;
	public ObjectGuid roundRobinPlayer; // GUID of the player having the Round-Robin ownership for the loot. If 0, round robin owner has released.
	public LootType loot_type;          // required for achievement system

	readonly List<ObjectGuid> PlayersLooting = new();
	readonly MultiMap<ObjectGuid, NotNormalLootItem> PlayerFFAItems = new();
	readonly LootMethod _lootMethod;
	readonly Dictionary<uint, LootRoll> _rolls = new(); // used if an item is under rolling
	readonly List<ObjectGuid> _allowedLooters = new();

	// Loot GUID
	readonly ObjectGuid _guid;
	readonly ObjectGuid _owner; // The WorldObject that holds this loot
	readonly ObjectGuid _lootMaster;
	ItemContext _itemContext;
	bool _wasOpened; // true if at least one player received the loot content
	uint _dungeonEncounterId;

	public Loot(Map map, ObjectGuid owner, LootType type, PlayerGroup group)
	{
		loot_type = type;
		_guid = map ? ObjectGuid.Create(HighGuid.LootObject, map.Id, 0, map.GenerateLowGuid(HighGuid.LootObject)) : ObjectGuid.Empty;
		_owner = owner;
		_itemContext = ItemContext.None;
		_lootMethod = group != null ? group.LootMethod : LootMethod.FreeForAll;
		_lootMaster = group != null ? group.MasterLooterGuid : ObjectGuid.Empty;
	}

	// Inserts the item into the loot (called by LootTemplate processors)
	public void AddItem(LootStoreItem item)
	{
		var proto = Global.ObjectMgr.GetItemTemplate(item.itemid);

		if (proto == null)
			return;

		var count = RandomHelper.URand(item.mincount, item.maxcount);
		var stacks = (uint)(count / proto.MaxStackSize + (Convert.ToBoolean(count % proto.MaxStackSize) ? 1 : 0));

		for (uint i = 0; i < stacks && items.Count < SharedConst.MaxNRLootItems; ++i)
		{
			LootItem generatedLoot = new(item)
			{
				context = _itemContext,
				count = (byte)Math.Min(count, proto.MaxStackSize),
				LootListId = (uint)items.Count
			};

			if (_itemContext != 0)
			{
				var bonusListIDs = Global.DB2Mgr.GetDefaultItemBonusTree(generatedLoot.itemid, _itemContext);
				generatedLoot.BonusListIDs.AddRange(bonusListIDs);
			}

			items.Add(generatedLoot);
			count -= proto.MaxStackSize;
		}
	}

	public bool AutoStore(Player player, byte bag, byte slot, bool broadcast = false, bool createdByPlayer = false)
	{
		var allLooted = true;

		for (uint i = 0; i < items.Count; ++i)
		{
			var lootItem = LootItemInSlot(i, player, out var ffaitem);

			if (lootItem == null || lootItem.is_looted)
				continue;

			if (!lootItem.HasAllowedLooter(GetGUID()))
				continue;

			if (lootItem.is_blocked)
				continue;

			// dont allow protected item to be looted by someone else
			if (!lootItem.rollWinnerGUID.IsEmpty && lootItem.rollWinnerGUID != GetGUID())
				continue;

			List<ItemPosCount> dest = new();
			var msg = player.CanStoreNewItem(bag, slot, dest, lootItem.itemid, lootItem.count);

			if (msg != InventoryResult.Ok && slot != ItemConst.NullSlot)
				msg = player.CanStoreNewItem(bag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);

			if (msg != InventoryResult.Ok && bag != ItemConst.NullBag)
				msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, lootItem.itemid, lootItem.count);

			if (msg != InventoryResult.Ok)
			{
				player.SendEquipError(msg, null, null, lootItem.itemid);
				allLooted = false;

				continue;
			}

			if (ffaitem != null)
				ffaitem.is_looted = true;

			if (!lootItem.freeforall)
				lootItem.is_looted = true;

			--unlootedCount;

			var pItem = player.StoreNewItem(dest, lootItem.itemid, true, lootItem.randomBonusListId, null, lootItem.context, lootItem.BonusListIDs);
			player.SendNewItem(pItem, lootItem.count, false, createdByPlayer, broadcast);
			player.ApplyItemLootedSpell(pItem, true);
		}

		return allLooted;
	}

	public LootItem GetItemInSlot(uint lootListId)
	{
		if (lootListId < items.Count)
			return items[(int)lootListId];

		return null;
	}

	// Calls processor of corresponding LootTemplate (which handles everything including references)
	public bool FillLoot(uint lootId, LootStore store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
	{
		// Must be provided
		if (lootOwner == null)
			return false;

		var tab = store.GetLootFor(lootId);

		if (tab == null)
		{
			if (!noEmptyError)
				Log.Logger.Error("Table '{0}' loot id #{1} used but it doesn't have records.", store.GetName(), lootId);

			return false;
		}

		_itemContext = context;

		tab.Process(this, store.IsRatesAllowed(), (byte)lootMode, 0); // Processing is done there, callback via Loot.AddItem()

		// Setting access rights for group loot case
		var group = lootOwner.Group;

		if (!personal && group != null)
		{
			if (loot_type == LootType.Corpse)
				roundRobinPlayer = lootOwner.GUID;

			for (var refe = group.FirstMember; refe != null; refe = refe.Next())
			{
				var player = refe.Source;

				if (player) // should actually be looted object instead of lootOwner but looter has to be really close so doesnt really matter
					if (player.IsAtGroupRewardDistance(lootOwner))
						FillNotNormalLootFor(player);
			}

			foreach (var item in items)
			{
				if (!item.follow_loot_rules || item.freeforall)
					continue;

				var proto = Global.ObjectMgr.GetItemTemplate(item.itemid);

				if (proto != null)
				{
					if (proto.Quality < group.LootThreshold)
						item.is_underthreshold = true;
					else
						switch (_lootMethod)
						{
							case LootMethod.MasterLoot:
							case LootMethod.GroupLoot:
							case LootMethod.NeedBeforeGreed:
							{
								item.is_blocked = true;

								break;
							}
							default:
								break;
						}
				}
			}
		}
		// ... for personal loot
		else
		{
			FillNotNormalLootFor(lootOwner);
		}

		return true;
	}

	public void Update()
	{
		foreach (var pair in _rolls.ToList())
			if (pair.Value.UpdateRoll())
				_rolls.Remove(pair.Key);
	}

	public void FillNotNormalLootFor(Player player)
	{
		var plguid = player.GUID;
		_allowedLooters.Add(plguid);

		List<NotNormalLootItem> ffaItems = new();

		foreach (var item in items)
		{
			if (!item.AllowedForPlayer(player, this))
				continue;

			item.AddAllowedLooter(player);

			if (item.freeforall)
			{
				ffaItems.Add(new NotNormalLootItem((byte)item.LootListId));
				++unlootedCount;
			}

			else if (!item.is_counted)
			{
				item.is_counted = true;
				++unlootedCount;
			}
		}

		if (!ffaItems.Empty())
			PlayerFFAItems[player.GUID] = ffaItems;
	}

	public void NotifyItemRemoved(byte lootListId, Map map)
	{
		// notify all players that are looting this that the item was removed
		// convert the index to the slot the player sees
		for (var i = 0; i < PlayersLooting.Count; ++i)
		{
			var item = items[lootListId];

			if (!item.GetAllowedLooters().Contains(PlayersLooting[i]))
				continue;

			var player = Global.ObjAccessor.GetPlayer(map, PlayersLooting[i]);

			if (player)
				player.SendNotifyLootItemRemoved(GetGUID(), GetOwnerGUID(), lootListId);
			else
				PlayersLooting.RemoveAt(i);
		}
	}

	public void NotifyMoneyRemoved(Map map)
	{
		// notify all players that are looting this that the money was removed
		for (var i = 0; i < PlayersLooting.Count; ++i)
		{
			var player = Global.ObjAccessor.GetPlayer(map, PlayersLooting[i]);

			if (player != null)
				player.SendNotifyLootMoneyRemoved(GetGUID());
			else
				PlayersLooting.RemoveAt(i);
		}
	}

	public void OnLootOpened(Map map, ObjectGuid looter)
	{
		AddLooter(looter);

		if (!_wasOpened)
		{
			_wasOpened = true;

			if (_lootMethod == LootMethod.GroupLoot || _lootMethod == LootMethod.NeedBeforeGreed)
			{
				ushort maxEnchantingSkill = 0;

				foreach (var allowedLooterGuid in _allowedLooters)
				{
					var allowedLooter = Global.ObjAccessor.GetPlayer(map, allowedLooterGuid);

					if (allowedLooter != null)
						maxEnchantingSkill = Math.Max(maxEnchantingSkill, allowedLooter.GetSkillValue(SkillType.Enchanting));
				}

				for (uint lootListId = 0; lootListId < items.Count; ++lootListId)
				{
					var item = items[(int)lootListId];

					if (!item.is_blocked)
						continue;

					LootRoll lootRoll = new();
					var inserted = _rolls.TryAdd(lootListId, lootRoll);

					if (!lootRoll.TryToStart(map, this, lootListId, maxEnchantingSkill))
						_rolls.Remove(lootListId);
				}
			}
			else if (_lootMethod == LootMethod.MasterLoot)
			{
				if (looter == _lootMaster)
				{
					var lootMaster = Global.ObjAccessor.GetPlayer(map, looter);

					if (lootMaster != null)
					{
						MasterLootCandidateList masterLootCandidateList = new()
						{
							LootObj = GetGUID(),
							Players = _allowedLooters
						};

						lootMaster.SendPacket(masterLootCandidateList);
					}
				}
			}
		}
	}

	public bool HasAllowedLooter(ObjectGuid looter)
	{
		return _allowedLooters.Contains(looter);
	}

	public void GenerateMoneyLoot(uint minAmount, uint maxAmount)
	{
		if (maxAmount > 0)
		{
			if (maxAmount <= minAmount)
				gold = (uint)(maxAmount * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
			else if ((maxAmount - minAmount) < 32700)
				gold = (uint)(RandomHelper.URand(minAmount, maxAmount) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney));
			else
				gold = (uint)(RandomHelper.URand(minAmount >> 8, maxAmount >> 8) * WorldConfig.GetFloatValue(WorldCfg.RateDropMoney)) << 8;
		}
	}

	public LootItem LootItemInSlot(uint lootSlot, Player player)
	{
		return LootItemInSlot(lootSlot, player, out _);
	}

	public LootItem LootItemInSlot(uint lootListId, Player player, out NotNormalLootItem ffaItem)
	{
		ffaItem = null;

		if (lootListId >= items.Count)
			return null;

		var item = items[(int)lootListId];
		var is_looted = item.is_looted;

		if (item.freeforall)
		{
			var itemList = PlayerFFAItems.LookupByKey(player.GUID);

			if (itemList != null)
				foreach (var notNormalLootItem in itemList)
					if (notNormalLootItem.LootListId == lootListId)
					{
						is_looted = notNormalLootItem.is_looted;
						ffaItem = notNormalLootItem;

						break;
					}
		}

		if (is_looted)
			return null;

		return item;
	}

	// return true if there is any item that is lootable for any player (not quest item, FFA or conditional)
	public bool HasItemForAll()
	{
		// Gold is always lootable
		if (gold != 0)
			return true;

		foreach (var item in items)
			if (!item.is_looted && item.follow_loot_rules && !item.freeforall && item.conditions.Empty())
				return true;

		return false;
	}

	// return true if there is any FFA, quest or conditional item for the player.
	public bool HasItemFor(Player player)
	{
		// quest items
		foreach (var lootItem in items)
			if (!lootItem.is_looted && !lootItem.follow_loot_rules && lootItem.GetAllowedLooters().Contains(player.GUID))
				return true;

		var ffaItems = GetPlayerFFAItems().LookupByKey(player.GUID);

		if (ffaItems != null)
		{
			var hasFfaItem = ffaItems.Any(ffaItem => !ffaItem.is_looted);

			if (hasFfaItem)
				return true;
		}

		return false;
	}

	// return true if there is any item over the group threshold (i.e. not underthreshold).
	public bool HasOverThresholdItem()
	{
		for (byte i = 0; i < items.Count; ++i)
			if (!items[i].is_looted && !items[i].is_underthreshold && !items[i].freeforall)
				return true;

		return false;
	}

	public void BuildLootResponse(LootResponse packet, Player viewer)
	{
		packet.Coins = gold;

		foreach (var item in items)
		{
			var uiType = item.GetUiTypeForPlayer(viewer, this);

			if (!uiType.HasValue)
				continue;

			LootItemData lootItem = new()
			{
				LootListID = (byte)item.LootListId,
				UIType = uiType.Value,
				Quantity = item.count,
				Loot = new ItemInstance(item)
			};

			packet.Items.Add(lootItem);
		}
	}

	public void NotifyLootList(Map map)
	{
		LootList lootList = new()
		{
			Owner = GetOwnerGUID(),
			LootObj = GetGUID()
		};

		if (GetLootMethod() == LootMethod.MasterLoot && HasOverThresholdItem())
			lootList.Master = GetLootMasterGUID();

		if (!roundRobinPlayer.IsEmpty)
			lootList.RoundRobinWinner = roundRobinPlayer;

		lootList.Write();

		foreach (var allowedLooterGuid in _allowedLooters)
		{
			var allowedLooter = Global.ObjAccessor.GetPlayer(map, allowedLooterGuid);

			if (allowedLooter != null)
				allowedLooter.SendPacket(lootList);
		}
	}

	public bool IsLooted()
	{
		return gold == 0 && unlootedCount == 0;
	}

	public void AddLooter(ObjectGuid guid)
	{
		PlayersLooting.Add(guid);
	}

	public void RemoveLooter(ObjectGuid guid)
	{
		PlayersLooting.Remove(guid);
	}

	public ObjectGuid GetGUID()
	{
		return _guid;
	}

	public ObjectGuid GetOwnerGUID()
	{
		return _owner;
	}

	public ItemContext GetItemContext()
	{
		return _itemContext;
	}

	public void SetItemContext(ItemContext context)
	{
		_itemContext = context;
	}

	public LootMethod GetLootMethod()
	{
		return _lootMethod;
	}

	public ObjectGuid GetLootMasterGUID()
	{
		return _lootMaster;
	}

	public uint GetDungeonEncounterId()
	{
		return _dungeonEncounterId;
	}

	public void SetDungeonEncounterId(uint dungeonEncounterId)
	{
		_dungeonEncounterId = dungeonEncounterId;
	}

	public MultiMap<ObjectGuid, NotNormalLootItem> GetPlayerFFAItems()
	{
		return PlayerFFAItems;
	}
}

public class AELootResult
{
	readonly List<ResultValue> _byOrder = new();
	readonly Dictionary<Item, int> _byItem = new();

	public void Add(Item item, byte count, LootType lootType, uint dungeonEncounterId)
	{
		var id = _byItem.LookupByKey(item);

		if (id != 0)
		{
			var resultValue = _byOrder[id];
			resultValue.count += count;
		}
		else
		{
			_byItem[item] = _byOrder.Count;
			ResultValue value;
			value.item = item;
			value.count = count;
			value.lootType = lootType;
			value.dungeonEncounterId = dungeonEncounterId;
			_byOrder.Add(value);
		}
	}

	public List<ResultValue> GetByOrder()
	{
		return _byOrder;
	}

	public struct ResultValue
	{
		public Item item;
		public byte count;
		public LootType lootType;
		public uint dungeonEncounterId;
	}
}